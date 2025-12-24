// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Collections.Generic;

/// <summary>
/// 一个接口，提供对 Dictionary2<TKey,TValue> 实例的键类型逆变和值类型协变访问
/// </summary>
/// <typeparam name="TKey">字典中键的逆变类型。</typeparam>
/// <typeparam name="TValue">字典中值的协变类型。</typeparam>
internal interface IDictionary2<in TKey, out TValue> : IDictionary2<TKey>
    where TKey : IEquatable<TKey>
    where TValue : class?
{
    /// <summary>
    /// 获取具有指定键的值
    /// </summary>
    /// <param name="key">要查找的键。</param>
    /// <returns>返回的值。</returns>
    /// <exception cref="ArgumentException">如果键不存在则抛出。</exception>
    TValue this[TKey key] { get; }
}