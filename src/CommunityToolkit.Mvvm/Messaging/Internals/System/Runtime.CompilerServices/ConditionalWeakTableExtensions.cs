// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !NET6_0_OR_GREATER

namespace System.Runtime.CompilerServices;

/// <summary>
/// 为 <see cref="ConditionalWeakTable{TKey, TValue}"/> 类型提供辅助方法的扩展类
/// </summary>
internal static class ConditionalWeakTableExtensions
{
    /// <summary>
    /// 尝试向表中添加一个新的键值对
    /// </summary>
    /// <typeparam name="TKey">表中存储项的键类型</typeparam>
    /// <typeparam name="TValue">表中存储的值类型</typeparam>
    /// <param name="table">要修改的输入 <see cref="ConditionalWeakTable{TKey, TValue}"/> 实例</param>
    /// <param name="key">要添加的键</param>
    /// <param name="value">要与键关联的值</param>
    /// <returns>如果键值对成功添加到表中则返回 true，如果键已存在则返回 false</returns>
    public static bool TryAdd<TKey, TValue>(this ConditionalWeakTable<TKey, TValue> table, TKey key, TValue value)
        where TKey : class
        where TValue : class?
    {
        // 在 .NET Standard 2.0 或 2.1 上，除了异常处理外没有其他方式实现此功能
        try
        {
            table.Add(key, value);

            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}

#endif