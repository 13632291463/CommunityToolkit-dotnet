// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Input;

namespace CommunityToolkit.Mvvm.Input;

/// <summary>
/// 一个属性，可用于从声明的方法自动生成 <see cref="ICommand"/> 属性。当此属性
/// 用于装饰方法时，生成器将根据方法的签名创建具有相应 <see cref="IRelayCommand"/> 接口的命令属性。
/// 如果使用无效的方法签名，生成器将报告错误。
/// <para>
/// 为了使用此属性，包含类型无需实现任何接口。生成的属性将被延迟分配，但其值不会改变，
/// 因此无需支持属性更改通知或其他附加功能。
/// </para>
/// <para>
/// 此属性可以这样使用:
/// <code>
/// partial class MyViewModel
/// {
///     [RelayCommand]
///     private void GreetUser(User? user)
///     {
///         Console.WriteLine($"Hello {user.Name}!");
///     }
/// }
/// </code>
/// 使用此方式，将生成类似这样的代码:
/// <code>
/// partial class MyViewModel
/// {
///     private RelayCommand? greetUserCommand;
///
///     public IRelayCommand GreetUserCommand => greetUserCommand ??= new RelayCommand(GreetUser);
/// }
/// </code>
/// </para>
/// <para>
/// 下列签名支持被注解的方法:
/// <code>
/// void Method();
/// </code>
/// 将生成一个 <see cref="IRelayCommand"/> 属性 (使用 <see cref="RelayCommand"/> 实例)。
/// <code>
/// void Method(T?);
/// </code>
/// 将生成一个 <see cref="IRelayCommand{T}"/> 属性 (使用 <see cref="RelayCommand{T}"/> 实例)。
/// <code>
/// Task Method();
/// Task Method(CancellationToken);
/// Task&lt;T&gt; Method();
/// Task&lt;T&gt; Method(CancellationToken);
/// </code>
/// 都将生成一个 <see cref="IAsyncRelayCommand"/> 属性 (使用 <see cref="AsyncRelayCommand{T}"/> 实例)。
/// <code>
/// Task Method(T?);
/// Task Method(T?, CancellationToken);
/// Task&lt;T&gt; Method(T?);
/// Task&lt;T&gt; Method(T?, CancellationToken);
/// </code>
/// 都将生成一个 <see cref="IAsyncRelayCommand{T}"/> 属性 (使用 <see cref="AsyncRelayCommand{T}"/> 实例)。
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class RelayCommandAttribute : Attribute
{
    /// <summary>
    /// 获取或设置用于检查生成的命令在任何给定时间是否可以执行的属性或方法的名称。引用的成员需要返回
    /// 一个 <see cref="bool"/> 值，并且必须具有与目标命令兼容的签名。
    /// </summary>
    public string? CanExecute { get; init; }

    /// <summary>
    /// 获取或设置一个值，指示是否允许异步命令的并发执行。
    /// <para>
    /// 当设置用于将生成 <see cref="AsyncRelayCommand"/> 或 <see cref="AsyncRelayCommand{T}"/> 属性的方法时，
    /// 这将修改这些命令在仍运行的命令尚未完成时调用新执行时的行为。这与使用
    /// <see cref="AsyncRelayCommand(Func{System.Threading.Tasks.Task}, AsyncRelayCommandOptions)"/> 等构造函数创建命令类型实例
    /// 并使用 <see cref="AsyncRelayCommandOptions.AllowConcurrentExecutions"/> 值相同。
    /// </para>
    /// </summary>
    /// <remarks>如果目标命令不映射到异步命令，则不能使用此属性。</remarks>
    public bool AllowConcurrentExecutions { get; init; }

    /// <summary>
    /// 获取或设置一个值，指示异常是否应传播到 <see cref="System.Threading.Tasks.TaskScheduler.UnobservedTaskException"/>。
    /// <para>
    /// 当设置用于将生成 <see cref="AsyncRelayCommand"/> 或 <see cref="AsyncRelayCommand{T}"/> 属性的方法时，
    /// 这将修改这些命令在底层操作抛出异常时的行为。这与使用
    /// <see cref="AsyncRelayCommand(Func{System.Threading.Tasks.Task}, AsyncRelayCommandOptions)"/> 等构造函数创建命令类型实例
    /// 并使用 <see cref="AsyncRelayCommandOptions.FlowExceptionsToTaskScheduler"/> 值相同。
    /// </para>
    /// </summary>
    /// <remarks>如果目标命令不映射到异步命令，则不能使用此属性。</remarks>
    public bool FlowExceptionsToTaskScheduler { get; init; }

    /// <summary>
    /// 获取或设置一个值，指示是否还应为异步命令生成取消命令。
    /// <para>
    /// 当设置为 <see langword="true"/> 时，将生成以下附加代码:
    /// <code>
    /// partial class MyViewModel
    /// {
    ///     private ICommand? loginUserCancelCommand;
    ///
    ///     public ICommand LoginUserCancelCommand => loginUserCancelCommand ??= LoginUserCommand.CreateCancelCommand();
    /// }
    /// </code>
    /// 其中 <c>LoginUserCommand</c> 是在类中定义的(或由此属性生成的) <see cref="IAsyncRelayCommand"/>。
    /// </para>
    /// </summary>
    /// <remarks>如果目标命令不映射到可取消的异步命令，则不能使用此属性。</remarks>
    public bool IncludeCancelCommand { get; init; }
}