// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Mvvm.ComponentModel.__Internals;

/// <summary>
/// 一个内部辅助类，用于支持与 <see cref="ObservableValidator"/> 相关的源生成器API。
/// 此类型不打算由用户代码直接使用。
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[Obsolete("This type is not intended to be used directly by user code")]
public static class __ObservableValidatorHelper
{
    /// <summary>
    /// 在目标实例上调用 <see cref="ObservableValidator.ValidateProperty(object?, string?)"/>。
    /// </summary>
    /// <param name="instance">目标 <see cref="ObservableValidator"/> 实例。</param>
    /// <param name="value">用于指定属性测试的值。</param>
    /// <param name="propertyName">要验证的属性名称。</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("This method is not intended to be called directly by user code")]
	/*
    [UnconditionalSuppressMessage(
        "ReflectionAnalysis",
        "IL2026:RequiresUnreferencedCode",
        Justification = "此辅助方法由公共API生成的代码调用，这些API已经具有适当的注释（我们不希望生成的代码产生开发人员无法修复的警告）。")]
    */
    [UnconditionalSuppressMessage(
        "ReflectionAnalysis",
        "IL2026:RequiresUnreferencedCode",
        Justification = "This helper is called by generated code from public APIs that have the proper annotations already (and we don't want generated code to produce warnings that developers cannot fix).")]

	public static void ValidateProperty(ObservableValidator instance, object? value, string propertyName)
    {
        instance.ValidateProperty(value, propertyName);
    }
}