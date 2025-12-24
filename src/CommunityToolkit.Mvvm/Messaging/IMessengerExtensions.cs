// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.Messaging.Internals;

namespace CommunityToolkit.Mvvm.Messaging;

/// <summary>
/// Extensions for the <see cref="IMessenger"/> type.
/// </summary>
public static partial class IMessengerExtensions
{
    /// <summary>
    /// 用于加载与 <see cref="Register{TMessage,TToken}(IMessenger,IRecipient{TMessage},TToken)"/> 方法关联的 <see cref="MethodInfo"/> 实例的容器类。
    /// 此类用于避免在仅引用 <see cref="IMessengerExtensions"/> 类型时（即使只是使用不需要此 <see cref="MethodInfo"/> 实例的方法）强制运行静态构造函数中的初始化代码。
    /// 我们实际上使用此类型来利用运行时对静态构造函数的延迟加载。
    /// </summary>
    private static class MethodInfos
    {
        /// <summary>
        /// 与 <see cref="Register{TMessage,TToken}(IMessenger,IRecipient{TMessage},TToken)"/> 关联的 <see cref="MethodInfo"/> 实例。
        /// </summary>
        public static readonly MethodInfo RegisterIRecipient = new Action<IMessenger, IRecipient<object>, Unit>(Register).Method.GetGenericMethodDefinition();
    }



    /// <summary>
    /// 一个非泛型版本的 <see cref="DiscoveredRecipients{TToken}"/> 类。
    /// 此类用于跟踪每个接收者类型的预加载注册操作，不区分通信通道。
    /// </summary>
    private static class DiscoveredRecipients
    {
        /// <summary>
        /// 用于跟踪每个接收者类型的预加载注册操作的 <see cref="ConditionalWeakTable{TKey,TValue}"/> 实例。
        /// 键为接收者类型，值为注册操作的委托。
        /// </summary>
        public static readonly ConditionalWeakTable<Type, Action<IMessenger, object>?> RegistrationMethods = new();
    }

    /// <summary>
    /// 一个静态容器类，用于将 <see cref="ConditionalWeakTable{TKey,TValue}"/> 实例与每个正在使用的 <typeparamref name="TToken"/> 类型关联。
    /// 这样做是因为我们只能使用单个类型作为键，但我们需要跟踪每个接收者类型在不同通信通道上的关联情况，每个通道由一个令牌标识。
    /// 由于令牌实际上是一个编译时参数，我们可以使用一个包装类让运行时为每个泛型类型实例化处理不同的实例。
    /// 这使我们只需关注正在检查的接收者类型。
    /// </summary>
    /// <typeparam name="TToken">指示要使用哪个通道的令牌类型。</typeparam>
    private static class DiscoveredRecipients<TToken>
        where TToken : IEquatable<TToken>
    {
        /// <summary>
        /// 用于跟踪每个接收者类型的预加载注册操作的 <see cref="ConditionalWeakTable{TKey,TValue}"/> 实例。
        /// 键为接收者类型，值为接受信使、接收者对象和令牌的注册操作委托。
        /// </summary>
        public static readonly ConditionalWeakTable<Type, Action<IMessenger, object, TToken>> RegistrationMethods = new();
    }


    /// <summary>
    /// 检查指定的接收者是否已注册接收特定类型的消息
    /// </summary>
    /// <typeparam name="TMessage">要为给定接收者检查的消息类型</typeparam>
    /// <param name="messenger">用于检查注册的 <see cref="IMessenger"/> 实例</param>
    /// <param name="recipient">要检查注册的目标接收者</param>
    /// <returns><paramref name="recipient"/> 是否已注册接收指定消息的布尔值</returns>
    /// <remarks>此方法将使用默认通道检查请求的注册</remarks>
    /// <exception cref="System.ArgumentNullException">当 <paramref name="messenger"/> 或 <paramref name="recipient"/> 为 <see langword="null"/> 时抛出</exception>
    public static bool IsRegistered<TMessage>(this IMessenger messenger, object recipient)
        where TMessage : class
    {
        // 验证输入参数不为null
        ArgumentNullException.ThrowIfNull(messenger);
        ArgumentNullException.ThrowIfNull(recipient);

        // 使用默认通道检查接收者是否已注册接收指定类型的消息
        return messenger.IsRegistered<TMessage, Unit>(recipient, default);
    }

