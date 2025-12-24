// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Input;
using CommunityToolkit.Mvvm.Input.Internals;

namespace CommunityToolkit.Mvvm.Input;

/// <summary>
/// IAsyncRelayCommand 类型的扩展方法
/// </summary>
public static class IAsyncRelayCommandExtensions
{
    /// <summary>
    /// 创建一个可以用于取消输入命令执行的 ICommand 实例。
    /// 返回的命令还将根据包装命令的状态通知其何时可以执行。
    /// </summary>
    /// <param name="command">要为其创建取消命令的输入 IAsyncRelayCommand 实例。</param>
    /// <returns>可用于监视和发出 command 取消信号的 ICommand 实例。</returns>
    /// <remarks>不能保证在使用相同参数多次调用时返回的实例是唯一的。</remarks>
    /// <exception cref="System.ArgumentNullException">当 command 为 null 时抛出。</exception>
    public static ICommand CreateCancelCommand(this IAsyncRelayCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        // 如果已知命令永远不会允许取消，则重用同一个实例
        if (command is ICancellationAwareCommand { IsCancellationSupported: false })
        {
            return DisabledCommand.Instance;
        }

        // 创建一个包装输入命令的新取消命令
        return new CancelCommand(command);
    }
}