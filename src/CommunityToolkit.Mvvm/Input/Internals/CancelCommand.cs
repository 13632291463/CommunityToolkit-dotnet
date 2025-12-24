// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Windows.Input;

namespace CommunityToolkit.Mvvm.Input.Internals;

/// <summary>
/// 一个实现 <see cref="ICommand"/> 接口的命令，用于包装 <see cref="IAsyncRelayCommand"/> 以支持取消操作
/// </summary>
internal sealed partial class CancelCommand : ICommand
{
    /// <summary>
    /// 被包装的 <see cref="IAsyncRelayCommand"/> 实例
    /// </summary>
    private readonly IAsyncRelayCommand command;

    /// <summary>
    /// 创建一个新的 <see cref="CancelCommand"/> 实例
    /// </summary>
    /// <param name="command">要包装的 <see cref="IAsyncRelayCommand"/> 实例</param>
    public CancelCommand(IAsyncRelayCommand command)
    {
        this.command = command;

        // 监听命令的属性变更事件，用于更新 CanExecute 状态
        this.command.PropertyChanged += OnPropertyChanged;
    }

    /// <inheritdoc/>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// 确定此命令是否可以执行
    /// </summary>
    /// <param name="parameter">命令参数，此实现中未使用</param>
    /// <returns>当关联的异步命令可以被取消时返回 true，否则返回 false</returns>
    public bool CanExecute(object? parameter)
    {
        return this.command.CanBeCanceled;
    }

    /// <summary>
    /// 执行取消命令
    /// </summary>
    /// <param name="parameter">命令参数，此实现中未使用</param>
    public void Execute(object? parameter)
    {
        this.command.Cancel();
    }

    /// <summary>
    /// 处理被包装命令的属性变更事件，当 CanBeCanceled 属性变更时触发 CanExecuteChanged 事件
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">属性变更事件参数</param>
    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null or nameof(IAsyncRelayCommand.CanBeCanceled))
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}