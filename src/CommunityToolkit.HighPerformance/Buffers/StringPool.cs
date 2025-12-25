// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using CommunityToolkit.HighPerformance.Helpers;
#if NET6_0_OR_GREATER
using BitOperations = System.Numerics.BitOperations;
#else
using BitOperations = CommunityToolkit.HighPerformance.Helpers.Internals.BitOperations;
#endif

namespace CommunityToolkit.HighPerformance.Buffers;

/// <summary>
/// 一个可配置的字符串池，用于缓存字符串实例。这可以减少创建多个字符数组转换为字符串时的内存分配。
/// 提供 GetOrAdd 方法来最小化重复字符串实例的数量，当达到最大容量时，会自动丢弃使用频率最低的值。
/// </summary>
public sealed class StringPool
{
    /// <summary>
    /// 默认构造函数使用的大小。
    /// </summary>
    private const int DefaultSize = 2048;

    /// <summary>
    /// StringPool 实例的最小大小。
    /// </summary>
    private const int MinimumSize = 32;

    /// <summary>
    /// 当前使用的 FixedSizePriorityMap 实例数组。
    /// </summary>
    private readonly FixedSizePriorityMap[] maps;

    /// <summary>
    /// 使用中的映射数量。
    /// </summary>
    private readonly int numberOfMaps;

    /// <summary>
    /// 初始化 StringPool 类的新实例。
    /// </summary>
    public StringPool()
        : this(DefaultSize)
    {
    }

    /// <summary>
    /// 初始化 StringPool 类的新实例。
    /// </summary>
    /// <param name="minimumSize">要创建的池的最小大小。</param>
    public StringPool(int minimumSize)
    {
        if (minimumSize <= 0)
        {
            ThrowArgumentOutOfRangeException();
        }

        // 设置最小大小
        minimumSize = Math.Max(minimumSize, MinimumSize);

        // 计算指定大小和因子的向上取整结果
        static void FindFactors(int size, int factor, out uint x, out uint y)
        {
            double a = Math.Sqrt((double)size / factor);
            double b = factor * a;

            x = BitOperations.RoundUpToPowerOf2((uint)a);
            y = BitOperations.RoundUpToPowerOf2((uint)b);
        }

        // 我们要找到两个2的幂次因子，使其乘积至少等于请求的大小
        // 为了找到最佳的因子组合（乘积尽可能接近请求的大小），我们测试几个我们认为可接受的比例，并选择最佳结果
        // 地图之间的比例影响分配的对象数量以及在地图上锁定时的多线程性能
        // 我们仍要限制这个数字，以避免出现地图数量相对于总大小过多的情况
        FindFactors(minimumSize, 2, out uint x2, out uint y2);
        FindFactors(minimumSize, 3, out uint x3, out uint y3);
        FindFactors(minimumSize, 4, out uint x4, out uint y4);

        uint p2 = x2 * y2;
        uint p3 = x3 * y3;
        uint p4 = x4 * y4;

        if (p3 < p2)
        {
            p2 = p3;
            x2 = x3;
            y2 = y3;
        }

        if (p4 < p2)
        {
            p2 = p4;
            x2 = x4;
            y2 = y4;
        }

        Span<FixedSizePriorityMap> span = this.maps = new FixedSizePriorityMap[x2];

        // 预先分配映射，因为每个桶只包含数组字段，该字段未预初始化，所以分配很小
        // 这让我们在获取字符串实例时可以锁定每个单独的映射
        foreach (ref FixedSizePriorityMap map in span)
        {
            map = new FixedSizePriorityMap((int)y2);
        }

        this.numberOfMaps = (int)x2;

        Size = (int)p2;
    }

    /// <summary>
    /// 获取共享的 StringPool 实例。
    /// </summary>
    /// <remarks>
    /// 共享池提供一个可重用的 StringPool 实例，可以被整个进程直接访问，
    /// 并缓存字符串实例。由于 StringPool 是线程安全的，因此共享实例可以
    /// 由多个线程并发使用，而无需手动同步。
    /// </remarks>
    public static StringPool Shared { get; } = new();

    /// <summary>
    /// 获取当前实例中可以存储的字符串总数。
    /// </summary>
    public int Size { get; }

