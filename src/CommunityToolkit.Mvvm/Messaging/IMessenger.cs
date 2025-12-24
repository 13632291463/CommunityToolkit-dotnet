// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace CommunityToolkit.Mvvm.Messaging;

/// <summary>
/// An interface for a type providing the ability to exchange messages between different objects.
/// This can be useful to decouple different modules of an application without having to keep strong
/// references to types being referenced. It is also possible to send messages to specific channels, uniquely
/// identified by a token, and to have different messengers in different sections of an applications.
/// In order to use the <see cref="IMessenger"/> functionalities, first define a message type, like so:
/// <code>
/// public sealed class LoginCompletedMessage { }
/// </code>
/// Then, register a recipient for this message:
/// <code>
/// Messenger.Default.Register&lt;MyRecipientType, LoginCompletedMessage&gt;(this, (r, m) =>
/// {
///     // Handle the message here...
/// });
/// </code>
/// The message handler here is a lambda expression taking two parameters: the recipient and the message.
/// This is done to avoid the allocations for the closures that would've been generated if the expression
/// had captured the current instance. The recipient type parameter is used so that the recipient can be
/// directly accessed within the handler without the need to manually perform type casts. This allows the
/// code to be less verbose and more reliable, as all the checks are done just at build time. If the handler
/// is defined within the same type as the recipient, it is also possible to directly access private members.
/// This allows the message handler to be a static method, which enables the C# compiler to perform a number
/// of additional memory optimizations (such as caching the delegate, avoiding unnecessary memory allocations).
/// Finally, send a message when needed, like so:
/// <code>
/// Messenger.Default.Send&lt;LoginCompletedMessage&gt;();
/// </code>
/// Additionally, the method group syntax can also be used to specify the message handler
/// to invoke when receiving a message, if a method with the right signature is available
/// in the current scope. This is helpful to keep the registration and handling logic separate.
/// Following up from the previous example, consider a class having this method:
/// <code>
/// private static void Receive(MyRecipientType recipient, LoginCompletedMessage message)
/// {
///     // Handle the message there
/// }
/// </code>
/// The registration can then be performed in a single line like so:
/// <code>
/// Messenger.Default.Register(this, Receive);
/// </code>
/// The C# compiler will automatically convert that expression to a <see cref="MessageHandler{TRecipient,TMessage}"/> instance
/// compatible with <see cref="IMessengerExtensions.Register{TRecipient,TMessage}(IMessenger,TRecipient,MessageHandler{TRecipient,TMessage})"/>.
/// This will also work if multiple overloads of that method are available, each handling a different
/// message type: the C# compiler will automatically pick the right one for the current message type.
/// It is also possible to register message handlers explicitly using the <see cref="IRecipient{TMessage}"/> interface.
/// To do so, the recipient just needs to implement the interface and then call the
/// <see cref="IMessengerExtensions.RegisterAll(IMessenger,object)"/> extension, which will automatically register
/// all the handlers that are declared by the recipient type. Registration for individual handlers is supported as well.
/// </summary>
public interface IMessenger
{
    /// <summary>
    /// Checks whether or not a given recipient has already been registered for a message.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to check for the given recipient.</typeparam>
    /// <typeparam name="TToken">The type of token to check the channel for.</typeparam>
    /// <param name="recipient">The target recipient to check the registration for.</param>
    /// <param name="token">The token used to identify the target channel to check.</param>
    /// <returns>Whether or not <paramref name="recipient"/> has already been registered for the specified message.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="recipient"/> or <paramref name="token"/> are <see langword="null"/>.</exception>
    bool IsRegistered<TMessage, TToken>(object recipient, TToken token)
        where TMessage : class
        where TToken : IEquatable<TToken>;

