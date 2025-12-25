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
/// 一个实现了 <see cref="IMemoryOwner{T}"/> 接口的类型，具有内置长度和快速 <see cref="Span{T}"/> 访问器。
/// </summary>
/// <typeparam name="T">当前实例中存储项的类型。</typeparam>
[DebuggerTypeProxy(typeof(MemoryDebugView<>))]
[DebuggerDisplay("{ToString(),raw}")]
public sealed class MemoryOwner<T> : IMemoryOwner<T>
{
    /// <summary>
    /// <see cref="array"/> 中的起始偏移量。
    /// </summary>
    private readonly int start;

#pragma warning disable IDE0032
    /// <summary>
    /// <see cref="array"/> 中的可用长度（从 <see cref="start"/> 开始）。
    /// </summary>
    private readonly int length;
#pragma warning restore IDE0032

    /// <summary>
    /// 用于租用 <see cref="array"/> 的 <see cref="ArrayPool{T}"/> 实例。
    /// </summary>
    private readonly ArrayPool<T> pool;

    /// <summary>
    /// 底层的 <typeparamref name="T"/> 数组。
    /// </summary>
    private T[]? array;

    /// <summary>
    /// 使用指定参数初始化 <see cref="MemoryOwner{T}"/> 类的新实例。
    /// </summary>
    /// <param name="length">要使用的新内存缓冲区的长度。</param>
    /// <param name="pool">要使用的 <see cref="ArrayPool{T}"/> 实例。</param>
    /// <param name="mode">指示要用于租用新缓冲区的分配模式。</param>
    private MemoryOwner(int length, ArrayPool<T> pool, AllocationMode mode)
    {
        this.start = 0;
        this.length = length;
        this.pool = pool;
        this.array = pool.Rent(length);

        // 根据分配模式清空数组内容
        if (mode == AllocationMode.Clear)
        {
            this.array.AsSpan(0, length).Clear();
        }
    }

    /// <summary>
    /// 使用指定参数初始化 <see cref="MemoryOwner{T}"/> 类的新实例。
    /// </summary>
    /// <param name="start">在 <paramref name="array"/> 中的起始偏移量。</param>
    /// <param name="length">要使用的数组长度。</param>
    /// <param name="pool">当前使用的 <see cref="ArrayPool{T}"/> 实例。</param>
    /// <param name="array">要使用的输入 <typeparamref name="T"/> 数组。</param>
    private MemoryOwner(int start, int length, ArrayPool<T> pool, T[] array)
    {
        this.start = start;
        this.length = length;
        this.pool = pool;
        this.array = array;
    }

