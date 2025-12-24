// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace CommunityToolkit.Mvvm.Input;

/// <summary>
/// 用于自定义 <see cref="AsyncRelayCommand"/> 和 <see cref="AsyncRelayCommand{T}"/> 实例行为的选项。
/// </summary>
[Flags]
public enum AsyncRelayCommandOptions
{
    /// <summary>
    /// 未指定选项。<see cref="AsyncRelayCommand"/> 和 <see cref="AsyncRelayCommand{T}"/> 类型将使用其默认行为：
    /// <list type="bullet">
    ///     <item>不允许并发执行：如果存在正在运行的异步执行，则命令被禁用。</item>
    ///     <item>
    ///         <para>
    ///             异常在调用上下文中抛出：调用 <see cref="AsyncRelayCommand.Execute(object?)"/> 将等待
    ///             操作返回的 <see cref="System.Threading.Tasks.Task"/>，并在调用上下文中传播异常。
    ///         </para>
    ///         <para>此行为与同步命令一致，其中 <see cref="RelayCommand.Execute(object?)"/> 中的异常行为相同。</para>
    ///     </item>
    /// </list>
    /// </summary>
    None = 0,

    /// <summary>
    /// <para>允许并发执行。此选项使同一个命令可以同时多次调用。</para>
    /// <para>
    /// 注意，在这种情况下应考虑以下附加事项：
    /// <list type="bullet">
    ///     <item>如果命令支持取消，当启动新调用时，先前的调用将自动被取消。</item>
    ///     <item><see cref="AsyncRelayCommand.ExecutionTask"/> 属性将始终表示最后启动的操作。</item>
    /// </list>
    /// </para>
    /// </summary>
    AllowConcurrentExecutions = 1 << 0,

    /// <summary>
    /// <para>异常不会在调用上下文中抛出，而是传播到 <see cref="System.Threading.Tasks.TaskScheduler.UnobservedTaskException"/>。</para>
    /// <para>
    /// 这会影响对 <see cref="AsyncRelayCommand.Execute(object?)"/> 调用的行为。当使用此选项时，如果操作失败，该异常将不会
    /// 在调用上下文中重新抛出（因为在那里没有等待它）。相反，它将流向 <see cref="System.Threading.Tasks.TaskScheduler.UnobservedTaskException"/>。
    /// </para>
    /// <para>
    /// 此选项启用了更高级的场景，其中可以使用 <see cref="AsyncRelayCommand.ExecutionTask"/> 属性来检查
    /// 已排队操作的状态。也就是说，即使操作失败或被取消，也可以通过访问此属性在稍后时间检索详细信息。
    /// </para>
    /// </summary>
    FlowExceptionsToTaskScheduler = 1 << 1
}