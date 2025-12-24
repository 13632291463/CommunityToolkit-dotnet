// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.Collections.Internals;

namespace CommunityToolkit.Mvvm.Collections;

/// <summary>
/// 一个可观察的组
/// 它将 <see cref="Key"/> 与 <see cref="ObservableCollection{T}"/> 关联起来
/// </summary>
/// <typeparam name="TKey">组键的类型</typeparam>
/// <typeparam name="TElement">组中元素的类型</typeparam>
[DebuggerDisplay("Key = {Key}, Count = {Count}")]
public sealed class ObservableGroup<TKey, TElement> : ObservableCollection<TElement>, IReadOnlyObservableGroup<TKey, TElement>
    where TKey : notnull
{
    /// <summary>
    /// 初始化 <see cref="ObservableGroup{TKey, TValue}"/> 类的新实例
    /// </summary>
    /// <param name="key">组的键</param>
    /// <exception cref="System.ArgumentNullException">如果 <paramref name="key"/> 为 <see langword="null"/> 则抛出异常</exception>
    public ObservableGroup(TKey key)
    {
        ArgumentNullException.For<TKey>.ThrowIfNull(key);

        this.key = key;
    }

    /// <summary>
    /// 初始化 <see cref="ObservableGroup{TKey, TValue}"/> 类的新实例
    /// </summary>
    /// <param name="grouping">用于填充组的分组</param>
    /// <exception cref="System.ArgumentNullException">如果 <paramref name="grouping"/> 为 <see langword="null"/> 则抛出异常</exception>
    public ObservableGroup(IGrouping<TKey, TElement> grouping)
        : base(grouping)
    {
        this.key = grouping.Key;
    }

    /// <summary>
    /// 初始化 <see cref="ObservableGroup{TKey, TValue}"/> 类的新实例
    /// </summary>
    /// <param name="key">组的键</param>
    /// <param name="collection">要添加到组中的初始数据集合</param>
    /// <exception cref="System.ArgumentNullException">如果 <paramref name="key"/> 或 <paramref name="collection"/> 为 <see langword="null"/> 则抛出异常</exception>
    public ObservableGroup(TKey key, IEnumerable<TElement> collection)
        : base(collection)
    {
        ArgumentNullException.For<TKey>.ThrowIfNull(key);

        this.key = key;
    }

    private TKey key;

    /// <summary>
    /// 获取或设置组的键
    /// </summary>
    /// <exception cref="System.ArgumentNullException">如果 <paramref name="value"/> 为 <see langword="null"/> 则抛出异常</exception>
    public TKey Key
    {
        get => this.key;
        set
        {
            ArgumentNullException.For<TKey>.ThrowIfNull(value);

            if (!EqualityComparer<TKey>.Default.Equals(this.key!, value))
            {
                this.key = value;

                OnPropertyChanged(ObservableGroupHelper.KeyChangedEventArgs);
            }
        }
    }

    /// <summary>
    /// 尝试获取底层的 <see cref="List{T}"/> 实例（如果存在）
    /// </summary>
    /// <param name="list">如果使用了 <see cref="List{T}"/>，则为结果</param>
    /// <returns>是否找到了 <see cref="List{T}"/> 实例</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryGetList([NotNullWhen(true)] out List<TElement>? list)
    {
        list = Items as List<TElement>;

        return list is not null;
    }

    /// <inheritdoc/>
    object IReadOnlyObservableGroup.Key => Key;

    /// <inheritdoc/>
    object? IReadOnlyObservableGroup.this[int index] => this[index];
}