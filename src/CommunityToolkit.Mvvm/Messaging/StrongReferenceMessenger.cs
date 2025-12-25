// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using CommunityToolkit.Mvvm.Messaging.Internals;

namespace CommunityToolkit.Mvvm.Messaging;

/// <summary>
/// 一个提供 <see cref="IMessenger"/> 接口参考实现的类。
/// </summary>
/// <remarks>
/// 此 <see cref="IMessenger"/> 实现使用强引用来跟踪注册的接收者，
/// 因此当它们不再需要时必须手动注销它们。
/// </remarks>
public sealed class StrongReferenceMessenger : IMessenger
{
    // 这个信使使用以下逻辑将存储的实例链接在一起：
    // --------------------------------------------------------------------------------------------------------
    //   Dictionary2<Recipient, HashSet<IMapping>> recipientsMap;
    //                   |                 \________________[*]IDictionary2<Recipient, IDictionary2<TToken>>
    //                   |                  \_______________[*]IDictionary2<Recipient, object?>       /
    //                   |                                           \_________/_________/___        / 
    //                   |\                       _(recipients registrations)_/         /    \      /
    //                   | \__________________   /    _____(channel registrations)_____/______\____/
    //                   |                    \ /    /      __________________________/        \
    //                   |                     /    /      /                                    \
    //                   |      Dictionary2<Recipient, object?> mapping = Mapping________________\
    //                   | __________________/    /                |         /                    \
    //                   |/                      /                 |        /                      \
    //    Dictionary2<Recipient, Dictionary2<TToken, object?>> mapping = Mapping<TToken>____________\
    //                                         /                  /       /  /
    //                   ___(Type2.TToken)____/                  /       /  /
    //                  /________________(Type2.TMessage)_______/_______/__/
    //                 /       ________________________________/
    //                /       /
    // Dictionary2<Type2, IMapping> typesMap;
    // --------------------------------------------------------------------------------------------------------
    // 每种 <TMessage, TToken> 的组合都会产生一个具体的 Mapping 类型（如果 TToken 是 Unit）或 Mapping<TToken> 类型，
    // 它持有从注册的接收者到处理程序的引用。当请求默认通道时，使用 Mapping，因为在这种情况下，
    // 每个接收者最多只会有一个处理程序，每种消息类型。在这种情况下，
    // 每个接收者将只跟踪消息调度器（存储为 object?，参见下面的注释），而不是存储将每个 TToken 值映射到该接收者对应调度器的字典
    // 对于自定义通道，调度器存储在 <TToken, object?> 字典中，这样每个接收者可以有最多一个注册处理程序
    // 对于给定的令牌，对于每个消息类型。请注意，注册的调度器仅存储为对象引用，因为
    // 它们可以是 null 或 MessageHandlerDispatcher.For<TRecipient, TMessage> 实例。
    //
    // 第一种情况发生在处理程序通过 IRecipient<TMessage> 实例注册时，而第二种情况是
    // 用于包装输入的 MessageHandler<TRecipient, TMessage> 实例。MessageHandlerDispatcher.For<TRecipient, TMessage>
    // 实例将被转换为 MessageHandlerDispatcher 以调用。这允许用户在
    // 每个已注册的接收者上保留类型信息，而不是必须在处理程序中手动将每个接收者转换为正确的类型
    // （另外，使用双重调度避免了别名委托类型的需要）。类型转换是保证的
    // 由于信使类型本身的工作方式 - 因为注册的处理程序总是在其各自的接收者上调用。
    //
    // 每个映射都存储在类型映射中，该映射将每对具体类型与其映射实例关联起来。映射实例
    // 公开为 IMapping 项目，因为每个项目都是具有不同组合的 TMessage 和 TToken 泛型类型参数的封闭类型
    // （或者对于默认通道，仅是 TMessage）。每个现有接收者也存储在主接收者映射中，
    // 连同该接收者的所有现有（字典）处理程序集（对于所有消息类型和令牌类型，如果有的话）。
    //
    // 只要接收者在任何现有映射中至少有一个注册处理程序，对于每个
    // 消息/令牌类型组合，它就会存储在主映射中。共享映射用于访问给定接收者的所有注册处理程序，
    // 不需要预先知道消息或令牌的类型，也不需要使用反射。这
    // 是类型映射中使用的相同方法，因为我们也将保存的项目公开为 IMapping 值。
    //
    // 请注意，每个存储在每个接收者关联集合中的映射也间接实现了 IDictionary2<Recipient, Token>
    // 或 IDictionary2<Recipient>，具有接收者当前使用的任何令牌类型（如果使用默认通道则没有）。这允许
    // 检索具有给定令牌类型的注册处理程序的类型封闭映射，对于任何消息类型，对于每个接收者，再次
    // 不需要使用反射。此共享映射用于从给定接收者取消注册消息，无论条件如何，
    // 按消息类型，按令牌，或针对特定的消息类型和令牌值对。

