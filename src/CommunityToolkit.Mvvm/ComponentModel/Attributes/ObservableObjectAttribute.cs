// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;

namespace CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// 一个属性，表示给定类型应具有来自 <see cref="ObservableObject"/> 的所有成员
/// 并生成到其中，以及 <see cref="INotifyPropertyChanged"/> 和 <see cref="INotifyPropertyChanging"/>
/// 接口。当您想在已从另一个类继承的类中获得与 <see cref="ObservableObject"/> 相同的功能时，这会很有用
/// （因为 C# 不支持多重继承）。此属性将触发源生成器
/// 将相同的 API 直接创建到装饰类中。
/// <para>
/// 此属性可以这样使用:
/// <code>
/// [ObservableObject]
/// partial class MyViewModel : SomeOtherClass
/// {
///     // 其他成员在这里...
/// }
/// </code>
/// </para>
/// 使用此方法后，<see cref="ObservableObject"/> 的相同 API 也可用于此类。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ObservableObjectAttribute : Attribute
{
}