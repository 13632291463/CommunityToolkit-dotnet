// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using CommunityToolkit.Mvvm.Messaging.Internals;

namespace CommunityToolkit.Mvvm.Messaging;

/// <summary>
/// 一个提供 <see cref="IMessenger"/> 接口参考实现的类
/// </summary>
/// <remarks>
/// <para>
/// 此 <see cref="IMessenger"/> 实现使用弱引用来跟踪注册的接收者，因此不需要手动取消注册不再需要的接收者
/// </para>
/// <para>
/// <see cref="WeakReferenceMessenger"/> 类型将在调用完整GC收集时自动执行内部修剪，因此无需手动调用 <see cref="Cleanup"/> 来
/// 确保内部数据结构尽可能精简和紧凑
/// </para>
/// </remarks>
public sealed class WeakReferenceMessenger : IMessenger
{
    // 这个消息传递器使用以下逻辑将存储的实例链接在一起：
    // --------------------------------------------------------------------------------------------------------
    //                          Dictionary2<TToken, MessageHandlerDispatcher?> mapping
    //                                        /                   /             /
    //                   ___(Type2.TToken)___/                   /             /         ___(if Type2.TToken is Unit)
    //                  /_________(Type2.TMessage)______________/             /         /
    //                 /                                    _________________/___MessageHandlerDispatcher?
    //                /                                    /                         \
    // Dictionary2<Type2, ConditionalWeakTable<object, object?>> recipientsMap;       \___(null if using IRecipient<TMessage>)
    // --------------------------------------------------------------------------------------------------------
    // 与强引用变体类似，每对消息和令牌类型都用作接收者映射中的键
    // 在这种情况下，字典中的值是 ConditionalWeakTable2<,> 实例，它们
    // 通过依赖句柄将每个注册的接收者链接到当前注册的处理程序映射。这
    // 确保只要它们关联的接收者仍然存活，处理程序就会保持存活（因此用户无需手动指示
    // 是否应保留给定处理程序以防它创建闭包）。
    // 每个条件表中的值可以是 Dictionary2<TToken, MessageHandlerDispatcher> 或 object。第一个
    // 情况用于使用除默认 Unit 类型之外的任何令牌类型时，因为在这种情况下，
    // 每个接收者可能需要单独跟踪多个处理程序。为了在不知道其类型参数的上下文中调用所有处理程序，
    // 处理程序存储为 MessageHandlerDispatcher 实例。有两种可能的情况：
    // 一个实例是 MessageHandlerDispatcher.For<TRecipient, TMessage> 类型，或者为 null。第一个是默认情况：
    // 每当通过 MessageHandler<TRecipient, TToken> 进行订阅时，该委托被包装在该类的实例中，
    // 以便它可以在内部跟踪使用的泛型上下文，以便可以在执行回调时检索它。如果订阅直接在实现
    // IRecipient<TMessage 的接收者上完成，则调度程序为 null，这只是一个标记。每当广播
    // 方法找到它时，它将直接在目标接收者上调用 IRecipient<TMessage.Receive，这避免了调度时的
    // 额外间接引用以及为处理程序分配额外包装类型的需要。最后，当通过 Unit 类型进行订阅时有特殊情况，
    // 这意味着使用默认通道时。在这种情况下，每个接收者只存储一个 MessageHandlerDispatcher 实例，而不是整个字典，因为
    // 每个接收者只能有一个处理程序。

    /// <summary>
    /// 当前注册的所有消息类型的接收者映射表
    /// </summary>
    private readonly Dictionary2<Type2, ConditionalWeakTable2<object, object?>> recipientsMap = new();

