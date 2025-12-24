// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Threading.Tasks;

namespace CommunityToolkit.Mvvm.Input;

/// <summary>
/// 一个扩展 <see cref="IRelayCommand"/> 的接口，以支持异步操作。
/// </summary>
public interface IAsyncRelayCommand : IRelayCommand, INotifyPropertyChanged
{
    /// <summary>
    /// 获取最后调度的 <see cref="Task"/>（如果可用）。
    /// 此属性在 <see cref="Task"/> 完成时通知更改。
    /// </summary>
    Task? ExecutionTask { get; }

    /// <summary>
    /// 获取一个值，该值指示当前是否可以取消正在运行的操作。
    /// </summary>
    /// <remarks>
    /// 实现此接口的类型应引发的事件的确切序列如下：
    /// <list type="bullet">
    /// <item>
    /// 命令最初未运行：<see cref="IsRunning"/>、<see cref="CanBeCanceled"/>
    /// 和 <see cref="IsCancellationRequested"/> 为 <see langword="false"/>。
    /// </item>
    /// <item>
    /// 命令开始运行：<see cref="IsRunning"/> 和 <see cref="CanBeCanceled"/> 切换到
    /// <see langword="true"/>。<see cref="IsCancellationRequested"/> 设置为 <see langword="false"/>。
    /// </item>
    /// <item>
    /// 如果操作被取消：<see cref="CanBeCanceled"/> 切换到 <see langword="false"/>
    /// 和 <see cref="IsCancellationRequested"/> 切换到 <see langword="true"/>。
    /// </item>
    /// <item>
    /// 操作完成：<see cref="IsRunning"/> 和 <see cref="CanBeCanceled"/> 切换
    /// 到 <see langword="false"/>。<see cref="IsCancellationRequested"/> 的状态未定义。
    /// </item>
    /// </list>
    /// 这仅适用于命令的底层逻辑实际支持取消的情况。如果不是这种情况，
    /// 则 <see cref="CanBeCanceled"/> 和 <see cref="IsCancellationRequested"/> 将始终保持
    /// <see langword="false"/>，无论命令的当前状态如何。
    /// </remarks>
    bool CanBeCanceled { get; }

    /// <summary>
    /// 获取一个值，该值指示是否已为当前操作发出取消请求。
    /// </summary>
    bool IsCancellationRequested { get; }

    /// <summary>
    /// 获取一个值，该值指示命令当前是否具有正在执行的待处理操作。
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// 提供 <see cref="System.Windows.Input.ICommand.Execute"/> 的更具体版本，
    /// 同时返回表示正在执行的异步操作的 <see cref="Task"/>。
    /// </summary>
    /// <param name="parameter">输入参数。</param>
    /// <returns>表示正在执行的异步操作的 <see cref="Task"/>。</returns>
    /// <exception cref="System.ArgumentException">如果 <paramref name="parameter"/> 与底层命令实现不兼容，则引发此异常。</exception>
    Task ExecuteAsync(object? parameter);

    /// <summary>
    /// 发出取消请求。
    /// </summary>
    /// <remarks>
    /// 如果底层命令未运行，或者不支持取消，则此方法将不执行任何操作。
    /// 请注意，即使取消成功，当前操作的完成可能也不是立即的。
    /// </remarks>
    void Cancel();
}