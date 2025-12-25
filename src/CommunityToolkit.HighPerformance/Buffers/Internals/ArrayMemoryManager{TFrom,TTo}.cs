// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance.Buffers.Internals.Interfaces;
using RuntimeHelpers = CommunityToolkit.HighPerformance.Helpers.Internals.RuntimeHelpers;

namespace CommunityToolkit.HighPerformance.Buffers.Internals;

/// <summary>
/// 一个自定义的 <see cref="MemoryManager{T}"/>，将 <typeparamref name="TFrom"/> 数组中的数据转换为 <typeparamref name="TTo"/> 值。
/// </summary>
/// <typeparam name="TFrom">要读取的源类型项。</typeparam>
/// <typeparam name="TTo">要将源项转换到的目标类型。</typeparam>
internal sealed class ArrayMemoryManager<TFrom, TTo> : MemoryManager<TTo>, IMemoryManager
    where TFrom : unmanaged
    where TTo : unmanaged
{
    /// <summary>
    /// 源 <typeparamref name="TFrom"/> 数组，从中读取数据。
    /// </summary>
    private readonly TFrom[] array;

    /// <summary>
    /// <see name="array"/> 中的起始偏移量。
    /// </summary>
    private readonly int offset;

    /// <summary>
    /// <see name="array"/> 的原始使用长度。
    /// </summary>
    private readonly int length;

    /// <summary>
    /// 初始化 <see cref="ArrayMemoryManager{TFrom, TTo}"/> 类的新实例。
    /// </summary>
    /// <param name="array">源 <typeparamref name="TFrom"/> 数组，从中读取数据。</param>
    /// <param name="offset"><paramref name="array"/> 中的起始偏移量。</param>
    /// <param name="length"><paramref name="array"/> 的原始使用长度。</param>
    public ArrayMemoryManager(TFrom[] array, int offset, int length)
    {
        this.array = array;
        this.offset = offset;
        this.length = length;
    }

    /// <summary>
    /// 获取表示此内存管理器数据的 Span<TTo>。
    /// </summary>
    /// <returns>表示此内存管理器数据的 Span<TTo>。</returns>
    public override Span<TTo> GetSpan()
    {
#if NETSTANDARD2_1_OR_GREATER
        ref TFrom r0 = ref this.array.DangerousGetReferenceAt(this.offset);
        ref TTo r1 = ref Unsafe.As<TFrom, TTo>(ref r0);
        int length = RuntimeHelpers.ConvertLength<TFrom, TTo>(this.length);

        return MemoryMarshal.CreateSpan(ref r1, length);
#else
        Span<TFrom> span = this.array.AsSpan(this.offset, this.length);

        // 我们依赖 MemoryMarshal.Cast 来处理计算新 Span 的有效大小。这也会使使用此类型以及直接转换 Span 的用户具有一致的行为。
        return MemoryMarshal.Cast<TFrom, TTo>(span);
#endif
    }

    /// <summary>
    /// 固定内存中的元素并返回 MemoryHandle。
    /// </summary>
    /// <param name="elementIndex">要固定的元素的索引（默认为 0）。</param>
    /// <returns>表示固定内存的 MemoryHandle。</returns>
    public override unsafe MemoryHandle Pin(int elementIndex = 0)
    {
        if ((uint)elementIndex >= (uint)(this.length * sizeof(TFrom) / sizeof(TTo)))
        {
            ThrowArgumentOutOfRangeExceptionForInvalidIndex();
        }

        nint bytePrefix = this.offset * sizeof(TFrom);
        nint byteSuffix = elementIndex * sizeof(TTo);
        nint byteOffset = bytePrefix + byteSuffix;

        GCHandle handle = GCHandle.Alloc(this.array, GCHandleType.Pinned);

        ref TFrom r0 = ref this.array.DangerousGetReference();
        ref byte r1 = ref Unsafe.As<TFrom, byte>(ref r0);
        ref byte r2 = ref Unsafe.AddByteOffset(ref r1, byteOffset);
        void* pi = Unsafe.AsPointer(ref r2);

        return new(pi, handle);
    }

    /// <summary>
    /// 取消固定先前固定的内存。
    /// </summary>
    public override void Unpin()
    {
    }

    /// <summary>
    /// 释放与此对象关联的资源。
    /// </summary>
    /// <param name="disposing">如果为 true，则表示正在显式释放对象；否则为 false。</param>
    protected override void Dispose(bool disposing)
    {
    }

    /// <summary>
    /// 获取指定偏移量和长度的 Memory<T> 实例，用于类型转换操作。
    /// </summary>
    /// <typeparam name="T">目标类型。</typeparam>
    /// <param name="offset">偏移量。</param>
    /// <param name="length">长度。</param>
    /// <returns>指定偏移量和长度的 Memory<T> 实例。</returns>
    public Memory<T> GetMemory<T>(int offset, int length)
        where T : unmanaged
    {
        // 我们需要计算新 Memory<T> 的正确偏移量和长度。局部偏移量是包装的 TFrom[] 数组中的原始偏移量，
        // 而输入偏移量是相对于当前正在转换的 Memory<TTo> 实例中的 TTo 项的偏移量。
        int absoluteOffset = this.offset + RuntimeHelpers.ConvertLength<TTo, TFrom>(offset);
        int absoluteLength = RuntimeHelpers.ConvertLength<TTo, TFrom>(length);

        // 当用户回到包装数组的原始类型时，我们需要特殊处理。在这种情况下，我们可以直接返回包装该数组的内存，
        // 具有调整后的偏移量和长度，而不需要内存管理器间接引用。
        if (typeof(T) == typeof(TFrom))
        {
            return (Memory<T>)(object)this.array.AsMemory(absoluteOffset, absoluteLength);
        }

        return new ArrayMemoryManager<TFrom, T>(this.array, absoluteOffset, absoluteLength).Memory;
    }

    /// <summary>
    /// 当 <see cref="Pin"/> 的目标索引无效时，抛出 <see cref="ArgumentOutOfRangeException"/> 异常。
    /// </summary>
    private static void ThrowArgumentOutOfRangeExceptionForInvalidIndex()
    {
        throw new ArgumentOutOfRangeException("elementIndex", "The input index is not in the valid range");
    }
}