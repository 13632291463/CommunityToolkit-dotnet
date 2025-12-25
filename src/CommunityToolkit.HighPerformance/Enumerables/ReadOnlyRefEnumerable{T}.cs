// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
#if NETSTANDARD2_1_OR_GREATER
using System.Runtime.InteropServices;
#endif
using CommunityToolkit.HighPerformance.Helpers.Internals;
using CommunityToolkit.HighPerformance.Memory.Internals;
#if !NETSTANDARD2_1_OR_GREATER
using RuntimeHelpers = CommunityToolkit.HighPerformance.Helpers.Internals.RuntimeHelpers;
#endif

namespace CommunityToolkit.HighPerformance.Enumerables;

/// <summary>
/// 一个遍历只读项的 ref 结构体，用于从任意内存位置进行枚举
/// </summary>
/// <typeparam name="T">要枚举的元素类型</typeparam>
public readonly ref struct ReadOnlyRefEnumerable<T>
{
#if NET8_0_OR_GREATER
    /// <summary>
    /// ReadOnlyRefEnumerable{T} 实例的 T 类型引用
    /// </summary>
    private readonly ref readonly T reference;

    /// <summary>
    /// 当前序列的长度
    /// </summary>
    private readonly int length;
#elif NETSTANDARD2_1_OR_GREATER
    /// <summary>
    /// 指向目标内存区域第一个元素的 ReadOnlySpan{T} 实例
    /// </summary>
    /// <remarks>ReadOnlySpan{T}.Length 字段映射到可用总长度</remarks>
    private readonly ReadOnlySpan<T> span;
#else
    /// <summary>
    /// 目标对象实例（如果存在）
    /// </summary>
    private readonly object? instance;

    /// <summary>
    /// instance 中的初始偏移量
    /// </summary>
    private readonly IntPtr offset;

    /// <summary>
    /// 序列的总可用长度
    /// </summary>
    private readonly int length;
#endif

    /// <summary>
    /// 要枚举的序列中元素之间的距离
    /// </summary>
    /// <remarks>距离指的是 T 类型元素，而不是字节偏移</remarks>
    private readonly int step;

#if NETSTANDARD2_1_OR_GREATER
#if !NET8_0_OR_GREATER
    /// <summary>
    /// 初始化 ReadOnlyRefEnumerable{T} 结构的新实例
    /// </summary>
    /// <param name="span">指向目标内存区域第一个元素的 ReadOnlySpan{T} 实例</param>
    /// <param name="step">要枚举的序列中元素之间的距离</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlyRefEnumerable(ReadOnlySpan<T> span, int step)
    {
#if NET8_0_OR_GREATER
        this.reference = ref MemoryMarshal.GetReference(span);
        this.length = span.Length;
#else
        this.span = span;
#endif
        this.step = step;
    }
#endif

    /// <summary>
    /// 初始化 ReadOnlyRefEnumerable{T} 结构的新实例
    /// </summary>
    /// <param name="reference">序列中第一个元素的引用</param>
    /// <param name="length">序列中的元素数量</param>
    /// <param name="step">要枚举的序列中元素之间的距离</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlyRefEnumerable(in T reference, int length, int step)
    {
#if NET8_0_OR_GREATER
        this.reference = ref reference;
        this.length = length;
        this.step = step;
#else
        this.span = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(reference), length);
        this.step = step;
#endif
    }

    /// <summary>
    /// 使用指定参数创建 ReadOnlyRefEnumerable{T} 结构的新实例
    /// </summary>
    /// <param name="value">要映射的第一个 T 类型元素的引用</param>
    /// <param name="length">序列中的元素数量</param>
    /// <param name="step">要枚举的序列中元素之间的距离</param>
    /// <returns>使用指定参数的 ReadOnlyRefEnumerable{T} 实例</returns>
    /// <exception cref="ArgumentOutOfRangeException">当参数为负数时抛出</exception>
    public static ReadOnlyRefEnumerable<T> DangerousCreate(in T value, int length, int step)
    {
        if (length < 0)
        {
            ThrowArgumentOutOfRangeExceptionForLength();
        }

        if (step < 0)
        {
            ThrowArgumentOutOfRangeExceptionForStep();
        }

        OverflowHelper.EnsureIsInNativeIntRange(length, 1, step);

        return new(in value, length, step);
    }
