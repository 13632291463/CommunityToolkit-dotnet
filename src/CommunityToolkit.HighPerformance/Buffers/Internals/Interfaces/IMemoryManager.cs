// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;

namespace CommunityToolkit.HighPerformance.Buffers.Internals.Interfaces;

/// <summary>
/// 一个用于 MemoryManager{T} 实例的接口，它可以重新解释其底层数据。
/// </summary>
internal interface IMemoryManager
{
    /// <summary>
    /// 创建一个新的 Memory{T}，它重新解释当前实例的底层数据。
    /// </summary>
    /// <typeparam name="T">要将项转换到的目标类型。</typeparam>
    /// <param name="offset">数据存储中的起始偏移量。</param>
    /// <param name="length">数据存储的原始使用长度。</param>
    /// <returns>一个指定类型的新 Memory{T} 实例，重新解释当前项。</returns>
    Memory<T> GetMemory<T>(int offset, int length)
        where T : unmanaged;
}