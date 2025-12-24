// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;

namespace CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// An attribute that indicates that a given type should implement the <see cref="INotifyPropertyChanged"/> interface and
/// have minimal built-in functionality to support it. This includes exposing the necessary event and having two methods
/// to raise it that mirror <see cref="ObservableObject.OnPropertyChanged(PropertyChangedEventArgs)"/> and
/// <see cref="ObservableObject.OnPropertyChanged(string?)"/>. For more extensive support, use <see cref="ObservableObjectAttribute"/>.
/// <para>
/// This attribute can be used as follows:
/// <code>
/// [INotifyPropertyChanged]
/// partial class MyViewModel : SomeOtherClass
/// {
///     // Other members here...
/// }
/// </code>
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class INotifyPropertyChangedAttribute : Attribute
{
    /// <summary>
    /// 获取或设置一个值，该值指示是否还要生成在 <see cref="ObservableObject"/> 中找到的所有附加帮助方法
    /// （例如 <see cref="ObservableObject.SetProperty{T}(ref T, T, string?)"/>）。
    /// 如果设置为 <see langword="false"/>，则只生成 <see cref="INotifyPropertyChanged.PropertyChanged"/> 事件
    /// 和两个 <see cref="ObservableObject.OnPropertyChanged(PropertyChangedEventArgs)"/> 重载。
    /// 默认值为 <see langword="true"/>。
    /// </summary>
    public bool IncludeAdditionalHelperMethods { get; init; } = true;
}
