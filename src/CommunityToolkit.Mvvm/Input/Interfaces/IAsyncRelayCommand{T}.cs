// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace CommunityToolkit.Mvvm.Input;

/// <summary>
/// 一个泛型接口，表示更具体的 <see cref="IAsyncRelayCommand"/> 版本
/// </summary>
/// <typeparam name="T">用作接口方法参数的类型。</typeparam>
/// <remarks>此接口用于解决基类的菱形问题。</remarks>
public interface IAsyncRelayCommand<in T> : IAsyncRelayCommand, IRelayCommand<T>
{
    /// <summary>
    /// 提供 <see cref="IAsyncRelayCommand.ExecuteAsync"/> 的强类型变体
    /// </summary>
    /// <param name="parameter">输入参数</param>
    /// <returns>表示正在执行的异步操作的 <see cref="Task"/></returns>
    Task ExecuteAsync(T? parameter);
}