    /// <summary>
    /// 获取一个空的 <see cref="MemoryOwner{T}"/> 实例。
    /// </summary>
    public static MemoryOwner<T> Empty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(0, ArrayPool<T>.Shared, AllocationMode.Default);
    }

    /// <summary>
    /// 使用指定参数创建新的 <see cref="MemoryOwner{T}"/> 实例。
    /// </summary>
    /// <param name="size">要使用的新内存缓冲区的长度。</param>
    /// <returns>指定长度的 <see cref="MemoryOwner{T}"/> 实例。</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="size"/> 无效时抛出。</exception>
    /// <remarks>此方法只是 <see langword="private"/> 构造函数的代理，为了清晰起见。</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MemoryOwner<T> Allocate(int size) => new(size, ArrayPool<T>.Shared, AllocationMode.Default);

    /// <summary>
    /// 使用指定参数创建新的 <see cref="MemoryOwner{T}"/> 实例。
    /// </summary>
    /// <param name="size">要使用的内存缓冲区的长度。</param>
    /// <param name="pool">当前使用的 <see cref="ArrayPool{T}"/> 实例。</param>
    /// <returns>指定长度的 <see cref="MemoryOwner{T}"/> 实例。</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="size"/> 无效时抛出。</exception>
    /// <remarks>此方法只是 <see langword="private"/> 构造函数的代理，为了清晰起见。</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MemoryOwner<T> Allocate(int size, ArrayPool<T> pool) => new(size, pool, AllocationMode.Default);

    /// <summary>
    /// 使用指定参数创建新的 <see cref="MemoryOwner{T}"/> 实例。
    /// </summary>
    /// <param name="size">要使用的新内存缓冲区的长度。</param>
    /// <param name="mode">指示要用于租用新缓冲区的分配模式。</param>
    /// <returns>指定长度的 <see cref="MemoryOwner{T}"/> 实例。</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="size"/> 无效时抛出。</exception>
    /// <remarks>此方法只是 <see langword="private"/> 构造函数的代理，为了清晰起见。</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MemoryOwner<T> Allocate(int size, AllocationMode mode) => new(size, ArrayPool<T>.Shared, mode);

    /// <summary>
    /// 使用指定参数创建新的 <see cref="MemoryOwner{T}"/> 实例。
    /// </summary>
    /// <param name="size">要使用的新内存缓冲区的长度。</param>
    /// <param name="pool">当前使用的 <see cref="ArrayPool{T}"/> 实例。</param>
    /// <param name="mode">指示要用于租用新缓冲区的分配模式。</param>
    /// <returns>指定长度的 <see cref="MemoryOwner{T}"/> 实例。</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="size"/> 无效时抛出。</exception>
    /// <remarks>此方法只是 <see langword="private"/> 构造函数的代理，为了清晰起见。</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MemoryOwner<T> Allocate(int size, ArrayPool<T> pool, AllocationMode mode) => new(size, pool, mode);

    /// <summary>
    /// 获取当前实例中的项目数量。
    /// </summary>
    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.length;
    }

    /// <inheritdoc/>
    public Memory<T> Memory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            T[]? array = this.array;

            if (array is null)
            {
                ThrowObjectDisposedException();
            }

            return new(array!, this.start, this.length);
        }
    }

    /// <summary>
    /// 获取包装当前实例内存的 <see cref="Span{T}"/>。
    /// </summary>
    public Span<T> Span
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            T[]? array = this.array;

            if (array is null)
            {
                ThrowObjectDisposedException();
            }

#if NET6_0_OR_GREATER
            ref T r0 = ref array!.DangerousGetReferenceAt(this.start);

            // 在 .NET 6+ 运行时中，我们可以手动从起始引用创建一个跨度以跳过参数验证，
            // 这些验证包括显式的空值检查、数组的协变检查以及起始偏移量和目标长度的实际验证。
            // 我们只在 .NET 6+ 中这样做，因为我们可以利用运行时特定的数组布局来快速访问初始元素，
            // 这使得这个技巧值得使用。否则，在需要至少访问静态字段以获取 SZ 数组对象中的基字节偏移量的运行时上，
            // 我们可以通过仅使用默认的 Span<T> 构造函数并支付额外条件分支的成本来获得更好的性能，
            // 特别是当 T 是值类型时，此时协变检查在 JIT 中被移除。
            return MemoryMarshal.CreateSpan(ref r0, this.length);
#else
            return new(array!, this.start, this.length);