    /// <summary>
    /// 将字符串实例存储在内部缓存中。
    /// </summary>
    /// <param name="value">要缓存的输入字符串实例。</param>
    public void Add(string value)
    {
        if (value.Length == 0)
        {
            return;
        }

        int hashcode = GetHashCode(value.AsSpan());
        int bucketIndex = hashcode & (this.numberOfMaps - 1);

        ref FixedSizePriorityMap map = ref this.maps.DangerousGetReferenceAt(bucketIndex);

        lock (map.SyncRoot)
        {
            map.Add(value, hashcode);
        }
    }

    /// <summary>
    /// 获取具有相同内容的缓存字符串实例，或存储输入的实例。
    /// </summary>
    /// <param name="value">包含要使用内容的输入字符串实例。</param>
    /// <returns>具有 value 内容的字符串实例，如果可能则已缓存。</returns>
    public string GetOrAdd(string value)
    {
        if (value.Length == 0)
        {
            return string.Empty;
        }

        int hashcode = GetHashCode(value.AsSpan());
        int bucketIndex = hashcode & (this.numberOfMaps - 1);

        ref FixedSizePriorityMap map = ref this.maps.DangerousGetReferenceAt(bucketIndex);

        lock (map.SyncRoot)
        {
            return map.GetOrAdd(value, hashcode);
        }
    }

    /// <summary>
    /// 获取具有相同内容的缓存字符串实例，或创建一个新实例。
    /// </summary>
    /// <param name="span">包含要使用内容的输入 ReadOnlySpan。</param>
    /// <returns>具有 span 内容的字符串实例，如果可能则已缓存。</returns>
    public string GetOrAdd(ReadOnlySpan<char> span)
    {
        if (span.IsEmpty)
        {
            return string.Empty;
        }

        int hashcode = GetHashCode(span);
        int bucketIndex = hashcode & (this.numberOfMaps - 1);

        ref FixedSizePriorityMap map = ref this.maps.DangerousGetReferenceAt(bucketIndex);

        lock (map.SyncRoot)
        {
            return map.GetOrAdd(span, hashcode);
        }
    }

    /// <summary>
    /// 获取具有相同内容（转换为Unicode）的缓存字符串实例，或创建一个新实例。
    /// </summary>
    /// <param name="span">包含要使用内容的输入 ReadOnlySpan，以指定编码。</param>
    /// <param name="encoding">用于解码 span 内容的 Encoding 实例。</param>
    /// <returns>具有 span 内容的字符串实例，如果可能则已缓存。</returns>
    public unsafe string GetOrAdd(ReadOnlySpan<byte> span, Encoding encoding)
    {
        if (span.IsEmpty)
        {
            return string.Empty;
        }

        int maxLength = encoding.GetMaxCharCount(span.Length);

        using SpanOwner<char> buffer = SpanOwner<char>.Allocate(maxLength);

        fixed (byte* source = span)
        fixed (char* destination = &buffer.DangerousGetReference())
        {
            int effectiveLength = encoding.GetChars(source, span.Length, destination, maxLength);

            return GetOrAdd(new ReadOnlySpan<char>(destination, effectiveLength));
        }
    }

    /// <summary>
    /// 尝试获取具有相同内容的缓存字符串实例（如果存在）。
    /// </summary>
    /// <param name="span">包含要使用内容的输入 ReadOnlySpan。</param>
    /// <param name="value">结果缓存的字符串实例（如果存在）。</param>
    /// <returns>是否找到目标字符串实例。</returns>
    public bool TryGet(ReadOnlySpan<char> span, [NotNullWhen(true)] out string? value)
    {
        if (span.IsEmpty)
        {
            value = string.Empty;

            return true;
        }

        int hashcode = GetHashCode(span);
        int bucketIndex = hashcode & (this.numberOfMaps - 1);

        ref FixedSizePriorityMap map = ref this.maps.DangerousGetReferenceAt(bucketIndex);

        lock (map.SyncRoot)
        {
            return map.TryGet(span, hashcode, out value);
        }
    }

    /// <summary>
    /// 重置当前实例及其关联的映射。
    /// </summary>
    public void Reset()
    {
        foreach (ref FixedSizePriorityMap map in this.maps.AsSpan())
        {
            lock (map.SyncRoot)
            {
                map.Reset();
            }
        }
    }