    /// <summary>
    /// 使用默认通道为给定接收者注册所有声明的消息处理程序。
    /// </summary>
    /// <param name="messenger">用于注册接收者的 <see cref="IMessenger"/> 实例。</param>
    /// <param name="recipient">将接收消息的接收者。</param>
    /// <remarks>有关更多信息，请参见 <see cref="RegisterAll{TToken}(IMessenger,object,TToken)"/> 的注释。</remarks>
    /// <exception cref="System.ArgumentNullException">当 <paramref name="messenger"/> 或 <paramref name="recipient"/> 为 <see langword="null"/> 时抛出。</exception>
    [RequiresUnreferencedCode(
        "This method requires the generated CommunityToolkit.Mvvm.Messaging.__Internals.__IMessengerExtensions type not to be removed to use the fast path. " +
        "If this type is removed by the linker, or if the target recipient was created dynamically and was missed by the source generator, a slower fallback " +
        "path using a compiled LINQ expression will be used. This will have more overhead in the first invocation of this method for any given recipient type.")]
    [RequiresDynamicCode(
        "This method requires the generated CommunityToolkit.Mvvm.Messaging.__Internals.__IMessengerExtensions type not to be removed to use the fast path. " +
        "If that is present, the method is AOT safe, as the only methods being invoked to register the messages will be the ones produced by the source generator. " +
        "If it isn't, this method will need to dynamically create the generic methods to register messages, which might not be available at runtime.")]
    public static void RegisterAll(this IMessenger messenger, object recipient)
    {
        ArgumentNullException.ThrowIfNull(messenger);
        ArgumentNullException.ThrowIfNull(recipient);

        // 使用条件弱表作为回调方法，该表将为我们处理线程安全。
        // 此回调将尝试为目标接收者类型查找生成的方法，然后调用它以获取稍后缓存和使用的委托。
        [RequiresUnreferencedCode("The type of the current instance cannot be statically discovered.")]
        static Action<IMessenger, object>? LoadRegistrationMethodsForType(Type recipientType)
        {
            if (recipientType.Assembly.GetType("CommunityToolkit.Mvvm.Messaging.__Internals.__IMessengerExtensions") is Type extensionsType &&
                extensionsType.GetMethod("CreateAllMessagesRegistrator", new[] { recipientType }) is MethodInfo methodInfo)
            {
                return (Action<IMessenger, object>)methodInfo.Invoke(null, new object?[] { null })!;
            }

            return null;
        }

        // 尝试获取缓存的委托（如果生成器已正确运行）
        Action<IMessenger, object>? registrationAction = DiscoveredRecipients.RegistrationMethods.GetValue(
            recipient.GetType(),
            LoadRegistrationMethodsForType);

        if (registrationAction is not null)
        {
            registrationAction(messenger, recipient);
        }
        else
        {
            messenger.RegisterAll(recipient, default(Unit));
        }
    }
    /// <summary>
    /// 为给定接收者注册所有声明的消息处理程序。
    /// </summary>
    /// <typeparam name="TToken">用于标识使用哪个通道接收消息的令牌类型。</typeparam>
    /// <param name="messenger">用于注册接收者的 <see cref="IMessenger"/> 实例。</param>
    /// <param name="recipient">将接收消息的接收者。</param>
    /// <param name="token">指示使用哪个通道的令牌。</param>
    /// <remarks>
    /// 此方法将注册 <paramref name="recipient"/> 实现的所有 <see cref="IRecipient{TMessage}"/> 接口对应的消息。
    /// 如果没有实现任何接口，则此方法将不执行任何操作。
    /// 请注意，与其他所有扩展不同，此方法将使用反射来查找要注册的处理程序。
    /// 但是，一旦完成注册，性能将与通过 <see cref="IMessenger"/> 接口的其他任何泛型扩展直接注册的处理程序完全相同。
    /// </remarks>
    /// <exception cref="System.ArgumentNullException">当 <paramref name="messenger"/>、<paramref name="recipient"/> 或 <paramref name="token"/> 为 <see langword="null"/> 时抛出。</exception>
    /*[RequiresUnreferencedCode(
        "此方法要求生成的 CommunityToolkit.Mvvm.Messaging.__Internals.__IMessengerExtensions 类型不能被移除以使用快速路径。 " +
        "如果此类型被链接器移除，或者如果目标接收者是动态创建的且被源生成器遗漏，则将使用编译的 LINQ 表达式的较慢回退路径。" +
        "这将在此方法针对任何给定接收者类型的首次调用中产生更多开销。")]
    [RequiresDynamicCode("用于注册消息的泛型方法在运行时可能不可用。")]*/
    [RequiresUnreferencedCode(
        "This method requires the generated CommunityToolkit.Mvvm.Messaging.__Internals.__IMessengerExtensions type not to be removed to use the fast path. " +
        "If this type is removed by the linker, or if the target recipient was created dynamically and was missed by the source generator, a slower fallback " +
        "path using a compiled LINQ expression will be used. This will have more overhead in the first invocation of this method for any given recipient type.")]
    [RequiresDynamicCode("The generic methods to register messages might not be available at runtime.")]

