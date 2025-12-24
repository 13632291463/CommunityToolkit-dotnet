// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This file is inspired from the MvvmLight library (lbugnion/MvvmLight),
// more info in ThirdPartyNotices.txt in the root of the project.

using System;
using System.Runtime.CompilerServices;

namespace CommunityToolkit.Mvvm.Input;

/// <summary>
/// 一个命令，其唯一目的是通过调用委托将功能传递给其他对象。
/// <see cref="CanExecute"/> 方法的默认返回值为 <see langword="true"/>。 
/// 此类型不允许您在 <see cref="Execute"/> 和 <see cref="CanExecute"/> 回调方法中接受命令参数。
/// </summary>
public sealed partial class RelayCommand : IRelayCommand
{
    /// <summary>
    /// 当使用 <see cref="Execute"/> 时要调用的 <see cref="Action"/>。
    /// </summary>
    private readonly Action execute;

    /// <summary>
    /// 当使用 <see cref="CanExecute"/> 时要调用的可选操作。
    /// </summary>
    private readonly Func<bool>? canExecute;

    /// <inheritdoc/>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// 初始化一个可以始终执行的 <see cref="RelayCommand"/> 类的新实例。
    /// </summary>
    /// <param name="execute">执行逻辑。</param>
    /// <exception cref="System.ArgumentNullException">如果 <paramref name="execute"/> 为 <see langword="null"/> 则抛出异常。</exception>
    public RelayCommand(Action execute)
    {
        ArgumentNullException.ThrowIfNull(execute);

        this.execute = execute;
    }

    /// <summary>
    /// 初始化 <see cref="RelayCommand"/> 类的新实例。
    /// </summary>
    /// <param name="execute">执行逻辑。</param>
    /// <param name="canExecute">执行状态逻辑。</param>
    /// <exception cref="System.ArgumentNullException">如果 <paramref name="execute"/> 或 <paramref name="canExecute"/> 为 <see langword="null"/> 则抛出异常。</exception>
    public RelayCommand(Action execute, Func<bool> canExecute)
    {
        ArgumentNullException.ThrowIfNull(execute);
        ArgumentNullException.ThrowIfNull(canExecute);

        this.execute = execute;
        this.canExecute = canExecute;
    }

    /// <summary>
    /// 通知命令执行状态已更改，触发 CanExecuteChanged 事件
    /// </summary>
    public void NotifyCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 确定此命令是否可以执行
    /// </summary>
    /// <param name="parameter">命令参数</param>
    /// <returns>如果命令可以执行则返回 true，否则返回 false</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CanExecute(object? parameter)
    {
        return this.canExecute?.Invoke() != false;
    }

    /// <summary>
    /// 执行命令逻辑
    /// </summary>
    /// <param name="parameter">命令参数</param>
    public void Execute(object? parameter)
    {
        this.execute();
    }
}