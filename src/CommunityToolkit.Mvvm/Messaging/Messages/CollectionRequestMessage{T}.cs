// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CommunityToolkit.Mvvm.Messaging.Messages;

/// <summary>
/// 用于可以接收多个回复的请求消息的类，可以直接使用或通过派生类使用
/// </summary>
/// <typeparam name="T">请求的类型</typeparam>
public class CollectionRequestMessage<T> : IEnumerable<T>
{
    // 存储响应的私有列表
    private readonly List<T> responses = new();

    /// <summary>
    /// 获取消息响应
    /// </summary>
    public IReadOnlyCollection<T> Responses => this.responses;

    /// <summary>
    /// 回复当前请求消息
    /// </summary>
    /// <param name="response">用于回复请求消息的响应</param>
    public void Reply(T response)
    {
        this.responses.Add(response);
    }

    /// <summary>
    /// 返回一个循环访问集合的枚举器
    /// </summary>
    /// <returns>用于循环访问集合的枚举器</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public IEnumerator<T> GetEnumerator()
    {
        return this.responses.GetEnumerator();
    }

    /// <summary>
    /// 返回一个循环访问集合的枚举器
    /// </summary>
    /// <returns>用于循环访问集合的枚举器</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}