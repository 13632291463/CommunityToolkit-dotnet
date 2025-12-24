// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// An attribute that can be used to support <see cref="IRelayCommand"/> properties in generated properties. When this attribute is
/// used, the generated property setter will also call <see cref="IRelayCommand.NotifyCanExecuteChanged"/> for the properties specified
/// in the attribute data, causing the validation logic for the command to be executed again. This can be useful to keep the code compact
/// when there are one or more dependent commands that should also be notified when a property is updated. If this attribute is used on
/// a property without <see cref="ObservablePropertyAttribute"/>, it is ignored (just like <see cref="NotifyPropertyChangedForAttribute"/>).
/// <para>
/// In order to use this attribute, the target property has to implement the <see cref="IRelayCommand"/> interface.
/// </para>
/// <para>
/// This attribute can be used as follows:
/// <code>
/// partial class MyViewModel : ObservableObject
/// {
///     [ObservableProperty]
///     [NotifyCanExecuteChangedFor(nameof(GreetUserCommand))]
///     public partial string Name { get; set; }
///
///     public IRelayCommand GreetUserCommand { get; }
/// }
/// </code>
/// </para>
/// And with this, code analogous to this will be generated:
/// <code>
/// partial class MyViewModel
/// {
///     public partial string Name
///     {
///         get => field;
///         set
///         {
///             if (SetProperty(ref field, value))
///             {
///                 GreetUserCommand.NotifyCanExecuteChanged();
///             }
///         }
///     }
/// }
/// </code>
/// </summary>
/// <remarks>
/// Just like <see cref="ObservablePropertyAttribute"/>, this attribute can also be used on fields as well.
/// </remarks>
/// <summary>
/// 指定当使用此特性标记的属性更改时，还应通知哪些命令的CanExecute状态发生更改。
/// 此特性应用于属性或字段，以指示当该属性或字段更改时，指定的命令应更新其CanExecute状态。
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
public sealed class NotifyCanExecuteChangedForAttribute : Attribute
{
    /// <summary>
    /// 初始化 <see cref="NotifyCanExecuteChangedForAttribute"/> 类的新实例。
    /// </summary>
    /// <param name="commandName">当标记的属性更改时也要通知的命令名称。</param>
    public NotifyCanExecuteChangedForAttribute(string commandName)
    {
        CommandNames = new[] { commandName };
    }

    /// <summary>
    /// 初始化 <see cref="NotifyCanExecuteChangedForAttribute"/> 类的新实例。
    /// </summary>
    /// <param name="commandName">当标记的属性更改时也要通知的命令名称。</param>
    /// <param name="otherCommandNames">
    /// 当标记的属性更改时也要通知的其他命令名称。此参数可选择性地
    /// 用于从同一特性指示一系列相关命令，以使代码更紧凑。
    /// </param>
    public NotifyCanExecuteChangedForAttribute(string commandName, params string[] otherCommandNames)
    {
        CommandNames = new[] { commandName }.Concat(otherCommandNames).ToArray();
    }

    /// <summary>
    /// 获取当标记的属性更改时也要通知的命令名称。
    /// </summary>
    public string[] CommandNames { get; }
}
