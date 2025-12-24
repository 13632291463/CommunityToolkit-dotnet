// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Collections.Generic;

/// <summary>
/// 一个基础接口，用于屏蔽 <see cref="Dictionary2{TKey,TValue}"/> 实例并公开非泛型功能。
/// 此接口提供对字典基本操作的抽象访问，不依赖于具体的键值类型
/// </summary>
internal interface IDictionary2
{
    /// <summary>
    /// 获取字典中条目的数量
    /// </summary>
    /// <returns>字典中键值对的数量</returns>
    int Count { get; }

    /// <summary>
    /// 清空当前字典中的所有条目
    /// </summary>
    /// <remarks>
    /// 此方法将移除字典中的所有键值对，使字典变为空状态
    /// </remarks>
    void Clear();
}