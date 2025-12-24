// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace CommunityToolkit.Mvvm.Messaging.Internals;

/// <summary>
/// 一个使用池化数组的简单缓冲区写入器实现
/// </summary>
/// <typeparam name="T">要在列表中存储的项的类型</typeparam>
/// <remarks>
/// 此类型是 <see langword="ref"/> <see langword="struct"/> 以避免对象分配并启用基于模式的 <see cref="IDisposable"/> 支持。我们不担心使用者不正确使用此类型，因为它是私有的且只能在父类型中访问
/// </remarks>
internal ref struct ArrayPoolBufferWriter<T>
{
    /// <summary>
    /// 扩展空数组时要使用的默认缓冲区大小
    /// </summary>
    private const int DefaultInitialBufferSize = 128;

    /// <summary>
    /// 底层 <typeparamref name="T"/> 数组
    /// </summary>
    private T[] array;

    /// <summary>
    /// 映射到 <see cref="array"/> 的跨度
    /// </summary>
    /// <remarks>所有写入都通过此方式完成以避免协变检查</remarks>
    private Span<T> span;

    /// <summary>
    /// 在 <see cref="array"/> 中的起始偏移量
    /// </summary>
    private int index;

    /// <summary>
    /// 创建 <see cref="ArrayPoolBufferWriter{T}"/> 结构的新实例
    /// </summary>
    /// <returns>新的 <see cref="ArrayPoolBufferWriter{T}"/> 实例</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArrayPoolBufferWriter<T> Create()
    {
        ArrayPoolBufferWriter<T> instance;

        instance.span = instance.array = ArrayPool<T>.Shared.Rent(DefaultInitialBufferSize);
        instance.index = 0;

        return instance;
    }

    /// <summary>
    /// 获取包含当前项的只读跨度
    /// </summary>
    public readonly ReadOnlySpan<T> Span
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.span.Slice(0, this.index);
    }

    /// <summary>
    /// 向当前集合添加新项
    /// </summary>
    /// <param name="item">要添加的项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        // 获取当前跨度和索引
        Span<T> span = this.span;
        int index = this.index;

        if ((uint)index < (uint)span.Length)
        {
            span[index] = item;

            this.index = index + 1;
        }
        else
        {
            // 当前数组空间不足，需要调整缓冲区大小并添加项
            ResizeBufferAndAdd(item);
        }
    }

    /// <summary>
    /// 重置底层数组和存储的项
    /// </summary>
    public void Reset()
    {
        Array.Clear(this.array, 0, this.index);

        this.index = 0;
    }

    /// <summary>
    /// 当没有空间容纳新项时调整 <see cref="array"/> 的大小，然后添加一项
    /// </summary>
    /// <param name="item">要添加的项</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ResizeBufferAndAdd(T item)
    {
        // 从数组池租借一个大小为当前索引四倍的新数组
        T[] rent = ArrayPool<T>.Shared.Rent(this.index << 2);

        // 将当前数组的内容复制到新数组
        Array.Copy(this.array, 0, rent, 0, this.index);
        // 清除当前数组的内容
        Array.Clear(this.array, 0, this.index);

        // 将当前数组返回到池中
        ArrayPool<T>.Shared.Return(this.array);

        // 更新当前数组和跨度引用
        this.span = this.array = rent;

        // 将新项添加到数组中
        this.span[this.index++] = item;
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public readonly void Dispose()
    {
        // 清除数组内容
        Array.Clear(this.array, 0, this.index);

        // 将数组返回到池中
        ArrayPool<T>.Shared.Return(this.array);
    }
}