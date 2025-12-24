// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace CommunityToolkit.Mvvm.Messaging.Internals;

/// <summary>
/// 调用给定 <see cref="MessageHandler{TRecipient, TMessage}"/> 回调的调度器类型
/// </summary>
/// <remarks>
/// 此类型用于在泛型参数未知时避免与 <see cref="Unsafe.As{T}(object)"/> 的类型别名问题。此外，这是一个抽象类而不是接口，
/// 因此当调用 <see cref="Invoke(object, object)"/> 时，将使用虚拟调度而不是接口存根调度，虚拟调度速度更快，间接层级更少
/// </remarks>
internal abstract class MessageHandlerDispatcher
{
    /// <summary>
    /// 在目标接收者上调用当前回调，并传入指定的消息
    /// </summary>
    /// <param name="recipient">消息的目标接收者</param>
    /// <param name="message">正在广播的消息</param>
    public abstract void Invoke(object recipient, object message);

    /// <summary>
    /// <see cref="MessageHandlerDispatcher"/> 的泛型版本
    /// </summary>
    /// <typeparam name="TRecipient">消息接收者的类型</typeparam>
    /// <typeparam name="TMessage">要接收的消息类型</typeparam>
    public sealed class For<TRecipient, TMessage> : MessageHandlerDispatcher
        where TRecipient : class
        where TMessage : class
    {
        /// <summary>
        /// 要调用的基础 <see cref="MessageHandler{TRecipient, TMessage}"/> 回调
        /// </summary>
        private readonly MessageHandler<TRecipient, TMessage> handler;

        /// <summary>
        /// 初始化 <see cref="For{TRecipient, TMessage}"/> 类的新实例
        /// </summary>
        /// <param name="handler">输入的 <see cref="MessageHandler{TRecipient, TMessage}"/> 实例</param>
        public For(MessageHandler<TRecipient, TMessage> handler)
        {
            this.handler = handler;
        }

        /// <summary>
        /// 使用指定的接收者和消息调用消息处理器
        /// </summary>
        /// <param name="recipient">消息的目标接收者</param>
        /// <param name="message">要处理的消息</param>
        public override void Invoke(object recipient, object message)
        {
            this.handler(Unsafe.As<TRecipient>(recipient), Unsafe.As<TMessage>(message));
        }
    }
}