    /// <summary>
    /// 初始化 <see cref="WeakReferenceMessenger"/> 类的新实例
    /// </summary>
    public WeakReferenceMessenger()
    {
        // GC 回调的代理函数。这需要是静态的并将目标实例作为
        // 输入参数，以避免 Gen2GcCallback 对象调用它时将其固定
        static void Gen2GcCallbackProxy(object target)
        {
            ((WeakReferenceMessenger)target).CleanupWithNonBlockingLock();
        }

        // 注册自动 GC 回调以触发非阻塞清理。这将确保
        // 当前 messenger 实例被修剪且没有未使用的剩余接收者映射
        // 这是必要的（以某种形式的清理，无论是显式的还是像这样自动的）
        // 因为 ConditionalWeakTable<TKey, TValue> 实例将在密钥（即接收者）被收集时立即移除键值对，
        // 导致它们自己的密钥（即映射每个消息和令牌类型的条件表的 Type2 实例）
        // 在根映射结构中保留但没有实际注册的接收者，这只会
        // 在以后的广播操作中枚举接收者时增加不必要的开销
        Gen2GcCallback.Register(Gen2GcCallbackProxy, this);
    }

    /// <summary>
    /// 获取默认的 <see cref="WeakReferenceMessenger"/> 实例
    /// </summary>
    public static WeakReferenceMessenger Default { get; } = new();

    /// <summary>
    /// 检查指定的接收者是否已注册了特定消息和令牌类型
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <typeparam name="TToken">令牌类型，必须实现 IEquatable&lt;TToken&gt;</typeparam>
    /// <param name="recipient">要检查的接收者对象</param>
    /// <param name="token">用于确定接收通道的令牌</param>
    /// <returns>如果接收者已注册，则返回 true；否则返回 false</returns>
    /// <exception cref="ArgumentNullException">当接收者或令牌为 null 时抛出</exception>
    public bool IsRegistered<TMessage, TToken>(object recipient, TToken token)
        where TMessage : class
        where TToken : IEquatable<TToken>
    {
        ArgumentNullException.ThrowIfNull(recipient);
        ArgumentNullException.For<TToken>.ThrowIfNull(token);

        lock (this.recipientsMap)
        {
            Type2 type2 = new(typeof(TMessage), typeof(TToken));

            // 获取与目标接收者关联的条件表，用于当前的令牌和消息类型对
            // 如果存在，则检查是否有匹配的令牌
            if (!this.recipientsMap.TryGetValue(type2, out ConditionalWeakTable2<object, object?>? table))
            {
                return false;
            }

            // Unit 令牌的特殊情况
            if (typeof(TToken) == typeof(Unit))
            {
                return table.TryGetValue(recipient, out _);
            }

            // 自定义令牌类型，因此每个接收者都有一个关联的映射
            return
                table.TryGetValue(recipient, out object? mapping) &&
                Unsafe.As<Dictionary2<TToken, object?>>(mapping!).ContainsKey(token);
        }
    }

    /// <summary>
    /// 为指定接收者注册消息处理程序
    /// </summary>
    /// <typeparam name="TRecipient">接收者类型</typeparam>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <typeparam name="TToken">令牌类型，必须实现 IEquatable&lt;TToken&gt;</typeparam>
    /// <param name="recipient">将接收消息的接收者</param>
    /// <param name="token">用于确定接收通道的令牌</param>
    /// <param name="handler">消息处理程序委托</param>
    /// <exception cref="ArgumentNullException">当接收者、令牌或处理程序为 null 时抛出</exception>
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
    /// 为给定消息类型注册接收者
    /// </summary>
    /// <typeparam name="TMessage">要接收的消息类型</typeparam>
    /// <typeparam name="TToken">用于选择要接收的消息的令牌类型</typeparam>
    /// <param name="recipient">将接收消息的接收者</param>
    /// <param name="token">用于确定接收通道的令牌</param>
    /// <exception cref="InvalidOperationException">尝试重复注册相同消息时抛出</exception>
    /// <remarks>
    /// 此方法是 <see cref="Register{TRecipient, TMessage, TToken}(TRecipient, TToken, MessageHandler{TRecipient, TMessage})"/> 的变体
    /// 专门用于实现 <see cref="IRecipient{TMessage}"/> 的接收者。请参阅此类顶部的更多注释，以及
    /// <see cref="Send{TMessage, TToken}(TMessage, TToken)"/> 和 <see cref="MessageHandlerDispatcher"/> 类型中的注释
    /// </remarks>
    internal void Register<TMessage, TToken>(IRecipient<TMessage> recipient, TToken token)
        where TMessage : class
        where TToken : IEquatable<TToken>
    {
        Register<TMessage, TToken>(recipient, token, null);
    }

