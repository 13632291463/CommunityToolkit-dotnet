// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input.Internals;

#pragma warning disable CS0618, CA1001

namespace CommunityToolkit.Mvvm.Input;

/// <summary>
/// 一个泛型命令，提供 <see cref="AsyncRelayCommand"/> 的更具体的版本。
/// </summary>
/// <typeparam name="T">作为输入传递给回调的参数类型。</typeparam>
public sealed partial class AsyncRelayCommand<T> : IAsyncRelayCommand<T>, ICancellationAwareCommand
{
    /// <summary>
    /// 当使用 <see cref="Execute(T)"/> 时要调用的 <see cref="Func{TResult}"/>。
    /// </summary>
    private readonly Func<T?, Task>? execute;

    /// <summary>
    /// 当使用 <see cref="Execute(object?)"/> 时要调用的可取消 <see cref="Func{T1,T2,TResult}"/>。
    /// </summary>
    private readonly Func<T?, CancellationToken, Task>? cancelableExecute;

    /// <summary>
    /// 当使用 <see cref="CanExecute(T)"/> 时要调用的可选操作。
    /// </summary>
    private readonly Predicate<T?>? canExecute;

    /// <summary>
    /// 当前命令设置的选项。
    /// </summary>
    private readonly AsyncRelayCommandOptions options;

    /// <summary>
    /// 用于取消 <see cref="cancelableExecute"/> 的 <see cref="CancellationTokenSource"/> 实例。
    /// </summary>
    private CancellationTokenSource? cancellationTokenSource;

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc/>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// 初始化 <see cref="AsyncRelayCommand{T}"/> 类的新实例。
    /// </summary>
    /// <param name="execute">执行逻辑。</param>
    /// <remarks>参见 <see cref="RelayCommand{T}(Action{T})"/> 中的说明。</remarks>
    /// <exception cref="System.ArgumentNullException">如果 <paramref name="execute"/> 为 <see langword="null"/> 则抛出。</exception>
    public AsyncRelayCommand(Func<T?, Task> execute)
    {
        ArgumentNullException.ThrowIfNull(execute);

        this.execute = execute;
    }

    /// <summary>
    /// 初始化 <see cref="AsyncRelayCommand{T}"/> 类的新实例。
    /// </summary>
    /// <param name="execute">执行逻辑。</param>
    /// <param name="options">用于配置异步命令的选项。</param>
    /// <remarks>参见 <see cref="RelayCommand{T}(Action{T})"/> 中的说明。</remarks>
    /// <exception cref="System.ArgumentNullException">如果 <paramref name="execute"/> 为 <see langword="null"/> 则抛出。</exception>
    public AsyncRelayCommand(Func<T?, Task> execute, AsyncRelayCommandOptions options)
    {
        ArgumentNullException.ThrowIfNull(execute);

        this.execute = execute;
        this.options = options;
    }

    /// <summary>
    /// 初始化 <see cref="AsyncRelayCommand{T}"/> 类的新实例。
    /// </summary>
    /// <param name="cancelableExecute">可取消的执行逻辑。</param>
    /// <remarks>参见 <see cref="RelayCommand{T}(Action{T})"/> 中的说明。</remarks>
    /// <exception cref="System.ArgumentNullException">如果 <paramref name="cancelableExecute"/> 为 <see langword="null"/> 则抛出。</exception>
    public AsyncRelayCommand(Func<T?, CancellationToken, Task> cancelableExecute)
    {
        ArgumentNullException.ThrowIfNull(cancelableExecute);

        this.cancelableExecute = cancelableExecute;
    }