    /// <summary>
    /// 一个包含缓存字符串实例组的可配置映射。
    /// </summary>
    /// <remarks>
    /// StringPool 中存储的这些实例始终按引用访问 - 本质上就像这个类型是一个类一样。
    /// 该类型也是私有的，因此用户无法直接访问它并意外复制实例，这会导致值与内部状态不同步的错误
    /// （即，因为实例将按值复制，所以原始字段不会共享）。
    /// 我们在此使用结构的原因是为了删除一个间接级别并提高访问 StringPool 类型中各个桶时的缓存局部性。
    /// </remarks>
    private struct FixedSizePriorityMap
    {
        /// <summary>
        /// 表示给定列表的结束索引。
        /// </summary>
        private const int EndOfList = -1;

        /// <summary>
        /// MapEntry 项的1索引数组，存储在 mapEntries 中。
        /// </summary>
        private readonly int[] buckets;

        /// <summary>
        /// 当前缓存条目的数组（即每个哈希组的列表）。
        /// </summary>
        private readonly MapEntry[] mapEntries;

        /// <summary>
        /// 与存储在 mapEntries 中的每个项目关联的优先级值数组。
        /// </summary>
        private readonly HeapEntry[] heapEntries;

        /// <summary>
        /// 地图中当前存储的项目数。
        /// </summary>
        private int count;

        /// <summary>
        /// heapEntries 中存储的项目的当前时间戳。
        /// </summary>
        private uint timestamp;

        /// <summary>
        /// 表示映射条目的类型，即列表中的节点。
        /// </summary>
        private struct MapEntry
        {
            /// <summary>
            /// Value 的预计算哈希码。
            /// </summary>
            public int HashCode;

            /// <summary>
            /// 在此条目中缓存的字符串实例。
            /// </summary>
            public string? Value;

            /// <summary>
            /// 当前列表中下一个节点的0索引。
            /// </summary>
            public int NextIndex;

            /// <summary>
            /// 与当前节点对应的堆条目的0索引。
            /// </summary>
            public int HeapIndex;
        }

        /// <summary>
        /// 表示堆条目的类型，用于为每个项目关联优先级。
        /// </summary>
        private struct HeapEntry
        {
            /// <summary>
            /// 当前条目的时间戳（即项目的优先级）。
            /// </summary>
            public uint Timestamp;

            /// <summary>
            /// 与当前项目对应的映射条目的0索引。
            /// </summary>
            public int MapIndex;
        }

        /// <summary>
        /// 初始化 FixedSizePriorityMap 结构的新实例。
        /// </summary>
        /// <param name="capacity">当前映射的固定容量。</param>
        public FixedSizePriorityMap(int capacity)
        {
            this.buckets = new int[capacity];
            this.mapEntries = new MapEntry[capacity];
            this.heapEntries = new HeapEntry[capacity];
            this.count = 0;
            this.timestamp = 0;
        }

        /// <summary>
        /// 获取可用于同步对当前实例的访问的对象。
        /// </summary>
        public readonly object SyncRoot
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.buckets;
        }

        /// <summary>
        /// 为当前实例实现 StringPool.Add 方法。
        /// </summary>
        /// <param name="value">要缓存的输入字符串实例。</param>
        /// <param name="hashcode">value 的预计算哈希码。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(string value, int hashcode)
        {
            ref string target = ref TryGet(value.AsSpan(), hashcode);

            if (Unsafe.IsNullRef(ref target))
            {
                Insert(value, hashcode);
            }
            else
            {
                target = value;
            }
        }

        /// <summary>
        /// 为当前实例实现 StringPool.GetOrAdd(string) 方法。
        /// </summary>
        /// <param name="value">包含要使用内容的输入字符串实例。</param>
        /// <param name="hashcode">value 的预计算哈希码。</param>
        /// <returns>具有 value 内容的字符串实例。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetOrAdd(string value, int hashcode)
        {
            ref string result = ref TryGet(value.AsSpan(), hashcode);

            if (!Unsafe.IsNullRef(ref result))
            {
                return result;
            }

            Insert(value, hashcode);

            return value;
        }

