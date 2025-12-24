// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NETSTANDARD2_1

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.CompilerServices;

/// <summary>
/// 一个包装 <see cref="ConditionalWeakTable{TKey,TValue}"/> 并提供自定义枚举器的类。
/// </summary>
/// <typeparam name="TKey">存储在表中的键的类型。</typeparam>
/// <typeparam name="TValue">存储在表中的值的类型。</typeparam>
internal sealed class ConditionalWeakTable2<TKey, TValue>
    where TKey : class
    where TValue : class?
{
    /// <summary>
    /// 底层的 <see cref="ConditionalWeakTable{TKey,TValue}"/> 实例。
    /// </summary>
    private readonly ConditionalWeakTable<TKey, TValue> table = new();

    /// <summary>
    /// 获取与指定键关联的值。
    /// </summary>
    /// <param name="key">要获取其值的键。</param>
    /// <param name="value">当此方法返回时，如果找到键，则包含与指定键关联的值；否则为 null。</param>
    /// <returns>如果 <see cref="ConditionalWeakTable{TKey,TValue}"/> 包含具有指定键的元素，则为 true；否则为 false。</returns>
    public bool TryGetValue(TKey key, [NotNullWhen(true)] out TValue? value)
    {
        return this.table.TryGetValue(key, out value);
    }

    /// <summary>
    /// 尝试将指定的键和值添加到表中。
    /// </summary>
    /// <param name="key">要添加的键。</param>
    /// <param name="value">要添加的值。</param>
    /// <returns>如果键值对成功添加到表中，则为 true；如果键已存在，则为 false。</returns>
    public bool TryAdd(TKey key, TValue value)
    {
        return this.table.TryAdd(key, value);
    }

    /// <summary>
    /// 获取与指定键关联的值，如果键不存在，则创建并添加新值。
    /// </summary>
    /// <param name="key">要获取值的键</param>
    /// <param name="createValueCallback">当键不存在时用于创建新值的回调函数</param>
    /// <returns>与指定键关联的值</returns>
    public TValue GetValue(TKey key, ConditionalWeakTable<TKey, TValue>.CreateValueCallback createValueCallback)
    {
        return this.table.GetValue(key, createValueCallback);
    }

    /// <summary>
    /// 从表中移除指定键及其关联的值。
    /// </summary>
    /// <param name="key">要移除的键</param>
    /// <returns>如果成功移除键值对则返回true，否则返回false</returns>
    public bool Remove(TKey key)
    {
        return this.table.Remove(key);
    }

    /// <summary>
    /// 获取枚举器，用于遍历ConditionalWeakTable2中的键值对。
    /// </summary>
    /// <returns>ConditionalWeakTable2的枚举器</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(this);

    /// <summary>
    /// A custom enumerator that traverses items in a <see cref="ConditionalWeakTable2{TKey, TValue}"/> instance.
    /// </summary>
    public ref struct Enumerator
    {
        /// <summary>
        /// 枚举器包装的 <see cref="IEnumerator{T}"/> 实例。
        /// </summary>
        private readonly IEnumerator<KeyValuePair<TKey, TValue>> enumerator;

        /// <summary>
        /// 初始化 <see cref="Enumerator"/> 结构的新实例。
        /// </summary>
        /// <param name="owner">枚举器所属的 <see cref="ConditionalWeakTable2{TKey, TValue}"/> 实例。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator(ConditionalWeakTable2<TKey, TValue> owner)
        {
            this.enumerator = ((IEnumerable<KeyValuePair<TKey, TValue>>)owner.table).GetEnumerator();
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        public readonly void Dispose()
        {
            this.enumerator.Dispose();
        }

        /// <inheritdoc cref="Collections.IEnumerator.MoveNext"/>
        public readonly bool MoveNext()
        {
            return this.enumerator.MoveNext();
        }

        /// <summary>
        /// 获取当前枚举位置的键。
        /// </summary>
        /// <returns>当前键</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly TKey GetKey()
        {
            return this.enumerator.Current.Key;
        }

        /// <summary>
        /// 获取当前枚举位置的值。
        /// </summary>
        /// <returns>当前值</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly TValue GetValue()
        {
            return this.enumerator.Current.Value;
        }
    }
}

#endif