    /// <summary>
    /// 为给定消息类型注册接收者
    /// </summary>
    /// <typeparam name="TMessage">要接收的消息类型</typeparam>
    /// <typeparam name="TToken">用于选择要接收的消息的令牌类型</typeparam>
    /// <param name="recipient">将接收消息的接收者</param>
    /// <param name="token">用于确定接收通道的令牌</param>
    /// <param name="dispatcher">输入的 <see cref="MessageHandlerDispatcher"/> 实例，或 null</param>
    /// <exception cref="InvalidOperationException">尝试重复注册相同消息时抛出</exception>
    private void Register<TMessage, TToken>(object recipient, TToken token, MessageHandlerDispatcher? dispatcher)
        where TMessage : class
        where TToken : IEquatable<TToken>
    {
        lock (this.recipientsMap)
        {
            Type2 type2 = new(typeof(TMessage), typeof(TToken));

            // 获取类型参数对的条件表，如果不存在则创建它
            ref ConditionalWeakTable2<object, object?>? mapping = ref this.recipientsMap.GetOrAddValueRef(type2);

            mapping ??= new ConditionalWeakTable2<object, object?>();

            // Unit 令牌的快速路径
            if (typeof(TToken) == typeof(Unit))
            {
                if (!mapping.TryAdd(recipient, dispatcher))
                {
                    ThrowInvalidOperationExceptionForDuplicateRegistration();
                }
            }
            else
            {
                // 为目标接收者获取或创建处理程序字典
                Dictionary2<TToken, object?>? map = Unsafe.As<Dictionary2<TToken, object?>>(mapping.GetValue(recipient, static _ => new Dictionary2<TToken, object?>())!);

                // 添加新的注册条目
                ref object? registeredHandler = ref map.GetOrAddValueRef(token);

                if (registeredHandler is not null)
                {
                    ThrowInvalidOperationExceptionForDuplicateRegistration();
                }

                // 存储输入的处理程序
                registeredHandler = dispatcher;
            }
        }
    }

    /// <summary>
    /// 取消注册指定接收者的所有消息订阅
    /// </summary>
    /// <param name="recipient">要取消注册的接收者对象</param>
    /// <exception cref="ArgumentNullException">当接收者为 null 时抛出</exception>
    public void UnregisterAll(object recipient)
    {
        ArgumentNullException.ThrowIfNull(recipient);

        lock (this.recipientsMap)
        {
            Dictionary2<Type2, ConditionalWeakTable2<object, object?>>.Enumerator enumerator = this.recipientsMap.GetEnumerator();

            // 遍历所有现有的条件表并移除所有
            // 以目标接收者为键的条目。我们在此处不执行清理，
            // 因为那是下面定义的单独方法的职责
            while (enumerator.MoveNext())
            {
                _ = enumerator.GetValue().Remove(recipient);
            }
        }
    }

