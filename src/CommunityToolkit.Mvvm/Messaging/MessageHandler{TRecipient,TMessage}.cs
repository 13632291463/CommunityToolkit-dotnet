// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Mvvm.Messaging;

/// <summary>
/// 用于表示接收消息时要调用的操作的委托类型
/// 提供接收者作为输入参数是为了允许消息注册避免创建闭包：
/// 如果需要调用接收者上的实例方法，则可以直接将接收者转换为正确的类型，
/// 然后从该实例访问本地方法
/// </summary>
/// <typeparam name="TRecipient">消息接收者的类型</typeparam>
/// <typeparam name="TMessage">要接收的消息类型</typeparam>
/// <param name="recipient">接收消息的接收者</param>
/// <param name="message">正在接收的消息</param>
public delegate void MessageHandler<in TRecipient, in TMessage>(TRecipient recipient, TMessage message)
    where TRecipient : class
    where TMessage : class;