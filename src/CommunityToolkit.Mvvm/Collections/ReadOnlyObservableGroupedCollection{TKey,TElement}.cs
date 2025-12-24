// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CommunityToolkit.Mvvm.Collections;

/// <summary>
/// 一个只读的分组列表。
/// </summary>
/// <typeparam name="TKey">分组键的类型。</typeparam>
/// <typeparam name="TElement">集合中元素的类型。</typeparam>
public sealed partial class ReadOnlyObservableGroupedCollection<TKey, TElement> : ReadOnlyObservableCollection<ReadOnlyObservableGroup<TKey, TElement>>, ILookup<TKey, TElement>
    where TKey : notnull
{
    /// <summary>
    /// 初始化 <see cref="ReadOnlyObservableGroupedCollection{TKey, TValue}"/> 类的新实例。
    /// </summary>
    /// <param name="collection">要包装的源集合。</param>
    /// <exception cref="ArgumentNullException">如果 <paramref name="collection"/> 为 <see langword="null"/> 则抛出异常。</exception>
    public ReadOnlyObservableGroupedCollection(ObservableCollection<ObservableGroup<TKey, TElement>> collection)
        : base(new ObservableCollection<ReadOnlyObservableGroup<TKey, TElement>>(collection?.Select(static g => new ReadOnlyObservableGroup<TKey, TElement>(g))!))
    {
        collection!.CollectionChanged += OnSourceCollectionChanged;
    }

    /// <summary>
    /// 初始化 <see cref="ReadOnlyObservableGroupedCollection{TKey, TValue}"/> 类的新实例。
    /// </summary>
    /// <param name="collection">要包装的源集合。</param>
    /// <exception cref="ArgumentNullException">如果 <paramref name="collection"/> 为 <see langword="null"/> 则抛出异常。</exception>
    public ReadOnlyObservableGroupedCollection(ObservableCollection<ReadOnlyObservableGroup<TKey, TElement>> collection)
        : base(collection)
    {
    }

    /// <summary>
    /// 获取具有指定键的分组中的元素。
    /// </summary>
    /// <param name="key">要查找的键。</param>
    /// <returns>与指定键匹配的元素集合；如果未找到则返回空集合。</returns>
    IEnumerable<TElement> ILookup<TKey, TElement>.this[TKey key]
    {
        get
        {
            IEnumerable<TElement>? result = null;

            if (key is not null)
            {
                result = FirstGroupByKeyOrDefault(key);
            }

            return result ?? Enumerable.Empty<TElement>();
        }
    }

    /// <summary>
    /// 确定是否包含具有指定键的分组。
    /// </summary>
    /// <param name="key">要查找的键。</param>
    /// <returns>如果包含具有指定键的分组则返回 true；否则返回 false。</returns>
    bool ILookup<TKey, TElement>.Contains(TKey key)
    {
        return key is not null && FirstGroupByKeyOrDefault(key) is not null;
    }

    /// <summary>
    /// 返回一个枚举器，用于遍历分组集合。
    /// </summary>
    /// <returns>分组集合的枚举器。</returns>
    IEnumerator<IGrouping<TKey, TElement>> IEnumerable<IGrouping<TKey, TElement>>.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// 当被包装的集合发生更改时，转发 <see cref="INotifyCollectionChanged.CollectionChanged"/> 事件。
    /// </summary>
    /// <param name="sender">被包装的集合（一个 <see cref="ReadOnlyObservableGroup{TKey, TValue}"/> 实例的 <see cref="ObservableCollection{T}"/>）。</param>
    /// <param name="e"><see cref="NotifyCollectionChangedEventArgs"/> 参数。</param>
    /// <exception cref="NotSupportedException">当请求范围操作时抛出。</exception>
    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // 即使 NotifyCollectionChangedEventArgs 允许多个项目，实际实现
        // 只是逐个报告更改。我们目前只考虑这种情况。如果在
        // 新版本的 .NET 中添加了此功能，则需要在新版本中更新此类型
        [DoesNotReturn]
        static void ThrowNotSupportedExceptionForRangeOperation()
        {
            //throw new NotSupportedException(
            //    "ReadOnlyObservableGroupedCollection<TKey, TValue> 不支持一次对多个项目进行操作。\n" +
            //    "如果抛出了此异常，可能意味着底层 ObservableCollection<T> 类型已添加了对批量项目更新的支持，而此实现尚未支持该功能。\n" +
            //    "请考虑在 https://aka.ms/toolkit/dotnet 中提交问题以报告此情况。");
            throw new NotSupportedException(
                "ReadOnlyObservableGroupedCollection<TKey, TValue> doesn't support operations on multiple items at once.\n" +
                "If this exception was thrown, it likely means support for batched item updates has been added to the " +
                "underlying ObservableCollection<T> type, and this implementation doesn't support that feature yet.\n" +
                "Please consider opening an issue in https://aka.ms/toolkit/dotnet to report this.");
        }

        // 内部 Items 列表是 ObservableCollection<ReadOnlyObservableGroup<TKey, TValue>>，所以直接转换总是成功的
        ObservableCollection<ReadOnlyObservableGroup<TKey, TElement>> items = (ObservableCollection<ReadOnlyObservableGroup<TKey, TElement>>)Items;

        switch (e.Action)
        {
            // 为"添加"操作插入单个项目，如果添加多个项目则失败
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems!.Count == 1)
                {
                    ObservableGroup<TKey, TElement> newItem = (ObservableGroup<TKey, TElement>)e.NewItems![0]!;

                    items.Insert(e.NewStartingIndex, new ReadOnlyObservableGroup<TKey, TElement>(newItem));
                }
                else if (e.NewItems!.Count > 1)
                {
                    ThrowNotSupportedExceptionForRangeOperation();
                }

                break;

            // 为"移除"操作在指定偏移处移除单个项目，如果移除多个项目则失败
            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems!.Count == 1)
                {
                    items.RemoveAt(e.OldStartingIndex);
                }
                else if (e.OldItems!.Count > 1)
                {
                    ThrowNotSupportedExceptionForRangeOperation();
                }

                break;

            // 为"替换"操作在指定偏移处替换单个项目，如果替换多个项目则失败
            case NotifyCollectionChangedAction.Replace:
                if (e.OldItems!.Count == 1 && e.NewItems!.Count == 1)
                {
                    ObservableGroup<TKey, TElement> replacedItem = (ObservableGroup<TKey, TElement>)e.NewItems![0]!;

                    items[e.OldStartingIndex] = new ReadOnlyObservableGroup<TKey, TElement>(replacedItem);
                }
                else if (e.OldItems!.Count > 1 || e.NewItems!.Count > 1)
                {
                    ThrowNotSupportedExceptionForRangeOperation();
                }

                break;

            // 为"移动"操作在偏移之间移动单个项目，如果移动多个项目则失败
            case NotifyCollectionChangedAction.Move:
                if (e.OldItems!.Count == 1 && e.NewItems!.Count == 1)
                {
                    items.Move(e.OldStartingIndex, e.NewStartingIndex);
                }
                else if (e.OldItems!.Count > 1 || e.NewItems!.Count > 1)
                {
                    ThrowNotSupportedExceptionForRangeOperation();
                }

                break;

            // "重置"操作只是正常转发
            case NotifyCollectionChangedAction.Reset:
                items.Clear();
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 返回第一个具有 <paramref name="key"/> 键的分组，如果未找到则返回 <see langword="null"/>。
    /// </summary>
    /// <param name="key">要查询的分组的键（假定不为 <see langword="null"/>）。</param>
    /// <returns>与 <paramref name="key"/> 匹配的第一个分组。</returns>
    private IEnumerable<TElement>? FirstGroupByKeyOrDefault(TKey key)
    {
        if (Items is List<ReadOnlyObservableGroup<TKey, TElement>> list)
        {
            foreach (ReadOnlyObservableGroup<TKey, TElement> group in list)
            {
                if (EqualityComparer<TKey>.Default.Equals(group.Key, key))
                {
                    return group;
                }
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static IEnumerable<TElement>? FirstGroupByKeyOrDefaultFallback(ReadOnlyObservableGroupedCollection<TKey, TElement> source, TKey key)
        {
            return Enumerable.FirstOrDefault<ReadOnlyObservableGroup<TKey, TElement>>(source, group => EqualityComparer<TKey>.Default.Equals(group.Key, key));
        }

        return FirstGroupByKeyOrDefaultFallback(this, key);
    }
}