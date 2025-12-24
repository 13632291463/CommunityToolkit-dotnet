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
/// 一个命令，它模仿 <see cref="RelayCommand"/> 的功能，另外
/// 接受一个返回 <see cref="Task"/> 的 <see cref="Func{TResult}"/> 作为执行
/// 操作，并提供一个 <see cref="ExecutionTask"/> 属性，当
/// <see cref="ExecuteAsync"/> 被调用以及返回的 <see cref="Task"/> 完成时通知更改。
/// </summary>
public sealed partial class AsyncRelayCommand : IAsyncRelayCommand, ICancellationAwareCommand
{
    /// <summary>
    /// <see cref="ExecutionTask"/> 的缓存 <see cref="PropertyChangedEventArgs"/>。
    /// </summary>
    internal static readonly PropertyChangedEventArgs ExecutionTaskChangedEventArgs = new(nameof(ExecutionTask));

    /// <summary>
    /// <see cref="CanBeCanceled"/> 的缓存 <see cref="PropertyChangedEventArgs"/>。
    /// </summary>
    internal static readonly PropertyChangedEventArgs CanBeCanceledChangedEventArgs = new(nameof(CanBeCanceled));

    /// <summary>
    /// <see cref="IsCancellationRequested"/> 的缓存 <see cref="PropertyChangedEventArgs"/>。
    /// </summary>
    internal static readonly PropertyChangedEventArgs IsCancellationRequestedChangedEventArgs = new(nameof(IsCancellationRequested));

    /// <summary>
    /// <see cref="IsRunning"/> 的缓存 <see cref="PropertyChangedEventArgs"/>。
    /// </summary>
    internal static readonly PropertyChangedEventArgs IsRunningChangedEventArgs = new(nameof(IsRunning));

    /// <summary>
    /// 当使用 <see cref="Execute"/> 时要调用的 <see cref="Func{TResult}"/>。
    /// </summary>
    private readonly Func<Task>? execute;

    /// <summary>
    /// 当使用 <see cref="Execute"/> 时要调用的可取消 <see cref="Func{T,TResult}"/>。
    /// </summary>
    /// <remarks>在这和 <see cref="execute"/> 之间只有一者不为 <see langword="null"/>。</remarks>
    private readonly Func<CancellationToken, Task>? cancelableExecute;

    /// <summary>
    /// 当使用 <see cref="CanExecute"/> 时要调用的可选操作。
    /// </summary>
    private readonly Func<bool>? canExecute;

    /// <summary>
    /// 当前命令设置的选项。
    /// </summary>
    private readonly AsyncRelayCommandOptions options;

