// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This file is inspired from the MvvmLight library (lbugnion/MvvmLight),
// more info in ThirdPartyNotices.txt in the root of the project.

namespace CommunityToolkit.Mvvm.Messaging.Messages;

/// <summary>
/// 用于广播 observable 对象中的属性更改的消息。
/// </summary>
/// <typeparam name="T">要广播更改的属性类型。</typeparam>
public class PropertyChangedMessage<T>
{
    /// <summary>
    /// 初始化 <see cref="PropertyChangedMessage{T}"/> 类的新实例。
    /// </summary>
    /// <param name="sender">广播消息的原始发送者。</param>
    /// <param name="propertyName">更改的属性名称。</param>
    /// <param name="oldValue">属性更改前的值。</param>
    /// <param name="newValue">属性更改后的值。</param>
    /// <exception cref="System.ArgumentNullException">当 <paramref name="sender"/> 为 <see langword="null"/> 时抛出。</exception>
    public PropertyChangedMessage(object sender, string? propertyName, T oldValue, T newValue)
    {
        ArgumentNullException.ThrowIfNull(sender);

        Sender = sender;
        PropertyName = propertyName;
        OldValue = oldValue;
        NewValue = newValue;
    }

    /// <summary>
    /// 获取广播消息的原始发送者。
    /// </summary>
    public object Sender { get; }

    /// <summary>
    /// 获取更改的属性名称。
    /// </summary>
    public string? PropertyName { get; }

    /// <summary>
    /// 获取属性更改前的值。
    /// </summary>
    public T OldValue { get; }

    /// <summary>
    /// 获取属性更改后的值。
    /// </summary>
    public T NewValue { get; }
}