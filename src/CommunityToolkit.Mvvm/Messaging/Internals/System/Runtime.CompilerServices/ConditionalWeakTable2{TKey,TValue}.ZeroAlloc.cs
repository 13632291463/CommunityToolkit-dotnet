// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using CommunityToolkit.Mvvm.Messaging;

namespace System.Runtime.CompilerServices;

/// <summary>
/// 一个自定义的 <see cref="ConditionalWeakTable{TKey, TValue}"/> 实例，专门针对 <see cref="WeakReferenceMessenger"/> 的使用进行了优化，
/// 特别是它提供了存储项目的零分配枚举功能。
/// </summary>
/// <typeparam name="TKey">表中存储项目的键类型。</typeparam>
/// <typeparam name="TValue">表中存储的值类型。</typeparam>
internal sealed class ConditionalWeakTable2<TKey, TValue>
    where TKey : class
    where TValue : class?
{
    /// <summary>
    /// 表的初始长度。必须是2的幂次方。
    /// </summary>
    private const int InitialCapacity = 8;

    /// <summary>
    /// 此锁保护表中所有数据的修改。读取者不获取此锁。
    /// </summary>
    private readonly object lockObject;

    /// <summary>
    /// 表的实际存储；在表增长时替换。
    /// </summary>
    private volatile Container container;

    /// <summary>
    /// 初始化 <see cref="ConditionalWeakTable2{TKey, TValue}"/> 类的新实例。
    /// </summary>
    public ConditionalWeakTable2()
    {
        this.lockObject = new object();
        this.container = new Container(this);
    }

    /// <inheritdoc cref="ConditionalWeakTable{TKey, TValue}.TryGetValue(TKey, out TValue)"/>
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        return this.container.TryGetValueWorker(key, out value);
    }

    /// <summary>
    /// 尝试向表中添加新的键值对。
    /// </summary>
    /// <param name="key">要添加的键。</param>
    /// <param name="value">要与键关联的值。</param>
    /// <returns>如果添加成功返回 true，否则返回 false。</returns>
    public bool TryAdd(TKey key, TValue value)
    {
        lock (this.lockObject)
        {
            int entryIndex = this.container.FindEntry(key, out _);

            if (entryIndex != -1)
            {
                return false;
            }

            CreateEntry(key, value);

            return true;
        }
    }

    /// <inheritdoc cref="ConditionalWeakTable{TKey, TValue}.Remove(TKey)"/>
    public bool Remove(TKey key)
    {
        lock (this.lockObject)
        {
            return this.container.Remove(key);
        }
    }

    /// <inheritdoc cref="ConditionalWeakTable{TKey, TValue}.GetValue(TKey, ConditionalWeakTable{TKey, TValue}.CreateValueCallback)"/>
    [UnconditionalSuppressMessage(
        "ReflectionAnalysis",
        "IL2091",
        Justification = "ConditionalWeakTable<TKey, TValue> is only referenced to reuse the callback delegate type, but no value is ever created through reflection.")]
    public TValue GetValue(TKey key, ConditionalWeakTable<TKey, TValue>.CreateValueCallback createValueCallback)
    {
        return TryGetValue(key, out TValue? existingValue) ?
            existingValue :
            GetValueLocked(key, createValueCallback);
    }

    /// <summary>
    /// 在锁下实现 <see cref="GetValue(TKey, ConditionalWeakTable{TKey, TValue}.CreateValueCallback)"/> 的功能。
    /// </summary>
    /// <param name="key">输入的键。</param>
    /// <param name="createValueCallback">用于创建新项目的回调函数。</param>
    /// <returns>要存储的新 <typeparamref name="TValue"/> 项目。</returns>
    [UnconditionalSuppressMessage(
        "ReflectionAnalysis",
        "IL2091",
        Justification = "ConditionalWeakTable<TKey, TValue> is only referenced to reuse the callback delegate type, but no value is ever created through reflection.")]
    private TValue GetValueLocked(TKey key, ConditionalWeakTable<TKey, TValue>.CreateValueCallback createValueCallback)
    {
        // 如果到达这里，说明键不在表中。在锁外调用回调函数以生成键的新值
        TValue newValue = createValueCallback(key);

        lock (this.lockObject)
        {
            // 现在已经获取了锁，必须重新检查以防在添加键时发生竞争
            if (this.container.TryGetValueWorker(key, out TValue? existingValue))
            {
                return existingValue;
            }
            else
            {
                // 在锁下验证已赢得添加键的竞争。现在添加它
                CreateEntry(key, newValue);

                return newValue;
            }
        }
    }

    /// <inheritdoc/>
    public Enumerator GetEnumerator()
    {
        // 这是针对此自定义表的优化，依赖于枚举器在消息传递类型中的使用方式。
        // 特别是，枚举器总是在 using 块中使用，这意味着 Dispose() 始终保证执行。
        // 由于我们无法移除表的内部锁（如下面详述，因为它对于确保容器复活时的一致性是必需的），
        // 加速迭代的解决方案是在枚举器的整个生命周期内预先获取锁，并在完成后一次性释放它。
        // 在此特定场景中这样做是可以的，因为枚举器的唯一用户是我们，所以没有其他线程的阻塞问题
        Monitor.Enter(this.lockObject);

        return new(this);
    }

    /// <summary>
    /// 为当前 <see cref="ConditionalWeakTable2{TKey, TValue}"/> 实例提供枚举器。
    /// </summary>
    public ref struct Enumerator
    {
        /// <summary>
        /// 父表，在释放时设为 null。
        /// </summary>
        private ConditionalWeakTable2<TKey, TValue> table;

        /// <summary>
        /// 应该枚举的容器中的最后索引（包含）。
        /// </summary>
        private readonly int maxIndexInclusive;

        /// <summary>
        /// 容器中的当前索引。
        /// </summary>
        private int currentIndex;

        /// <summary>
        /// 当前键（如果可用）。
        /// </summary>
        private TKey? key;

        /// <summary>
        /// 当前值（如果可用）。
        /// </summary>
        private TValue? value;

        /// <summary>
        /// 初始化 <see cref="Enumerator"/> 类的新实例。
        /// </summary>
        /// <param name="table">正在枚举的输入 <see cref="ConditionalWeakTable2{TKey, TValue}"/> 实例。</param>
        public Enumerator(ConditionalWeakTable2<TKey, TValue> table)
        {
            // 存储对父表的引用并增加其活动枚举器计数
            this.table = table;

            Container container = table.container;

            if (container is null || container.FirstFreeEntry == 0)
            {
                // 最大索引与当前索引相同以防止枚举
                this.maxIndexInclusive = -1;
            }
            else
            {
                // 存储要枚举的最大索引
                this.maxIndexInclusive = container.FirstFreeEntry - 1;
            }

            this.currentIndex = -1;
            this.key = null;
            this.value = null;
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        public void Dispose()
        {
            // 释放锁
            Monitor.Exit(this.table.lockObject);

            this.table = null!;

            // 确保我们不保留最后当前项的引用
            this.key = null;
            this.value = null;
        }

        /// <inheritdoc cref="IEnumerator.MoveNext"/>
        public bool MoveNext()
        {
            // 从表中，我们必须获取当前容器。自从获取枚举器以来，这可能已更改，
            // 但由于至少有一个活动枚举器，索引到对的映射不应更改（如下面详述）。
            // 如果表（或更确切地说是当时的容器）已被终结，这将是 null。
            Container c = this.table.container;

            int currentIndex = this.currentIndex;
            int maxIndexInclusive = this.maxIndexInclusive;

            // 我们有容器。找到要返回的下一个条目（如果存在）。我们需要循环，
            // 因为我们可能尝试获取已删除或已收集的条目，在这种情况下我们再次尝试。
            while (currentIndex < maxIndexInclusive)
            {
                currentIndex++;

                if (c.TryGetEntry(currentIndex, out this.key, out this.value))
                {
                    this.currentIndex = currentIndex;

                    return true;
                }
            }

            this.currentIndex = currentIndex;

            return false;
        }

        /// <summary>
        /// 获取当前键。
        /// </summary>
        /// <returns>当前键。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly TKey GetKey()
        {
            return this.key!;
        }

        /// <summary>
        /// 获取当前值。
        /// </summary>
        /// <returns>当前值。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly TValue GetValue()
        {
            return this.value!;
        }
    }

    /// <summary>
    /// 添加新键值对的工作方法。如果容器已满则调整容器大小。
    /// </summary>
    /// <param name="key">新条目的键。</param>
    /// <param name="value">新条目的值。</param>
    private void CreateEntry(TKey key, TValue value)
    {
        Container container = this.container;

        if (!container.HasCapacity)
        {
            this.container = container = container.Resize();
        }

        container.CreateEntryNoResize(key, value);
    }

    /// <summary>
    /// <see cref="ConditionalWeakTable2{TKey, TValue}"/> 实例中的单个条目。
    /// </summary>
    private struct Entry
    {
        /// <summary>
        /// 使用键的弱引用和值的强引用来保存键和值，
        /// 只有在不通过值访问的情况下可以到达键时才遍历值。
        /// </summary>
        public DependentHandle depHnd;

        /// <summary>
        /// 键的哈希码缓存副本。
        /// </summary>
        public int HashCode;

        /// <summary>
        /// 下一个条目的索引，如果是最后一个则为 -1。
        /// </summary>
        public int Next;
    }

    /// <summary>
    /// 容器保存表的实际数据。给定的 Container 实例始终具有相同的容量。当需要更多容量时，
    /// 我们创建一个新的 Container，将旧的复制到新的，然后丢弃旧的。这有助于实现表的无锁读取，
    /// 因为读取器从不需要处理由于重新哈希导致的条目移动。
    /// </summary>
    private sealed class Container
    {
        /// <summary>
        /// 与此容器关联的 <see cref="ConditionalWeakTable2{TKey, TValue}"/>。
        /// </summary>
        private readonly ConditionalWeakTable2<TKey, TValue> parent;

        /// <summary>
        /// <c>buckets[hashcode &amp; (buckets.Length - 1)]</c> 包含桶中第一个条目的索引（如果为空则为 -1）。
        /// </summary>
        private int[] buckets;

        /// <summary>
        /// 包含存储的依赖句柄的表条目
        /// </summary>
        private Entry[] entries;

        /// <summary>
        /// <c>firstFreeEntry &lt; entries.Length => table</c> 有容量，条目从表的底部增长。
        /// </summary>
        private int firstFreeEntry;

        /// <summary>
        /// 标志检测是否因 OOM 或其他后台异常导致退出锁。
        /// </summary>
        private bool invalid;

        /// <summary>
        /// 初始终结时设置为 true
        /// </summary>
        private bool finalized;

        /// <summary>
        /// 用于确保下一个分配的容器在当前容器被 GC 之前不会被终结。
        /// </summary>
        private volatile object? oldKeepAlive;

        /// <summary>
        /// 初始化 <see cref="Container"/> 类的新实例。
        /// </summary>
        /// <param name="parent">与此实例关联的输入 <see cref="ConditionalWeakTable2{TKey, TValue}"/> 对象。</param>
        internal Container(ConditionalWeakTable2<TKey, TValue> parent)
        {
            this.buckets = new int[InitialCapacity];

            for (int i = 0; i < this.buckets.Length; i++)
            {
                this.buckets[i] = -1;
            }

            this.entries = new Entry[InitialCapacity];

            // 只在所有分配成功完成后才存储父对象。
            // 否则，在增长或清除容器时，我们可能会结束分配一个在构造过程中失败（OOM）的新容器，
            // 但该容器被终结并最终清除关联 CWT 中的某些其他容器。
            this.parent = parent;
        }

        /// <summary>
        /// 初始化 <see cref="Container"/> 类的新实例。
        /// </summary>
        /// <param name="parent">与此实例关联的输入 <see cref="ConditionalWeakTable2{TKey, TValue}"/> 对象。</param>
        /// <param name="buckets">桶数组。</param>
        /// <param name="entries">条目数组。</param>
        /// <param name="firstFreeEntry">第一个空闲条目的索引。</param>
        private Container(ConditionalWeakTable2<TKey, TValue> parent, int[] buckets, Entry[] entries, int firstFreeEntry)
        {
            this.parent = parent;
            this.buckets = buckets;
            this.entries = entries;
            this.firstFreeEntry = firstFreeEntry;
        }

        /// <summary>
        /// 获取当前容器的容量。
        /// </summary>
        internal bool HasCapacity => this.firstFreeEntry < this.entries.Length;

        /// <summary>
        /// 获取第一个空闲条目的索引。
        /// </summary>
        internal int FirstFreeEntry => this.firstFreeEntry;

        /// <summary>
        /// 添加新键值对的工作方法。容器不能已满。
        /// </summary>
        /// <param name="key">要添加的键。</param>
        /// <param name="value">要添加的值。</param>
        internal void CreateEntryNoResize(TKey key, TValue value)
        {
            VerifyIntegrity();

            this.invalid = true;

            int hashCode = RuntimeHelpers.GetHashCode(key) & int.MaxValue;
            int newEntry = this.firstFreeEntry++;

            this.entries[newEntry].HashCode = hashCode;
            this.entries[newEntry].depHnd = new DependentHandle(key, value);

            int bucket = hashCode & (this.buckets.Length - 1);

            this.entries[newEntry].Next = this.buckets[bucket];

            // 此写入必须是易变的，因为我们可能与并发读取器竞争。如果它们看到新条目，
            // 它们也必须看到此方法中较早的所有写入操作
            Volatile.Write(ref this.buckets[bucket], newEntry);

            this.invalid = false;
        }

        /// <summary>
        /// 查找键值对的工作方法。必须持有锁。
        /// </summary>
        /// <param name="key">要查找的键。</param>
        /// <param name="value">输出的值。</param>
        /// <returns>如果找到键返回 true，否则返回 false。</returns>
        internal bool TryGetValueWorker(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            int entryIndex = FindEntry(key, out object? secondary);

            value = Unsafe.As<TValue>(secondary);

            return entryIndex != -1;
        }

        /// <summary>
        /// 如果未找到则返回 -1（如果键在 FindEntry 期间过期，则可以将其视为"未找到"）。
        /// 必须持有锁，或准备在持有锁时重试搜索。
        /// </summary>
        /// <param name="key">要查找的键。</param>
        /// <param name="value">输出的值。</param>
        /// <returns>找到的条目索引，未找到则返回 -1。</returns>
        /// <remarks>此方法要求 <paramref name="value"/> 位于堆栈上以正确跟踪。</remarks>
        internal int FindEntry(TKey key, out object? value)
        {
            int hashCode = RuntimeHelpers.GetHashCode(key) & int.MaxValue;
            int bucket = hashCode & (this.buckets.Length - 1);

            for (int entriesIndex = Volatile.Read(ref this.buckets[bucket]); entriesIndex != -1; entriesIndex = this.entries[entriesIndex].Next)
            {
                if (this.entries[entriesIndex].HashCode == hashCode)
                {
                    // if (_entries[entriesIndex].depHnd.UnsafeGetTargetAndDependent(out value) == key)
                    (object? oKey, value) = this.entries[entriesIndex].depHnd.TargetAndDependent;

                    if (oKey == key)
                    {
                        // 确保我们在访问 DependentHandle 时不会被终结
                        GC.KeepAlive(this);

                        return entriesIndex;
                    }
                }
            }

            // 确保我们在访问 DependentHandle 时不会被终结
            GC.KeepAlive(this);

            value = null;

            return -1;
        }

        /// <summary>
        /// 获取指定条目索引处的条目。
        /// </summary>
        /// <param name="index">要获取的条目索引。</param>
        /// <param name="key">输出的键。</param>
        /// <param name="value">输出的值。</param>
        /// <returns>如果成功获取返回 true，否则返回 false。</returns>
        internal bool TryGetEntry(int index, [NotNullWhen(true)] out TKey? key, [MaybeNullWhen(false)] out TValue value)
        {
            if (index < this.entries.Length)
            {
                // object? oKey = entries[index].depHnd.UnsafeGetTargetAndDependent(out object? oValue);
                (object? oKey, object? oValue) = this.entries[index].depHnd.TargetAndDependent;

                // 确保我们在访问 DependentHandle 时不会被终结
                GC.KeepAlive(this);

                if (oKey != null)
                {
                    key = Unsafe.As<TKey>(oKey);
                    value = Unsafe.As<TValue>(oValue)!;

                    return true;
                }
            }

            key = default;
            value = default;

            return false;
        }

        /// <summary>
        /// 从表中移除指定的键（如果存在）。
        /// </summary>
        /// <param name="key">要移除的键。</param>
        /// <returns>如果成功移除返回 true，否则返回 false。</returns>
        internal bool Remove(TKey key)
        {
            VerifyIntegrity();

            int entryIndex = FindEntry(key, out _);

            if (entryIndex != -1)
            {
                RemoveIndex(entryIndex);

                return true;
            }

            return false;
        }

        /// <summary>
        /// 移除指定索引处的给定条目。
        /// </summary>
        /// <param name="entryIndex">要移除的条目索引。</param>
        private void RemoveIndex(int entryIndex)
        {
            ref Entry entry = ref this.entries[entryIndex];

            // 我们不会在此处释放句柄，因为我们可能与已经看到哈希码的读取器竞争。
            // 相反，我们只是覆盖条目的哈希码，因此后续读取将忽略它。
            // 句柄将在容器终结器中释放，在表调整大小或丢弃后
            Volatile.Write(ref entry.HashCode, -1);

            // 同时清除键以允许 GC 收集条目指向的对象
            // entry.depHnd.UnsafeSetTargetToNull();
            entry.depHnd.Target = null;
        }

        /// <summary>
        /// 调整大小，并从桶列表中清除过期的键。必须持有 <see cref="lockObject"/>。
        /// </summary>
        /// <returns>新的容器实例。</returns>
        /// <remarks>
        /// <see cref="firstFreeEntry"/> 在退出时小于 <c>entries.Length</c>，即表至少有一个空闲条目。
        /// </remarks>
        internal Container Resize()
        {
            bool hasExpiredEntries = false;
            int newSize = this.buckets.Length;

            // 如果存在任何过期或移除的键，我们不会调整大小
            for (int entriesIndex = 0; entriesIndex < this.entries.Length; entriesIndex++)
            {
                ref Entry entry = ref this.entries[entriesIndex];

                if (entry.HashCode == -1)
                {
                    // 条目已被移除
                    hasExpiredEntries = true;

                    break;
                }

                if (entry.depHnd.IsAllocated &&
                    // entry.depHnd.UnsafeGetTarget() is null)
                    entry.depHnd.Target is null)
                {
                    // 条目已过期
                    hasExpiredEntries = true;

                    break;
                }
            }

            if (!hasExpiredEntries)
            {
                // 不需要在此处检查溢出，尝试分配新数组时将抛出异常
                newSize = this.buckets.Length * 2;
            }

            return Resize(newSize);
        }

        /// <summary>
        /// 创建具有当前项目的新 <see cref="Container"/>，指定大小。
        /// </summary>
        /// <param name="newSize">新的请求大小。</param>
        /// <returns>具有请求大小的新 <see cref="Container"/> 实例。</returns>
        internal Container Resize(int newSize)
        {
            // 重新分配桶和条目，并从头开始重建桶和条目。这既用于清除具有过期键的条目，
            // 也用于将新条目放入正确的桶中
            int[] newBuckets = new int[newSize];

            for (int bucketIndex = 0; bucketIndex < newBuckets.Length; bucketIndex++)
            {
                newBuckets[bucketIndex] = -1;
            }

            Entry[] newEntries = new Entry[newSize];
            int newEntriesIndex = 0;

            // 没有活动的枚举器，这意味着我们希望通过删除过期/移除的条目来进行压缩
            for (int entriesIndex = 0; entriesIndex < this.entries.Length; entriesIndex++)
            {
                ref Entry oldEntry = ref this.entries[entriesIndex];
                int hashCode = oldEntry.HashCode;
                DependentHandle depHnd = oldEntry.depHnd;

                if (hashCode != -1 && depHnd.IsAllocated)
                {
                    // if (depHnd.UnsafeGetTarget() is not null)
                    if (depHnd.Target is not null)
                    {
                        ref Entry newEntry = ref newEntries[newEntriesIndex];

                        // 条目正在使用且未过期。将其链接到适当的桶列表中
                        newEntry.HashCode = hashCode;
                        newEntry.depHnd = depHnd;

                        int bucket = hashCode & (newBuckets.Length - 1);

                        newEntry.Next = newBuckets[bucket];
                        newBuckets[bucket] = newEntriesIndex;
                        newEntriesIndex++;
                    }
                    else
                    {
                        // 假设项目已移除，以便此容器的终结器将清理此依赖句柄
                        Volatile.Write(ref oldEntry.HashCode, -1);
                    }
                }
            }

            // 创建新容器。我们希望将从旧容器到新容器的依赖句柄释放责任转移，
            // 并确保在旧容器可能仍在使用时新容器不会被终结。因此，我们存储从旧容器到新容器的引用，
            // 这将使新容器在旧容器仍被使用时保持活动状态
            Container newContainer = new(this.parent!, newBuckets, newEntries, newEntriesIndex);

            // 一旦设置，旧容器的终结器将不会释放已转移的依赖句柄
            this.oldKeepAlive = newContainer;

            // 确保我们在访问 DependentHandles 时不会被终结
            GC.KeepAlive(this);

            return newContainer;
        }

        /// <summary>
        /// 验证当前实例是否有效。
        /// </summary>
        /// <exception cref="InvalidOperationException">如果当前实例无效则抛出异常。</exception>
        private void VerifyIntegrity()
        {
            if (this.invalid)
            {
                static void Throw() => throw new InvalidOperationException("The current collection is in a corrupted state.");

                Throw();
            }
        }

        /// <summary>
        /// 终结当前 <see cref="Container"/> 实例。
        /// </summary>
        ~Container()
        {
            // 如果容器无效，则跳过执行任何操作，包括容器对象已分配但其关联表从未设置的情况
            if (this.invalid || this.parent is null)
            {
                return;
            }

            // 可能 ConditionalWeakTable2 已被复活，在这种情况下代码可能会在容器被终结时访问它。
            // 我们不支持终结后的使用，但我们也不希望通过对依赖句柄的使用或在它们被释放后可能发生的损坏来潜在地破坏状态。
            // 为了避免这种情况，如果可能另一个线程有通过 CWT 对此容器的引用，我们移除这样的引用并重新注册终结：
            // 下次时，我们可以确定没有对此容器的剩余引用，我们可以无担心损坏地清理依赖句柄
            if (!this.finalized)
            {
                this.finalized = true;

                lock (this.parent.lockObject)
                {
                    if (this.parent.container == this)
                    {
                        this.parent.container = null!;
                    }
                }

                // 下次终结时，我们将确定没有剩余引用
                GC.ReRegisterForFinalize(this);

                return;
            }

            Entry[] entries = this.entries;

            this.invalid = true;
            this.entries = null!;
            this.buckets = null!;

            if (entries != null)
            {
                for (int entriesIndex = 0; entriesIndex < entries.Length; entriesIndex++)
                {
                    // 我们需要在两种情况下释放句柄：
                    // - 如果此容器仍拥有依赖句柄（意味着所有权未转移到替换它的另一个容器），则应释放它
                    // - 如果此容器删除了条目，那么即使通常情况下所有权已转移到另一个容器，
                    //   已删除的条目不会转移，因此此容器必须释放它们
                    if (this.oldKeepAlive is null || entries[entriesIndex].HashCode == -1)
                    {
                        entries[entriesIndex].depHnd.Dispose();
                    }
                }
            }
        }
    }
}

#endif