    public static void RegisterAll<TToken>(this IMessenger messenger, object recipient, TToken token)
        where TToken : IEquatable<TToken>
    {
        ArgumentNullException.ThrowIfNull(messenger);
        ArgumentNullException.ThrowIfNull(recipient);
        ArgumentNullException.For<TToken>.ThrowIfNull(token);

        // 我们使用此方法作为条件弱表的回调，它将为我们处理线程安全性。
        // 此第一个回调将尝试为目标接收者类型找到生成的方法，然后调用它以获取要缓存和稍后使用的委托。
        // 在这种情况下，我们还需要首先创建目标方法的泛型实例化。
        // [RequiresUnreferencedCode("无法静态发现当前实例的类型。")]
        // [RequiresDynamicCode("用于注册消息的泛型方法在运行时可能不可用。")]
        [RequiresUnreferencedCode("The type of the current instance cannot be statically discovered.")]
        [RequiresDynamicCode("The generic methods to register messages might not be available at runtime.")]
        static Action<IMessenger, object, TToken> LoadRegistrationMethodsForType(Type recipientType)
        {
            if (recipientType.Assembly.GetType("CommunityToolkit.Mvvm.Messaging.__Internals.__IMessengerExtensions") is Type extensionsType &&
                extensionsType.GetMethod("CreateAllMessagesRegistratorWithToken", new[] { recipientType }) is MethodInfo methodInfo)
            {
                MethodInfo genericMethodInfo = methodInfo.MakeGenericMethod(typeof(TToken));

                return (Action<IMessenger, object, TToken>)genericMethodInfo.Invoke(null, new object?[] { null })!;
            }

            return LoadRegistrationMethodsForTypeFallback(recipientType);
        }

        // 当找不到生成的方法时的回退方法。
        // 此方法仅在每个接收者类型和令牌类型上调用一次，因此我们不担心
        // 使它超级高效，我们可以为清晰起见使用 LINQ 代码。
        // LINQ 代码生成膨胀并不真正重要，原因相同。
        // [RequiresDynamicCode("用于注册消息的泛型方法在运行时可能不可用。")]
        [RequiresDynamicCode("The generic methods to register messages might not be available at runtime.")]
        static Action<IMessenger, object, TToken> LoadRegistrationMethodsForTypeFallback(Type recipientType)
        {
            // 获取验证方法集合
            MethodInfo[] registrationMethods = (
                from interfaceType in recipientType.GetInterfaces()
                where interfaceType.IsGenericType &&
                      interfaceType.GetGenericTypeDefinition() == typeof(IRecipient<>)
                let messageType = interfaceType.GenericTypeArguments[0]
                select MethodInfos.RegisterIRecipient.MakeGenericMethod(messageType, typeof(TToken))).ToArray();

            // 如果没有要注册的消息处理程序，则使用短路径
            if (registrationMethods.Length == 0)
            {
                return static (_, _, _) => { };
            }

            // 输入参数（IMessenger 实例、非泛型接收者、令牌）
            ParameterExpression arg0 = Expression.Parameter(typeof(IMessenger));
            ParameterExpression arg1 = Expression.Parameter(typeof(object));
            ParameterExpression arg2 = Expression.Parameter(typeof(TToken));

            // 声明一个从 (RecipientType)recipient 转换得到的局部变量
            UnaryExpression inst1 = Expression.Convert(arg1, recipientType);

            // 我们需要一个执行所有声明消息类型注册的单个编译 LINQ 表达式。
            // 为此，我们创建一个包含各个消息注册（对于每个 IRecipient<T>）的展开调用的块。
            // 下面的代码将生成以下块表达式：
            // ===============================================================================
            // {
            //     var inst1 = (RecipientType)arg1;
            //     IMessengerExtensions.Register<T0, TToken>(arg0, inst1, arg2);
            //     IMessengerExtensions.Register<T1, TToken>(arg0, inst1, arg2);
            //     ...
            //     IMessengerExtensions.Register<TN, TToken>(arg0, inst1, arg2);
            // }
            // ===============================================================================
            // 我们还添加一个显式对象转换，将输入接收者类型转换为实际的特定类型，
            // 以便暴露的消息处理程序可访问。
            BlockExpression body = Expression.Block(
                from registrationMethod in registrationMethods
                select Expression.Call(registrationMethod, new Expression[]
                {
                        arg0,
                        inst1,
                        arg2
                }));

            return Expression.Lambda<Action<IMessenger, object, TToken>>(body, arg0, arg1, arg2).Compile();
        }

        // 获取或计算当前接收者类型的注册方法。
        // 如 CommunityToolkit.Diagnostics.TypeExtensions.ToTypeString 中所述，我们使用 lambda
        // 表达式而不是方法组表达式以利用静态初始化的委托，
        // 并避免此方法每次调用时的重复分配。
        // 有关此问题的更多信息，请参阅 https://github.com/dotnet/roslyn/issues/5835 的相关问题。
        Action<IMessenger, object, TToken> registrationAction = DiscoveredRecipients<TToken>.RegistrationMethods.GetValue(
            recipient.GetType(),
            LoadRegistrationMethodsForType);

        // 调用缓存的委托以实际执行消息注册
        registrationAction(messenger, recipient, token);
    }
    /// <summary>
    /// 为给定类型的消息注册接收者
    /// </summary>
    /// <typeparam name="TMessage">要接收的消息类型</typeparam>
    /// <param name="messenger">用于注册接收者的 <see cref="IMessenger"/> 实例</param>
    /// <param name="recipient">将接收消息的接收者</param>
    /// <exception cref="InvalidOperationException">尝试两次注册相同消息时抛出</exception>
    /// <remarks>此方法将使用默认通道执行请求的注册</remarks>
    /// <exception cref="System.ArgumentNullException">当 <paramref name="messenger"/> 或 <paramref name="recipient"/> 为 <see langword="null"/> 时抛出</exception>
    public static void Register<TMessage>(this IMessenger messenger, IRecipient<TMessage> recipient)
        where TMessage : class
    {
        // 验证输入参数不为null
        ArgumentNullException.ThrowIfNull(messenger);
        ArgumentNullException.ThrowIfNull(recipient);

        // 根据信使的具体类型进行不同的处理
        if (messenger is WeakReferenceMessenger weakReferenceMessenger)
        {
            // 使用弱引用信使注册消息
            weakReferenceMessenger.Register<TMessage, Unit>(recipient, default);
        }
        else if (messenger is StrongReferenceMessenger strongReferenceMessenger)
        {
            // 使用强引用信使注册消息
            strongReferenceMessenger.Register<TMessage, Unit>(recipient, default);
        }
        else
        {
            // 对于其他类型的信使，使用通用注册方法
            messenger.Register<IRecipient<TMessage>, TMessage, Unit>(recipient, default, static (r, m) => r.Receive(m));
        }
    }