#else
    /// <summary>
    /// 初始化 ReadOnlyRefEnumerable{T} 结构的新实例
    /// </summary>
    /// <param name="instance">目标对象实例</param>
    /// <param name="offset">instance 中的初始偏移量</param>
    /// <param name="length">序列中的元素数量</param>
    /// <param name="step">要枚举的序列中元素之间的距离</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlyRefEnumerable(object? instance, IntPtr offset, int length, int step)
    {
        this.instance = instance;
        this.offset = offset;
        this.length = length;
        this.step = step;
    }
#endif

    /// <summary>
    /// 获取序列的总可用长度
    /// </summary>
    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if NET8_0_OR_GREATER
        get => this.length;
#elif NETSTANDARD2_1_OR_GREATER
        get => this.span.Length;
#else
        get => this.length;
#endif
    }

    /// <summary>
    /// 获取指定零基索引处的元素
    /// </summary>
    /// <param name="index">元素的零基索引</param>
    /// <returns>指定索引处元素的引用</returns>
    /// <exception cref="IndexOutOfRangeException">
    /// 当 index 参数无效时抛出
    /// </exception>
    public ref readonly T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((uint)index >= (uint)Length)
            {
                ThrowHelper.ThrowIndexOutOfRangeException();
            }

#if NET8_0_OR_GREATER
            ref T r0 = ref Unsafe.AsRef(in this.reference);
#elif NETSTANDARD2_1_OR_GREATER
            ref T r0 = ref MemoryMarshal.GetReference(this.span);
#else
            ref T r0 = ref RuntimeHelpers.GetObjectDataAtOffsetOrPointerReference<T>(this.instance, this.offset);
#endif
            nint offset = (nint)(uint)index * (nint)(uint)this.step;
            ref T ri = ref Unsafe.Add(ref r0, offset);

            return ref ri;
        }
    }

#if NETSTANDARD2_1_OR_GREATER
    /// <summary>
    /// 获取指定零基索引处的元素
    /// </summary>
    /// <param name="index">元素的零基索引</param>
    /// <returns>指定索引处元素的引用</returns>
    /// <exception cref="IndexOutOfRangeException">
    /// 当 index 参数无效时抛出
    /// </exception>
    public ref readonly T this[Index index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref this[index.GetOffset(Length)];
    }
#endif

    /// <inheritdoc cref="System.Collections.IEnumerable.GetEnumerator"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator()
    {
#if NET8_0_OR_GREATER
        return new(in this.reference, this.length, this.step);
#elif NETSTANDARD2_1_OR_GREATER
        return new(this.span, this.step);
#else
        return new(this.instance, this.offset, this.length, this.step);
#endif
    }

    /// <summary>
    /// 将此 ReadOnlyRefEnumerable{T} 的内容复制到目标 RefEnumerable{T} 实例
    /// </summary>
    /// <param name="destination">目标 RefEnumerable{T} 实例</param>
    /// <exception cref="ArgumentException">
    /// 当 destination 比源 ReadOnlyRefEnumerable{T} 实例短时抛出
    /// </exception>
    public void CopyTo(RefEnumerable<T> destination)
    {
#if NET8_0_OR_GREATER
        if (this.step == 1)
        {
            destination.CopyFrom(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this.reference), this.length));

            return;
        }

        if (destination.Step == 1)
        {
            CopyTo(MemoryMarshal.CreateSpan(ref destination.Reference, destination.Length));

            return;
        }

        ref T sourceRef = ref Unsafe.AsRef(in this.reference);
        ref T destinationRef = ref destination.Reference;
        int sourceLength = this.length;
        int destinationLength = destination.Length;
#elif NETSTANDARD2_1_OR_GREATER
        if (this.step == 1)
        {
            destination.CopyFrom(this.span);

            return;
        }

        if (destination.Step == 1)
        {
            CopyTo(destination.Span);

            return;
        }

        ref T sourceRef = ref this.span.DangerousGetReference();
        ref T destinationRef = ref destination.Span.DangerousGetReference();
        int sourceLength = this.span.Length;
        int destinationLength = destination.Span.Length;
#else
        ref T sourceRef = ref RuntimeHelpers.GetObjectDataAtOffsetOrPointerReference<T>(this.instance, this.offset);
        ref T destinationRef = ref RuntimeHelpers.GetObjectDataAtOffsetOrPointerReference<T>(destination.Instance, destination.Offset);
        int sourceLength = this.length;
        int destinationLength = destination.Length;
