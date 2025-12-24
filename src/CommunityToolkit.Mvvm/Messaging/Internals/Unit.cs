// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

namespace CommunityToolkit.Mvvm.Messaging.Internals;

/// <summary>
/// 一个空类型，表示一个没有特定值的通用令牌
/// </summary>
internal readonly struct Unit : IEquatable<Unit>
{
    /// <summary>
    /// 检查当前实例是否与另一个Unit实例相等
    /// </summary>
    /// <param name="other">要与当前实例比较的Unit实例</param>
    /// <returns>始终返回true，因为所有Unit实例都被认为是相等的</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Unit other)
    {
        return true;
    }

    /// <summary>
    /// 检查当前实例是否与指定对象相等
    /// </summary>
    /// <param name="obj">要与当前实例比较的对象</param>
    /// <returns>如果对象是Unit类型则返回true，否则返回false</returns>
    public override bool Equals(object? obj)
    {
        return obj is Unit;
    }

    /// <summary>
    /// 获取当前实例的哈希代码
    /// </summary>
    /// <returns>始终返回0</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        return 0;
    }
}