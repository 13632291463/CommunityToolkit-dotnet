// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CommunityToolkit.Mvvm.Collections;

/// <summary>
/// 一个可观察的分组集合，它是一个包含可观察组的列表，并实现了 ILookup 接口
/// </summary>
/// <typeparam name="TKey">分组键的类型</typeparam>
/// <typeparam name="TElement">集合中元素的类型</typeparam>
public sealed partial class ObservableGroupedCollection<TKey, TElement> : ObservableCollection<ObservableGroup<TKey, TElement>>, ILookup<TKey, TElement>
    where TKey : notnull
{
    /// <summary>
    /// 初始化 <see cref="ObservableGroupedCollection{TKey, TElement}"/> 类的新实例
    /// </summary>
    public ObservableGroupedCollection()
    {
    }

    /// <summary>
    /// 使用指定的分组集合初始化 <see cref="ObservableGroupedCollection{TKey, TElement}"/> 类的新实例
    /// </summary>
    /// <param name="collection">要添加到分组集合中的初始数据</param>
    /// <exception cref="System.ArgumentNullException">当 <paramref name="collection"/> 为 null 时抛出</exception>
    public ObservableGroupedCollection(IEnumerable<IGrouping<TKey, TElement>> collection)
        : base(collection?.Select(static group => new ObservableGroup<TKey, TElement>(group))!)
    {
    }

    /// <summary>
    /// 获取与指定键关联的元素集合
    /// </summary>
    /// <param name="key">用于查找元素的键</param>
    /// <returns>与指定键关联的元素集合；如果未找到键，则返回空集合</returns>
    IEnumerable<TElement> ILookup<TKey, TElement>.this[TKey key]
    {
        get
        {
            IEnumerable<TElement>? result = null;

            if (key is not null)
            {
                // 尝试通过键查找第一个匹配的组
                result = this.FirstGroupByKeyOrDefault(key);
            }

            // 如果未找到匹配的组，则返回空集合
            return result ?? Enumerable.Empty<TElement>();
        }
    }

    /// <summary>
    /// 尝试获取底层的 List 实例（如果存在）
    /// </summary>
    /// <param name="list">获取到的 List 实例（如果存在）</param>
    /// <returns>是否找到了 List 实例</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryGetList([NotNullWhen(true)] out List<ObservableGroup<TKey, TElement>>? list)
    {
        list = Items as List<ObservableGroup<TKey, TElement>>;

        return list is not null;
    }

    /// <summary>
    /// 确定集合是否包含指定的键
    /// </summary>
    /// <param name="key">要查找的键</param>
    /// <returns>如果集合包含指定的键，则为 true；否则为 false</returns>
    bool ILookup<TKey, TElement>.Contains(TKey key)
    {
        return key is not null && this.FirstGroupByKey(key) is not null;
    }

    /// <summary>
    /// 返回一个循环访问分组集合的枚举器
    /// </summary>
    /// <returns>用于循环访问分组集合的枚举器</returns>
    IEnumerator<IGrouping<TKey, TElement>> IEnumerable<IGrouping<TKey, TElement>>.GetEnumerator()
    {
        return GetEnumerator();
    }
}