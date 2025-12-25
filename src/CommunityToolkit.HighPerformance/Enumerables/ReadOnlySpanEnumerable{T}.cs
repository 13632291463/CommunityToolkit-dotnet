// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CommunityToolkit.HighPerformance.Enumerables;

/// <summary>
/// 一个枚举给定 ReadOnlySpan{T} 实例中元素的 ref 结构体
/// </summary>
/// <typeparam name="T">要枚举的元素类型</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public ref struct ReadOnlySpanEnumerable<T>
{
    /// <summary>
    /// 源 ReadOnlySpan{T} 实例
    /// </summary>
    private readonly ReadOnlySpan<T> span;

    /// <summary>
    /// 当前在 span 中的索引
    /// </summary>
    private int index;

    /// <summary>
    /// 初始化 ReadOnlySpanEnumerable{T} 结构体的新实例
    /// </summary>
    /// <param name="span">源 ReadOnlySpan{T} 实例</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpanEnumerable(ReadOnlySpan<T> span)
    {
        this.span = span;
        this.index = -1;
    }

    /// <summary>
    /// 实现鸭式类型化的 IEnumerable{T}.GetEnumerator 方法
    /// </summary>
    /// <returns>针对当前 ReadOnlySpan{T} 值的 ReadOnlySpanEnumerable{T} 实例</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpanEnumerable<T> GetEnumerator() => this;

    /// <summary>
    /// 实现鸭式类型化的 System.Collections.IEnumerator.MoveNext 方法
    /// </summary>
    /// <returns>如果新元素可用则为 true，否则为 false</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        return ++this.index < this.span.Length;
    }

    /// <summary>
    /// 获取鸭式类型化的 IEnumerator{T}.Current 属性
    /// </summary>
    public readonly Item Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
#if NETSTANDARD2_1_OR_GREATER
            ref T r0 = ref MemoryMarshal.GetReference(this.span);
            ref T ri = ref Unsafe.Add(ref r0, (nint)(uint)this.index);

            // See comment in SpanEnumerable<T> about this
            return new(ref ri, this.index);
#else
            return new(this.span, this.index);
#endif
        }
    }

    /// <summary>
    /// 一个来自源 Span{T} 实例的项
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly ref struct Item
    {
#if NET8_0_OR_GREATER
        /// <summary>
        /// Item 实例的 T 引用
        /// </summary>
        private readonly ref readonly T reference;

        /// <summary>
        /// 当前 Item 实例的索引
        /// </summary>
        private readonly int index;
#else
        /// <summary>
        /// 源 ReadOnlySpan{T} 实例
        /// </summary>
        private readonly ReadOnlySpan<T> span;
#endif

#if NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// 初始化 Item 结构体的新实例
        /// </summary>
        /// <param name="value">指向目标值的引用</param>
        /// <param name="index">目标值的索引</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Item(ref T value, int index)
        {
#if NET8_0_OR_GREATER
            this.reference = ref value;
            this.index = index;
#else
            this.span = MemoryMarshal.CreateReadOnlySpan(ref value, index);
#endif
        }
#else
        /// <summary>
        /// 当前在 span 中的索引
        /// </summary>
        private readonly int index;

        /// <summary>
        /// 初始化 Item 结构体的新实例
        /// </summary>
        /// <param name="span">源 ReadOnlySpan{T} 实例</param>
        /// <param name="index">span 中的当前索引</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Item(ReadOnlySpan<T> span, int index)
        {
            this.span = span;
            this.index = index;
        }
#endif

        /// <summary>
        /// 获取对当前值的引用
        /// </summary>
        public ref readonly T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if NET8_0_OR_GREATER
                return ref this.reference;
#elif NETSTANDARD2_1_OR_GREATER
                return ref MemoryMarshal.GetReference(this.span);
#else
                ref T r0 = ref MemoryMarshal.GetReference(this.span);
                ref T ri = ref Unsafe.Add(ref r0, (nint)(uint)this.index);

                return ref ri;
#endif
            }
        }

        /// <summary>
        /// 获取当前索引
        /// </summary>
        public int Index
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if NET8_0_OR_GREATER
                return this.index;
#elif NETSTANDARD2_1_OR_GREATER
                return this.span.Length;
#else
                return this.index;
#endif
            }
        }
    }
}