    /// <summary>
    /// 当前注册接收者的集合，以及与它们连接的消息接收器的链接。
    /// </summary>
    /// <remarks>
    /// 此集合用于允许对所有现有注册接收者进行无反射访问，
    /// 从 <see cref="UnregisterAll"/> 和此类型中的其他方法，
    /// 以便可以移除所有现有处理程序，而无需动态创建
    /// 用于映射处理程序的各种字典的泛型类型参数。
    /// </remarks>
    private readonly Dictionary2<Recipient, HashSet<IMapping>> recipientsMap = new();

    /// <summary>
    /// 各种类型组合的 <see cref="Mapping"/> 和 <see cref="Mapping{TToken}"/> 实例。
    /// </summary>
    /// <remarks>
    /// 值只是 <see cref="IDictionary2{T}"/> 类型，因为我们事先不知道类型参数。
    /// 每种方法都依赖 <see cref="GetOrAddMapping{TMessage,TToken}"/> 来获取每对泛型参数的类型安全实例
    /// <see cref="Mapping"/> 或 <see cref="Mapping{TToken}"/> 类，用于每对正在使用的泛型参数。
    /// </remarks>
    private readonly Dictionary2<Type2, IMapping> typesMap = new();

    /// <summary>
    /// 获取默认的 <see cref="StrongReferenceMessenger"/> 实例。
    /// </summary>
    public static StrongReferenceMessenger Default { get; } = new();

    /// <summary>
    /// 检查指定的接收者是否已注册接收指定类型的消息和令牌
    /// </summary>
    /// <typeparam name="TMessage">要接收的消息类型</typeparam>
    /// <typeparam name="TToken">用于选择接收消息通道的令牌类型</typeparam>
    /// <param name="recipient">要检查的接收者</param>
    /// <param name="token">用于确定接收通道的令牌</param>
    /// <returns>如果接收者已注册则返回true，否则返回false</returns>
    public bool IsRegistered<TMessage, TToken>(object recipient, TToken token)
        where TMessage : class
        where TToken : IEquatable<TToken>
    {
        ArgumentNullException.ThrowIfNull(recipient);
        ArgumentNullException.For<TToken>.ThrowIfNull(token);

        lock (this.recipientsMap)
        {
            if (typeof(TToken) == typeof(Unit))
            {
                if (!TryGetMapping<TMessage>(out Mapping? mapping))
                {
                    return false;
                }

                Recipient key = new(recipient);

                return mapping.ContainsKey(key);
            }
            else
            {
                if (!TryGetMapping<TMessage, TToken>(out Mapping<TToken>? mapping))
                {
                    return false;
                }

                Recipient key = new(recipient);

                return
                    mapping.TryGetValue(key, out Dictionary2<TToken, object?>? handlers) &&
                    handlers.ContainsKey(token);
            }
        }
    }

    /// <summary>
    /// 为指定的接收者注册一个消息处理程序
    /// </summary>
    /// <typeparam name="TRecipient">接收者类型</typeparam>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <typeparam name="TToken">令牌类型</typeparam>
    /// <param name="recipient">将接收消息的接收者</param>
    /// <param name="token">用于确定接收通道的令牌</param>
    /// <param name="handler">消息处理程序</param>
    public void Register<TRecipient, TMessage, TToken>(TRecipient recipient, TToken token, MessageHandler<TRecipient, TMessage> handler)
        where TRecipient : class
        where TMessage : class
        where TToken : IEquatable<TToken>
    {
        ArgumentNullException.ThrowIfNull(recipient);
        ArgumentNullException.For<TToken>.ThrowIfNull(token);
        ArgumentNullException.ThrowIfNull(handler);

        Register<TMessage, TToken>(recipient, token, new MessageHandlerDispatcher.For<TRecipient, TMessage>(handler));
    }