    /// <summary>
    /// 用于取消 <see cref="cancelableExecute"/> 的 <see cref="CancellationTokenSource"/> 实例。
    /// </summary>
    /// <remarks>仅在 <see cref="cancelableExecute"/> 不为 <see langword="null"/> 时使用。</remarks>
    private CancellationTokenSource? cancellationTokenSource;

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc/>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncRelayCommand"/> class.
    /// </summary>
    /// <param name="execute">The execution logic.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="execute"/> is <see langword="null"/>.</exception>
    public AsyncRelayCommand(Func<Task> execute)
    {
        ArgumentNullException.ThrowIfNull(execute);

        this.execute = execute;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncRelayCommand"/> class.
    /// </summary>
    /// <param name="execute">The execution logic.</param>
    /// <param name="options">The options to use to configure the async command.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="execute"/> is <see langword="null"/>.</exception>
    public AsyncRelayCommand(Func<Task> execute, AsyncRelayCommandOptions options)
    {
        ArgumentNullException.ThrowIfNull(execute);

        this.execute = execute;
        this.options = options;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncRelayCommand"/> class.
    /// </summary>
    /// <param name="cancelableExecute">The cancelable execution logic.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="cancelableExecute"/> is <see langword="null"/>.</exception>
    public AsyncRelayCommand(Func<CancellationToken, Task> cancelableExecute)
    {
        ArgumentNullException.ThrowIfNull(cancelableExecute);

        this.cancelableExecute = cancelableExecute;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncRelayCommand"/> class.
    /// </summary>
    /// <param name="cancelableExecute">The cancelable execution logic.</param>
    /// <param name="options">The options to use to configure the async command.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="cancelableExecute"/> is <see langword="null"/>.</exception>
    public AsyncRelayCommand(Func<CancellationToken, Task> cancelableExecute, AsyncRelayCommandOptions options)
    {
        ArgumentNullException.ThrowIfNull(cancelableExecute);

        this.cancelableExecute = cancelableExecute;
        this.options = options;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncRelayCommand"/> class.
    /// </summary>
    /// <param name="execute">The execution logic.</param>
    /// <param name="canExecute">The execution status logic.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="execute"/> or <paramref name="canExecute"/> are <see langword="null"/>.</exception>
    public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute)
    {
        ArgumentNullException.ThrowIfNull(execute);
        ArgumentNullException.ThrowIfNull(canExecute);

        this.execute = execute;
        this.canExecute = canExecute;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncRelayCommand"/> class.
    /// </summary>
    /// <param name="execute">The execution logic.</param>
    /// <param name="canExecute">The execution status logic.</param>
    /// <param name="options">The options to use to configure the async command.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="execute"/> or <paramref name="canExecute"/> are <see langword="null"/>.</exception>
    public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute, AsyncRelayCommandOptions options)
    {
        ArgumentNullException.ThrowIfNull(execute);
        ArgumentNullException.ThrowIfNull(canExecute);

        this.execute = execute;
        this.canExecute = canExecute;
        this.options = options;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncRelayCommand"/> class.
    /// </summary>
    /// <param name="cancelableExecute">The cancelable execution logic.</param>
    /// <param name="canExecute">The execution status logic.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="cancelableExecute"/> or <paramref name="canExecute"/> are <see langword="null"/>.</exception>
    public AsyncRelayCommand(Func<CancellationToken, Task> cancelableExecute, Func<bool> canExecute)
    {
        ArgumentNullException.ThrowIfNull(cancelableExecute);
        ArgumentNullException.ThrowIfNull(canExecute);

        this.cancelableExecute = cancelableExecute;
        this.canExecute = canExecute;
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncRelayCommand"/> class.
    /// </summary>
    /// <param name="cancelableExecute">The cancelable execution logic.</param>
    /// <param name="canExecute">The execution status logic.</param>
    /// <param name="options">The options to use to configure the async command.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="cancelableExecute"/> or <paramref name="canExecute"/> are <see langword="null"/>.</exception>
    public AsyncRelayCommand(Func<CancellationToken, Task> cancelableExecute, Func<bool> canExecute, AsyncRelayCommandOptions options)
    {
        ArgumentNullException.ThrowIfNull(cancelableExecute);
        ArgumentNullException.ThrowIfNull(canExecute);

        this.cancelableExecute = cancelableExecute;
        this.canExecute = canExecute;
        this.options = options;
    }
    /// <summary>
    /// 执行任务属性，当任务执行时会通知更改
    /// </summary>
    private Task? executionTask;

    /// <inheritdoc/>
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

            PropertyChanged?.Invoke(this, ExecutionTaskChangedEventArgs);
            PropertyChanged?.Invoke(this, IsRunningChangedEventArgs);

            bool isAlreadyCompletedOrNull = value?.IsCompleted ?? true;

            if (this.cancellationTokenSource is not null)
            {
                PropertyChanged?.Invoke(this, CanBeCanceledChangedEventArgs);
                PropertyChanged?.Invoke(this, IsCancellationRequestedChangedEventArgs);
            }

            // The branch is on a condition evaluated before raising the events above if
            // needed, to avoid race conditions with a task completing right after them.
            if (isAlreadyCompletedOrNull)
            {
                return;
            }

            // 监控执行任务的完成状态，并在完成后更新属性
            static async void MonitorTask(AsyncRelayCommand @this, Task task)
            {
                await task.GetAwaitableWithoutEndValidation();

                if (ReferenceEquals(@this.executionTask, task))
                {
                    @this.PropertyChanged?.Invoke(@this, ExecutionTaskChangedEventArgs);
                    @this.PropertyChanged?.Invoke(@this, IsRunningChangedEventArgs);

                    if (@this.cancellationTokenSource is not null)
                    {
                        @this.PropertyChanged?.Invoke(@this, CanBeCanceledChangedEventArgs);
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

    /// <inheritdoc/>
    public bool CanBeCanceled => IsRunning && this.cancellationTokenSource is { IsCancellationRequested: false };

    /// <inheritdoc/>
    public bool IsCancellationRequested => this.cancellationTokenSource is { IsCancellationRequested: true };

    /// <inheritdoc/>
    public bool IsRunning => ExecutionTask is { IsCompleted: false };

    /// <inheritdoc/>
    bool ICancellationAwareCommand.IsCancellationSupported => this.execute is null;

    /// <summary>
    /// 引发 CanExecuteChanged 事件，通知命令执行状态可能已更改
    /// </summary>
    public void NotifyCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 确定此命令是否可以在其当前状态下执行
    /// </summary>
    /// <param name="parameter">命令使用的数据。如果不需要数据，则可以将此参数设置为 null。</param>
    /// <returns>如果可以执行命令，则为 true；否则为 false。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CanExecute(object? parameter)
    {
        bool canExecute = this.canExecute?.Invoke() != false;

        return canExecute && ((this.options & AsyncRelayCommandOptions.AllowConcurrentExecutions) != 0 || ExecutionTask is not { IsCompleted: false });
    }

    /// <summary>
    /// 执行命令逻辑
    /// </summary>
    /// <param name="parameter">命令使用的数据。如果不需要数据，则可以将此参数设置为 null。</param>
    public void Execute(object? parameter)
    {
        Task executionTask = ExecuteAsync(parameter);

        // 如果异常不应流向任务调度程序，则等待结果任务。这委托给一个
        // 单独的方法，以便在设置了选项的情况下使这个方法更简洁
        if ((this.options & AsyncRelayCommandOptions.FlowExceptionsToTaskScheduler) == 0)
        {
            AwaitAndThrowIfFailed(executionTask);
        }
    }

    /// <summary>
    /// 异步执行命令逻辑
    /// </summary>
    /// <param name="parameter">命令使用的数据。如果不需要数据，则可以将此参数设置为 null。</param>
    /// <returns>表示异步执行操作的任务</returns>
    public Task ExecuteAsync(object? parameter)
    {
        Task executionTask;

        if (this.execute is not null)
        {
            // 非可取消命令委托
            executionTask = ExecutionTask = this.execute();
        }
        else
        {
            // 取消之前的操作（如果有待处理的）
            this.cancellationTokenSource?.Cancel();

            CancellationTokenSource cancellationTokenSource = this.cancellationTokenSource = new();

            // 使用新的链接令牌调用可取消命令委托
            executionTask = ExecutionTask = this.cancelableExecute!(cancellationTokenSource.Token);
        }

        // 如果禁用了并发执行，则通知执行状态更改
        if ((this.options & AsyncRelayCommandOptions.AllowConcurrentExecutions) == 0)
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        return executionTask;
    }

    /// <summary>
    /// 取消当前正在运行的可取消任务
    /// </summary>
    public void Cancel()
    {
        if (this.cancellationTokenSource is CancellationTokenSource { IsCancellationRequested: false } cancellationTokenSource)
        {
            cancellationTokenSource.Cancel();

            PropertyChanged?.Invoke(this, CanBeCanceledChangedEventArgs);
            PropertyChanged?.Invoke(this, IsCancellationRequestedChangedEventArgs);
        }
    }

    /// <summary>
    /// 等待输入的 <see cref="Task"/>，如果任务失败则在调用上下文中抛出异常
    /// </summary>
    /// <param name="executionTask">要等待的输入 <see cref="Task"/> 实例</param>
    internal static async void AwaitAndThrowIfFailed(Task executionTask)
    {
        // 注意：这个方法是有意设计为一个异步 void 方法来等待输入任务。这样做是为了
        // 如果异步中继命令被同步调用（例如，当调用 Execute 时，例如从绑定中）,
        // 包装委托中的异常不会被忽略或仅通过 ExecutionTask 属性可见，而是会在
        // 原始同步上下文中重新抛出。这使得行为更符合正常命令的工作方式（异常
        // 也会被正常传播到调用者上下文），并防止应用程序在方法出错时进入不一致状态
        // 而没有其他组件被通知。也可以不等待此任务并忽略异常，然后从 ExecutionTask 属性
        // 手动检查它们，通过使用 AsyncRelayCommandOptions.FlowExceptionsToTaskScheduler 选项
        // 构造异步命令实例。这将导致跳过此调用，并且异常将要么正常通过该属性可用，要么
        // 流向静态 TaskScheduler.UnobservedTaskException 事件（如果未被观察，例如用于日志记录）。
        await executionTask;
    }
}