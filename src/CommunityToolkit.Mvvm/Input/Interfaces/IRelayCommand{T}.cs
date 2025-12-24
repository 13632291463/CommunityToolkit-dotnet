// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Input;

namespace CommunityToolkit.Mvvm.Input;

/// <summary>
/// 一个泛型接口，表示更具体的 <see cref="IRelayCommand"/> 版本
/// </summary>
/// <typeparam name="T">用作接口方法参数的类型</typeparam>
public interface IRelayCommand<in T> : IRelayCommand
{
    /// <summary>
    /// 提供 <see cref="ICommand.CanExecute(object)"/> 的强类型变体
    /// </summary>
    /// <param name="parameter">输入参数</param>
    /// <returns>当前命令是否可以执行</returns>
    /// <remarks>如果 <typeparamref name="T"/> 是值类型，请使用此重载以避免装箱</remarks>
    bool CanExecute(T? parameter);

    /// <summary>
    /// 提供 <see cref="ICommand.Execute(object)"/> 的强类型变体
    /// </summary>
    /// <param name="parameter">输入参数</param>
    /// <remarks>如果 <typeparamref name="T"/> 是值类型，请使用此重载以避免装箱</remarks>
    void Execute(T? parameter);
}