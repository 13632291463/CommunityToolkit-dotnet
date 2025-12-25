// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CommunityToolkit.Common.Deferred;

/// <summary>
/// 为延迟事件提供 <see cref="EventHandler{TEventArgs}"/> 扩展方法
/// </summary>
public static class EventHandlerExtensions
{
    /// <summary>
    /// 使用 <see cref="DeferredEventArgs"/> 异步调用 <see cref="EventHandler{TEventArgs}"/>
    /// </summary>
    /// <typeparam name="T">事件参数类型</typeparam>
    /// <param name="eventHandler">要调用的 <see cref="EventHandler{TEventArgs}"/></param>
    /// <param name="sender">事件发送者</param>
    /// <param name="eventArgs">事件参数实例</param>
    /// <returns>等待延迟事件处理程序的 <see cref="Task"/></returns>
    public static Task InvokeAsync<T>(this EventHandler<T>? eventHandler, object sender, T eventArgs)
        where T : DeferredEventArgs
    {
        return InvokeAsync(eventHandler, sender, eventArgs, CancellationToken.None);
    }

    /// <summary>
    /// 使用 <see cref="CancellationToken"/> 和 <see cref="DeferredEventArgs"/> 异步调用 <see cref="EventHandler{TEventArgs}"/>
    /// </summary>
    /// <typeparam name="T">事件参数类型</typeparam>
    /// <param name="eventHandler">要调用的 <see cref="EventHandler{TEventArgs}"/></param>
    /// <param name="sender">事件发送者</param>
    /// <param name="eventArgs">事件参数实例</param>
    /// <param name="cancellationToken">取消令牌选项</param>
    /// <returns>等待延迟事件处理程序的 <see cref="Task"/></returns>
    public static Task InvokeAsync<T>(this EventHandler<T>? eventHandler, object sender, T eventArgs, CancellationToken cancellationToken)
        where T : DeferredEventArgs
    {
        // 如果没有事件处理程序，返回已完成的任务
        if (eventHandler == null)
        {
            return Task.CompletedTask;
        }

        // 获取事件处理程序列表并为每个处理程序创建任务
        Task[]? tasks = eventHandler.GetInvocationList()
            .OfType<EventHandler<T>>()
            .Select(invocationDelegate =>
            {
                // 检查是否请求了取消
                cancellationToken.ThrowIfCancellationRequested();

                // 调用事件处理程序
                invocationDelegate(sender, eventArgs);

#pragma warning disable CS0618 // Type or member is obsolete
                // 获取当前延迟对象并重置
                EventDeferral? deferral = eventArgs.GetCurrentDeferralAndReset();

                // 等待延迟完成或返回已完成的任务
                return deferral?.WaitForCompletion(cancellationToken) ?? Task.CompletedTask;
#pragma warning restore CS0618 // Type or member is obsolete
            })
            .ToArray();

        // 等待所有任务完成
        return Task.WhenAll(tasks);
    }
}