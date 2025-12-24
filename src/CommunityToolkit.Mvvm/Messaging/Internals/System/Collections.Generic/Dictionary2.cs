// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic;

/// <summary>
/// 一个专门用于 messenger 类型的 <see cref="Dictionary{TKey, TValue}"/> 实现
/// </summary>
/// <typeparam name="TKey">字典中键的类型</typeparam>
/// <typeparam name="TValue">字典中值的类型</typeparam>
[DebuggerDisplay("Count = {Count}")]
internal class Dictionary2<TKey, TValue> : IDictionary2<TKey, TValue>
    where TKey : IEquatable<TKey>
    where TValue : class?
{
    /// <summary>
    /// 指示空闲链表开始的索引
    /// </summary>
    private const int StartOfFreeList = -3;

    /// <summary>
    /// 存储在 <see cref="entries"/> 中的 <see cref="Entry"/> 项的基于1的索引数组
    /// </summary>
    private int[] buckets;

    /// <summary>
    /// 当前存储的键值对数组（即每个哈希组的列表）
    /// </summary>
    private Entry[] entries;

    /// <summary>
    /// 用于在查找时加速获取目标桶的系数
    /// </summary>
    private ulong fastModMultiplier;

    /// <summary>
    /// 映射中当前存储的项数
    /// </summary>
    private int count;

    /// <summary>
    /// 在 <see cref="entries"/> 中空闲列表开始的基于1的索引
    /// </summary>
    private int freeList;

    /// <summary>
    /// 空项的总数
    /// </summary>
    private int freeCount;

    /// <summary>
    /// 初始化 <see cref="Dictionary2{TKey, TValue}"/> 类的新实例
    /// </summary>
    public Dictionary2()
    {
        Initialize(0);
    }

    /// <inheritdoc/>
    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.count - this.freeCount;
    }

    /// <inheritdoc/>
    public TValue this[TKey key]
    {
        get
        {
            ref TValue value = ref FindValue(key);

            if (!Unsafe.IsNullRef(ref value))
            {
                return value;
            }

            ThrowArgumentExceptionForKeyNotFound(key);

            return default!;
        }
    }

    /// <inheritdoc/>
    public void Clear()
    {
        int count = this.count;

        if (count > 0)
        {
#if NETSTANDARD2_0_OR_GREATER
            Array.Clear(this.buckets!, 0, this.buckets!.Length);
#else
            Array.Clear(this.buckets!);
#endif

            this.count = 0;
            this.freeList = -1;
            this.freeCount = 0;

            Array.Clear(this.entries!, 0, count);
        }
    }

    /// <summary>
    /// 检查字典是否包含指定键的对
    /// </summary>
    /// <param name="key">要查找的键</param>
    /// <returns>如果字典中存在键则返回 true，否则返回 false</returns>
    public bool ContainsKey(TKey key)
    {
        return !Unsafe.IsNullRef(ref FindValue(key));
    }

    /// <summary>
    /// 如果存在则获取指定键的值
    /// </summary>
    /// <param name="key">要查找的键</param>
    /// <param name="value">找到的值，否则为 <see langword="default"/></param>
    /// <returns>如果存在键则返回 true，否则返回 false</returns>
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        ref TValue valRef = ref FindValue(key);

        if (!Unsafe.IsNullRef(ref valRef))
        {
            value = valRef;
            return true;
        }

        value = default;

        return false;
    }

    /// <inheritdoc/>
    public bool TryRemove(TKey key)
    {
        uint hashCode = (uint)key.GetHashCode();
        ref int bucket = ref GetBucket(hashCode);
        Entry[]? entries = this.entries;
        int last = -1;
        int i = bucket - 1;

        // 遍历哈希桶中的条目，查找匹配的键
        while (i >= 0)
        {
            ref Entry entry = ref entries[i];

            if (entry.HashCode == hashCode && entry.Key.Equals(key))
            {
                // 从链表中移除找到的条目
                if (last < 0)
                {
                    bucket = entry.Next + 1;
                }
                else
                {
                    entries[last].Next = entry.Next;
                }

                // 将被移除的条目添加到空闲列表中
                entry.Next = StartOfFreeList - this.freeList;

#if NETSTANDARD2_1 || NET6_0_OR_GREATER
                if (RuntimeHelpers.IsReferenceOrContainsReferences<TKey>())
#endif
                {
                    entry.Key = default!;
                }

#if NETSTANDARD2_1 || NET6_0_OR_GREATER
                if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>())
#endif
                {
                    entry.Value = default!;
                }

                this.freeList = i;
                this.freeCount++;

                return true;
            }

            last = i;
            i = entry.Next;
        }

        return false;
    }

    /// <summary>
    /// 获取指定键的值，或者如果键不存在，则添加一个条目并返回该值的引用。这使得可以在单次查找操作中添加或更新值
    /// </summary>
    /// <param name="key">要查找的键</param>
    /// <returns>新值或现有值的引用</returns>
    public ref TValue? GetOrAddValueRef(TKey key)
    {
        Entry[] entries = this.entries;
        uint hashCode = (uint)key.GetHashCode();
        ref int bucket = ref GetBucket(hashCode);
        int i = bucket - 1;

        // 查找键是否已存在
        while (true)
        {
            if ((uint)i >= (uint)entries.Length)
            {
                break;
            }

            if (entries[i].HashCode == hashCode && entries[i].Key.Equals(key))
            {
                return ref entries[i].Value!;
            }

            i = entries[i].Next;
        }

        int index;

        // 如果有空闲空间，从空闲列表中分配
        if (this.freeCount > 0)
        {
            index = this.freeList;

            this.freeList = StartOfFreeList - entries[this.freeList].Next;
            this.freeCount--;
        }
        else
        {
            int count = this.count;

            // 如果需要，调整大小
            if (count == entries.Length)
            {
                Resize();
                bucket = ref GetBucket(hashCode);
            }

            index = count;

            this.count = count + 1;

            entries = this.entries;
        }

        ref Entry entry = ref entries![index];

        // 设置新条目的属性
        entry.HashCode = hashCode;
        entry.Next = bucket - 1;
        entry.Key = key;
        entry.Value = default!;
        bucket = index + 1;

        return ref entry.Value!;
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(this);

    /// <summary>
    /// <see cref="Dictionary2{TKey,TValue}"/> 的枚举器
    /// </summary>
    public ref struct Enumerator
    {
        /// <summary>
        /// 正在枚举的条目
        /// </summary>
        private readonly Entry[] entries;

        /// <summary>
        /// 当前枚举索引
        /// </summary>
        private int index;

        /// <summary>
        /// 当前字典计数
        /// </summary>
        private readonly int count;

        /// <summary>
        /// 创建新的 <see cref="Enumerator"/> 实例
        /// </summary>
        /// <param name="dictionary">要枚举的输入字典</param>
        internal Enumerator(Dictionary2<TKey, TValue> dictionary)
        {
            this.entries = dictionary.entries;
            this.index = 0;
            this.count = dictionary.count;
        }

        /// <inheritdoc cref="IEnumerator.MoveNext"/>
        public bool MoveNext()
        {
            while ((uint)this.index < (uint)this.count)
            {
                // 我们需要提前增加当前索引，以便即使用户不访问枚举器中的任何可用属性，
                // 我们也能正确跟踪字典中的当前位置。由于这是一种可能性，我们不能依赖
                // 其中一个属性在再次调用 MoveNext 之前递增索引。我们在这里偏离了标准的枚举器
                // API 表面，直接暴露 Key/Value 属性，并尽量减少内存复制。
                // 出于同样的原因，我们还删除了 KeyValuePair<TKey, TValue> 字段，
                // 而是依赖于属性从索引指向的当前条目直接访问目标实例（向后调整以考虑此处的递增）
                if (this.entries![this.index++].Next >= -1)
                {
                    return true;
                }
            }

            this.index = this.count + 1;

            return false;
        }

        /// <summary>
        /// 获取当前键
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly TKey GetKey()
        {
            return this.entries[this.index - 1].Key;
        }

        /// <summary>
        /// 获取当前值
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly TValue GetValue()
        {
            return this.entries[this.index - 1].Value!;
        }
    }

    /// <summary>
    /// 获取指定键的值
    /// </summary>
    /// <param name="key">要查找的键</param>
    /// <returns>现有值的引用</returns>
    private unsafe ref TValue FindValue(TKey key)
    {
        ref Entry entry = ref *(Entry*)null;
        uint hashCode = (uint)key.GetHashCode();
        int i = GetBucket(hashCode);
        Entry[] entries = this.entries;

        i--;
        do
        {
            if ((uint)i >= (uint)entries.Length)
            {
                goto ReturnNotFound;
            }

            entry = ref entries[i];

            if (entry.HashCode == hashCode && entry.Key.Equals(key))
            {
                goto ReturnFound;
            }

            i = entry.Next;
        }
        while (true);

        ReturnFound:
        ref TValue value = ref entry.Value!;

        Return:
        return ref value;

        ReturnNotFound:
        value = ref *(TValue*)null;

        goto Return;
    }

    /// <summary>
    /// 初始化当前实例
    /// </summary>
    /// <param name="capacity">目标容量</param>
    [MemberNotNull(nameof(buckets), nameof(entries))]
    private void Initialize(int capacity)
    {
        int size = HashHelpers.GetPrime(capacity);
        int[] buckets = new int[size];
        Entry[] entries = new Entry[size];

        this.freeList = -1;
        this.fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)size);
        this.buckets = buckets;
        this.entries = entries;
    }

    /// <summary>
    /// 调整当前字典的大小以减少冲突
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Resize()
    {
        int newSize = HashHelpers.ExpandPrime(this.count);
        Entry[] entries = new Entry[newSize];
        int count = this.count;

        Array.Copy(this.entries, entries, count);

        this.buckets = new int[newSize];
        this.fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)newSize);

        // 重新哈希现有条目
        for (int i = 0; i < count; i++)
        {
            if (entries[i].Next >= -1)
            {
                ref int bucket = ref GetBucket(entries[i].HashCode);

                entries[i].Next = bucket - 1;
                bucket = i + 1;
            }
        }

        this.entries = entries;
    }

    /// <summary>
    /// 从输入哈希码获取目标桶的引用
    /// </summary>
    /// <param name="hashCode">输入哈希码</param>
    /// <returns>目标桶的引用</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref int GetBucket(uint hashCode)
    {
        int[] buckets = this.buckets!;

        return ref buckets[HashHelpers.FastMod(hashCode, (uint)buckets.Length, this.fastModMultiplier)];
    }

    /// <summary>
    /// 表示映射条目的类型，即链表中的节点
    /// </summary>
    private struct Entry
    {
        /// <summary>
        /// <see cref="Key"/> 的缓存哈希码
        /// </summary>
        public uint HashCode;

        /// <summary>
        /// 链中下一个条目的基于0的索引：-1 表示链结束
        /// 通过改变符号并减去3来编码此条目是否属于空闲列表，
        /// 所以 -2 表示空闲列表结束，-3 表示索引0但在空闲列表上，-4 表示索引1但在空闲列表上，等等
        /// </summary>
        public int Next;

        /// <summary>
        /// 当前节点中的键
        /// </summary>
        public TKey Key;

        /// <summary>
        /// 当前节点中的值（如果存在）
        /// </summary>
        public TValue? Value;
    }

    /// <summary>
    /// 尝试加载具有缺失键的元素时抛出 <see cref="ArgumentException"/>
    /// </summary>
    private static void ThrowArgumentExceptionForKeyNotFound(TKey key)
    {
        throw new ArgumentException($"The target key {key} was not present in the dictionary");
    }
}