    /// <summary>
    /// 为给定类型的消息注册接收者
    /// </summary>
    /// <typeparam name="TMessage">要接收的消息类型</typeparam>
    /// <typeparam name="TToken">用于标识使用哪个通道接收消息的令牌类型</typeparam>
    /// <param name="messenger">用于注册接收者的 <see cref="IMessenger"/> 实例</param>
    /// <param name="recipient">将接收消息的接收者</param>
    /// <param name="token">指示使用哪个通道的令牌</param>
    /// <exception cref="InvalidOperationException">尝试两次注册相同消息时抛出</exception>
    /// <remarks>此方法将使用默认通道执行请求的注册</remarks>
    /// <exception cref="System.ArgumentNullException">当 <paramref name="messenger"/>、<paramref name="recipient"/> 或 <paramref name="token"/> 为 <see langword="null"/> 时抛出</exception>
    public static void Register<TMessage, TToken>(this IMessenger messenger, IRecipient<TMessage> recipient, TToken token)
        where TMessage : class
        where TToken : IEquatable<TToken>
    {
        // 验证输入参数不为null
        ArgumentNullException.ThrowIfNull(messenger);
        ArgumentNullException.ThrowIfNull(recipient);
        ArgumentNullException.For<TToken>.ThrowIfNull(token);

        // 根据信使的具体类型进行不同的处理
        if (messenger is WeakReferenceMessenger weakReferenceMessenger)
        {
            // 使用弱引用信使和令牌注册消息
            weakReferenceMessenger.Register(recipient, token);
        }
        else if (messenger is StrongReferenceMessenger strongReferenceMessenger)
        {
            // 使用强引用信使和令牌注册消息
            strongReferenceMessenger.Register(recipient, token);
        }
        else
        {
            // 对于其他类型的信使，使用通用注册方法
            messenger.Register<IRecipient<TMessage>, TMessage, TToken>(recipient, token, static (r, m) => r.Receive(m));
        }
    }

