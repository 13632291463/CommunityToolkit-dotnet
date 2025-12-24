// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Collections.Generic;

/// <summary>
/// 为 Dictionary2<TKey,TValue> 实例提供键类型逆变访问的接口。
/// </summary>
/// <typeparam name="TKey">字典中键的逆变类型。</typeparam>
internal interface IDictionary2<in TKey> : IDictionary2
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// 尝试移除具有指定键的值（如果存在）。
    /// </summary>
    /// <param name="key">要移除的值的键。</param>
    /// <returns>键是否存在。</returns>
    bool TryRemove(TKey key);
}