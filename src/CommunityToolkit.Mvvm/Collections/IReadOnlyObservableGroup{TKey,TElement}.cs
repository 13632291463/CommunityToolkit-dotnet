// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

namespace CommunityToolkit.Mvvm.Collections;

/// <summary>
/// 表示一个分组项集合的接口
/// </summary>
/// <typeparam name="TKey">分组键的类型</typeparam>
/// <typeparam name="TElement">分组中元素的类型</typeparam>
public interface IReadOnlyObservableGroup<out TKey, out TElement> : IReadOnlyObservableGroup<TKey>, IReadOnlyList<TElement>, IGrouping<TKey, TElement>
    where TKey : notnull
{
    /// <summary>
    /// 获取当前集合中指定索引处的元素
    /// </summary>
    /// <param name="index">要获取元素的从零开始的索引</param>
    /// <returns>只读列表中指定索引处的元素</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">如果索引超出范围则抛出此异常</exception>
    new TElement this[int index] { get; }
}