        /// <summary>
        /// 为当前实例实现 StringPool.GetOrAdd(ReadOnlySpan{char}) 方法。
        /// </summary>
        /// <param name="span">包含要使用内容的输入 ReadOnlySpan。</param>
        /// <param name="hashcode">span 的预计算哈希码。</param>
        /// <returns>具有 span 内容的字符串实例，如果可能则已缓存。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetOrAdd(ReadOnlySpan<char> span, int hashcode)
        {
            ref string result = ref TryGet(span, hashcode);

            if (!Unsafe.IsNullRef(ref result))
            {
                return result;
            }

            string value = span.ToString();

            Insert(value, hashcode);

            return value;
        }

        /// <summary>
        /// 为当前实例实现 StringPool.TryGet 方法。
        /// </summary>
        /// <param name="span">包含要使用内容的输入 ReadOnlySpan。</param>
        /// <param name="hashcode">span 的预计算哈希码。</param>
        /// <param name="value">结果缓存的字符串实例（如果存在）。</param>
        /// <returns>是否找到目标字符串实例。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(ReadOnlySpan<char> span, int hashcode, [NotNullWhen(true)] out string? value)
        {
            ref string result = ref TryGet(span, hashcode);

            if (!Unsafe.IsNullRef(ref result))
            {
                value = result;

                return true;
            }

            value = null;

            return false;
        }

