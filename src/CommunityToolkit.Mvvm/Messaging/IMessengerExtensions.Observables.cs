// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
#if NET8_0_OR_GREATER && WINDOWS
using WinRT;
#endif

namespace CommunityToolkit.Mvvm.Messaging;

/// <inheritdoc/>
/// <summary>
/// 为IMessenger接口提供可观察扩展方法的扩展类
/// </summary>
partial class IMessengerExtensions
{
    /// <summary>
    /// 创建一个IObservable{T}实例，用于接收通过信使广播的指定类型消息的通知
    /// </summary>
    /// <typeparam name="TMessage">要通过结果IObservable{T}实例接收通知的消息类型</typeparam>
    /// <param name="messenger">用于注册接收者的IMessenger实例</param>
    /// <returns>用于接收TMessage类型消息广播通知的IObservable{T}实例</returns>
    /// <exception cref="System.ArgumentNullException">当messenger参数为null时抛出</exception>
    public static IObservable<TMessage> CreateObservable<TMessage>(this IMessenger messenger)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(messenger);

        return new Observable<TMessage>(messenger);
    }

    /// <summary>
    /// 创建一个IObservable{T}实例，用于接收通过信使广播的指定类型消息的通知
    /// </summary>
    /// <typeparam name="TMessage">要通过结果IObservable{T}实例接收通知的消息类型</typeparam>
    /// <typeparam name="TToken">用于标识使用哪个通道接收消息的令牌类型</typeparam>
    /// <param name="messenger">用于注册接收者的IMessenger实例</param>
    /// <param name="token">用于确定要使用的接收通道的令牌</param>
    /// <returns>用于接收TMessage类型消息广播通知的IObservable{T}实例</returns>
    /// <exception cref="System.ArgumentNullException">当messenger或token参数为null时抛出</exception>
    public static IObservable<TMessage> CreateObservable<TMessage, TToken>(this IMessenger messenger, TToken token)
        where TMessage : class
        where TToken : IEquatable<TToken>
    {
        ArgumentNullException.ThrowIfNull(messenger);
        ArgumentNullException.For<TToken>.ThrowIfNull(token);

        return new Observable<TMessage, TToken>(messenger, token);
    }

    /// <summary>
    /// 用于给定消息类型的IObservable{T}实现
    /// </summary>
    /// <typeparam name="TMessage">要监听的消息类型</typeparam>
    private sealed class Observable<TMessage> : IObservable<TMessage>
        where TMessage : class
    {
        /// <summary>
        /// 用于注册接收者的IMessenger实例
        /// </summary>
        private readonly IMessenger messenger;

        /// <summary>
        /// 使用给定参数创建新的Observable{TMessage}实例
        /// </summary>
        /// <param name="messenger">用于注册接收者的IMessenger实例</param>
        public Observable(IMessenger messenger)
        {
            this.messenger = messenger;
        }

        /// <inheritdoc/>
        public IDisposable Subscribe(IObserver<TMessage> observer)
        {
            return new Recipient(this.messenger, observer);
        }

        /// <summary>
        /// Observable{TMessage}的IRecipient{TMessage}实现
        /// </summary>
#if NET8_0_OR_GREATER && WINDOWS
        [WinRTExposedType(typeof(WinRTManagedOnlyTypeDetails))]
#endif
        private sealed class Recipient : IRecipient<TMessage>, IDisposable
        {
            /// <summary>
            /// 用于注册接收者的IMessenger实例
            /// </summary>
            private readonly IMessenger messenger;

            /// <summary>
            /// 当前使用的IObserver{T}实例
            /// </summary>
            private readonly IObserver<TMessage> observer;

            /// <summary>
            /// 使用指定参数创建新的Recipient实例
            /// </summary>
            /// <param name="messenger">用于注册接收者的IMessenger实例</param>
            /// <param name="observer">用于创建接收者的IObserver{T}实例</param>
            public Recipient(IMessenger messenger, IObserver<TMessage> observer)
            {
                this.messenger = messenger;
                this.observer = observer;

                messenger.Register(this);
            }

            /// <inheritdoc/>
            public void Receive(TMessage message)
            {
                this.observer.OnNext(message);
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                this.messenger.Unregister<TMessage>(this);
            }
        }
    }

    /// <summary>
    /// 用于给定消息和令牌类型的IObservable{T}实现
    /// </summary>
    /// <typeparam name="TMessage">要监听的消息类型</typeparam>
    /// <typeparam name="TToken">用于标识使用哪个通道接收消息的令牌类型</typeparam>
    private sealed class Observable<TMessage, TToken> : IObservable<TMessage>
        where TMessage : class
        where TToken : IEquatable<TToken>
    {
        /// <summary>
        /// 用于注册接收者的IMessenger实例
        /// </summary>
        private readonly IMessenger messenger;

        /// <summary>
        /// 用于确定要使用的接收通道的令牌
        /// </summary>
        private readonly TToken token;

        /// <summary>
        /// 使用给定参数创建新的Observable{TMessage, TToken}实例
        /// </summary>
        /// <param name="messenger">用于注册接收者的IMessenger实例</param>
        /// <param name="token">用于确定要使用的接收通道的令牌</param>
        public Observable(IMessenger messenger, TToken token)
        {
            this.messenger = messenger;
            this.token = token;
        }

        /// <inheritdoc/>
        public IDisposable Subscribe(IObserver<TMessage> observer)
        {
            return new Recipient(this.messenger, observer, this.token);
        }

        /// <summary>
        /// Observable{TMessage, TToken}的IRecipient{TMessage}实现
        /// </summary>
#if NET8_0_OR_GREATER && WINDOWS
        [WinRTExposedType(typeof(WinRTManagedOnlyTypeDetails))]
#endif
        private sealed class Recipient : IRecipient<TMessage>, IDisposable
        {
            /// <summary>
            /// 用于注册接收者的IMessenger实例
            /// </summary>
            private readonly IMessenger messenger;

            /// <summary>
            /// 当前使用的IObserver{T}实例
            /// </summary>
            private readonly IObserver<TMessage> observer;

            /// <summary>
            /// 用于确定要使用的接收通道的令牌
            /// </summary>
            private readonly TToken token;

            /// <summary>
            /// 使用指定参数创建新的Recipient实例
            /// </summary>
            /// <param name="messenger">用于注册接收者的IMessenger实例</param>
            /// <param name="observer">用于创建接收者的IObserver{T}实例</param>
            /// <param name="token">用于确定要使用的接收通道的令牌</param>
            public Recipient(IMessenger messenger, IObserver<TMessage> observer, TToken token)
            {
                this.messenger = messenger;
                this.observer = observer;
                this.token = token;

                messenger.Register(this, token);
            }

            /// <inheritdoc/>
            public void Receive(TMessage message)
            {
                this.observer.OnNext(message);
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                this.messenger.Unregister<TMessage, TToken>(this, this.token);
            }
        }
    }
}