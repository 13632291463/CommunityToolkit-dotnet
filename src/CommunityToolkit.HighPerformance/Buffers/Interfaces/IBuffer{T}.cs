// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;

namespace CommunityToolkit.HighPerformance.Buffers;

/// <summary>
/// 一个接口，扩展了 <see cref="IBufferWriter{T}"/> 的功能，增加了检查已写入数据的能力，
/// 并可以重置底层缓冲区以从头开始再次写入。
/// </summary>
/// <typeparam name="T">当前缓冲区中元素的类型。</typeparam>
public interface IBuffer<T> : IBufferWriter<T>
{
    /// <summary>
    /// 获取到目前为止写入底层缓冲区的数据，作为 <see cref="ReadOnlyMemory{T}"/> 返回。
    /// </summary>
    ReadOnlyMemory<T> WrittenMemory { get; }

    /// <summary>
    /// 获取到目前为止写入底层缓冲区的数据，作为 <see cref="ReadOnlySpan{T}"/> 返回。
    /// </summary>
    ReadOnlySpan<T> WrittenSpan { get; }

    /// <summary>
    /// 获取到目前为止写入底层缓冲区的数据量。
    /// </summary>
    int WrittenCount { get; }

    /// <summary>
    /// 获取底层缓冲区内的总空间大小。
    /// </summary>
    int Capacity { get; }

    /// <summary>
    /// 获取仍可写入而不会强制底层缓冲区增长的空间量。
    /// </summary>
    int FreeCapacity { get; }

    /// <summary>
    /// 清除写入底层缓冲区的数据。
    /// </summary>
    /// <remarks>
    /// 在尝试重新使用 <see cref="IBuffer{T}"/> 实例之前，必须先清除它。
    /// </remarks>
    void Clear();
}