// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Input;

namespace CommunityToolkit.Mvvm.Input.Internals;

/// <summary>
/// 一个始终处于禁用状态的可重用 <see cref="ICommand"/> 实例。
/// </summary>
/// <remarks>
/// 此类实现 ICommand 接口，但始终返回 CanExecute 为 false，Execute 方法不执行任何操作。
/// 用于需要禁用命令的场景。
/// </remarks>
internal sealed partial class DisabledCommand : ICommand
{
    /// <inheritdoc/>
    /// <remarks>
    /// 此事件的添加和移除操作均为空实现，因为此命令永远不会触发状态更改。
    /// </remarks>
    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    /// <summary>
    /// 获取共享的、可重用的 <see cref="DisabledCommand"/> 实例。
    /// </summary>
    /// <remarks>
    /// 此实例可以在多个对象之间安全地共享使用，而不用担心此静态实例会使其他对象保持存活状态，
    /// 因为事件使用了自定义访问器，该访问器仅丢弃项目（因为事件永远不会被引发）。
    /// 因此，此实例永远不会充当其他对象的根节点。
    /// </remarks>
    public static DisabledCommand Instance { get; } = new();

    /// <summary>
    /// 确定此命令是否可以针对指定参数执行。
    /// </summary>
    /// <param name="parameter">此命令的参数。可以为 null。</param>
    /// <returns>始终返回 false，表示此命令始终不可执行。</returns>
    public bool CanExecute(object? parameter)
    {
        return false;
    }

    /// <summary>
    /// 执行命令逻辑。
    /// </summary>
    /// <param name="parameter">此命令的参数。可以为 null。</param>
    /// <remarks>
    /// 此方法不执行任何操作，因为此命令始终处于禁用状态。
    /// </remarks>
    public void Execute(object? parameter)
    {
    }
}