#endif

        if ((uint)destinationLength < (uint)sourceLength)
        {
            ThrowArgumentExceptionForDestinationTooShort();
        }

        RefEnumerableHelper.CopyTo(ref sourceRef, ref destinationRef, (nint)(uint)sourceLength, (nint)(uint)this.step, (nint)(uint)destination.Step);
    }

    /// <summary>
    /// 尝试将当前 ReadOnlyRefEnumerable{T} 实例复制到目标 RefEnumerable{T}
    /// </summary>
    /// <param name="destination">复制操作的目标 RefEnumerable{T}</param>
    /// <returns>操作是否成功</returns>
    public bool TryCopyTo(RefEnumerable<T> destination)
    {
#if NET8_0_OR_GREATER
        int sourceLength = this.length;
        int destinationLength = destination.Length;
#elif NETSTANDARD2_1_OR_GREATER
        int sourceLength = this.span.Length;
        int destinationLength = destination.Span.Length;
#else
        int sourceLength = this.length;
        int destinationLength = destination.Length;
#endif

        if (destinationLength >= sourceLength)
        {
            CopyTo(destination);

            return true;
        }

        return false;
    }

    /// <summary>
    /// 将此 RefEnumerable{T} 的内容复制到目标 Span{T} 实例
    /// </summary>
    /// <param name="destination">目标 Span{T} 实例</param>
    /// <exception cref="ArgumentException">
    /// 当 destination 比源 RefEnumerable{T} 实例短时抛出
    /// </exception>
    public void CopyTo(Span<T> destination)
    {
#if NET8_0_OR_GREATER
        if (this.step == 1)
        {
            MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this.reference), this.length).CopyTo(destination);

            return;
        }

        ref T sourceRef = ref Unsafe.AsRef(in this.reference);
        int length = this.length;
#elif NETSTANDARD2_1_OR_GREATER
        if (this.step == 1)
        {
            this.span.CopyTo(destination);

            return;
        }

        ref T sourceRef = ref this.span.DangerousGetReference();
        int length = this.span.Length;
#else
        ref T sourceRef = ref RuntimeHelpers.GetObjectDataAtOffsetOrPointerReference<T>(this.instance, this.offset);
        int length = this.length;
