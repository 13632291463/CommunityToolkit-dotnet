// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CommunityToolkit.Mvvm.Collections;

/// <summary>
/// 用于简化 <see cref="ObservableGroupedCollection{TKey, TElement}"/> 使用的扩展方法
/// </summary>
public static class ObservableGroupedCollectionExtensions
{
    /// <summary>
    /// 返回具有指定键的第一个组
    /// </summary>
    /// <typeparam name="TKey">组键的类型</typeparam>
    /// <typeparam name="TElement">集合中项目的类型</typeparam>
    /// <param name="source">源 <see cref="ObservableGroupedCollection{TKey, TElement}"/> 实例</param>
    /// <param name="key">要查询的组的键</param>
    /// <returns>与 <paramref name="key"/> 匹配的第一个组</returns>
    /// <exception cref="ArgumentNullException">如果 <paramref name="source"/> 或 <paramref name="key"/> 为 <see langword="null"/> 则抛出异常</exception>
    /// <exception cref="InvalidOperationException">目标组不存在</exception>
    public static ObservableGroup<TKey, TElement> FirstGroupByKey<TKey, TElement>(this ObservableGroupedCollection<TKey, TElement> source, TKey key)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.For<TKey>.ThrowIfNull(key);

        ObservableGroup<TKey, TElement>? group = source.FirstGroupByKeyOrDefault(key);

        if (group is null)
        {
            [DoesNotReturn]
            static void ThrowArgumentExceptionForKeyNotFound()
            {
                throw new InvalidOperationException("The requested key was not present in the collection.");
            }

            ThrowArgumentExceptionForKeyNotFound();
        }

