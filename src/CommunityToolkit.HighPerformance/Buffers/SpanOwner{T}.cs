// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
#if NET6_0_OR_GREATER
using System.Runtime.InteropServices;
#endif
using CommunityToolkit.HighPerformance.Buffers.Views;

namespace CommunityToolkit.HighPerformance.Buffers;

/// <summary>
/// 一个仅栈类型，具有租用指定长度缓冲区并从中获取 <see cref="Span{T}"/> 的能力。
/// 此类型模拟 <see cref="MemoryOwner{T}"/>，但没有分配和进一步优化。
/// 由于这是一个仅栈类型，它依赖于C# 8中引入的鸭子类型 <see cref="IDisposable"/> 模式。
/// 它应该这样使用：
/// <code>
/// using (SpanOwner&lt;byte> buffer = SpanOwner&lt;byte>.Allocate(1024))
/// {
///     // 在这里使用缓冲区...
/// }
/// </code>
/// 一旦代码离开 <see langword="using"/> 块的范围，底层缓冲区将自动
/// 被释放。 <see cref="SpanOwner{T}"/> 中的API依赖于这种模式来获得额外的性能，例如，它们不会执行
/// 在 <see cref="MemoryOwner{T}"/> 中完成的额外检查以确保缓冲区在返回
/// <see cref="Memory{T}"/> 或 <see cref="Span{T}"/> 实例之前没有被释放。
/// 因此，此类型应始终与 <see langword="using"/> 块或表达式一起使用。
/// 不这样做将导致底层缓冲区不会返回到共享池。
/// </summary>
/// <typeparam name="T">要在当前实例中存储的项的类型。</typeparam>
[DebuggerTypeProxy(typeof(MemoryDebugView<>))]
[DebuggerDisplay("{ToString(),raw}")]
public readonly ref struct SpanOwner<T>
{
#pragma warning disable IDE0032
    /// <summary>
    /// <see cref="array"/> 中的可用长度。
    /// </summary>
    private readonly int length;
#pragma warning restore IDE0032

    /// <summary>
    /// 用于租用 <see cref="array"/> 的 <see cref="ArrayPool{T}"/> 实例。
    /// </summary>
    private readonly ArrayPool<T> pool;

    /// <summary>
    /// 底层 <typeparamref name="T"/> 数组。
    /// </summary>
    private readonly T[] array;

    /// <summary>
    /// 使用指定参数初始化 <see cref="SpanOwner{T}"/> 结构的新实例。
    /// </summary>
    /// <param name="length">要使用的新内存缓冲区的长度。</param>
    /// <param name="pool">要使用的 <see cref="ArrayPool{T}"/> 实例。</param>
    /// <param name="mode">指示要用于租用新缓冲区的分配模式。</param>
    private SpanOwner(int length, ArrayPool<T> pool, AllocationMode mode)
    {
        this.length = length;
        this.pool = pool;
        this.array = pool.Rent(length);

        // 如果分配模式为清除，则清空数组的指定长度部分
        if (mode == AllocationMode.Clear)
        {
            this.array.AsSpan(0, length).Clear();
        }
    }

    /// <summary>
    /// 获取一个空的 <see cref="SpanOwner{T}"/> 实例。
    /// </summary>
    public static SpanOwner<T> Empty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(0, ArrayPool<T>.Shared, AllocationMode.Default);
    }

    /// <summary>
    /// 使用指定参数创建 <see cref="SpanOwner{T}"/> 实例的新实例。
    /// </summary>
    /// <param name="size">要使用的新内存缓冲区的长度。</param>
    /// <returns>指定长度的 <see cref="SpanOwner{T}"/> 实例。</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="size"/> 无效时抛出。</exception>
    /// <remarks>此方法只是 <see langword="private"/> 构造函数的代理，为了清晰起见。</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SpanOwner<T> Allocate(int size) => new(size, ArrayPool<T>.Shared, AllocationMode.Default);

    /// <summary>
    /// 使用指定参数创建 <see cref="SpanOwner{T}"/> 实例的新实例。
    /// </summary>
    /// <param name="size">要使用的新内存缓冲区的长度。</param>
    /// <param name="pool">要使用的 <see cref="ArrayPool{T}"/> 实例。</param>
    /// <returns>指定长度的 <see cref="SpanOwner{T}"/> 实例。</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="size"/> 无效时抛出。</exception>
    /// <remarks>此方法只是 <see langword="private"/> 构造函数的代理，为了清晰起见。</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SpanOwner<T> Allocate(int size, ArrayPool<T> pool) => new(size, pool, AllocationMode.Default);

    /// <summary>
    /// 使用指定参数创建 <see cref="SpanOwner{T}"/> 实例的新实例。
    /// </summary>
    /// <param name="size">要使用的新内存缓冲区的长度。</param>
    /// <param name="mode">指示要用于租用新缓冲区的分配模式。</param>
    /// <returns>指定长度的 <see cref="SpanOwner{T}"/> 实例。</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="size"/> 无效时抛出。</exception>
    /// <remarks>此方法只是 <see langword="private"/> 构造函数的代理，为了清晰起见。</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SpanOwner<T> Allocate(int size, AllocationMode mode) => new(size, ArrayPool<T>.Shared, mode);

    /// <summary>
    /// 使用指定参数创建 <see cref="SpanOwner{T}"/> 实例的新实例。
    /// </summary>
    /// <param name="size">要使用的新内存缓冲区的长度。</param>
    /// <param name="pool">要使用的 <see cref="ArrayPool{T}"/> 实例。</param>
    /// <param name="mode">指示要用于租用新缓冲区的分配模式。</param>
    /// <returns>指定长度的 <see cref="SpanOwner{T}"/> 实例。</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="size"/> 无效时抛出。</exception>
    /// <remarks>此方法只是 <see langword="private"/> 构造函数的代理，为了清晰起见。</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SpanOwner<T> Allocate(int size, ArrayPool<T> pool, AllocationMode mode) => new(size, pool, mode);

    /// <summary>
    /// 获取当前实例中的项数
    /// </summary>
    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.length;
    }

    /// <summary>
    /// 获取包装当前实例内存的 <see cref="Span{T}"/>。
    /// </summary>
    public Span<T> Span
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
#if NET6_0_OR_GREATER
            // 对于 .NET 6 及更高版本，使用 MemoryMarshal.CreateSpan 创建 Span
            ref T r0 = ref this.array!.DangerousGetReference();

            return MemoryMarshal.CreateSpan(ref r0, this.length);
