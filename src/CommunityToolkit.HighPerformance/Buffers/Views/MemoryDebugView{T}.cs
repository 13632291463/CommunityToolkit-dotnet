// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace CommunityToolkit.HighPerformance.Buffers.Views;

/// <summary>
/// 用于以一维布局显示项的调试代理类
/// </summary>
/// <typeparam name="T">要显示的项的类型</typeparam>
internal sealed class MemoryDebugView<T>
{
    /// <summary>
    /// 使用指定参数初始化 MemoryDebugView 类的新实例
    /// </summary>
    /// <param name="arrayPoolBufferWriter">包含要显示项的输入 ArrayPoolBufferWriter 实例</param>
    public MemoryDebugView(ArrayPoolBufferWriter<T>? arrayPoolBufferWriter)
    {
        this.Items = arrayPoolBufferWriter?.WrittenSpan.ToArray();
    }

    /// <summary>
    /// 使用指定参数初始化 MemoryDebugView 类的新实例
    /// </summary>
    /// <param name="memoryBufferWriter">包含要显示项的输入 MemoryBufferWriter 实例</param>
    public MemoryDebugView(MemoryBufferWriter<T>? memoryBufferWriter)
    {
        this.Items = memoryBufferWriter?.WrittenSpan.ToArray();
    }

    /// <summary>
    /// 使用指定参数初始化 MemoryDebugView 类的新实例
    /// </summary>
    /// <param name="memoryOwner">包含要显示项的输入 MemoryOwner 实例</param>
    public MemoryDebugView(MemoryOwner<T>? memoryOwner)
    {
        this.Items = memoryOwner?.Span.ToArray();
    }

    /// <summary>
    /// 使用指定参数初始化 MemoryDebugView 类的新实例
    /// </summary>
    /// <param name="spanOwner">包含要显示项的输入 SpanOwner 实例</param>
    public MemoryDebugView(SpanOwner<T> spanOwner)
    {
        this.Items = spanOwner.Span.ToArray();
    }

    /// <summary>
    /// 获取当前实例要显示的项
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
    public T[]? Items { get; }
}