#endif
        }
    }

    /// <summary>
    /// 返回对当前实例中的第一个元素的引用，不进行边界检查。
    /// </summary>
    /// <returns>对当前实例中的第一个元素的引用。</returns>
    /// <exception cref="ObjectDisposedException">当正在使用的缓冲区已被释放时抛出。</exception>
    /// <remarks>
    /// 此方法不对底层缓冲区执行边界检查，但会检查缓冲区本身是否已被释放。
    /// 此检查不应被删除，它也是不提供获取指定偏移量处引用的方法的原因。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T DangerousGetReference()
    {
        T[]? array = this.array;

        if (array is null)
        {
            ThrowObjectDisposedException();
        }

        return ref array!.DangerousGetReferenceAt(this.start);
    }

    /// <summary>
    /// 获取包装底层 <typeparamref name="T"/> 数组的 <see cref="ArraySegment{T}"/> 实例。
    /// </summary>
    /// <returns>包装底层 <typeparamref name="T"/> 数组的 <see cref="ArraySegment{T}"/> 实例。</returns>
    /// <exception cref="ObjectDisposedException">当正在使用的缓冲区已被释放时抛出。</exception>
    /// <remarks>
    /// 此方法旨在与只接受数组作为输入的 API 一起使用，应谨慎使用。
    /// 特别是，返回的数组是从数组池租用的，调用者有责任确保在当前 <see cref="MemoryOwner{T}"/> 实例被释放后不使用它。
    /// 这样做被认为是未定义的行为，因为相同的数组可能正在另一个 <see cref="MemoryOwner{T}"/> 实例中使用。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ArraySegment<T> DangerousGetArray()
    {
        T[]? array = this.array;

        if (array is null)
        {
            ThrowObjectDisposedException();
        }

        return new(array!, this.start, this.length);
    }

    /// <summary>
    /// 切片当前使用的缓冲区并返回一个新的 <see cref="MemoryOwner{T}"/> 实例。
    /// </summary>
    /// <param name="start">当前缓冲区内的起始偏移量。</param>
    /// <param name="length">要使用的缓冲区长度。</param>
    /// <returns>使用目标项目范围的新 <see cref="MemoryOwner{T}"/> 实例。</returns>
    /// <exception cref="ObjectDisposedException">当正在使用的缓冲区已被释放时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="start"/> 或 <paramref name="length"/> 无效时抛出。</exception>
    /// <remarks>
    /// 使用此方法将释放当前实例，仅应在租用过大缓冲区然后调整大小时使用，
    /// 以避免必须租用新大小的新缓冲区并将前一个项目复制到新缓冲区，
    /// 或需要额外的变量/字段来手动跟踪 <see cref="MemoryOwner{T}"/> 实例中的使用范围。
    /// </remarks>
    public MemoryOwner<T> Slice(int start, int length)
    {
        T[]? array = this.array;

        if (array is null)
        {
            ThrowObjectDisposedException();
        }

        this.array = null;

        // 验证起始偏移量是否在有效范围内
        if ((uint)start > this.length)
        {
            ThrowInvalidOffsetException();
        }

        // 验证长度是否在有效范围内
        if ((uint)length > (this.length - start))
        {
            ThrowInvalidLengthException();
        }

        // 我们正在转移底层数组的所有权，因此当前
        // 实例不再需要被释放。由于这一点，我们可以手动
        // 抑制终结器以减少垃圾回收器的开销。
        GC.SuppressFinalize(this);

        return new(start, length, this.pool, array!);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        T[]? array = this.array;

        if (array is null)
        {
            return;
        }

        this.array = null;

        // 将数组返回到池中
        this.pool.Return(array);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        // 通常我们会抛出异常如果数组已被释放，
        // 但在这种情况下，我们会返回非格式化的
        // 表示作为后备，因为 ToString 方法
        // 通常不应抛出异常。
        if (typeof(T) == typeof(char) &&
            this.array is char[] chars)
        {
            return new(chars, this.start, this.length);
        }

        // 与 Span<T> 中使用的相同表示
        return $"CommunityToolkit.HighPerformance.Buffers.MemoryOwner<{typeof(T)}>[{this.length}]";
    }

    /// <summary>
    /// 当 <see cref="array"/> 为 <see langword="null"/> 时抛出 <see cref="ObjectDisposedException"/>。
    /// </summary>
    private static void ThrowObjectDisposedException()
    {
        // "当前缓冲区已被释放"
        throw new ObjectDisposedException(nameof(MemoryOwner<T>), "The current buffer has already been disposed");
    }

    /// <summary>
    /// 当 <see cref="start"/> 无效时抛出 <see cref="ArgumentOutOfRangeException"/>。
    /// </summary>
    private static void ThrowInvalidOffsetException()
    {
        // "输入的起始参数无效"
        throw new ArgumentOutOfRangeException(nameof(start), "The input start parameter was not valid");
    }

    /// <summary>
    /// 当 <see cref="length"/> 无效时抛出 <see cref="ArgumentOutOfRangeException"/>。
    /// </summary>
    private static void ThrowInvalidLengthException()
    {
        // "输入的长度参数无效"
        throw new ArgumentOutOfRangeException(nameof(length), "The input length parameter was not valid");
    }
}