    /// <summary>
    /// 为给定类型的消息注册接收者
    /// </summary>
    /// <typeparam name="TMessage">要接收的消息类型</typeparam>
    /// <param name="messenger">用于注册接收者的 <see cref="IMessenger"/> 实例</param>
    /// <param name="recipient">将接收消息的接收者</param>
    /// <param name="handler">接收到消息时要调用的 <see cref="MessageHandler{TRecipient,TMessage}"/></param>
    /// <exception cref="InvalidOperationException">尝试两次注册相同消息时抛出</exception>
    /// <remarks>此方法将使用默认通道执行请求的注册</remarks>
    /// <exception cref="System.ArgumentNullException">当 <paramref name="messenger"/>、<paramref name="recipient"/> 或 <paramref name="handler"/> 为 <see langword="null"/> 时抛出</exception>
    public static void Register<TMessage>(this IMessenger messenger, object recipient, MessageHandler<object, TMessage> handler)
        where TMessage : class
    {
        // 验证输入参数不为null
        ArgumentNullException.ThrowIfNull(messenger);
        ArgumentNullException.ThrowIfNull(recipient);
        ArgumentNullException.ThrowIfNull(handler);

        // 使用默认的Unit令牌注册消息
        messenger.Register(recipient, default(Unit), handler);
    }

    /// <summary>
    /// 为给定类型的消息注册接收者
    /// </summary>
    /// <typeparam name="TRecipient">消息接收者的类型</typeparam>
    /// <typeparam name="TMessage">要接收的消息类型</typeparam>
    /// <param name="messenger">用于注册接收者的 <see cref="IMessenger"/> 实例</param>
    /// <param name="recipient">将接收消息的接收者</param>
    /// <param name="handler">接收到消息时要调用的 <see cref="MessageHandler{TRecipient,TMessage}"/></param>
    /// <exception cref="InvalidOperationException">尝试两次注册相同消息时抛出</exception>
    /// <remarks>此方法将使用默认通道执行请求的注册</remarks>
    /// <exception cref="System.ArgumentNullException">当 <paramref name="messenger"/>、<paramref name="recipient"/> 或 <paramref name="handler"/> 为 <see langword="null"/> 时抛出</exception>
    public static void Register<TRecipient, TMessage>(this IMessenger messenger, TRecipient recipient, MessageHandler<TRecipient, TMessage> handler)
        where TRecipient : class
        where TMessage : class
    {
        // 验证输入参数不为null
        ArgumentNullException.ThrowIfNull(messenger);
        ArgumentNullException.ThrowIfNull(recipient);
        ArgumentNullException.ThrowIfNull(handler);

        // 使用默认的Unit令牌注册消息
        messenger.Register(recipient, default(Unit), handler);
    }
    /// <summary>
    /// 向指定类型的消息注册接收者。
    /// </summary>
    /// <typeparam name="TMessage">要接收的消息类型。</typeparam>
    /// <typeparam name="TToken">用于选择要接收消息的令牌类型。</typeparam>
    /// <param name="messenger">用于注册接收者的 <see cref="IMessenger"/> 实例。</param>
    /// <param name="recipient">将接收消息的接收者。</param>
    /// <param name="token">用于确定要使用的接收通道的令牌。</param>
    /// <param name="handler">接收到消息时要调用的 <see cref="MessageHandler{TRecipient,TMessage}"/>。</param>
    /// <exception cref="InvalidOperationException">尝试两次注册相同消息时抛出。</exception>
    /// <exception cref="System.ArgumentNullException">当 <paramref name="messenger"/>、<paramref name="recipient"/> 或 <paramref name="handler"/> 为 <see langword="null"/> 时抛出。</exception>
    public static void Register<TMessage, TToken>(this IMessenger messenger, object recipient, TToken token, MessageHandler<object, TMessage> handler)
        where TMessage : class
        where TToken : IEquatable<TToken>
    {
        ArgumentNullException.ThrowIfNull(messenger);
        ArgumentNullException.ThrowIfNull(recipient);
        ArgumentNullException.For<TToken>.ThrowIfNull(token);
        ArgumentNullException.ThrowIfNull(handler);

        messenger.Register(recipient, token, handler);
    }

