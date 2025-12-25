// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance.Buffers.Internals.Interfaces;
using RuntimeHelpers = CommunityToolkit.HighPerformance.Helpers.Internals.RuntimeHelpers;

namespace CommunityToolkit.HighPerformance.Buffers.Internals;

/// <summary>
/// 一个自定义的 <see cref="MemoryManager{T}"/>，将 <typeparamref name="TFrom"/> 类型的 <see cref="MemoryManager{T}"/> 中的数据转换为 <typeparamref name="TTo"/> 类型值
/// </summary>
/// <typeparam name="TFrom">要读取的源类型</typeparam>
/// <typeparam name="TTo">要将源项转换到的目标类型</typeparam>
internal sealed class ProxyMemoryManager<TFrom, TTo> : MemoryManager<TTo>, IMemoryManager
    where TFrom : unmanaged
    where TTo : unmanaged
{
    /// <summary>
    /// 要读取数据的源 <see cref="MemoryManager{T}"/>
    /// </summary>
    private readonly MemoryManager<TFrom> memoryManager;

    /// <summary>
    /// 在 <see name="memoryManager"/> 中的起始偏移量
    /// </summary>
    private readonly int offset;

    /// <summary>
    /// <see name="memoryManager"/> 的原始使用长度
    /// </summary>
    private readonly int length;

    /// <summary>
    /// 初始化 <see cref="ProxyMemoryManager{TFrom, TTo}"/> 类的新实例
    /// </summary>
    /// <param name="memoryManager">要读取数据的源 <see cref="MemoryManager{T}"/></param>
    /// <param name="offset">在 <paramref name="memoryManager"/> 中的起始偏移量</param>
    /// <param name="length"><paramref name="memoryManager"/> 的原始使用长度</param>
    public ProxyMemoryManager(MemoryManager<TFrom> memoryManager, int offset, int length)
    {
        this.memoryManager = memoryManager;
        this.offset = offset;
        this.length = length;
    }

    /// <summary>
    /// 获取当前内存管理器的跨度
    /// </summary>
    /// <returns>转换后的 <see cref="Span{T}"/> 对象</returns>
    public override Span<TTo> GetSpan()
    {
        // 从源内存管理器获取跨度并按偏移量和长度切片
        Span<TFrom> span = this.memoryManager.GetSpan().Slice(this.offset, this.length);

        // 将源类型的跨度转换为目标类型的跨度
        return MemoryMarshal.Cast<TFrom, TTo>(span);
    }

    /// <summary>
    /// 固定内存管理器中的某个元素并返回内存句柄
    /// </summary>
    /// <param name="elementIndex">要固定的元素索引</param>
    /// <returns>指向固定内存的 <see cref="MemoryHandle"/></returns>
    public override unsafe MemoryHandle Pin(int elementIndex = 0)
    {
        // 检查元素索引是否在有效范围内
        if ((uint)elementIndex >= (uint)(this.length * sizeof(TFrom) / sizeof(TTo)))
        {
            ThrowArgumentExceptionForInvalidIndex();
        }

        // 计算字节前缀、后缀和偏移量
        int bytePrefix = this.offset * sizeof(TFrom);
        int byteSuffix = elementIndex * sizeof(TTo);
        int byteOffset = bytePrefix + byteSuffix;
        int shiftedOffset = Math.DivRem(byteOffset, sizeof(TFrom), out int remainder);

        // 验证偏移量是否对齐
        if (remainder != 0)
        {
            ThrowArgumentExceptionForInvalidAlignment();
        }

        // 固定源内存管理器中的元素
        return this.memoryManager.Pin(shiftedOffset);
    }

    /// <summary>
    /// 解除内存固定
    /// </summary>
    public override void Unpin()
    {
        // 解除源内存管理器的固定
        this.memoryManager.Unpin();
    }

    /// <summary>
    /// 释放当前实例占用的资源
    /// </summary>
    /// <param name="disposing">是否正在释放资源</param>
    protected override void Dispose(bool disposing)
    {
        // 释放源内存管理器
        ((IDisposable)this.memoryManager).Dispose();
    }

    /// <summary>
    /// 获取指定偏移量和长度的内存对象
    /// </summary>
    /// <typeparam name="T">内存中元素的类型</typeparam>
    /// <param name="offset">内存中的偏移量</param>
    /// <param name="length">内存中的长度</param>
    /// <returns>指定类型的内存对象</returns>
    public Memory<T> GetMemory<T>(int offset, int length)
        where T : unmanaged
    {
        // 计算绝对偏移量和长度
        int absoluteOffset = this.offset + RuntimeHelpers.ConvertLength<TTo, TFrom>(offset);
        int absoluteLength = RuntimeHelpers.ConvertLength<TTo, TFrom>(length);

        // 如果目标类型与源类型相同，直接返回切片后的内存
        if (typeof(T) == typeof(TFrom))
        {
            return (Memory<T>)(object)this.memoryManager.Memory.Slice(absoluteOffset, absoluteLength);
        }

        // 否则创建新的代理内存管理器
        return new ProxyMemoryManager<TFrom, T>(this.memoryManager, absoluteOffset, absoluteLength).Memory;
    }

    /// <summary>
    /// 当 <see cref="Pin"/> 的目标索引无效时抛出 <see cref="ArgumentOutOfRangeException"/>
    /// </summary>
    private static void ThrowArgumentExceptionForInvalidIndex()
    {
		// "输入的索引不在有效范围内"
        throw new ArgumentOutOfRangeException("elementIndex", "The input index is not in the valid range");
    }

    /// <summary>
    /// 当 <see cref="Pin"/> 接收到无效的目标索引时抛出 <see cref="ArgumentOutOfRangeException"/>
    /// </summary>
    private static void ThrowArgumentExceptionForInvalidAlignment()
    {
		// "输入的索引未导致对齐的项访问"
        throw new ArgumentOutOfRangeException("elementIndex", "The input index doesn't result in an aligned item access");
    }
}