    /// <summary>
    /// 为IRecipient注册消息处理程序
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <typeparam name="TToken">令牌类型</typeparam>
    /// <param name="recipient">接收消息的接收者</param>
    /// <param name="token">用于确定接收通道的令牌</param>
    internal void Register<TMessage, TToken>(IRecipient<TMessage> recipient, TToken token)
        where TMessage : class
        where TToken : IEquatable<TToken>
    {
        Register<TMessage, TToken>(recipient, token, null);
    }

    /// <summary>
    /// 为给定类型的消息注册一个接收者
    /// </summary>
    /// <typeparam name="TMessage">要接收的消息类型</typeparam>
    /// <typeparam name="TToken">用于选择接收消息通道的令牌类型</typeparam>
    /// <param name="recipient">将接收消息的接收者</param>
    /// <param name="token">用于确定接收通道的令牌</param>
    /// <param name="dispatcher">要注册的输入 <see cref="MessageHandlerDispatcher"/> 实例，或null</param>
    /// <exception cref="InvalidOperationException">尝试重复注册相同消息时抛出</exception>
    private void Register<TMessage, TToken>(object recipient, TToken token, MessageHandlerDispatcher? dispatcher)
       where TMessage : class
       where TToken : IEquatable<TToken>
    {
        lock (this.recipientsMap)
        {
            Recipient key = new(recipient);
            IMapping mapping;

            // Unit令牌的快速路径
            if (typeof(TToken) == typeof(Unit))
            {
                // 获取此接收者的<TMessage>注册列表
                Mapping underlyingMapping = GetOrAddMapping<TMessage>();
                ref object? registeredHandler = ref underlyingMapping.GetOrAddValueRef(key);

                if (registeredHandler is not null)
                {
                    ThrowInvalidOperationExceptionForDuplicateRegistration();
                }

                // 存储输入的处理程序
                registeredHandler = dispatcher;

                mapping = underlyingMapping;
            }
            else
            {
                // 获取此接收者的<TMessage, TToken>注册列表
                Mapping<TToken> underlyingMapping = GetOrAddMapping<TMessage, TToken>();
                ref Dictionary2<TToken, object?>? map = ref underlyingMapping.GetOrAddValueRef(key);

                map ??= new Dictionary2<TToken, object?>();

                // 添加新的注册条目
                ref object? registeredHandler = ref map.GetOrAddValueRef(token);

                if (registeredHandler is not null)
                {
                    ThrowInvalidOperationExceptionForDuplicateRegistration();
                }

                registeredHandler = dispatcher;
                mapping = underlyingMapping;
            }

            // 确保此注册映射被跟踪到当前接收者
            ref HashSet<IMapping>? set = ref this.recipientsMap.GetOrAddValueRef(key);

            set ??= new HashSet<IMapping>();

            _ = set.Add(mapping);
        }
    }

    /// <summary>
    /// 注销指定接收者的所有消息注册
    /// </summary>
    /// <param name="recipient">要注销的接收者</param>
    public void UnregisterAll(object recipient)
    {
        ArgumentNullException.ThrowIfNull(recipient);

        lock (this.recipientsMap)
        {
            // 如果接收者没有任何已注册的消息，则忽略
            Recipient key = new(recipient);

            if (!this.recipientsMap.TryGetValue(key, out HashSet<IMapping>? set))
            {
                return;
            }

            // 移除接收者的所有注册处理程序列表
            foreach (IMapping mapping in set)
            {
                if (mapping.TryRemove(key) &&
                    mapping.Count == 0)
                {
                    // 此处的映射实际上都是具有未知类型参数的Mapping<,>类型
                    // 如果在移除当前接收者后某个映射变为空，则意味着
                    // 没有注册任何接收者用于给定的消息和令牌类型对。在这种情况下，
                    // 我们也会从typesMap中移除映射。保留键的原因是
                    // 从字典（哈希集合）中移除项目在最佳情况下只需要O(1)，
                    // 而如果我们尝试在每次移除操作时迭代整个字典，
                    // 则最低成本将是O(n)。
                    _ = this.typesMap.TryRemove(mapping.TypeArguments);
                }
            }

            // 移除接收者映射中的关联集合
            _ = this.recipientsMap.TryRemove(key);
        }
    }