    /// <summary>
    /// 初始化 <see cref="AsyncRelayCommand{T}"/> 类的新实例。
    /// </summary>
    /// <param name="cancelableExecute">可取消的执行逻辑。</param>
    /// <param name="options">用于配置异步命令的选项。</param>
    /// <remarks>参见 <see cref="RelayCommand{T}(Action{T})"/> 中的说明。</remarks>
    /// <exception cref="System.ArgumentNullException">如果 <paramref name="cancelableExecute"/> 为 <see langword="null"/> 则抛出。</exception>
    public AsyncRelayCommand(Func<T?, CancellationToken, Task> cancelableExecute, AsyncRelayCommandOptions options)
    {
        ArgumentNullException.ThrowIfNull(cancelableExecute);

        this.cancelableExecute = cancelableExecute;
        this.options = options;
    }

    /// <summary>
    /// 初始化 <see cref="AsyncRelayCommand{T}"/> 类的新实例。
    /// </summary>
    /// <param name="execute">执行逻辑。</param>
    /// <param name="canExecute">执行状态逻辑。</param>
    /// <remarks>参见 <see cref="RelayCommand{T}(Action{T})"/> 中的说明。</remarks>
    /// <exception cref="System.ArgumentNullException">如果 <paramref name="execute"/> 或 <paramref name="canExecute"/> 为 <see langword="null"/> 则抛出。</exception>
    public AsyncRelayCommand(Func<T?, Task> execute, Predicate<T?> canExecute)
    {
        ArgumentNullException.ThrowIfNull(execute);
        ArgumentNullException.ThrowIfNull(canExecute);

        this.execute = execute;
        this.canExecute = canExecute;
    }

    /// <summary>
    /// 初始化 <see cref="AsyncRelayCommand{T}"/> 类的新实例。
    /// </summary>
    /// <param name="execute">执行逻辑。</param>
    /// <param name="canExecute">执行状态逻辑。</param>
    /// <param name="options">用于配置异步命令的选项。</param>
    /// <remarks>参见 <see cref="RelayCommand{T}(Action{T})"/> 中的说明。</remarks>
    /// <exception cref="System.ArgumentNullException">如果 <paramref name="execute"/> 或 <paramref name="canExecute"/> 为 <see langword="null"/> 则抛出。</exception>
    public AsyncRelayCommand(Func<T?, Task> execute, Predicate<T?> canExecute, AsyncRelayCommandOptions options)
    {
        ArgumentNullException.ThrowIfNull(execute);
        ArgumentNullException.ThrowIfNull(canExecute);

        this.execute = execute;
        this.canExecute = canExecute;
        this.options = options;
    }

    /// <summary>
    /// 初始化 <see cref="AsyncRelayCommand{T}"/> 类的新实例。
    /// </summary>
    /// <param name="cancelableExecute">可取消的执行逻辑。</param>
    /// <param name="canExecute">执行状态逻辑。</param>
    /// <remarks>参见 <see cref="RelayCommand{T}(Action{T})"/> 中的说明。</remarks>
    /// <exception cref="System.ArgumentNullException">如果 <paramref name="cancelableExecute"/> 或 <paramref name="canExecute"/> 为 <see langword="null"/> 则抛出。</exception>
    public AsyncRelayCommand(Func<T?, CancellationToken, Task> cancelableExecute, Predicate<T?> canExecute)
    {
        ArgumentNullException.ThrowIfNull(cancelableExecute);
        ArgumentNullException.ThrowIfNull(canExecute);

        this.cancelableExecute = cancelableExecute;
        this.canExecute = canExecute;
    }

    /// <summary>
    /// 初始化 <see cref="AsyncRelayCommand{T}"/> 类的新实例。
    /// </summary>
    /// <param name="cancelableExecute">可取消的执行逻辑。</param>
    /// <param name="canExecute">执行状态逻辑。</param>
    /// <param name="options">用于配置异步命令的选项。</param>
    /// <remarks>参见 <see cref="RelayCommand{T}(Action{T})"/> 中的说明。</remarks>
    /// <exception cref="System.ArgumentNullException">如果 <paramref name="cancelableExecute"/> 或 <paramref name="canExecute"/> 为 <see langword="null"/> 则抛出。</exception>
    public AsyncRelayCommand(Func<T?, CancellationToken, Task> cancelableExecute, Predicate<T?> canExecute, AsyncRelayCommandOptions options)
    {
        ArgumentNullException.ThrowIfNull(cancelableExecute);
        ArgumentNullException.ThrowIfNull(canExecute);

        this.cancelableExecute = cancelableExecute;
        this.canExecute = canExecute;
        this.options = options;
    }