        /// <summary>
        /// 重置当前实例并丢弃所有缓存的值。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            this.buckets.AsSpan().Clear();
            this.mapEntries.AsSpan().Clear();
            this.heapEntries.AsSpan().Clear();
            this.count = 0;
            this.timestamp = 0;
        }

        /// <summary>
        /// 尝试获取目标字符串实例（如果存在），并返回对它的引用。
        /// </summary>
        /// <param name="span">包含要使用内容的输入 ReadOnlySpan。</param>
        /// <param name="hashcode">span 的预计算哈希码。</param>
        /// <returns>对可能包含目标字符串实例的槽的引用。</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe ref string TryGet(ReadOnlySpan<char> span, int hashcode)
        {
            ref MapEntry mapEntriesRef = ref this.mapEntries.DangerousGetReference();
            ref MapEntry entry = ref *(MapEntry*)null;
            int length = this.buckets.Length;
            int bucketIndex = hashcode & (length - 1);

            for (int i = this.buckets.DangerousGetReferenceAt(bucketIndex) - 1;
                 (uint)i < (uint)length;
                 i = entry.NextIndex)
            {
                entry = ref Unsafe.Add(ref mapEntriesRef, (nint)(uint)i);

                if (entry.HashCode == hashcode &&
                    entry.Value!.AsSpan().SequenceEqual(span))
                {
                    UpdateTimestamp(ref entry.HeapIndex);

                    return ref entry.Value!;
                }
            }

            return ref *(string*)null;
        }

        /// <summary>
        /// 在当前映射中插入新的字符串实例，如有需要释放空间。
        /// </summary>
        /// <param name="value">要存储的新字符串实例。</param>
        /// <param name="hashcode">value 的预计算哈希码。</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Insert(string value, int hashcode)
        {
            ref int bucketsRef = ref this.buckets.DangerousGetReference();
            ref MapEntry mapEntriesRef = ref this.mapEntries.DangerousGetReference();
            ref HeapEntry heapEntriesRef = ref this.heapEntries.DangerousGetReference();
            int entryIndex, heapIndex;

            // 如果当前映射已满，首先获取最旧的值，即堆中的第一个项目
            // 然后，通过删除该值来释放槽，并在该空槽中插入新值
            if (this.count == this.mapEntries.Length)
            {
                entryIndex = heapEntriesRef.MapIndex;
                heapIndex = 0;

                ref MapEntry removedEntry = ref Unsafe.Add(ref mapEntriesRef, (nint)(uint)entryIndex);

                // 在这种情况下，移除逻辑可以被极大优化，因为我们可以
                // 通过在目标映射节点上查找来检索目标条目的预计算哈希码，
                // 并且我们还可以跳过遍历目标链时的所有比较，因为我们预先知道
                // 包含要从映射中删除的项目的节点的索引。
                Remove(removedEntry.HashCode, entryIndex);
            }
            else
            {
                // 如果空闲列表不为空，获取该映射节点并更新字段
                entryIndex = this.count;
                heapIndex = this.count;
            }

            int bucketIndex = hashcode & (this.buckets.Length - 1);
            ref int targetBucket = ref Unsafe.Add(ref bucketsRef, (nint)(uint)bucketIndex);
            ref MapEntry targetMapEntry = ref Unsafe.Add(ref mapEntriesRef, (nint)(uint)entryIndex);
            ref HeapEntry targetHeapEntry = ref Unsafe.Add(ref heapEntriesRef, (nint)(uint)heapIndex);

            // 分配新映射条目中的值
            targetMapEntry.HashCode = hashcode;
            targetMapEntry.Value = value;
            targetMapEntry.NextIndex = targetBucket - 1;
            targetMapEntry.HeapIndex = heapIndex;

            // 更新桶槽和当前计数
            targetBucket = entryIndex + 1;
            this.count++;

            // 将堆节点与当前条目链接
            targetHeapEntry.MapIndex = entryIndex;

            // 更新时间戳并重新平衡堆
            UpdateTimestamp(ref targetMapEntry.HeapIndex);
        }

        /// <summary>
        /// 从映射中移除指定的字符串实例以释放一个槽。
        /// </summary>
        /// <param name="hashcode">要移除实例的预计算哈希码。</param>
        /// <param name="mapIndex">要移除的目标映射节点的索引。</param>
        /// <remarks>字符串实例需要已经存在于映射中。</remarks>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Remove(int hashcode, int mapIndex)
        {
            ref MapEntry mapEntriesRef = ref this.mapEntries.DangerousGetReference();
            int bucketIndex = hashcode & (this.buckets.Length - 1);
            int entryIndex = this.buckets.DangerousGetReferenceAt(bucketIndex) - 1;
            int lastIndex = EndOfList;

            // 我们可以只使用未定义的循环，因为我们要查找的输入值
            // 保证存在于映射中
            while (true)
            {
                ref MapEntry candidate = ref Unsafe.Add(ref mapEntriesRef, (nint)(uint)entryIndex);

                // 检查当前值是否匹配
                if (entryIndex == mapIndex)
                {
                    // 如果这不是第一个列表节点，也更新父节点
                    if (lastIndex != EndOfList)
                    {
                        ref MapEntry lastEntry = ref Unsafe.Add(ref mapEntriesRef, (nint)(uint)lastIndex);

                        lastEntry.NextIndex = candidate.NextIndex;
                    }
                    else
                    {
                        // 否则，从桶槽更新目标索引
                        this.buckets.DangerousGetReferenceAt(bucketIndex) = candidate.NextIndex + 1;
                    }

                    this.count--;

                    return;
                }

                // 移动到当前列表中的下一个节点
                lastIndex = entryIndex;
                entryIndex = candidate.NextIndex;
            }
        }

        /// <summary>
        /// 更新指定索引处的堆节点的时间戳（然后同步回）。
        /// </summary>
        /// <param name="heapIndex">要更新的目标堆节点的索引。</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void UpdateTimestamp(ref int heapIndex)
        {
            int currentIndex = heapIndex;
            int count = this.count;
            ref MapEntry mapEntriesRef = ref this.mapEntries.DangerousGetReference();
            ref HeapEntry heapEntriesRef = ref this.heapEntries.DangerousGetReference();
            ref HeapEntry root = ref Unsafe.Add(ref heapEntriesRef, (nint)(uint)currentIndex);
            uint timestamp = this.timestamp;

            // 检查增加要更新的堆节点的当前时间戳是否会导致溢出
            // 如果发生这种情况，我们可能会违反最小堆属性（每个节点的值必须始终<=其子节点的值）
            // 例如，如果我们在更新非根节点，则可能发生这种情况
            // 在这种情况下，我们可能会得到一个值小于其父节点值的节点
            // 为了避免这种情况，我们只需检查当前时间戳值，如果达到最大值，
            // 我们将重新初始化整个堆。这是在一个非内联调用中完成的，所以不会增加此方法中的代码生成大小
            // 重新初始化只需按广度优先顺序（即逐层）遍历堆，并从0开始为所有节点分配递增的时间戳
            // 当前时间戳的值然后仅设置为当前大小
            if (timestamp == uint.MaxValue)
            {
                // 我们在这里使用 goto，因为很少采用此路径。这样做
                // 会导致生成的汇编代码在采用此分支时包含一个前向跳转到回退路径
                // 而正常执行路径则不需要执行任何跳转。这是为了减少在此处未达到此点的所有调用中引入的开销
                goto Fallback;
            }

            Downheap:

            // 为要更新的堆节点分配新时间戳。我们使用本地递增时间戳而不是使用系统计时器
            // 因为这大大减少了开销和系统调用的时间。uint 类型提供了足够大的范围，
            // 并且用户不太可能耗尽它（特别是考虑到每个映射都有一个单独的计数器）
            root.Timestamp = this.timestamp = timestamp + 1;

            // 一旦时间戳更新（这将导致堆变得不平衡），开始下沉循环以再次平衡堆
            while (true)
            {
                // 堆是0索引的（以便数组长度可以保持与该类型中其他数组使用的2的幂值相同）
                // 这意味着每个节点的子节点位于位置：
                //   - 左：(2 * n) + 1
                //   - 右：(2 * n) + 2
                ref HeapEntry minimum = ref root;
                int left = (currentIndex * 2) + 1;
                int right = (currentIndex * 2) + 2;
                int targetIndex = currentIndex;

                // 检查并更新左子节点（如需要）
                if (left < count)
                {
                    ref HeapEntry child = ref Unsafe.Add(ref heapEntriesRef, (nint)(uint)left);

                    if (child.Timestamp < minimum.Timestamp)
                    {
                        minimum = ref child;
                        targetIndex = left;
                    }
                }

                // 与上面相同的右子节点检查
                if (right < count)
                {
                    ref HeapEntry child = ref Unsafe.Add(ref heapEntriesRef, (nint)(uint)right);

                    if (child.Timestamp < minimum.Timestamp)
                    {
                        minimum = ref child;
                        targetIndex = right;
                    }
                }

                // 如果没有待处理的交换，我们可以在此停止
                // 返回前，我们更新目标索引
                if (Unsafe.AreSame(ref root, ref minimum))
                {
                    heapIndex = targetIndex;

                    return;
                }

                // 更新相应映射条目中的索引（考虑交换）
                Unsafe.Add(ref mapEntriesRef, (nint)(uint)root.MapIndex).HeapIndex = targetIndex;
                Unsafe.Add(ref mapEntriesRef, (nint)(uint)minimum.MapIndex).HeapIndex = currentIndex;

                currentIndex = targetIndex;

                // 交换父节点和子节点（使最小值上浮）
                HeapEntry temp = root;

                root = minimum;
                minimum = temp;

                // 更新对根节点的引用
                root = ref Unsafe.Add(ref heapEntriesRef, (nint)(uint)currentIndex);
            }

            Fallback:

            UpdateAllTimestamps();

            // 在更新所有时间戳后，如果堆包含N个项目，则右下角的节点将具有N-1的值
            // 由于在开始下沉执行之前时间戳会增加1，这里我们简单地将本地时间戳
            // 更新为N-1，以便上面的代码将当前更新的节点的时间戳设置为恰好N
            timestamp = (uint)(count - 1);

            goto Downheap;
        }

        /// <summary>
        /// 按递增顺序更新所有当前堆节点的时间戳。
        /// 堆始终保证是完全二叉树，因此当它包含给定数量的节点时，
        /// 这些节点都从数组的开始处连续排列。
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly void UpdateAllTimestamps()
        {
            int count = this.count;
            ref HeapEntry heapEntriesRef = ref this.heapEntries.DangerousGetReference();

            for (int i = 0; i < count; i++)
            {
                Unsafe.Add(ref heapEntriesRef, (nint)(uint)i).Timestamp = (uint)i;
            }
        }
    }

    /// <summary>
    /// 获取给定 ReadOnlySpan 实例的（正）哈希码。
    /// </summary>
    /// <param name="span">输入的 ReadOnlySpan 实例。</param>
    /// <returns>span 的哈希码。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetHashCode(ReadOnlySpan<char> span)
    {
        return HashCode<char>.Combine(span);
    }

    /// <summary>
    /// 当请求的大小超过容量时抛出 ArgumentException。
    /// </summary>
    private static void ThrowArgumentOutOfRangeException()
    {
		// "请求的大小必须大于0"
        throw new ArgumentOutOfRangeException("minimumSize", "The requested size must be greater than 0");
    }
}