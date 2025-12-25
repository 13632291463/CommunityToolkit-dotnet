// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NETSTANDARD2_1_OR_GREATER

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance.Helpers;

namespace CommunityToolkit.HighPerformance.Buffers.Internals;

/// <summary>
/// 一个自定义的 <see cref="MemoryManager{T}"/>，可以包装任意的 <see cref="object"/> 实例。
/// </summary>
/// <typeparam name="T">目标内存区域中元素的类型。</typeparam>
internal sealed class RawObjectMemoryManager<T> : MemoryManager<T>
{
    /// <summary>
    /// 目标 <see cref="object"/> 实例。
    /// </summary>
    private readonly object instance;

    /// <summary>
    /// 在 <see cref="instance"/> 中的初始偏移量。
    /// </summary>
    private readonly IntPtr offset;

    /// <summary>
    /// 目标内存区域的长度。
    /// </summary>
    private readonly int length;

    /// <summary>
    /// 初始化 <see cref="RawObjectMemoryManager{T}"/> 类的新实例。
    /// </summary>
    /// <param name="instance">目标 <see cref="object"/> 实例。</param>
    /// <param name="offset">在 <paramref name="instance"/> 中的起始偏移量。</param>
    /// <param name="length">在 <paramref name="instance"/> 中的可用长度。</param>
    public RawObjectMemoryManager(object instance, IntPtr offset, int length)
    {
        this.instance = instance;
        this.offset = offset;
        this.length = length;
    }

    /// <summary>
    /// 获取一个表示当前内存管理器所管理内存区域的 Span<T> 对象
    /// </summary>
    /// <returns>表示当前内存管理器所管理内存区域的 Span<T> 对象</returns>
    public override Span<T> GetSpan()
    {
        ref T r0 = ref ObjectMarshal.DangerousGetObjectDataReferenceAt<T>(this.instance, this.offset);

        return MemoryMarshal.CreateSpan(ref r0, this.length);
    }

    /// <summary>
    /// 固定内存中的指定元素并返回一个 MemoryHandle
    /// </summary>
    /// <param name="elementIndex">要固定的元素的索引，默认为 0</param>
    /// <returns>表示固定内存的 MemoryHandle</returns>
    public override unsafe MemoryHandle Pin(int elementIndex = 0)
    {
        if ((uint)elementIndex >= (uint)this.length)
        {
            ThrowArgumentOutOfRangeExceptionForInvalidElementIndex();
        }

        // 为包含非可直接复制数据的数组分配固定句柄会失败并抛出异常
        // 这是预期行为，当尝试固定通过传统方式（例如通过隐式 T[] 数组转换）获取的 Memory<T> 实例时
        // 如果 T 是引用类型或包含引用的类型，也会发生同样的情况
        GCHandle handle = GCHandle.Alloc(this.instance, GCHandleType.Pinned);
        ref T r0 = ref ObjectMarshal.DangerousGetObjectDataReferenceAt<T>(this.instance, this.offset);
        ref T r1 = ref Unsafe.Add(ref r0, (nint)(uint)elementIndex);
        void* p = Unsafe.AsPointer(ref r1);

        return new(p, handle);
    }

    /// <summary>
    /// 取消固定之前固定的内存
    /// </summary>
    public override void Unpin()
    {
    }

    /// <summary>
    /// 释放当前对象所占用的资源
    /// </summary>
    /// <param name="disposing">是否正在释放资源</param>
    protected override void Dispose(bool disposing)
    {
    }

    /// <summary>
    /// 当 <see cref="Pin"/> 的输入索引无效时，抛出 <see cref="ArgumentOutOfRangeException"/> 异常
    /// </summary>
    private static void ThrowArgumentOutOfRangeExceptionForInvalidElementIndex()
    {
        throw new ArgumentOutOfRangeException("elementIndex", "The input element index was not in the valid range");
    }
}

#endif