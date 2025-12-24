// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable CS8618

namespace CommunityToolkit.Mvvm.Messaging.Messages;

/// <summary>
/// 请求消息的类，可以直接使用或通过派生类使用
/// </summary>
/// <typeparam name="T">请求的类型</typeparam>
public class RequestMessage<T>
{
    private T response;

    /// <summary>
    /// 获取消息响应
    /// </summary>
    /// <exception cref="InvalidOperationException">当 <see cref="HasReceivedResponse"/> 为 <see langword="false"/> 时抛出</exception>
    public T Response
    {
        get
        {
            if (!HasReceivedResponse)
            {
                ThrowInvalidOperationExceptionForNoResponseReceived();
            }

            return this.response;
        }
    }

    /// <summary>
    /// 获取一个值，该值指示是否已为此实例分配响应
    /// </summary>
    public bool HasReceivedResponse { get; private set; }

    /// <summary>
    /// 回复当前请求消息
    /// </summary>
    /// <param name="response">用于回复请求消息的响应</param>
    /// <exception cref="InvalidOperationException">当 <see cref="Response"/> 已设置时抛出</exception>
    public void Reply(T response)
    {
        if (HasReceivedResponse)
        {
            ThrowInvalidOperationExceptionForDuplicateReply();
        }

        HasReceivedResponse = true;

        this.response = response;
    }

    /// <summary>
    /// 隐式地从给定的 <see cref="RequestMessage{T}"/> 实例获取响应
    /// </summary>
    /// <param name="message">输入的 <see cref="RequestMessage{T}"/> 实例</param>
    /// <returns>请求消息的响应值</returns>
    /// <exception cref="System.ArgumentNullException">当 <paramref name="message"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="InvalidOperationException">当 <see cref="HasReceivedResponse"/> 为 <see langword="false"/> 时抛出</exception>
    public static implicit operator T(RequestMessage<T> message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return message.Response;
    }

    /// <summary>
    /// 当响应不可用时抛出 <see cref="InvalidOperationException"/>
    /// </summary>
    [DoesNotReturn]
    private static void ThrowInvalidOperationExceptionForNoResponseReceived()
    {
        throw new InvalidOperationException("No response was received for the given request message.");
    }

    /// <summary>
    /// 当 <see cref="Reply"/> 被调用两次时抛出 <see cref="InvalidOperationException"/>
    /// </summary>
    [DoesNotReturn]
    private static void ThrowInvalidOperationExceptionForDuplicateReply()
    {
        throw new InvalidOperationException("A response has already been issued for the current message.");
    }
}