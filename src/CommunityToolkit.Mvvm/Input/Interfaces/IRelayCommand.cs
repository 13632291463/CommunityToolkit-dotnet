// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Input;

namespace CommunityToolkit.Mvvm.Input;

/// <summary>
/// 一个接口，扩展 <see cref="ICommand"/>，具有外部触发 <see cref="ICommand.CanExecuteChanged"/> 事件的能力
/// </summary>
public interface IRelayCommand : ICommand
{
    /// <summary>
    /// 通知 <see cref="ICommand.CanExecute"/> 属性已更改
    /// </summary>
    void NotifyCanExecuteChanged();
}