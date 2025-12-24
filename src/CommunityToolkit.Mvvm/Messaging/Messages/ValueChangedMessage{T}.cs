// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Mvvm.Messaging.Messages;

/// <summary>
/// 一个基础消息类，用于在特定值发生改变时发出信号
/// </summary>
/// <typeparam name="T">发生变化的值的类型</typeparam>
public class ValueChangedMessage<T>
{
    /// <summary>
    /// 初始化 ValueChangedMessage<T> 类的新实例
    /// </summary>
    /// <param name="value">发生变化的值</param>
    public ValueChangedMessage(T value)
    {
        Value = value;
    }

    /// <summary>
    /// 获取发生变化的值
    /// </summary>
    public T Value { get; }
}