    private Task? executionTask;

    /// <summary>
    /// 获取或设置当前执行的任务
    /// </summary>
    public Task? ExecutionTask
    {
        get => this.executionTask;
        private set
        {
            if (ReferenceEquals(this.executionTask, value))
            {
                return;
            }

            this.executionTask = value;

            PropertyChanged?.Invoke(this, AsyncRelayCommand.ExecutionTaskChangedEventArgs);
            PropertyChanged?.Invoke(this, AsyncRelayCommand.IsRunningChangedEventArgs);

            bool isAlreadyCompletedOrNull = value?.IsCompleted ?? true;

            if (this.cancellationTokenSource is not null)
            {
                PropertyChanged?.Invoke(this, AsyncRelayCommand.CanBeCanceledChangedEventArgs);
                PropertyChanged?.Invoke(this, AsyncRelayCommand.IsCancellationRequestedChangedEventArgs);
            }

            if (isAlreadyCompletedOrNull)
            {
                return;
            }

            // 监视任务完成情况并更新相关属性
            static async void MonitorTask(AsyncRelayCommand<T> @this, Task task)
            {
                await task.GetAwaitableWithoutEndValidation();

                if (ReferenceEquals(@this.executionTask, task))
                {
                    @this.PropertyChanged?.Invoke(@this, AsyncRelayCommand.ExecutionTaskChangedEventArgs);
                    @this.PropertyChanged?.Invoke(@this, AsyncRelayCommand.IsRunningChangedEventArgs);
                    
                    if (@this.cancellationTokenSource is not null)
                    {
                        @this.PropertyChanged?.Invoke(@this, AsyncRelayCommand.CanBeCanceledChangedEventArgs);
                    }

                    if ((@this.options & AsyncRelayCommandOptions.AllowConcurrentExecutions) == 0)
                    {
                        @this.CanExecuteChanged?.Invoke(@this, EventArgs.Empty);
                    }
                }
            }

            MonitorTask(this, value!);
        }
    }

    /// <summary>
    /// 获取一个值，指示命令是否可以被取消
    /// </summary>
    public bool CanBeCanceled => IsRunning && this.cancellationTokenSource is { IsCancellationRequested: false };

    /// <summary>
    /// 获取一个值，指示是否已请求取消
    /// </summary>
    public bool IsCancellationRequested => this.cancellationTokenSource is { IsCancellationRequested: true };

    /// <summary>
    /// 获取一个值，指示命令是否正在运行
    /// </summary>
    public bool IsRunning => ExecutionTask is { IsCompleted: false };

    /// <summary>
    /// 获取一个值，指示命令是否支持取消
    /// </summary>
    bool ICancellationAwareCommand.IsCancellationSupported => this.execute is null;

    /// <summary>
    /// 通知命令管理器该命令的可执行状态可能已更改
    /// </summary>
    public void NotifyCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 确定在指定状态参数的情况下，此命令是否可以执行
    /// </summary>
    /// <param name="parameter">用于确定命令是否可以执行的参数</param>
    /// <returns>如果可以执行此命令，则为 true；否则为 false</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CanExecute(T? parameter)
    {
        bool canExecute = this.canExecute?.Invoke(parameter) != false;

        return canExecute && ((this.options & AsyncRelayCommandOptions.AllowConcurrentExecutions) != 0 || ExecutionTask is not { IsCompleted: false });
    }

