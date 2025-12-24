// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Mvvm.Collections;

/// <summary>
/// 表示一个分组项目的集合接口
/// </summary>
/// <typeparam name="TKey">分组键的类型</typeparam>
public interface IReadOnlyObservableGroup<out TKey> : IReadOnlyObservableGroup
    where TKey : notnull
{
    /// <summary>
    /// 获取当前集合的键
    /// </summary>
    /// <returns>当前分组的键值</returns>
    new TKey Key { get; }
}