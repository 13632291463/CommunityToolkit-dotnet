// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace CommunityToolkit.Mvvm.Collections;

/// <summary>
/// 一个用于分组集合项的接口
/// </summary>
public interface IReadOnlyObservableGroup : INotifyPropertyChanged, INotifyCollectionChanged, IEnumerable
{
    /// <summary>
    /// 获取当前集合的键
    /// </summary>
    object Key { get; }

    /// <summary>
    /// 获取当前分组集合中的项目数
    /// </summary>
    int Count { get; }

    /// <summary>
    /// 获取当前集合中指定索引处的元素
    /// </summary>
    /// <param name="index">要获取的元素的基于零的索引</param>
    /// <returns>只读列表中指定索引处的元素</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">如果索引超出范围则抛出异常</exception>
    object? this[int index] { get; }
}