    /// <summary>
    /// 注销指定接收者和令牌的所有消息注册
    /// </summary>
    /// <typeparam name="TToken">令牌类型</typeparam>
    /// <param name="recipient">要注销的接收者</param>
    /// <param name="token">用于确定接收通道的令牌</param>
    public void UnregisterAll<TToken>(object recipient, TToken token)
        where TToken : IEquatable<TToken>
    {
        ArgumentNullException.ThrowIfNull(recipient);
        ArgumentNullException.For<TToken>.ThrowIfNull(token);

        // 此方法从不使用unit类型调用，因此不实现此路径。此
        // 异常不应该被抛出，它只是用于双重检查以防引入错误
        // 导致此路径以某种方式被调用。此类型是内部的，因此使用者
        // 永远不能在此处传递它，并且（不应该）有任何公开的API
        // 会导致此路径被采用。当使用默认通道时，仅支持UnregisterAll(object)
        // 它将注销所有接收者，而不考虑所选的通道。
        if (typeof(TToken) == typeof(Unit))
        {
            throw new NotImplementedException();
        }

        bool lockTaken = false;
        object[]? maps = null;
        int i = 0;

        // 我们在这里使用显式的try/finally块而不是lock语法，以便我们可以在一个
        // 单一的块中同时释放锁并清除和返回缓冲区到池中。我们声明
        // 缓冲区在这里并在此外部finally块中清除并返回它的原因是
        // 执行此操作不需要保持锁，释放它之前执行此最后
        // 步骤减少了锁定的时间，从而减少了锁
        // 在此方法并发调用的多线程场景中的争用。
        try
        {
            Monitor.Enter(this.recipientsMap, ref lockTaken);

            // 获取接收者的共享映射集合（如果存在）
            Recipient key = new(recipient);

            if (!this.recipientsMap.TryGetValue(key, out HashSet<IMapping>? set))
            {
                return;
            }

            // 将目标接收者的候选映射复制到本地数组，因为我们在迭代时不能修改
            // 集合的内容。租用的缓冲区过大，还将包括
            // 使用不同令牌注册的消息处理程序。请注意
            // 我们只使用一个对象数组来最小化租用缓冲区的总数，这些缓冲区
            // 将留在共享池中未使用，除了当它们在此处租用时。相反，我们使用
            // 一种用户可能也会在库中使用的类型，这增加了
            // 在此重用现有缓冲区的机会。当我们需要引用
            // 存储在缓冲区中的项目并带有我们知道它将具有的类型时，我们使用Unsafe.As<T>来避免
            // 转换中的昂贵类型检查，因为已经知道赋值将是有效的。
            maps = ArrayPool<object>.Shared.Rent(set.Count);

            foreach (IMapping item in set)
            {
                // 选择所有使用相同令牌类型的映射
                if (item is IDictionary2<Recipient, IDictionary2<TToken>> mapping)
                {
                    maps[i++] = mapping;
                }
            }

            // 遍历所有本地映射。这些是所有当前
            // 存在的处理程序映射，对于任何给定类型的消息，使用
            // 目标接收者的当前令牌类型。我们大量依赖于
            // 接口，因为能够遍历所有可用映射
            // 而无需提前知道具体类型，并且无需
            // 处理反射：我们只需检查封闭接口的类型
            // 是否与当前使用的令牌类型匹配，并对这些实例进行操作。
            foreach (object obj in maps.AsSpan(0, i))
            {
                IDictionary2<Recipient, IDictionary2<TToken>>? handlersMap = Unsafe.As<IDictionary2<Recipient, IDictionary2<TToken>>>(obj);

                // 我们不需要映射是否包含接收者，因为
                // 映射序列已经从包含所有
                // 目标接收者的映射集合中复制：保证在此处存在。
                IDictionary2<TToken> holder = handlersMap[key];

                // 尝试移除输入令牌的注册处理程序，
                // 对于当前消息类型（在此处未知）。
                if (holder.TryRemove(token) &&
                    holder.Count == 0)
                {
                    // 如果映射为空，则完全从其容器中移除接收者
                    _ = handlersMap.TryRemove(key);

                    IMapping mapping = Unsafe.As<IMapping>(handlersMap);

                    // 此接收者在此组合的消息和令牌类型上不再有注册
                    // 因此可以从其关联集合中移除此映射。 
                    _ = set.Remove(mapping);

                    // 如果结果集合为空，则这意味着此接收者
                    // 对于任何消息或令牌类型都没有更多处理程序，所以接收者也可以
                    // 从所有现有接收者映射中移除，该映射至少有一个处理程序。
                    if (set.Count == 0)
                    {
                        _ = this.recipientsMap.TryRemove(key);
                    }

                    // 如果所有接收者的所有消息类型和令牌类型都没有处理程序了，
                    // 完全移除此接收者的映射集合，并移除对它的强引用。这与
                    // 仅调用UnregisterAll(recipient)将达到的情况相同。
                    if (handlersMap.Count == 0)
                    {
                        _ = this.typesMap.TryRemove(mapping.TypeArguments);
                    }
                }
            }
        }
        finally
        {
            // 释放锁，如果确实获取了它
            if (lockTaken)
            {
                Monitor.Exit(this.recipientsMap);
            }

            // 如果租用了映射数组，则将其返回到共享池中。
            // 删除引用以避免共享内存池中的泄漏。
            // 我们手动创建一个span并清除它作为一个小优化，因为
            // 从池中租用的数组可能比请求的大小大。
            if (maps is not null)
            {
                maps.AsSpan(0, i).Clear();

                ArrayPool<object>.Shared.Return(maps);
            }
        }
    }

