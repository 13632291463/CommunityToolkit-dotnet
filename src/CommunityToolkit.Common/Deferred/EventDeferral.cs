// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
#if NET8_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CA1063

namespace CommunityToolkit.Common.Deferred;

/// <summary>
/// 由 <see cref="DeferredEventArgs"/> 提供的延迟处理句柄
/// </summary>
public class EventDeferral : IDisposable
{
#if NET8_0_OR_GREATER
    private readonly TaskCompletionSource taskCompletionSource = new();
#else
    private readonly TaskCompletionSource<object?> taskCompletionSource = new();
#endif

    /// <summary>
    /// 初始化 <see cref="EventDeferral"/> 类的新实例
    /// </summary>
    internal EventDeferral()
    {
    }

    /// <summary>
    /// 完成延迟处理时调用此方法
    /// </summary>
    public void Complete()
    {
#if NET8_0_OR_GREATER
        this.taskCompletionSource.TrySetResult();
#else
        this.taskCompletionSource.TrySetResult(null);
#endif
    }

    /// <summary>
    /// 等待 <see cref="EventDeferral"/> 被事件处理程序完成
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示异步操作的任务</returns>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
	// "这是仅供事件处理程序扩展类使用的内部方法，公共调用者应调用 DeferredEventArgs 上的 GetDeferral() 方法。"
    [Obsolete("This is an internal only method to be used by EventHandler extension classes, public callers should call GetDeferral() instead on the DeferredEventArgs.")]
    public async Task WaitForCompletion(CancellationToken cancellationToken)
    {
        using (cancellationToken.Register(
#if NET8_0_OR_GREATER
            callback: static obj => Unsafe.As<EventDeferral>(obj!).taskCompletionSource.TrySetCanceled(),
#else
            callback: static obj => ((EventDeferral)obj).taskCompletionSource.TrySetCanceled(),
#endif
            state: this))
        {
#if NET8_0_OR_GREATER
            await this.taskCompletionSource.Task;
#else
            _ = await this.taskCompletionSource.Task;
#endif
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Complete();
    }
}