    /// <summary>
    /// 从给定类型的消息中注销接收者。
    /// </summary>
    /// <typeparam name="TMessage">要停止接收的消息类型。</typeparam>
    /// <param name="messenger">用于注销接收者的 <see cref="IMessenger"/> 实例。</param>
    /// <param name="recipient">要注销的接收者。</param>
    /// <remarks>
    /// 此方法仅将目标接收者从默认通道注销。
    /// 如果接收者没有注册的处理程序，则此方法不执行任何操作。
    /// </remarks>
    /// <exception cref="System.ArgumentNullException">当 <paramref name="messenger"/> 或 <paramref name="recipient"/> 为 <see langword="null"/> 时抛出。</exception>
    public static void Unregister<TMessage>(this IMessenger messenger, object recipient)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(messenger);
        ArgumentNullException.ThrowIfNull(recipient);

        messenger.Unregister<TMessage, Unit>(recipient, default);
    }

    /// <summary>
    /// 将指定类型的消息发送给所有已注册的接收者。
    /// </summary>
    /// <typeparam name="TMessage">要发送的消息类型。</typeparam>
    /// <param name="messenger">用于发送消息的 <see cref="IMessenger"/> 实例。</param>
    /// <returns>已发送的消息。</returns>
    /// <remarks>
    /// 当消息类型公开无参数构造函数时，此方法是 <see cref="Send{TMessage}(IMessenger,TMessage)"/> 的简写：
    /// 它将自动创建一个新的 <typeparamref name="TMessage"/> 实例并将其发送给接收者。
    /// </remarks>
    /// <exception cref="System.ArgumentNullException">当 <paramref name="messenger"/> 为 <see langword="null"/> 时抛出。</exception>
    public static TMessage Send<TMessage>(this IMessenger messenger)
        where TMessage : class, new()
    {
        ArgumentNullException.ThrowIfNull(messenger);

        return messenger.Send(new TMessage(), default(Unit));
    }

    /// <summary>
    /// 将指定类型的消息发送给所有已注册的接收者。
    /// </summary>
    /// <typeparam name="TMessage">要发送的消息类型。</typeparam>
    /// <param name="messenger">用于发送消息的 <see cref="IMessenger"/> 实例。</param>
    /// <param name="message">要发送的消息。</param>
    /// <returns>发送的消息（即 <paramref name="message"/>）。</returns>
    /// <exception cref="System.ArgumentNullException">当 <paramref name="messenger"/> 或 <paramref name="message"/> 为 <see langword="null"/> 时抛出。</exception>
    public static TMessage Send<TMessage>(this IMessenger messenger, TMessage message)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(messenger);
        ArgumentNullException.ThrowIfNull(message);

        return messenger.Send(message, default(Unit));
    }

    /// <summary>
    /// 将指定类型的消息发送给所有已注册的接收者。
    /// </summary>
    /// <typeparam name="TMessage">要发送的消息类型。</typeparam>
    /// <typeparam name="TToken">用于标识使用哪个通道发送消息的令牌类型。</typeparam>
    /// <param name="messenger">用于发送消息的 <see cref="IMessenger"/> 实例。</param>
    /// <param name="token">指示要使用哪个通道的令牌。</param>
    /// <returns>已发送的消息。</returns>
    /// <remarks>
    /// 此方法将自动创建一个新的 <typeparamref name="TMessage"/> 实例，
    /// 就像 <see cref="Send{TMessage}(IMessenger)"/> 一样，然后将其发送给正确的接收者。
    /// </remarks>
    /// <exception cref="System.ArgumentNullException">当 <paramref name="messenger"/> 或 <paramref name="token"/> 为 <see langword="null"/> 时抛出。</exception>
    public static TMessage Send<TMessage, TToken>(this IMessenger messenger, TToken token)
        where TMessage : class, new()
        where TToken : IEquatable<TToken>
    {
        ArgumentNullException.ThrowIfNull(messenger);
        ArgumentNullException.For<TToken>.ThrowIfNull(token);

        return messenger.Send(new TMessage(), token);
    }
}