#endif
        if ((uint)destination.Length < (uint)length)
        {
            ThrowArgumentExceptionForDestinationTooShort();
        }

        ref T destinationRef = ref destination.DangerousGetReference();

        RefEnumerableHelper.CopyTo(ref sourceRef, ref destinationRef, (nint)(uint)length, (nint)(uint)this.step);
    }

    /// <summary>
    /// 尝试将当前 RefEnumerable{T} 实例复制到目标 Span{T}
    /// </summary>
    /// <param name="destination">复制操作的目标 Span{T}</param>
    /// <returns>操作是否成功</returns>
    public bool TryCopyTo(Span<T> destination)
    {
#if NET8_0_OR_GREATER
        int length = this.length;
#elif NETSTANDARD2_1_OR_GREATER
        int length = this.span.Length;
#else
        int length = this.length;
#endif

        if (destination.Length >= length)
        {
            CopyTo(destination);

            return true;
        }

        return false;
    }

    /// <inheritdoc cref="RefEnumerable{T}.ToArray"/>
    public T[] ToArray()
    {
#if NET8_0_OR_GREATER
        int length = this.length;
#elif NETSTANDARD2_1_OR_GREATER
        int length = this.span.Length;
#else
        int length = this.length;
#endif

        // 如果没有数据则返回空数组
        if (length == 0)
        {
            return Array.Empty<T>();
        }

        T[] array = new T[length];

        CopyTo(array);

        return array;
    }

    /// <summary>
    /// 隐式转换 RefEnumerable{T} 实例为 ReadOnlyRefEnumerable{T} 实例
    /// </summary>
    /// <param name="enumerable">输入的 RefEnumerable{T} 实例</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlyRefEnumerable<T>(RefEnumerable<T> enumerable)
    {
#if NET8_0_OR_GREATER
        return new(in enumerable.Reference, enumerable.Length, enumerable.Step);
#elif NETSTANDARD2_1_OR_GREATER
        return new(enumerable.Span, enumerable.Step);
#else
        return new(enumerable.Instance, enumerable.Offset, enumerable.Length, enumerable.Step);
#endif
    }

    /// <summary>
    /// 用于遍历 ReadOnlyRefEnumerable{T} 实例中项的自定义枚举器类型
    /// </summary>
    public ref struct Enumerator
    {
#if NET8_0_OR_GREATER
        /// <inheritdoc cref="ReadOnlyRefEnumerable{T}.reference"/>
        private readonly ref readonly T reference;

        /// <inheritdoc cref="ReadOnlyRefEnumerable{T}.length"/>
        private readonly int length;
#elif NETSTANDARD2_1_OR_GREATER
        /// <inheritdoc cref="ReadOnlyRefEnumerable{T}.span"/>
        private readonly ReadOnlySpan<T> span;
#else
        /// <inheritdoc cref="ReadOnlyRefEnumerable{T}.instance"/>
        private readonly object? instance;

        /// <inheritdoc cref="ReadOnlyRefEnumerable{T}.offset"/>
        private readonly IntPtr offset;

        /// <inheritdoc cref="ReadOnlyRefEnumerable{T}.length"/>
        private readonly int length;
#endif

        /// <inheritdoc cref="ReadOnlyRefEnumerable{T}.step"/>
        private readonly int step;

        /// <summary>
        /// 序列中的当前位置
        /// </summary>
        private int position;

#if NET8_0_OR_GREATER
        /// <summary>
        /// 初始化 Enumerator 结构的新实例
        /// </summary>
        /// <param name="reference">序列第一个元素的 T 类型引用</param>
        /// <param name="length">序列长度</param>
        /// <param name="step">要枚举的序列中元素之间的距离</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(in T reference, int length, int step)
        {
            this.reference = ref reference;
            this.length = length;
            this.step = step;
            this.position = -1;
        }
#elif NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// 初始化 Enumerator 结构的新实例
        /// </summary>
        /// <param name="span">包含要遍历项信息的 ReadOnlySpan{T} 实例</param>
        /// <param name="step">要枚举的序列中元素之间的距离</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(ReadOnlySpan<T> span, int step)
        {
            this.span = span;
            this.step = step;
            this.position = -1;
        }
#else
        /// <summary>
        /// 初始化 Enumerator 结构的新实例
        /// </summary>
        /// <param name="instance">目标对象实例</param>
        /// <param name="offset">instance 中的初始偏移量</param>
        /// <param name="length">序列中的元素数量</param>
        /// <param name="step">要枚举的序列中元素之间的距离</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(object? instance, IntPtr offset, int length, int step)
        {
            this.instance = instance;
            this.offset = offset;
            this.length = length;
            this.step = step;
            this.position = -1;
        }
#endif

        /// <inheritdoc cref="System.Collections.IEnumerator.MoveNext"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
#if NET8_0_OR_GREATER
            return ++this.position < this.length;
#elif NETSTANDARD2_1_OR_GREATER
            return ++this.position < this.span.Length;
#else
            return ++this.position < this.length;
#endif
        }

        /// <inheritdoc cref="System.Collections.Generic.IEnumerator{T}.Current"/>
        public readonly ref readonly T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if NET8_0_OR_GREATER
                ref T r0 = ref Unsafe.AsRef(in this.reference);
#elif NETSTANDARD2_1_OR_GREATER
                ref T r0 = ref this.span.DangerousGetReference();
#else
                ref T r0 = ref RuntimeHelpers.GetObjectDataAtOffsetOrPointerReference<T>(this.instance, this.offset);
#endif
                nint offset = (nint)(uint)this.position * (nint)(uint)this.step;
                ref T ri = ref Unsafe.Add(ref r0, offset);

                return ref ri;
            }
        }
    }

#if NETSTANDARD2_1_OR_GREATER
    /// <summary>
    /// 当 "length" 参数无效时抛出 ArgumentOutOfRangeException
    /// </summary>
    private static void ThrowArgumentOutOfRangeExceptionForLength()
    {
        throw new ArgumentOutOfRangeException("length");
    }

    /// <summary>
    /// 当 "step" 参数无效时抛出 ArgumentOutOfRangeException
    /// </summary>
    private static void ThrowArgumentOutOfRangeExceptionForStep()
    {
        throw new ArgumentOutOfRangeException("step");
    }
#endif

    /// <summary>
    /// 当目标 span 太短时抛出 ArgumentException
    /// </summary>
    private static void ThrowArgumentExceptionForDestinationTooShort()
    {
        throw new ArgumentException("The target span is too short to copy all the current items to.");
    }
}