    /// <summary>
    /// 取消注册指定接收者的特定令牌的所有消息订阅
    /// </summary>
    /// <typeparam name="TToken">令牌类型，必须实现 IEquatable&lt;TToken&gt;</typeparam>
    /// <param name="recipient">要取消注册的接收者对象</param>
    /// <param name="token">要取消注册的令牌</param>
    /// <exception cref="ArgumentNullException">当接收者或令牌为 null 时抛出</exception>
    public void UnregisterAll<TToken>(object recipient, TToken token)
        where TToken : IEquatable<TToken>
    {
        ArgumentNullException.ThrowIfNull(recipient);
        ArgumentNullException.For<TToken>.ThrowIfNull(token);

        // 此方法从不使用 Unit 类型调用。请参阅 StrongReferenceMessenger 中相应方法的注释
        // 了解更多详细信息
        if (typeof(TToken) == typeof(Unit))
        {
            throw new NotImplementedException();
        }

        lock (this.recipientsMap)
        {
            Dictionary2<Type2, ConditionalWeakTable2<object, object?>>.Enumerator enumerator = this.recipientsMap.GetEnumerator();

            // 与上面相同，区别在于这次我们只遍历
            // 具有匹配令牌类型的键的条件表，并且只尝试移除
            // 具有匹配令牌的处理程序（如果存在）
            while (enumerator.MoveNext())
            {
                if (enumerator.GetKey().TToken == typeof(TToken))
                {
                    if (enumerator.GetValue().TryGetValue(recipient, out object? mapping))
                    {
                        _ = Unsafe.As<Dictionary2<TToken, object?>>(mapping!).TryRemove(token);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 取消注册指定接收者和令牌的特定消息类型
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <typeparam name="TToken">令牌类型，必须实现 IEquatable&lt;TToken&gt;</typeparam>
    /// <param name="recipient">要取消注册的接收者对象</param>
    /// <param name="token">用于确定接收通道的令牌</param>
    /// <exception cref="ArgumentNullException">当接收者或令牌为 null 时抛出</exception>
    public void Unregister<TMessage, TToken>(object recipient, TToken token)
        where TMessage : class
        where TToken : IEquatable<TToken>
    {
        ArgumentNullException.ThrowIfNull(recipient);
        ArgumentNullException.For<TToken>.ThrowIfNull(token);

        lock (this.recipientsMap)
        {
            Type2 type2 = new(typeof(TMessage), typeof(TToken));

            // 获取消息和令牌类型的组合的目标映射表，
            // 并移除具有匹配令牌的处理程序（整个映射），如果存在
            if (this.recipientsMap.TryGetValue(type2, out ConditionalWeakTable2<object, object?>? value))
            {
                if (typeof(TToken) == typeof(Unit))
                {
                    _ = value.Remove(recipient);
                }
                else if (value.TryGetValue(recipient, out object? mapping))
                {
                    _ = Unsafe.As<Dictionary2<TToken, object?>>(mapping!).TryRemove(token);
                }
            }
        }
    }

    /// <summary>
    /// 向所有注册的接收者发送消息
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <typeparam name="TToken">令牌类型，必须实现 IEquatable&lt;TToken&gt;</typeparam>
    /// <param name="message">要发送的消息</param>
    /// <param name="token">用于确定接收通道的令牌</param>
    /// <returns>发送的消息</returns>
    /// <exception cref="ArgumentNullException">当消息或令牌为 null 时抛出</exception>
    public TMessage Send<TMessage, TToken>(TMessage message, TToken token)
        where TMessage : class
        where TToken : IEquatable<TToken>
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.For<TToken>.ThrowIfNull(token);

        ArrayPoolBufferWriter<object?> bufferWriter;
        int i = 0;

        lock (this.recipientsMap)
        {
            Type2 type2 = new(typeof(TMessage), typeof(TToken));

            // 尝试获取目标表
            if (!this.recipientsMap.TryGetValue(type2, out ConditionalWeakTable2<object, object?>? table))
            {
                return message;
            }

            bufferWriter = ArrayPoolBufferWriter<object?>.Create();

            // 我们需要一个局部的、临时的副本，包含所有待调用的接收者和处理程序，
            // 以避免处理程序在我们持有锁时取消注册消息的问题。为了做到这一点，
            // 我们可以遍历使用的条件表来枚举所有现有的接收者
            // 对于此调用对应的消息和令牌类型的对，并跟踪
            // 具有匹配令牌的处理程序及其相应的接收者
            using ConditionalWeakTable2<object, object?>.Enumerator enumerator = table.GetEnumerator();

            while (enumerator.MoveNext())
            {
                if (typeof(TToken) == typeof(Unit))
                {
                    bufferWriter.Add(enumerator.GetValue());
                    bufferWriter.Add(enumerator.GetKey());
                    i++;
                }
                else
                {
                    Dictionary2<TToken, object?>? map = Unsafe.As<Dictionary2<TToken, object?>>(enumerator.GetValue()!);

                    if (map.TryGetValue(token, out object? handler))
                    {
                        bufferWriter.Add(handler);
                        bufferWriter.Add(enumerator.GetKey());
                        i++;
                    }
                }
            }
        }

        try
        {
            SendAll(bufferWriter.Span, i, message);
        }
        finally
        {
            bufferWriter.Dispose();
        }

        return message;
    }

    /// <summary>
    /// 实现 <see cref="Send{TMessage, TToken}(TMessage, TToken)"/> 的广播逻辑
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <param name="pairs">包含处理程序和接收者对的只读跨度</param>
    /// <param name="i">对的数量</param>
    /// <param name="message">要发送的消息</param>
    /// <remarks>
    /// 此方法不是局部函数，以避免因 <c>TToken</c> 可能是值类型而导致的多次编译，
    /// 这会导致专门化的代码，由于具现化泛型。这是为了解决 Roslyn 的限制，
    /// 该限制导致局部函数中的未使用类型参数不会在合成方法中被丢弃。此外，将此循环保留在
    /// EH 块（<see langword="try"/> 块）之外可以帮助产生稍微更好的代码生成
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void SendAll<TMessage>(ReadOnlySpan<object?> pairs, int i, TMessage message)
        where TMessage : class
    {
        // 此 Slice 调用执行循环的边界检查，以防 i 以某种方式错误
        // 其余实现依赖于边界检查移除和手动完成的循环强度降低（这导致 20% 的速度提升）
        // 在广播期间，因为 JIT 无法识别此模式。跳过下面的检查是经过证明的安全优化：
        // 切片正好有 2 * i 个元素（由于此切片），并且每个循环迭代处理一对元素
        // 循环在初始引用到达末尾时结束，并且在每次迭代结束时增加 2
        // 以结束于目标引用。目标是跨度，显然意味着长度是恒定的
        ReadOnlySpan<object?> slice = pairs.Slice(0, 2 * i);

        ref object? sliceStart = ref MemoryMarshal.GetReference(slice);
        ref object? sliceEnd = ref Unsafe.Add(ref sliceStart, slice.Length);

        while (Unsafe.IsAddressLessThan(ref sliceStart, ref sliceEnd))
        {
            object? handler = sliceStart;
            object recipient = Unsafe.Add(ref sliceStart, 1)!;

            // 在这里我们需要区分两种可能的情况：要么接收者是通过
            // IRecipient<TMessage> 接口注册的，要么是使用自定义处理程序注册的。在第一种情况下，
            // 存储在 messenger 中的处理程序只是 null，因此我们可以检查并分支到
            // 直接调用 IRecipient<TMessage> 的快速路径。否则，
            // 我们将使用标准的双调度方法。此检查特别方便
            // 因为我们只需检查 null 即可确定使用了哪种注册类型，而无需
            // 在 messenger 中存储任何其他信息。这将生成如下代码，
            // 优势是代码紧凑且不需要使用任何额外的寄存器：
            // =============================
            // L0000: test rcx, rcx
            // L0003: jne short L0040
            // =============================
            // 这非常快。首先进行此条件检查的原因是
            // 我们正在执行手动（基于 null 的）受保护虚拟化：如果处理程序是标记
            // 类型而不是实际处理程序，那么我们知道接收者实现了
            // IRecipient<TMessage>，因此我们可以转换为它并直接调用它。这避免了
            // 在注册时存储代理回调，也避免了一个间接层
            // （调用委托然后调用实际方法）。附加说明：此
            // 模式确保下面的两个转换实际上不别名不兼容的引用
            // 类型（换句话说，如果它们是安全转换，这两个转换都会成功），这使代码
            // 不依赖于未定义行为来正确运行（即，我们不别名委托）
            if (handler is null)
            {
                Unsafe.As<IRecipient<TMessage>>(recipient).Receive(message);
            }
            else
            {
                Unsafe.As<MessageHandlerDispatcher>(handler).Invoke(recipient, message);
            }

            sliceStart = ref Unsafe.Add(ref sliceStart, 2);
        }
    }

    /// <summary>
    /// 清理内部数据结构，移除已垃圾回收的接收者
    /// </summary>
    public void Cleanup()
    {
        lock (this.recipientsMap)
        {
            CleanupWithoutLock();
        }
    }

    /// <summary>
    /// 重置 messenger，清除所有注册的接收者和消息处理程序
    /// </summary>
    public void Reset()
    {
        lock (this.recipientsMap)
        {
            this.recipientsMap.Clear();
        }
    }

    /// <summary>
    /// 在不锁定当前实例的情况下执行清理。此方法必须在
    /// 已获取 <see cref="recipientsMap"/> 锁时调用
    /// </summary>
    private void CleanupWithNonBlockingLock()
    {
        object lockObject = this.recipientsMap;
        bool lockTaken = false;

        try
        {
            Monitor.TryEnter(lockObject, ref lockTaken);

            if (lockTaken)
            {
                CleanupWithoutLock();
            }
        }
        finally
        {
            if (lockTaken)
            {
                Monitor.Exit(lockObject);
            }
        }
    }

    /// <summary>
    /// 在不锁定当前实例的情况下执行清理。此方法必须在
    /// 已获取 <see cref="recipientsMap"/> 锁时调用
    /// </summary>
    private void CleanupWithoutLock()
    {
        using ArrayPoolBufferWriter<Type2> type2s = ArrayPoolBufferWriter<Type2>.Create();
        using ArrayPoolBufferWriter<object> emptyRecipients = ArrayPoolBufferWriter<object>.Create();

        Dictionary2<Type2, ConditionalWeakTable2<object, object?>>.Enumerator type2Enumerator = this.recipientsMap.GetEnumerator();

        // 首先，我们遍历所有当前注册的令牌和消息类型的对
        // 这些代表了至少有一个已注册处理程序的所有泛型参数组合，
        // 除了那些接收者已经被收集的组合
        while (type2Enumerator.MoveNext())
        {
            emptyRecipients.Reset();

            bool hasAtLeastOneHandler = false;

            if (type2Enumerator.GetKey().TToken == typeof(Unit))
            {
                // 当令牌类型是 Unit 时，不会有已注册但没有处理程序的接收者，
                // 因为当单个处理程序被取消订阅时，接收者也会立即被移除
                // 因此，我们需要检查消息是否至少存在一个接收者
                using ConditionalWeakTable2<object, object?>.Enumerator recipientsEnumerator = type2Enumerator.GetValue().GetEnumerator();

                while (recipientsEnumerator.MoveNext())
                {
                    hasAtLeastOneHandler = true;

                    break;
                }
            }
            else
            {
                // 遍历当前存活的接收者以查找没有处理程序的接收者。我们跟踪
                // 找到的接收者以在循环外移除它们（不能在枚举期间修改）
                using (ConditionalWeakTable2<object, object?>.Enumerator recipientsEnumerator = type2Enumerator.GetValue().GetEnumerator())
                {
                    while (recipientsEnumerator.MoveNext())
                    {
                        if (Unsafe.As<IDictionary2>(recipientsEnumerator.GetValue()!).Count == 0)
                        {
                            emptyRecipients.Add(recipientsEnumerator.GetKey());
                        }
                        else
                        {
                            hasAtLeastOneHandler = true;
                        }
                    }
                }

                // 移除仍存活但没有处理程序的接收者的处理程序映射
                foreach (object recipient in emptyRecipients.Span)
                {
                    _ = type2Enumerator.GetValue().Remove(recipient);
                }
            }

            // 跟踪没有接收者或处理程序的类型组合
            if (!hasAtLeastOneHandler)
            {
                type2s.Add(type2Enumerator.GetKey());
            }
        }

        // 移除所有没有处理程序的映射
        foreach (Type2 key in type2s.Span)
        {
            _ = this.recipientsMap.TryRemove(key);
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