// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NETSTANDARD2_0

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.CompilerServices;

/// <summary>
/// ConditionalWeakTable{TKey,TValue} 的包装器
/// 通过辅助列表将枚举支持向后移植到 .NET Standard 2.0
/// </summary>
/// <typeparam name="TKey">要在表中存储的项的键</typeparam>
/// <typeparam name="TValue">要在表中存储的值</typeparam>
internal sealed class ConditionalWeakTable2<TKey, TValue>
    where TKey : class
    where TValue : class?
{
    /// <summary>
    /// 底层 ConditionalWeakTable{TKey,TValue} 实例
    /// </summary>
    private readonly ConditionalWeakTable<TKey, TValue> table = new();

    /// <summary>
    /// 用于在 table 中存储键的辅助链表。当 BCL 中没有枚举支持时，
    /// 需要此功能来暴露现有键的枚举能力
    /// </summary>
    private readonly LinkedList<WeakReference<TKey>> keys = new();

    /// <summary>
    /// 获取与指定键关联的值
    /// </summary>
    /// <param name="key">要获取值的键</param>
    /// <param name="value">如果找到键，则包含与指定键关联的值；否则为默认值</param>
    /// <returns>如果 ConditionalWeakTable2 包含具有指定键的元素，则为 true；否则为 false</returns>
    public bool TryGetValue(TKey key, [NotNullWhen(true)] out TValue? value)
    {
        return this.table.TryGetValue(key, out value);
    }

    /// <summary>
    /// 尝试将指定的键和值添加到 ConditionalWeakTable2 中
    /// </summary>
    /// <param name="key">要添加的键</param>
    /// <param name="value">要添加的值</param>
    /// <returns>如果键值对已成功添加，则为 true；如果键已存在，则为 false</returns>
    public bool TryAdd(TKey key, TValue value)
    {
        if (!this.table.TryAdd(key, value))
        {
            return false;
        }

        // 检查键列表是否包含给定的键
        // 如果包含，我们可以在此处停止并返回结果
        foreach (WeakReference<TKey> node in this.keys)
        {
            if (node.TryGetTarget(out TKey? target) &&
                ReferenceEquals(target, key))
            {
                return true;
            }
        }

        // 将键添加到弱引用列表中以跟踪它
        _ = this.keys.AddFirst(new WeakReference<TKey>(key));

        return true;
    }

    /// <summary>
    /// 获取与指定键关联的值，如果键不存在则创建新值
    /// </summary>
    /// <param name="key">要获取值的键</param>
    /// <param name="createValueCallback">用于创建值的回调函数</param>
    /// <returns>与指定键关联的值</returns>
    public TValue GetValue(TKey key, ConditionalWeakTable<TKey, TValue>.CreateValueCallback createValueCallback)
    {
        // 获取或创建值。当此方法返回时，键将在表中存在
        TValue value = this.table.GetValue(key, createValueCallback);

        // 检查键列表是否包含给定的键
        // 如果包含，我们可以在此处停止并返回结果
        foreach (WeakReference<TKey> node in this.keys)
        {
            if (node.TryGetTarget(out TKey? target) &&
                ReferenceEquals(target, key))
            {
                return value;
            }
        }

        // 将键添加到弱引用列表中以跟踪它
        _ = this.keys.AddFirst(new WeakReference<TKey>(key));

        return value;
    }

    /// <summary>
    /// 从 ConditionalWeakTable2 中移除指定的键
    /// </summary>
    /// <param name="key">要移除的键</param>
    /// <returns>如果已成功移除键，则为 true；如果键不存在，则为 false</returns>
    public bool Remove(TKey key)
    {
        return this.table.Remove(key);
    }

    /// <summary>
    /// 获取枚举器以遍历 ConditionalWeakTable2 中的项
    /// </summary>
    /// <returns>ConditionalWeakTable2 的枚举器</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(this);

    /// <summary>
    /// 一个自定义枚举器，用于遍历 ConditionalWeakTable{TKey, TValue} 实例中的项
    /// </summary>
    public ref struct Enumerator
    {
        /// <summary>
        /// 枚举器的所有者 ConditionalWeakTable2{TKey, TValue} 实例
        /// </summary>
        private readonly ConditionalWeakTable2<TKey, TValue> owner;

        /// <summary>
        /// 当前的 LinkedListNode{T}，如果存在
        /// </summary>
        private LinkedListNode<WeakReference<TKey>>? node;

        /// <summary>
        /// 当前键（如果可用）
        /// </summary>
        private TKey? key;

        /// <summary>
        /// 当前值（如果可用）
        /// </summary>
        private TValue? value;

        /// <summary>
        /// 指示是否已至少调用一次 MoveNext
        /// </summary>
        private bool isFirstMoveNextPending;

        /// <summary>
        /// 初始化 ConditionalWeakTable2.Enumerator 结构的新实例
        /// </summary>
        /// <param name="owner">枚举器的所有者 ConditionalWeakTable2{TKey, TValue} 实例</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator(ConditionalWeakTable2<TKey, TValue> owner)
        {
            this.owner = owner;
            this.node = null;
            this.key = null;
            this.value = null;
            this.isFirstMoveNextPending = true;
        }

        /// <summary>
        /// 释放枚举器使用的资源
        /// </summary>
        public readonly void Dispose()
        {
        }

        /// <summary>
        /// 将枚举器推进到集合中的下一个元素
        /// </summary>
        /// <returns>如果枚举器成功地推进到下一个元素，则为 true；如果枚举器已通过集合的末尾，则为 false</returns>
        public bool MoveNext()
        {
            LinkedListNode<WeakReference<TKey>>? node;

            if (!this.isFirstMoveNextPending)
            {
                node = this.node!.Next;
            }
            else
            {
                node = this.owner.keys.First;

                this.isFirstMoveNextPending = false;
            }

            while (node is not null)
            {
                LinkedListNode<WeakReference<TKey>>? nextNode = node.Next;

                // 获取当前节点的键和值
                if (node.Value.TryGetTarget(out TKey? target) &&
                    this.owner.table.TryGetValue(target!, out TValue? value))
                {
                    this.node = node;
                    this.key = target;
                    this.value = value;

                    return true;
                }
                else
                {
                    // 如果当前键已被回收，则修剪列表
                    this.owner.keys.Remove(node);
                }

                node = nextNode;
            }

            return false;
        }

        /// <summary>
        /// 获取当前键
        /// </summary>
        /// <returns>当前键</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly TKey GetKey()
        {
            return this.key!;
        }

        /// <summary>
        /// 获取当前值
        /// </summary>
        /// <returns>当前值</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly TValue GetValue()
        {
            return this.value!;
        }
    }
}

#endif