    /// <summary>
    /// 注销指定接收者和令牌的消息处理程序
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <typeparam name="TToken">令牌类型</typeparam>
    /// <param name="recipient">要注销的接收者</param>
    /// <param name="token">用于确定接收通道的令牌</param>
    public void Unregister<TMessage, TToken>(object recipient, TToken token)
        where TMessage : class
        where TToken : IEquatable<TToken>
    {
        ArgumentNullException.ThrowIfNull(recipient);
        ArgumentNullException.For<TToken>.ThrowIfNull(token);

        lock (this.recipientsMap)
        {
            if (typeof(TToken) == typeof(Unit))
            {
                // 获取注册列表（如果可用）
                if (!TryGetMapping<TMessage>(out Mapping? mapping))
                {
                    return;
                }

                Recipient key = new(recipient);

                // 移除处理程序（对于unit类型只能有一个）
                if (!mapping.TryRemove(key))
                {
                    return;
                }

                // 完全从此容器中移除映射，并从当前映射中移除到映射本身的链接
                // 之间现有的已注册接收者（或整个接收者）。这与下面相同，
                // 除了对于unit类型只能有一个处理程序，所以
                // 移除它已经意味着目标接收者没有剩余的处理程序了。
                _ = mapping.TryRemove(key);

                // 如果此类型组合没有剩余处理程序，则删除它
                if (mapping.Count == 0)
                {
                    _ = this.typesMap.TryRemove(mapping.TypeArguments);
                }

                HashSet<IMapping> set = this.recipientsMap[key];

                // 当前映射对此接收者不再有任何剩余处理程序了
                // 移除它，然后如果这是最后一个处理程序，则也移除接收者
                // 再次，这与下面相同，但假设unit类型
                _ = set.Remove(mapping);

                if (set.Count == 0)
                {
                    _ = this.recipientsMap.TryRemove(key);
                }
            }
            else
            {
                // 获取注册列表（如果可用）
                if (!TryGetMapping<TMessage, TToken>(out Mapping<TToken>? mapping))
                {
                    return;
                }

                Recipient key = new(recipient);

                if (!mapping.TryGetValue(key, out Dictionary2<TToken, object?>? dictionary))
                {
                    return;
                }

                // 移除目标处理程序
                if (dictionary.TryRemove(token) &&
                    dictionary.Count == 0)
                {
                    // 如果映射为空，这意味着当前接收者对于当前的<TMessage, TToken>组合
                    // 没有任何注册的处理程序，无论
                    // 令牌值（即接收该类型消息的通道）如何
                    // 我们可以完全从此容器中移除映射，并从当前映射中移除到映射本身的链接
                    // 之间现有的已注册接收者（或整个接收者）。
                    _ = mapping.TryRemove(key);

                    // 如果此类型组合没有剩余处理程序，则删除它
                    if (mapping.Count == 0)
                    {
                        _ = this.typesMap.TryRemove(mapping.TypeArguments);
                    }

                    HashSet<IMapping> set = this.recipientsMap[key];

                    // 当前映射对此接收者不再有任何剩余处理程序了
                    _ = set.Remove(mapping);

                    // 如果当前接收者没有任何剩余处理程序，则移除它
                    if (set.Count == 0)
                    {
                        _ = this.recipientsMap.TryRemove(key);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 发送消息给已注册的接收者
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <typeparam name="TToken">令牌类型</typeparam>
    /// <param name="message">要发送的消息</param>
    /// <param name="token">用于确定接收通道的令牌</param>
    /// <returns>发送的消息</returns>
    public TMessage Send<TMessage, TToken>(TMessage message, TToken token)
        where TMessage : class
        where TToken : IEquatable<TToken>
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.For<TToken>.ThrowIfNull(token);

        object?[] rentedArray;
        Span<object?> pairs;
        int i = 0;

        lock (this.recipientsMap)
        {
            if (typeof(TToken) == typeof(Unit))
            {
                // 检查是否有任何已注册的接收者
                if (!TryGetMapping<TMessage>(out Mapping? mapping))
                {
                    goto End;
                }

                // 检查剩余处理程序的数量，见下文
                int totalHandlersCount = mapping.Count;

                if (totalHandlersCount == 0)
                {
                    goto End;
                }

                pairs = rentedArray = ArrayPool<object?>.Shared.Rent(2 * totalHandlersCount);

                // 与下面相同的逻辑，只是这里我们只遍历每个接收者的一个处理程序
                Dictionary2<Recipient, object?>.Enumerator mappingEnumerator = mapping.GetEnumerator();

                while (mappingEnumerator.MoveNext())
                {
                    pairs[2 * i] = mappingEnumerator.GetValue();
                    pairs[(2 * i) + 1] = mappingEnumerator.GetKey().Target;
                    i++;
                }
            }
            else
            {
                // 检查是否有任何已注册的接收者
                if (!TryGetMapping<TMessage, TToken>(out Mapping<TToken>? mapping))
                {
                    goto End;
                }

                // 我们需要复制当前注册的处理程序的本地副本，因为用户可能
                // 尝试从当前现有处理程序之一中注销（或注册）新处理程序
                // 处理程序。我们可以使用内存池来重用数组，以最小化
                // 平均内存使用量。实际上，我们通常只需要支付复制项目的微小开销
                // 在最坏的情况下，所有接收者
                // 将有一个与输入令牌匹配的注册处理程序，这意味着我们可能
                // 有最多与映射中接收者数量相等的待调用处理程序
                // 在映射中。这依赖于令牌是唯一的事实，并且
                // 与给定令牌关联的只有一个处理程序。我们可以使用这个上限作为池中租用数组的请求
                // 大小，这保证了我们有足够的空间
                int totalHandlersCount = mapping.Count;

                if (totalHandlersCount == 0)
                {
                    goto End;
                }

                // 租用数组并将其分配给一个span，该span将用于访问值
                // 我们这样做是为了避免数组协变检查的缓慢在下面的循环中
                pairs = rentedArray = ArrayPool<object?>.Shared.Rent(2 * totalHandlersCount);

                // 复制处理程序到本地集合
                // 此时数组过大，因为它还包括
                // 不同令牌的处理程序。我们可以重用相同的变量
                // 来计算稍后要调用的匹配处理程序的数量
                // 这将是租用缓冲区中有效处理程序的数组切片
                Dictionary2<Recipient, Dictionary2<TToken, object?>>.Enumerator mappingEnumerator = mapping.GetEnumerator();

                // 显式使用枚举器，因为我们使用的是自定义的
                // 没有暴露单个标准Current属性的枚举器
                while (mappingEnumerator.MoveNext())
                {
                    // 如果令牌是接收者的匹配项，则选择目标处理程序
                    if (mappingEnumerator.GetValue().TryGetValue(token, out object? handler))
                    {
                        // 此span访问应该始终保证有效，因为
                        // 数组大小根据当前注册处理程序的总数设置，
                        // 这将始终大于或等于通过之前测试的项目数
                        // 尽管如此，我们仍在此处使用检查的span访问以确保永远不会发生
                        // 超出边界的写入，即使逻辑上方存在错误也是如此
                        pairs[2 * i] = handler;
                        pairs[(2 * i) + 1] = mappingEnumerator.GetKey().Target;
                        i++;
                    }
                }
            }
        }

        try
        {
            // 核心广播逻辑与弱引用信使的相同
            WeakReferenceMessenger.SendAll(pairs, i, message);
        }
        finally
        {
            // 与之前一样，我们还需要先清除它以避免可能的长期
            // 持续的内存泄漏，因为剩余引用存储在池中
            Array.Clear(rentedArray, 0, 2 * i);

            ArrayPool<object?>.Shared.Return(rentedArray);
        }

        End:
        return message;
    }

    /// <summary>
    /// 清理信使中的任何不需要的对象引用
    /// </summary>
    void IMessenger.Cleanup()
    {
        // 当前实现不需要任何类型的清理操作，因为
        // 所有内部数据结构在添加或移除接收者时
        // 已经保持同步。此方法通过显式接口
        // 实现，以便直接使用此类型的开发人员不会在
        // API 表面看到它（因为它在这里是无操作的）。
    }

    /// <summary>
    /// 重置信使到空状态
    /// </summary>
    public void Reset()
    {
        lock (this.recipientsMap)
        {
            this.recipientsMap.Clear();
            this.typesMap.Clear();
        }
    }

    /// <summary>
    /// 尝试获取当前注册接收者的 <see cref="Mapping"/> 实例
    /// 用于输入的 <typeparamref name="TMessage"/> 类型
    /// </summary>
    /// <typeparam name="TMessage">要发送的消息类型</typeparam>
    /// <param name="mapping">找到的 <see cref="Mapping"/> 实例（如果找到）</param>
    /// <returns>是否找到所需的 <see cref="Mapping"/> 实例</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetMapping<TMessage>([NotNullWhen(true)] out Mapping? mapping)
        where TMessage : class
    {
        Type2 key = new(typeof(TMessage), typeof(Unit));

        if (this.typesMap.TryGetValue(key, out IMapping? target))
        {
            // 此方法和以下方法是唯一处理类型映射中值的方法，
            // 在这里我们确定对象引用是指向
            // 正确类型的实例。使用不安全的转换跳过两个条件分支并且更快
            mapping = Unsafe.As<Mapping>(target);

            return true;
        }

        mapping = null;

        return false;
    }

    /// <summary>
    /// 尝试获取当前注册接收者的 <see cref="Mapping{TToken}"/> 实例
    /// 用于 <typeparamref name="TMessage"/> 和 <typeparamref name="TToken"/> 类型的组合
    /// </summary>
    /// <typeparam name="TMessage">要发送的消息类型</typeparam>
    /// <typeparam name="TToken">用于确定发送消息通道的令牌类型</typeparam>
    /// <param name="mapping">找到的 <see cref="Mapping{TToken}"/> 实例（如果找到）</param>
    /// <returns>是否找到所需的 <see cref="Mapping{TToken}"/> 实例</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetMapping<TMessage, TToken>([NotNullWhen(true)] out Mapping<TToken>? mapping)
        where TMessage : class
        where TToken : IEquatable<TToken>
    {
        Type2 key = new(typeof(TMessage), typeof(TToken));

        if (this.typesMap.TryGetValue(key, out IMapping? target))
        {
            mapping = Unsafe.As<Mapping<TToken>>(target);

            return true;
        }

        mapping = null;

        return false;
    }

    /// <summary>
    /// 获取当前注册接收者的 <see cref="Mapping"/> 实例
    /// 用于输入的 <typeparamref name="TMessage"/> 类型
    /// </summary>
    /// <typeparam name="TMessage">要发送的消息类型</typeparam>
    /// <returns>具有请求类型参数的 <see cref="Mapping"/> 实例</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Mapping GetOrAddMapping<TMessage>()
        where TMessage : class
    {
        Type2 key = new(typeof(TMessage), typeof(Unit));
        ref IMapping? target = ref this.typesMap.GetOrAddValueRef(key);

        target ??= Mapping.Create<TMessage>();

        return Unsafe.As<Mapping>(target);
    }

    /// <summary>
    /// 获取当前注册接收者的 <see cref="Mapping{TToken}"/> 实例
    /// 用于 <typeparamref name="TMessage"/> 和 <typeparamref name="TToken"/> 类型的组合
    /// </summary>
    /// <typeparam name="TMessage">要发送的消息类型</typeparam>
    /// <typeparam name="TToken">用于确定发送消息通道的令牌类型</typeparam>
    /// <returns>具有请求类型参数的 <see cref="Mapping{TToken}"/> 实例</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Mapping<TToken> GetOrAddMapping<TMessage, TToken>()
        where TMessage : class
        where TToken : IEquatable<TToken>
    {
        Type2 key = new(typeof(TMessage), typeof(TToken));
        ref IMapping? target = ref this.typesMap.GetOrAddValueRef(key);

        target ??= Mapping<TToken>.Create<TMessage>();

        return Unsafe.As<Mapping<TToken>>(target);
    }

    /// <summary>
    /// 表示接收者和每个通信通道的处理程序视图链接的映射类型
    /// </summary>
    /// <remarks>
    /// 此类型是 <see cref="Mapping{TToken}"/> 针对 <see cref="Unit"/> 令牌的特化
    /// </remarks>
    private sealed class Mapping : Dictionary2<Recipient, object?>, IMapping
    {
        /// <summary>
        /// 初始化 <see cref="Mapping"/> 类的新实例
        /// </summary>
        /// <param name="messageType">正在使用的消息类型</param>
        private Mapping(Type messageType)
        {
            TypeArguments = new Type2(messageType, typeof(Unit));
        }

        /// <summary>
        /// 创建 <see cref="Mapping"/> 类的新实例
        /// </summary>
        /// <typeparam name="TMessage">要接收的消息类型</typeparam>
        /// <returns>新的 <see cref="Mapping"/> 实例</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Mapping Create<TMessage>()
            where TMessage : class
        {
            return new(typeof(TMessage));
        }

        /// <inheritdoc/>
        public Type2 TypeArguments { get; }
    }

    /// <summary>
    /// 表示接收者和每个通信通道的处理程序视图链接的映射类型
    /// </summary>
    /// <typeparam name="TToken">用于选择要接收消息的令牌类型</typeparam>
    /// <remarks>
    /// 为简单起见定义此类型，并作为C#中不支持对开放泛型类型使用类型别名的解决方案
    /// （类型别名只能用于具体、封闭类型）。
    /// </remarks>
    private sealed class Mapping<TToken> : Dictionary2<Recipient, Dictionary2<TToken, object?>>, IMapping
        where TToken : IEquatable<TToken>
    {
        /// <summary>
        /// 初始化 <see cref="Mapping{TToken}"/> 类的新实例
        /// </summary>
        /// <param name="messageType">正在使用的消息类型</param>
        private Mapping(Type messageType)
        {
            TypeArguments = new Type2(messageType, typeof(TToken));
        }

        /// <summary>
        /// 创建 <see cref="Mapping{TToken}"/> 类的新实例
        /// </summary>
        /// <typeparam name="TMessage">要接收的消息类型</typeparam>
        /// <returns>新的 <see cref="Mapping{TToken}"/> 实例</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Mapping<TToken> Create<TMessage>()
            where TMessage : class
        {
            return new(typeof(TMessage));
        }

        /// <inheritdoc/>
        public Type2 TypeArguments { get; }
    }

    /// <summary>
    /// <see cref="Mapping"/> 和 <see cref="Mapping{TToken}"/> 类型的接口，
    /// 允许检索给定泛型实例的类型参数，而无需任何先验知识关于这些参数
    /// </summary>
    private interface IMapping : IDictionary2<Recipient>
    {
        /// <summary>
        /// 获取表示当前类型参数的 <see cref="Type2"/> 实例
        /// </summary>
        Type2 TypeArguments { get; }
    }

    /// <summary>
    /// 表示接收者的简单类型
    /// </summary>
    /// <remarks>
    /// 此类型用于在每个映射字典中启用快速索引，
    /// 因为它充当了 <see cref="GetHashCode"/> 和
    /// <see cref="Equals(object?)"/> 方法的外部重写，用于任意对象，删除了虚调用
    /// 并防止此上下文中的实例重写这些方法
    /// 使用此类型保证所有相等操作始终仅基于每个已注册接收者的引用相等，
    /// 而不管其类型如何
    /// </remarks>
    private readonly struct Recipient : IEquatable<Recipient>
    {
        /// <summary>
        /// 已注册的接收者
        /// </summary>
        public readonly object Target;

        /// <summary>
        /// 初始化 <see cref="Recipient"/> 结构的新实例
        /// </summary>
        /// <param name="target">目标接收者实例</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Recipient(object target)
        {
            this.Target = target;
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Recipient other)
        {
            return ReferenceEquals(this.Target, other.Target);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is Recipient other && Equals(other);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return RuntimeHelpers.GetHashCode(this.Target);
        }
    }

    /// <summary>
    /// 尝试添加重复处理程序时抛出 <see cref="InvalidOperationException"/>
    /// </summary>
    private static void ThrowInvalidOperationExceptionForDuplicateRegistration()
    {
        throw new InvalidOperationException("The target recipient has already subscribed to the target message.");
    }
}