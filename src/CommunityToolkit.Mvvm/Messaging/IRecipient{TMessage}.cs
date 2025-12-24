// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Mvvm.Messaging;

/// <summary>
/// An interface for a recipient that declares a registration for a specific message type.
/// </summary>
/// <typeparam name="TMessage">The type of message to receive.</typeparam>
public interface IRecipient<in TMessage>
    where TMessage : class
{
    /// <summary>
    /// 接收指定类型 <typeparamref name="TMessage"/> 的消息实例
    /// </summary>
    /// <typeparam name="TMessage">要接收的消息类型</typeparam>
    /// <param name="message">正在接收的消息实例</param>
    void Receive(TMessage message);
}