#else
            // 对于较早版本，直接使用数组、偏移量和长度创建 Span
            return new(this.array, 0, this.length);
#endif
        }
    }

    /// <summary>
    /// 返回对当前实例中第一个元素的引用，不进行边界检查。
    /// </summary>
    /// <returns>对当前实例中第一个元素的引用。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T DangerousGetReference()
    {
        return ref this.array.DangerousGetReference();
    }

    /// <summary>
    /// 获取包装正在使用的底层 <typeparamref name="T"/> 数组的 <see cref="ArraySegment{T}"/> 实例。
    /// </summary>
    /// <returns>包装正在使用的底层 <typeparamref name="T"/> 数组的 <see cref="ArraySegment{T}"/> 实例。</returns>
    /// <remarks>
    /// 此方法用于处理仅接受数组作为输入的 API，并且应谨慎使用。
    /// 特别是，返回的数组是从数组池租用的，调用者有责任确保在当前 <see cref="SpanOwner{T}"/> 实例被释放后
    /// 不再使用它。这样做被认为是未定义行为，
    /// 因为相同的数组可能在另一个 <see cref="SpanOwner{T}"/> 实例中使用。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ArraySegment<T> DangerousGetArray()
    {
        return new(this.array!, 0, this.length);
    }

    /// <summary>
    /// 实现鸭子类型的 <see cref="IDisposable.Dispose"/> 方法。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        // 将数组返回到池中
        this.pool.Return(this.array);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        // 如果类型为字符且数组为字符数组，则创建字符串
        if (typeof(T) == typeof(char) &&
            this.array is char[] chars)
        {
            return new(chars, 0, this.length);
        }

        // 与 Span<T> 中使用的相同表示形式
        return $"CommunityToolkit.HighPerformance.Buffers.SpanOwner<{typeof(T)}>[{this.length}]";
    }
}