    /// <summary>
    /// 确定在指定状态参数的情况下，此命令是否可以执行
    /// </summary>
    /// <param name="parameter">用于确定命令是否可以执行的参数</param>
    /// <returns>如果可以执行此命令，则为 true；否则为 false</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CanExecute(object? parameter)
    {
        // 特殊情况处理，参见 RelayCommand<T>.CanExecute(object?) 了解更多信息
        if (parameter is null && default(T) is not null)
        {
            return false;
        }

        if (!RelayCommand<T>.TryGetCommandArgument(parameter, out T? result))
        {
            RelayCommand<T>.ThrowArgumentExceptionForInvalidCommandArgument(parameter);
        }

        return CanExecute(result);
    }

    /// <summary>
    /// 对其可执行性委托传递的参数执行与命令关联的逻辑
    /// </summary>
    /// <param name="parameter">命令使用的数据。如果命令不需要传递数据，则可以将此对象设置为 null</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Execute(T? parameter)
    {
        Task executionTask = ExecuteAsync(parameter);

        if ((this.options & AsyncRelayCommandOptions.FlowExceptionsToTaskScheduler) == 0)
        {
            AsyncRelayCommand.AwaitAndThrowIfFailed(executionTask);
        }
    }

    /// <summary>
    /// 对其可执行性委托传递的参数执行与命令关联的逻辑
    /// </summary>
    /// <param name="parameter">命令使用的数据。如果命令不需要传递数据，则可以将此对象设置为 null</param>
    public void Execute(object? parameter)
    {
        if (!RelayCommand<T>.TryGetCommandArgument(parameter, out T? result))
        {
            RelayCommand<T>.ThrowArgumentExceptionForInvalidCommandArgument(parameter);
        }

        Execute(result);
    }

    /// <summary>
    /// 异步执行与命令关联的逻辑
    /// </summary>
    /// <param name="parameter">命令使用的数据。如果命令不需要传递数据，则可以将此对象设置为 null</param>
    /// <returns>表示异步操作的任务</returns>
    public Task ExecuteAsync(T? parameter)
    {
        Task executionTask;

        if (this.execute is not null)
        {
            // 非可取消命令委托
            executionTask = ExecutionTask = this.execute(parameter);
        }
        else
        {
            // 取消之前的操作（如果存在待处理操作）
            this.cancellationTokenSource?.Cancel();

            CancellationTokenSource cancellationTokenSource = this.cancellationTokenSource = new();

            // 使用新的链接令牌调用可取消命令委托
            executionTask = ExecutionTask = this.cancelableExecute!(parameter, cancellationTokenSource.Token);
        }

        // 如果禁用并发执行，则通知可执行性更改
        if ((this.options & AsyncRelayCommandOptions.AllowConcurrentExecutions) == 0)
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        return executionTask;
    }

    /// <summary>
    /// 异步执行与命令关联的逻辑
    /// </summary>
    /// <param name="parameter">命令使用的数据。如果命令不需要传递数据，则可以将此对象设置为 null</param>
    /// <returns>表示异步操作的任务</returns>
    public Task ExecuteAsync(object? parameter)
    {
        if (!RelayCommand<T>.TryGetCommandArgument(parameter, out T? result))
        {
            RelayCommand<T>.ThrowArgumentExceptionForInvalidCommandArgument(parameter);
        }

        return ExecuteAsync(result);
    }

    /// <summary>
    /// 尝试取消当前正在执行的操作
    /// </summary>
    public void Cancel()
    {
        if (this.cancellationTokenSource is CancellationTokenSource { IsCancellationRequested: false } cancellationTokenSource)
        {
            cancellationTokenSource.Cancel();

            PropertyChanged?.Invoke(this, AsyncRelayCommand.CanBeCanceledChangedEventArgs);
            PropertyChanged?.Invoke(this, AsyncRelayCommand.IsCancellationRequestedChangedEventArgs);
        }
    }
}