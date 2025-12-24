// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

namespace CommunityToolkit.Mvvm.Messaging.Internals;

/// <summary>
/// 表示不可变类型对的简单类型
/// </summary>
/// <remarks>
/// 此类型替换简单的 <see cref="ValueTuple{T1,T2}"/>，因为它在
/// <see cref="GetHashCode"/> 和 <see cref="IEquatable{T}.Equals(T)"/> 方法中更快，
/// 而且与值元组不同的是，它将其字段公开为不可变的。此外，
/// <see cref="TMessage"/> 和 <see cref="TToken"/> 字段提供了比
/// <see cref="ValueTuple{T1,T2}.Item1"/> 和 <see cref="ValueTuple{T1,T2}.Item2"/> 更清晰的代码阅读体验
/// </remarks>
internal readonly struct Type2 : IEquatable<Type2>
{
    /// <summary>
    /// 注册消息的类型
    /// </summary>
    public readonly Type TMessage;

    /// <summary>
    /// 注册令牌的类型
    /// </summary>
    public readonly Type TToken;

    /// <summary>
    /// 初始化 <see cref="Type2"/> 结构的新实例
    /// </summary>
    /// <param name="tMessage">注册消息的类型</param>
    /// <param name="tToken">注册令牌的类型</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Type2(Type tMessage, Type tToken)
    {
        this.TMessage = tMessage;
        this.TToken = tToken;
    }

    /// <summary>
    /// 确定当前实例是否等于另一个 Type2 实例
    /// </summary>
    /// <param name="other">要与当前实例比较的 Type2 实例</param>
    /// <returns>如果当前实例等于 other 参数，则为 true；否则为 false</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Type2 other)
    {
        // 我们不能只使用引用相等，因为从技术上讲这不保证有效
        // 并且在极少数情况下可能会失败（例如，在不同程序集之间进行类型转发时）。
        // 相反，我们可以使用 == 运算符来比较相等性，
        // 这仍然避免了调用 Type.Equals 的 callvirt 开销，
        // 在 .NET Core 等运行时中也作为 JIT 内在函数实现
        return
            this.TMessage == other.TMessage &&
            this.TToken == other.TToken;
    }

    /// <summary>
    /// 确定指定对象是否等于当前实例
    /// </summary>
    /// <param name="obj">要与当前实例比较的对象</param>
    /// <returns>如果指定对象等于当前实例，则为 true；否则为 false</returns>
    public override bool Equals(object? obj)
    {
        return obj is Type2 other && Equals(other);
    }

    /// <summary>
    /// 返回当前实例的哈希代码
    /// </summary>
    /// <returns>当前实例的哈希代码</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        // 要组合两个哈希值，我们可以简单地使用快速 djb2 哈希算法。不幸的是
        // 我们无法在此处跳过 callvirt（例如使用 RuntimeHelpers.GetHashCode，
        // 如在其他情况下），因为如上所述的一些边缘情况可能会导致破坏问题。
        // 但是，由于此方法通常不在热路径中使用（例如，消息广播
        // 仅在最初检索目标映射时调用几次），因此调用虚方法的轻微开销
        // 实际上不会造成可察觉的差异
        int hash = this.TMessage.GetHashCode();

        hash = (hash << 5) + hash;

        hash += this.TToken.GetHashCode();

        return hash;
    }
}