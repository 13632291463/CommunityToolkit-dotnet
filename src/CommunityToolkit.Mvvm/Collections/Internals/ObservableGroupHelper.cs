// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;

namespace CommunityToolkit.Mvvm.Collections.Internals;

/// <summary>
/// ObservableGroup{TKey, TValue} 类型的辅助类
/// </summary>
internal static class ObservableGroupHelper
{
    /// <summary>
    /// 为 IReadOnlyObservableGroup.Key 属性缓存的 PropertyChangedEventArgs
    /// </summary>
    public static readonly PropertyChangedEventArgs KeyChangedEventArgs = new(nameof(IReadOnlyObservableGroup.Key));
}