        return group;
    }

    /// <summary>
    /// 返回具有指定键的第一个组，如果未找到则返回 <see langword="null"/>
    /// </summary>
    /// <typeparam name="TKey">组键的类型</typeparam>
    /// <typeparam name="TElement">集合中项目的类型</typeparam>
    /// <param name="source">源 <see cref="ObservableGroupedCollection{TKey, TElement}"/> 实例</param>
    /// <param name="key">要查询的组的键</param>
    /// <returns>与 <paramref name="key"/> 匹配的第一个组或 <see langword="null"/></returns>
    /// <exception cref="ArgumentNullException">如果 <paramref name="source"/> 或 <paramref name="key"/> 为 <see langword="null"/> 则抛出异常</exception>
    public static ObservableGroup<TKey, TElement>? FirstGroupByKeyOrDefault<TKey, TElement>(this ObservableGroupedCollection<TKey, TElement> source, TKey key)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.For<TKey>.ThrowIfNull(key);

        // 这个模式在此文件中广泛使用，许多公共 API 都有一个先循环检索列表，然后是回退循环，
        // 有时具有相同逻辑，但针对集合本身。这样做是为了优化：如果列表可用，我们可以直接迭代它，
        // 这将使用 List<T>.Enumerator 并避免分配（枚举器是结构体）、额外的间接（枚举器包装列表而不是
        // 外部集合）、以及额外开销（使用值枚举器避免接口存根调度）。
        // 因此，以下重复逻辑是有意的，并非实际重复，因为它会导致不同的代码结果。
        if (source.TryGetList(out List<ObservableGroup<TKey, TElement>>? list))
        {
            foreach (ObservableGroup<TKey, TElement> group in list)
            {
                if (EqualityComparer<TKey>.Default.Equals(group.Key, key))
                {
                    return group;
                }
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ObservableGroup<TKey, TElement>? FirstGroupByKeyOrDefaultFallback(ObservableGroupedCollection<TKey, TElement> source, TKey key)
        {
            return Enumerable.FirstOrDefault<ObservableGroup<TKey, TElement>>(source, group => EqualityComparer<TKey>.Default.Equals(group.Key, key));
        }

        return FirstGroupByKeyOrDefaultFallback(source, key);
    }

    /// <summary>
    /// 将键值 <see cref="ObservableGroup{TKey, TElement}"/> 项添加到目标 <see cref="ObservableGroupedCollection{TKey, TElement}"/>
    /// </summary>
    /// <typeparam name="TKey">组键的类型</typeparam>
    /// <typeparam name="TElement">集合中项目的类型</typeparam>
    /// <param name="source">源 <see cref="ObservableGroupedCollection{TKey, TElement}"/> 实例</param>
    /// <param name="key">要添加的组的键</param>
    /// <returns>添加的 <see cref="ObservableGroup{TKey, TValue}"/></returns>
    /// <exception cref="ArgumentNullException">如果 <paramref name="source"/> 或 <paramref name="key"/> 为 <see langword="null"/> 则抛出异常</exception>
    public static ObservableGroup<TKey, TElement> AddGroup<TKey, TElement>(this ObservableGroupedCollection<TKey, TElement> source, TKey key)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.For<TKey>.ThrowIfNull(key);

        ObservableGroup<TKey, TElement> group = new(key);

        source.Add(group);

        return group;
    }

    /// <summary>
    /// 将键集合 <see cref="ObservableGroup{TKey, TElement}"/> 项添加到目标 <see cref="ObservableGroupedCollection{TKey, TElement}"/>
    /// </summary>
    /// <typeparam name="TKey">组键的类型</typeparam>
    /// <typeparam name="TElement">集合中项目的类型</typeparam>
    /// <param name="source">源 <see cref="ObservableGroupedCollection{TKey, TElement}"/> 实例</param>
    /// <param name="grouping">要添加的项目组</param>
    /// <returns>添加的 <see cref="ObservableGroup{TKey, TValue}"/></returns>
    /// <exception cref="ArgumentNullException">如果 <paramref name="source"/> 或 <paramref name="grouping"/> 为 <see langword="null"/> 则抛出异常</exception>
    public static ObservableGroup<TKey, TElement> AddGroup<TKey, TElement>(this ObservableGroupedCollection<TKey, TElement> source, IGrouping<TKey, TElement> grouping)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(grouping);

        ObservableGroup<TKey, TElement> group = new(grouping);

        source.Add(group);

        return group;
    }

    /// <summary>
    /// 将键集合 <see cref="ObservableGroup{TKey, TElement}"/> 项添加到目标 <see cref="ObservableGroupedCollection{TKey, TElement}"/>
    /// </summary>
    /// <typeparam name="TKey">组键的类型</typeparam>
    /// <typeparam name="TElement">集合中项目的类型</typeparam>
    /// <param name="source">源 <see cref="ObservableGroupedCollection{TKey, TElement}"/> 实例</param>
    /// <param name="key">将添加 <paramref name="collection"/> 的组的键</param>
    /// <param name="collection">要添加的集合</param>
    /// <returns>添加的 <see cref="ObservableGroup{TKey, TElement}"/></returns>
    /// <exception cref="ArgumentNullException">如果 <paramref name="source"/>、<paramref name="key"/> 或 <paramref name="collection"/> 为 <see langword="null"/> 则抛出异常</exception>
    public static ObservableGroup<TKey, TElement> AddGroup<TKey, TElement>(this ObservableGroupedCollection<TKey, TElement> source, TKey key, IEnumerable<TElement> collection)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.For<TKey>.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(collection);

        ObservableGroup<TKey, TElement> group = new(key, collection);

        source.Add(group);

        return group;
    }

    /// <summary>
    /// 将键值 <see cref="ObservableGroup{TKey, TElement}"/> 项插入到目标 <see cref="ObservableGroupedCollection{TKey, TElement}"/>
    /// </summary>
    /// <typeparam name="TKey">组键的类型</typeparam>
    /// <typeparam name="TElement">集合中项目的类型</typeparam>
    /// <param name="source">源 <see cref="ObservableGroupedCollection{TKey, TElement}"/> 实例</param>
    /// <param name="key">要添加的组的键</param>
    /// <returns>添加的 <see cref="ObservableGroup{TKey, TValue}"/></returns>
    /// <exception cref="ArgumentNullException">如果 <paramref name="source"/> 或 <paramref name="key"/> 为 <see langword="null"/> 则抛出异常</exception>
    public static ObservableGroup<TKey, TElement> InsertGroup<TKey, TElement>(this ObservableGroupedCollection<TKey, TElement> source, TKey key)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.For<TKey>.ThrowIfNull(key);

        if (source.TryGetList(out List<ObservableGroup<TKey, TElement>>? list))
        {
            int index = 0;

            foreach (ObservableGroup<TKey, TElement> group in list)
            {
                // 比较键值，找到合适的插入位置
                if (Comparer<TKey>.Default.Compare(key, group.Key) < 0)
                {
                    break;
                }

                index++;
            }

            ObservableGroup<TKey, TElement> newGroup = new(key);

            source.Insert(index, newGroup);

            return newGroup;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ObservableGroup<TKey, TElement> InsertGroupFallback(ObservableGroupedCollection<TKey, TElement> source, TKey key)
        {
            int index = 0;

            foreach (ObservableGroup<TKey, TElement> group in source)
            {
                // 比较键值，找到合适的插入位置
                if (Comparer<TKey>.Default.Compare(key, group.Key) < 0)
                {
                    break;
                }

                index++;
            }

            ObservableGroup<TKey, TElement> newGroup = new(key);

            source.Insert(index, newGroup);

            return newGroup;
        }

        return InsertGroupFallback(source, key);
    }

    /// <summary>
    /// 将键值 <see cref="ObservableGroup{TKey, TElement}"/> 项插入到目标 <see cref="ObservableGroupedCollection{TKey, TElement}"/>
    /// </summary>
    /// <typeparam name="TKey">组键的类型</typeparam>
    /// <typeparam name="TElement">集合中项目的类型</typeparam>
    /// <param name="source">源 <see cref="ObservableGroupedCollection{TKey, TElement}"/> 实例</param>
    /// <param name="grouping">要添加的项目组</param>
    /// <returns>添加的 <see cref="ObservableGroup{TKey, TValue}"/></returns>
    /// <exception cref="ArgumentNullException">如果 <paramref name="source"/> 或 <paramref name="grouping"/> 为 <see langword="null"/> 则抛出异常</exception>
    public static ObservableGroup<TKey, TElement> InsertGroup<TKey, TElement>(this ObservableGroupedCollection<TKey, TElement> source, IGrouping<TKey, TElement> grouping)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(grouping);

        if (source.TryGetList(out List<ObservableGroup<TKey, TElement>>? list))
        {
            int index = 0;

            foreach (ObservableGroup<TKey, TElement> group in list)
            {
                // 比较键值，找到合适的插入位置
                if (Comparer<TKey>.Default.Compare(grouping.Key, group.Key) < 0)
                {
                    break;
                }

                index++;
            }

            ObservableGroup<TKey, TElement> newGroup = new(grouping);

            source.Insert(index, newGroup);

            return newGroup;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ObservableGroup<TKey, TElement> InsertGroupFallback(ObservableGroupedCollection<TKey, TElement> source, IGrouping<TKey, TElement> grouping)
        {
            int index = 0;

            foreach (ObservableGroup<TKey, TElement> group in source)
            {
                // 比较键值，找到合适的插入位置
                if (Comparer<TKey>.Default.Compare(grouping.Key, group.Key) < 0)
                {
                    break;
                }

                index++;
            }

            ObservableGroup<TKey, TElement> newGroup = new(grouping);

            source.Insert(index, newGroup);

            return newGroup;
        }

        return InsertGroupFallback(source, grouping);
    }

    /// <summary>
    /// 将键值 <see cref="ObservableGroup{TKey, TElement}"/> 项插入到目标 <see cref="ObservableGroupedCollection{TKey, TElement}"/>
    /// </summary>
    /// <typeparam name="TKey">组键的类型</typeparam>
    /// <typeparam name="TElement">集合中项目的类型</typeparam>
    /// <param name="source">源 <see cref="ObservableGroupedCollection{TKey, TElement}"/> 实例</param>
    /// <param name="key">将添加 <paramref name="collection"/> 的组的键</param>
    /// <param name="collection">要添加的集合</param>
    /// <returns>添加的 <see cref="ObservableGroup{TKey, TValue}"/></returns>
    /// <exception cref="ArgumentNullException">如果 <paramref name="source"/>、<paramref name="key"/> 或 <paramref name="collection"/> 为 <see langword="null"/> 则抛出异常</exception>
    public static ObservableGroup<TKey, TElement> InsertGroup<TKey, TElement>(this ObservableGroupedCollection<TKey, TElement> source, TKey key, IEnumerable<TElement> collection)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.For<TKey>.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(collection);

        if (source.TryGetList(out List<ObservableGroup<TKey, TElement>>? list))
        {
            int index = 0;

            foreach (ObservableGroup<TKey, TElement> group in list)
            {
                // 比较键值，找到合适的插入位置
                if (Comparer<TKey>.Default.Compare(key, group.Key) < 0)
                {
                    break;
                }

                index++;
            }

            ObservableGroup<TKey, TElement> newGroup = new(key, collection);

            source.Insert(index, newGroup);

            return newGroup;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ObservableGroup<TKey, TElement> InsertGroupFallback(ObservableGroupedCollection<TKey, TElement> source, TKey key, IEnumerable<TElement> collection)
        {
            int index = 0;

            foreach (ObservableGroup<TKey, TElement> group in source)
            {
                // 比较键值，找到合适的插入位置
                if (Comparer<TKey>.Default.Compare(key, group.Key) < 0)
                {
                    break;
                }

                index++;
            }

            ObservableGroup<TKey, TElement> newGroup = new(key, collection);

            source.Insert(index, newGroup);

            return newGroup;
        }

        return InsertGroupFallback(source, key, collection);
    }

    /// <summary>
    /// 将键值 <see cref="ObservableGroup{TKey, TElement}"/> 项插入到目标 <see cref="ObservableGroupedCollection{TKey, TElement}"/>
    /// </summary>
    /// <typeparam name="TKey">组键的类型</typeparam>
    /// <typeparam name="TElement">集合中项目的类型</typeparam>
    /// <param name="source">源 <see cref="ObservableGroupedCollection{TKey, TElement}"/> 实例</param>
    /// <param name="key">要添加的组的键</param>
    /// <param name="comparer">用于在正确位置插入 <typeparamref name="TKey"/> 的 <see cref="IComparer{T}"/> 实例</param>
    /// <returns>添加的 <see cref="ObservableGroup{TKey, TValue}"/></returns>
    /// <exception cref="ArgumentNullException">如果 <paramref name="source"/>、<paramref name="key"/> 或 <paramref name="comparer"/> 为 <see langword="null"/> 则抛出异常</exception>
    public static ObservableGroup<TKey, TElement> InsertGroup<TKey, TElement>(this ObservableGroupedCollection<TKey, TElement> source, TKey key, IComparer<TKey> comparer)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.For<TKey>.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(comparer);

        if (source.TryGetList(out List<ObservableGroup<TKey, TElement>>? list))
        {
            int index = 0;

            foreach (ObservableGroup<TKey, TElement> group in list)
            {
                // 使用指定比较器比较键值，找到合适的插入位置
                if (comparer.Compare(key, group.Key) < 0)
                {
                    break;
                }

                index++;
            }

            ObservableGroup<TKey, TElement> newGroup = new(key);

            source.Insert(index, newGroup);

            return newGroup;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ObservableGroup<TKey, TElement> InsertGroupFallback(ObservableGroupedCollection<TKey, TElement> source, TKey key, IComparer<TKey> comparer)
        {
            int index = 0;

            foreach (ObservableGroup<TKey, TElement> group in source)
            {
                // 使用指定比较器比较键值，找到合适的插入位置
                if (comparer.Compare(key, group.Key) < 0)
                {
                    break;
                }

                index++;
            }

            ObservableGroup<TKey, TElement> newGroup = new(key);

            source.Insert(index, newGroup);

            return newGroup;
        }

        return InsertGroupFallback(source, key, comparer);
    }

    /// <summary>
    /// 将键值 <see cref="ObservableGroup{TKey, TElement}"/> 项插入到目标 <see cref="ObservableGroupedCollection{TKey, TElement}"/>
    /// </summary>
    /// <typeparam name="TKey">组键的类型</typeparam>
    /// <typeparam name="TElement">集合中项目的类型</typeparam>
    /// <param name="source">源 <see cref="ObservableGroupedCollection{TKey, TElement}"/> 实例</param>
    /// <param name="grouping">要添加的项目组</param>
    /// <param name="comparer">用于在正确位置插入 <typeparamref name="TKey"/> 的 <see cref="IComparer{T}"/> 实例</param>
    /// <returns>添加的 <see cref="ObservableGroup{TKey, TValue}"/></returns>
    /// <exception cref="ArgumentNullException">如果 <paramref name="source"/>、<paramref name="grouping"/> 或 <paramref name="comparer"/> 为 <see langword="null"/> 则抛出异常</exception>
    public static ObservableGroup<TKey, TElement> InsertGroup<TKey, TElement>(this ObservableGroupedCollection<TKey, TElement> source, IGrouping<TKey, TElement> grouping, IComparer<TKey> comparer)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(grouping);
        ArgumentNullException.ThrowIfNull(comparer);

        if (source.TryGetList(out List<ObservableGroup<TKey, TElement>>? list))
        {
            int index = 0;

            foreach (ObservableGroup<TKey, TElement> group in list)
            {
                // 使用指定比较器比较键值，找到合适的插入位置
                if (comparer.Compare(grouping.Key, group.Key) < 0)
                {
                    break;
                }

                index++;
            }

            ObservableGroup<TKey, TElement> newGroup = new(grouping);

            source.Insert(index, newGroup);

            return newGroup;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ObservableGroup<TKey, TElement> InsertGroupFallback(ObservableGroupedCollection<TKey, TElement> source, IGrouping<TKey, TElement> grouping, IComparer<TKey> comparer)
        {
            int index = 0;

            foreach (ObservableGroup<TKey, TElement> group in source)
            {
                // 使用指定比较器比较键值，找到合适的插入位置
                if (comparer.Compare(grouping.Key, group.Key) < 0)
                {
                    break;
                }

                index++;
            }

            ObservableGroup<TKey, TElement> newGroup = new(grouping);

            source.Insert(index, newGroup);

            return newGroup;
        }

        return InsertGroupFallback(source, grouping, comparer);
    }

    /// <summary>
    /// 将键值 <see cref="ObservableGroup{TKey, TElement}"/> 项插入到目标 <see cref="ObservableGroupedCollection{TKey, TElement}"/>
    /// </summary>
    /// <typeparam name="TKey">组键的类型</typeparam>
    /// <typeparam name="TElement">集合中项目的类型</typeparam>
    /// <param name="source">源 <see cref="ObservableGroupedCollection{TKey, TElement}"/> 实例</param>
    /// <param name="key">将添加 <paramref name="collection"/> 的组的键</param>
    /// <param name="comparer">用于在正确位置插入 <typeparamref name="TKey"/> 的 <see cref="IComparer{T}"/> 实例</param>
    /// <param name="collection">要添加的集合</param>
    /// <returns>添加的 <see cref="ObservableGroup{TKey, TValue}"/></returns>
    /// <exception cref="ArgumentNullException">如果 <paramref name="source"/>、<paramref name="key"/>、<paramref name="comparer"/> 或 <paramref name="collection"/> 为 <see langword="null"/> 则抛出异常</exception>
    public static ObservableGroup<TKey, TElement> InsertGroup<TKey, TElement>(this ObservableGroupedCollection<TKey, TElement> source, TKey key, IComparer<TKey> comparer, IEnumerable<TElement> collection)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.For<TKey>.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(comparer);
        ArgumentNullException.ThrowIfNull(collection);

        if (source.TryGetList(out List<ObservableGroup<TKey, TElement>>? list))
        {
            int index = 0;

            foreach (ObservableGroup<TKey, TElement> group in list)
            {
                // 使用指定比较器比较键值，找到合适的插入位置
                if (comparer.Compare(key, group.Key) < 0)
                {
                    break;
                }

                index++;
            }

            ObservableGroup<TKey, TElement> newGroup = new(key, collection);

            source.Insert(index, newGroup);

            return newGroup;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ObservableGroup<TKey, TElement> InsertGroupFallback(ObservableGroupedCollection<TKey, TElement> source, TKey key, IComparer<TKey> comparer, IEnumerable<TElement> collection)
        {
            int index = 0;

            foreach (ObservableGroup<TKey, TElement> group in source)
            {
                // 使用指定比较器比较键值，找到合适的插入位置
                if (comparer.Compare(key, group.Key) < 0)
                {
                    break;
                }

                index++;
            }

            ObservableGroup<TKey, TElement> newGroup = new(key, collection);

            source.Insert(index, newGroup);

            return newGroup;
        }

        return InsertGroupFallback(source, key, comparer, collection);
    }

    /// <summary>
    /// 将 <paramref name="item"/> 添加到具有 <paramref name="key"/> 键的第一个组中
    /// 如果组不存在，则将添加该组
    /// </summary>
    /// <typeparam name="TKey">组键的类型</typeparam>
    /// <typeparam name="TElement">集合中项目的类型</typeparam>
    /// <param name="source">源 <see cref="ObservableGroupedCollection{TKey, TElement}"/> 实例</param>
    /// <param name="key">应将 <paramref name="item"/> 添加到的组的键</param>
    /// <param name="item">要添加的项目</param>
    /// <returns><see cref="ObservableGroup{TKey, TElement}"/> 的实例，它将接收值。它将是现有组或新组</returns>
    /// <exception cref="ArgumentNullException">如果 <paramref name="source"/> 或 <paramref name="key"/> 为 <see langword="null"/> 则抛出异常</exception>
    public static ObservableGroup<TKey, TElement> AddItem<TKey, TElement>(this ObservableGroupedCollection<TKey, TElement> source, TKey key, TElement item)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.For<TKey>.ThrowIfNull(key);

        ObservableGroup<TKey, TElement>? group = source.FirstGroupByKeyOrDefault(key);

        if (group is null)
        {
            group = new ObservableGroup<TKey, TElement>(key) { item };

            source.Add(group);
        }
        else
        {
            group.Add(item);
        }

        return group;
    }

    /// <summary>
    /// 将 <paramref name="item"/> 插入到具有 <paramref name="key"/> 键的第一个组中
    /// </summary>
    /// <typeparam name="TKey">组键的类型</typeparam>
    /// <typeparam name="TElement">集合中项目的类型</typeparam>
    /// <param name="source">源 <see cref="ObservableGroupedCollection{TKey, TElement}"/> 实例</param>
    /// <param name="key">要插入 <paramref name="item"/> 的组的键</param>
    /// <param name="item">要添加的项目</param>
    /// <returns>将接收值的 <see cref="ObservableGroup{TKey, TElement}"/> 的实例</returns>
    /// <exception cref="ArgumentNullException">如果 <paramref name="source"/> 或 <paramref name="key"/> 为 <see langword="null"/> 则抛出异常</exception>
    public static ObservableGroup<TKey, TElement> InsertItem<TKey, TElement>(this ObservableGroupedCollection<TKey, TElement> source, TKey key, TElement item)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.For<TKey>.ThrowIfNull(key);

        ObservableGroup<TKey, TElement>? group = source.FirstGroupByKeyOrDefault(key);

        if (group is null)
        {
            group = source.InsertGroup(key, new[] { item });
        }
        else if (group.TryGetList(out List<TElement>? list))
        {
            int index = 0;

            foreach (TElement element in list)
            {
                // 比较元素，找到合适的插入位置
                if (Comparer<TElement>.Default.Compare(item, element) < 0)
                {
                    break;
                }

                index++;
            }

            group.Insert(index, item);
        }
        else
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            static void InsertItemFallback(ObservableCollection<TElement> source, TElement item)
            {
                int index = 0;

                foreach (TElement element in source)
                {
                    // 比较元素，找到合适的插入位置
                    if (Comparer<TElement>.Default.Compare(item, element) < 0)
                    {
                        break;
                    }

                    index++;
                }

                source.Insert(index, item);
            }

            InsertItemFallback(group, item);
        }

        return group;
    }

    /// <summary>
    /// 将 <paramref name="item"/> 插入到具有 <paramref name="key"/> 键的第一个组中
    /// </summary>
    /// <typeparam name="TKey">组键的类型</typeparam>
    /// <typeparam name="TElement">集合中项目的类型</typeparam>
    /// <param name="source">源 <see cref="ObservableGroupedCollection{TKey, TElement}"/> 实例</param>
    /// <param name="key">要插入 <paramref name="item"/> 的组的键</param>
    /// <param name="keyComparer">用于比较键的 <see cref="IComparer{T}"/> 实例</param>
    /// <param name="item">要添加的项目</param>
    /// <param name="itemComparer">用于比较元素的 <see cref="IComparer{T}"/> 实例</param>
    /// <returns>将接收值的 <see cref="ObservableGroup{TKey, TElement}"/> 的实例</returns>
    /// <exception cref="ArgumentNullException">如果 <paramref name="source"/>、<paramref name="key"/>、<paramref name="keyComparer"/> 或 <paramref name="itemComparer"/> 为 <see langword="null"/> 则抛出异常</exception>
    public static ObservableGroup<TKey, TElement> InsertItem<TKey, TElement>(
        this ObservableGroupedCollection<TKey, TElement> source,
        TKey key,
        IComparer<TKey> keyComparer,
        TElement item,
        IComparer<TElement> itemComparer)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.For<TKey>.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(keyComparer);
        ArgumentNullException.ThrowIfNull(itemComparer);

        ObservableGroup<TKey, TElement>? group = source.FirstGroupByKeyOrDefault(key);

        if (group is null)
        {
            group = source.InsertGroup(key, keyComparer, new[] { item });
        }
        else if (group.TryGetList(out List<TElement>? list))
        {
            int index = 0;

            foreach (TElement element in list)
            {
                // 使用指定比较器比较元素，找到合适的插入位置
                if (itemComparer.Compare(item, element) < 0)
                {
                    break;
                }

                index++;
            }

            group.Insert(index, item);
        }
        else
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            static void InsertItemFallback(ObservableCollection<TElement> source, TElement item, IComparer<TElement> comparer)
            {
                int index = 0;

                foreach (TElement element in source)
                {
                    // 使用指定比较器比较元素，找到合适的插入位置
                    if (comparer.Compare(item, element) < 0)
                    {
                        break;
                    }

                    index++;
                }

                source.Insert(index, item);
            }

            InsertItemFallback(group, item, itemComparer);
        }

        return group;
    }

    /// <summary>
    /// 从 <paramref name="source"/> 分组集合中移除具有 <paramref name="key"/> 的第一个组
    /// 如果组不存在则不会执行任何操作
    /// </summary>
    /// <typeparam name="TKey">组键的类型</typeparam>
    /// <typeparam name="TValue">集合中项目的类型</typeparam>
    /// <param name="source">源 <see cref="ObservableGroupedCollection{TKey, TValue}"/> 实例</param>
    /// <param name="key">要移除的组的键</param>
    /// <exception cref="ArgumentNullException">如果 <paramref name="source"/> 或 <paramref name="key"/> 为 <see langword="null"/> 则抛出异常</exception>
    public static void RemoveGroup<TKey, TValue>(this ObservableGroupedCollection<TKey, TValue> source, TKey key)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.For<TKey>.ThrowIfNull(key);

        if (source.TryGetList(out List<ObservableGroup<TKey, TValue>>? list))
        {
            int index = 0;

            foreach (ObservableGroup<TKey, TValue> group in list)
            {
                if (EqualityComparer<TKey>.Default.Equals(group.Key, key))
                {
                    source.RemoveAt(index);

                    return;
                }

                index++;
            }
        }
        else
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            static void RemoveGroupFallback(ObservableGroupedCollection<TKey, TValue> source, TKey key)
            {
                int index = 0;

                foreach (ObservableGroup<TKey, TValue> group in source)
                {
                    if (EqualityComparer<TKey>.Default.Equals(group.Key, key))
                    {
                        source.RemoveAt(index);
                        return;
                    }

                    index++;
                }
            }

            RemoveGroupFallback(source, key);
        }
    }

    /// <summary>
    /// 从 <paramref name="source"/> 分组集合中移除具有 <paramref name="key"/> 的第一个组中的第一个 <paramref name="item"/>
    /// 如果组或项目不存在则不会执行任何操作
    /// </summary>
    /// <typeparam name="TKey">组键的类型</typeparam>
    /// <typeparam name="TValue">集合中项目的类型</typeparam>
    /// <param name="source">源 <see cref="ObservableGroupedCollection{TKey, TValue}"/> 实例</param>
    /// <param name="key">应从中移除 <paramref name="item"/> 的组的键</param>
    /// <param name="item">要移除的项目</param>
    /// <param name="removeGroupIfEmpty">如果为 true（默认值），则组在变空后将被移除</param>
    /// <exception cref="ArgumentNullException">如果 <paramref name="source"/> 或 <paramref name="key"/> 为 <see langword="null"/> 则抛出异常</exception>
    public static void RemoveItem<TKey, TValue>(this ObservableGroupedCollection<TKey, TValue> source, TKey key, TValue item, bool removeGroupIfEmpty = true)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.For<TKey>.ThrowIfNull(key);

        if (source.TryGetList(out List<ObservableGroup<TKey, TValue>>? list))
        {
            int index = 0;

            foreach (ObservableGroup<TKey, TValue> group in list)
            {
                if (EqualityComparer<TKey>.Default.Equals(group.Key, key))
                {
                    if (group.Remove(item) &&
                        removeGroupIfEmpty &&
                        group.Count == 0)
                    {
                        source.RemoveAt(index);
                    }

                    return;
                }

                index++;
            }
        }
        else
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            static void RemoveItemFallback(ObservableGroupedCollection<TKey, TValue> source, TKey key, TValue item, bool removeGroupIfEmpty)
            {
                int index = 0;

                foreach (ObservableGroup<TKey, TValue> group in source)
                {
                    if (EqualityComparer<TKey>.Default.Equals(group.Key, key))
                    {
                        if (group.Remove(item) &&
                            removeGroupIfEmpty &&
                            group.Count == 0)
                        {
                            source.RemoveAt(index);
                        }

                        return;
                    }

                    index++;
                }
            }

            RemoveItemFallback(source, key, item, removeGroupIfEmpty);
        }
    }
}