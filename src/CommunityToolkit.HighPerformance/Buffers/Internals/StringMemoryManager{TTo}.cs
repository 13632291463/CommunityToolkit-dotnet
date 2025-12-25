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
/// 一个自定义的 <see cref="MemoryManager{T}"/>，将 <see cref="string"/> 中的数据转换为 <typeparamref name="TTo"/> 值。
/// </summary>
/// <typeparam name="TTo">将源字符转换为目标的类型。</typeparam>
internal sealed class StringMemoryManager<TTo> : MemoryManager<TTo>, IMemoryManager
    where TTo : unmanaged
{
    /// <summary>
    /// 从中读取数据的源 <see cref="string"/>。
    /// </summary>
    private readonly string text;

    /// <summary>
    /// 在 <see name="array"/> 中的起始偏移量。
    /// </summary>
    private readonly int offset;

    /// <summary>
    /// <see name="array"/> 的原始使用长度。
    /// </summary>
    private readonly int length;

    /// <summary>
    /// 初始化 <see cref="StringMemoryManager{T}"/> 类的新实例。
    /// </summary>
    /// <param name="text">从中读取数据的源 <see cref="string"/>。</param>
    /// <param name="offset"><paramref name="text"/> 中的起始偏移量。</param>
    /// <param name="length"><paramref name="text"/> 的原始使用长度。</param>
    public StringMemoryManager(string text, int offset, int length)
    {
        this.text = text;
        this.offset = offset;
        this.length = length;
    }

    /// <summary>
    /// 获取表示当前内存管理器数据的 Span<TTo>。
    /// </summary>
    /// <returns>表示当前内存管理器数据的 Span<TTo>。</returns>
    public override Span<TTo> GetSpan()
    {
#if NETSTANDARD2_1_OR_GREATER
        // 获取源字符串中指定偏移量处的字符引用
        ref char r0 = ref this.text.DangerousGetReferenceAt(this.offset);
        // 将字符引用转换为目标类型的引用
        ref TTo r1 = ref Unsafe.As<char, TTo>(ref r0);
        // 计算转换后的长度
        int length = RuntimeHelpers.ConvertLength<char, TTo>(this.length);

        return MemoryMarshal.CreateSpan(ref r1, length);
#else
        // 创建源字符串的只读内存片段
        ReadOnlyMemory<char> memory = this.text.AsMemory(this.offset, this.length);
        // 将只读内存转换为可写 Span
        Span<char> span = MemoryMarshal.AsMemory(memory).Span;

        return MemoryMarshal.Cast<char, TTo>(span);
#endif
    }

    /// <summary>
    /// 锁定内存中的指定元素，返回 MemoryHandle 用于访问该元素的固定地址。
    /// </summary>
    /// <param name="elementIndex">要锁定的元素索引。</param>
    /// <returns>包含固定内存地址的 MemoryHandle。</returns>
    public override unsafe MemoryHandle Pin(int elementIndex = 0)
    {
        // 验证元素索引是否在有效范围内
        if ((uint)elementIndex >= (uint)(this.length * sizeof(char) / sizeof(TTo)))
        {
            ThrowArgumentOutOfRangeExceptionForInvalidIndex();
        }

        // 计算字节偏移量：起始偏移量 + 元素索引对应的字节偏移
        nint bytePrefix = this.offset * sizeof(char);
        nint byteSuffix = elementIndex * sizeof(TTo);
        nint byteOffset = bytePrefix + byteSuffix;

        // 固定源字符串在内存中的位置
        GCHandle handle = GCHandle.Alloc(this.text, GCHandleType.Pinned);

        // 获取字符串的起始引用并转换为字节引用
        ref char r0 = ref this.text.DangerousGetReference();
        ref byte r1 = ref Unsafe.As<char, byte>(ref r0);
        // 添加字节偏移以定位到目标元素
        ref byte r2 = ref Unsafe.AddByteOffset(ref r1, byteOffset);
        void* pi = Unsafe.AsPointer(ref r2);

        return new(pi, handle);
    }

    /// <summary>
    /// 解除对内存的锁定状态，释放之前通过 Pin 方法固定的内存引用。
    /// </summary>
    public override void Unpin()
    {
    }

    /// <summary>
    /// 释放 StringMemoryManager<TTo> 类的实例资源。
    /// </summary>
    /// <param name="disposing">是否正在显式释放资源。</param>
    protected override void Dispose(bool disposing)
    {
    }

    /// <summary>
    /// 获取指定偏移量和长度的内存片段，类型转换为指定的泛型类型 T。
    /// </summary>
    /// <typeparam name="T">要转换到的目标类型。</typeparam>
    /// <param name="offset">相对于当前内存管理器的偏移量。</param>
    /// <param name="length">要获取的内存片段长度。</param>
    /// <returns>转换后的 Memory<T> 对象。</returns>
    public Memory<T> GetMemory<T>(int offset, int length)
        where T : unmanaged
    {
        // 计算绝对偏移量和长度，转换回字符类型单位
        int absoluteOffset = this.offset + RuntimeHelpers.ConvertLength<TTo, char>(offset);
        int absoluteLength = RuntimeHelpers.ConvertLength<TTo, char>(length);

        if (typeof(T) == typeof(char))
        {
            // 如果目标类型是 char，则直接从源字符串创建内存片段
            ReadOnlyMemory<char> memory = this.text.AsMemory(absoluteOffset, absoluteLength);

            return (Memory<T>)(object)MemoryMarshal.AsMemory(memory);
        }

        // 否则创建一个新的 StringMemoryManager<T> 实例
        return new StringMemoryManager<T>(this.text, absoluteOffset, absoluteLength).Memory;
    }

    /// <summary>
    /// 当 <see cref="Pin"/> 的目标索引无效时，抛出 <see cref="ArgumentOutOfRangeException"/> 异常。
    /// </summary>
    private static void ThrowArgumentOutOfRangeExceptionForInvalidIndex()
    {
        throw new ArgumentOutOfRangeException("elementIndex", "The input index is not in the valid range");
    }
}