    /// <summary>
    /// Registers a recipient for a given type of message.
    /// </summary>
    /// <typeparam name="TRecipient">The type of recipient for the message.</typeparam>
    /// <typeparam name="TMessage">The type of message to receive.</typeparam>
    /// <typeparam name="TToken">The type of token to use to pick the messages to receive.</typeparam>
    /// <param name="recipient">The recipient that will receive the messages.</param>
    /// <param name="token">A token used to determine the receiving channel to use.</param>
    /// <param name="handler">The <see cref="MessageHandler{TRecipient,TMessage}"/> to invoke when a message is received.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="recipient"/>, <paramref name="token"/> or <paramref name="handler"/> are <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when trying to register the same message twice.</exception>
    void Register<TRecipient, TMessage, TToken>(TRecipient recipient, TToken token, MessageHandler<TRecipient, TMessage> handler)
        where TRecipient : class
        where TMessage : class
        where TToken : IEquatable<TToken>;

    /// <summary>
    /// Unregisters a recipient from all registered messages.
    /// </summary>
    /// <param name="recipient">The recipient to unregister.</param>
    /// <remarks>
    /// This method will unregister the target recipient across all channels.
    /// Use this method as an easy way to lose all references to a target recipient.
    /// If the recipient has no registered handler, this method does nothing.
    /// </remarks>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="recipient"/> is <see langword="null"/>.</exception>
    void UnregisterAll(object recipient);

    /// <summary>
    /// Unregisters a recipient from all messages on a specific channel.
    /// </summary>
    /// <typeparam name="TToken">The type of token to identify what channel to unregister from.</typeparam>
    /// <param name="recipient">The recipient to unregister.</param>
    /// <param name="token">The token to use to identify which handlers to unregister.</param>
    /// <remarks>If the recipient has no registered handler, this method does nothing.</remarks>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="recipient"/> or <paramref name="token"/> are <see langword="null"/>.</exception>
    void UnregisterAll<TToken>(object recipient, TToken token)
        where TToken : IEquatable<TToken>;

    /// <summary>
    /// Unregisters a recipient from messages of a given type.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to stop receiving.</typeparam>
    /// <typeparam name="TToken">The type of token to identify what channel to unregister from.</typeparam>
    /// <param name="recipient">The recipient to unregister.</param>
    /// <param name="token">The token to use to identify which handlers to unregister.</param>
    /// <remarks>If the recipient has no registered handler, this method does nothing.</remarks>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="recipient"/> or <paramref name="token"/> are <see langword="null"/>.</exception>
    void Unregister<TMessage, TToken>(object recipient, TToken token)
        where TMessage : class
        where TToken : IEquatable<TToken>;

    /// <summary>
    /// 向所有已注册的接收者发送指定类型的消息。
    /// </summary>
    /// <typeparam name="TMessage">要发送的消息类型。</typeparam>
    /// <typeparam name="TToken">用于标识使用哪个通道发送消息的令牌类型。</typeparam>
    /// <param name="message">要发送的消息。</param>
    /// <param name="token">指示要使用哪个通道的令牌。</param>
    /// <returns>发送的消息（即 <paramref name="message"/>）。</returns>
    /// <exception cref="System.ArgumentNullException">当 <paramref name="message"/> 或 <paramref name="token"/> 为 <see langword="null"/> 时抛出。</exception>
    TMessage Send<TMessage, TToken>(TMessage message, TToken token)
        where TMessage : class
        where TToken : IEquatable<TToken>;

    /// <summary>
    /// 对当前消息传递器执行清理操作
    /// </summary>
    /// <remarks>
    /// 调用此方法不会注销任何当前已注册的接收者。
    /// 此方法可用于执行清理操作，例如修剪消息传递器实现的内部数据结构。
    /// </remarks>
    void Cleanup();

    /// <summary>
    /// 重置 <see cref="IMessenger"/> 实例并注销所有已注册的接收者。
    /// 此方法会清除所有已注册的消息订阅者，使信使恢复到初始状态。
    /// </summary>
    /// <returns>无返回值</returns>
    void Reset();
}
