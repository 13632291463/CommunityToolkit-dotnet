// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// An attribute that can be used to support <see cref="ObservablePropertyAttribute"/> in generated properties, when applied to fields and properties
/// contained in a type that is either inheriting from <see cref="ObservableRecipient"/>, or annotated with <see cref="ObservableRecipientAttribute"/>.
/// When this attribute is used, the generated property setter will also call <see cref="ObservableRecipient.Broadcast{T}(T, T, string?)"/>.
/// This allows generated properties to opt-in into broadcasting behavior without having to fallback into a full explicit observable property.
/// <para>
/// This attribute can be used as follows:
/// <code>
/// partial class MyViewModel : ObservableRecipient
/// {
///     [ObservableProperty]
///     [NotifyPropertyChangedRecipients]
///     public partial string Username;
/// }
/// </code>
/// </para>
/// <para>
/// And with this, code analogous to this will be generated:
/// <code>
/// partial class MyViewModel
/// {
///     public partial string Username
///     {
///         get => field;
///         set => SetProperty(ref field, value, broadcast: true);
///     }
/// }
/// </code>
/// </para>
/// <para>
/// This attribute can also be added to a class, and if so it will affect all generated properties in that type and inherited types.
/// </para>
/// </summary>
/// <remarks>
/// Just like <see cref="ObservablePropertyAttribute"/>, this attribute can also be used on fields as well.
/// </remarks>
/// <summary>
/// 一个可用于支持 <see cref="ObservablePropertyAttribute"/> 在生成的属性中的属性，当应用于继承自 <see cref="ObservableRecipient"/> 或使用 <see cref="ObservableRecipientAttribute"/> 注释的类型中的字段和属性时。
/// 当使用此属性时，生成的属性 setter 还将调用 <see cref="ObservableRecipient.Broadcast{T}(T, T, string?)"/>。
/// 这允许生成的属性选择加入广播行为，而无需回退到完整的显式可观察属性。
/// </summary>
/// <remarks>
/// 与 <see cref="ObservablePropertyAttribute"/> 一样，此属性也可以用于字段。
/// 此属性也可以添加到类上，如果是这样，它将影响该类型和继承类型中的所有生成的属性。
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class NotifyPropertyChangedRecipientsAttribute : Attribute
{
}