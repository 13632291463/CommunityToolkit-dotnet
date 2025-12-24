// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This file is inspired from the MvvmLight library (lbugnion/MvvmLight),
// more info in ThirdPartyNotices.txt in the root of the project.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace CommunityToolkit.Mvvm.Input;

/// <summary>
/// 一个泛型命令，其唯一目的是通过调用委托将功能传递给其他对象。
/// CanExecute 方法的默认返回值为 <see langword="true"/>。此类允许您在 Execute(T) 和 CanExecute(T) 回调方法中接受命令参数
/// </summary>
/// <typeparam name="T">作为输入传递给回调的参数类型</typeparam>
public sealed partial class RelayCommand<T> : IRelayCommand<T>
{
    /// <summary>
    /// 当调用 Execute(T) 时要调用的 <see cref="Action"/>
    /// </summary>
    private readonly Action<T?> execute;

    /// <summary>
    /// 调用 CanExecute(T) 时可选的调用操作
    /// </summary>
    private readonly Predicate<T?>? canExecute;

    /// <inheritdoc/>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// 初始化 <see cref="RelayCommand{T}"/> 类的新实例，该实例始终可以执行
    /// </summary>
    /// <param name="execute">执行逻辑</param>
    /// <remarks>
    /// 由于 <see cref="System.Windows.Input.ICommand"/> 接口公开了接受可空 <see cref="object"/> 参数的方法，
    /// 因此如果 <typeparamref name="T"/> 是引用类型，建议始终将其声明为可空，并在 <paramref name="execute"/> 中始终执行检查
    /// </remarks>
    /// <exception cref="System.ArgumentNullException">当 <paramref name="execute"/> 为 <see langword="null"/> 时抛出</exception>
    public RelayCommand(Action<T?> execute)
    {
        ArgumentNullException.ThrowIfNull(execute);

        this.execute = execute;
    }

    /// <summary>
    /// 初始化 <see cref="RelayCommand{T}"/> 类的新实例
    /// </summary>
    /// <param name="execute">执行逻辑</param>
    /// <param name="canExecute">执行状态逻辑</param>
    /// <remarks>参见 <see cref="RelayCommand{T}(Action{T})"/> 中的注释</remarks>
    /// <exception cref="System.ArgumentNullException">当 <paramref name="execute"/> 或 <paramref name="canExecute"/> 为 <see langword="null"/> 时抛出</exception>
    public RelayCommand(Action<T?> execute, Predicate<T?> canExecute)
    {
        ArgumentNullException.ThrowIfNull(execute);
        ArgumentNullException.ThrowIfNull(canExecute);

        this.execute = execute;
        this.canExecute = canExecute;
    }

    /// <summary>
    /// 通知命令的可执行状态已更改
    /// </summary>
    public void NotifyCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 确定此命令是否可执行
    /// </summary>
    /// <param name="parameter">命令参数</param>
    /// <returns>如果命令可执行则返回 true，否则返回 false</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CanExecute(T? parameter)
    {
        return this.canExecute?.Invoke(parameter) != false;
    }

    /// <summary>
    /// 确定此命令是否可执行
    /// </summary>
    /// <param name="parameter">命令参数</param>
    /// <returns>如果命令可执行则返回 true，否则返回 false</returns>
    public bool CanExecute(object? parameter)
    {
        // 特殊处理值类型参数类型的空值情况
        // 这确保在初始化期间不会抛出异常
        if (parameter is null && default(T) is not null)
        {
            return false;
        }

        if (!TryGetCommandArgument(parameter, out T? result))
        {
            ThrowArgumentExceptionForInvalidCommandArgument(parameter);
        }

        return CanExecute(result);
    }

    /// <summary>
    /// 执行命令
    /// </summary>
    /// <param name="parameter">命令参数</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Execute(T? parameter)
    {
        this.execute(parameter);
    }

    /// <summary>
    /// 执行命令
    /// </summary>
    /// <param name="parameter">命令参数</param>
    public void Execute(object? parameter)
    {
        if (!TryGetCommandArgument(parameter, out T? result))
        {
            ThrowArgumentExceptionForInvalidCommandArgument(parameter);
        }

        Execute(result);
    }

    /// <summary>
    /// 尝试从输入对象获取兼容类型 T 的命令参数
    /// </summary>
    /// <param name="parameter">输入参数</param>
    /// <param name="result">结果 T 值（如果存在）</param>
    /// <returns>是否可以检索到兼容的命令参数</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGetCommandArgument(object? parameter, out T? result)
    {
        // 如果参数为空且 T 的默认值也为 null，则参数有效
        // T 可能是引用类型或可空值类型
        if (parameter is null && default(T) is null)
        {
            result = default;

            return true;
        }

        // 检查参数是否为 T 值，所以 T 是引用类型时可能是类型的实例或派生类型，
        // 如果 T 是接口则是接口实现，如果 T 是值类型则可能是装箱的值类型
        if (parameter is T argument)
        {
            result = argument;

            return true;
        }

        result = default;

        return false;
    }

    /// <summary>
    /// 当使用无效的命令参数时抛出 <see cref="ArgumentException"/> 异常
    /// </summary>
    /// <param name="parameter">输入参数</param>
    /// <exception cref="ArgumentException">在参数无效时抛出异常，提供有关无效参数的信息</exception>
    [DoesNotReturn]
    internal static void ThrowArgumentExceptionForInvalidCommandArgument(object? parameter)
    {
        /// <summary>
        /// 获取与无效参数相关的异常
        /// </summary>
        /// <param name="parameter">输入参数</param>
        /// <returns>ArgumentException 异常实例</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static Exception GetException(object? parameter)
        {
            if (parameter is null)
            {
                // 当参数为空时创建异常消息
                return new ArgumentException($"Parameter \"{nameof(parameter)}\" (object) must not be null, as the command type requires an argument of type {typeof(T)}.", nameof(parameter));
            }

            // 当参数类型不匹配时创建异常消息
            return new ArgumentException($"Parameter \"{nameof(parameter)}\" (object) cannot be of type {parameter.GetType()}, as the command type requires an argument of type {typeof(T)}.", nameof(parameter));
        }

        throw GetException(parameter);
    }
}