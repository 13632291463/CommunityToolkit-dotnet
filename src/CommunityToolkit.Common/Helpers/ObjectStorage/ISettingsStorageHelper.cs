// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Helpers;

/// <summary>
/// 用于使用键值对存储数据的服务接口。
/// </summary>
/// <typeparam name="TKey">用于访问值的键的类型。</typeparam>
public interface ISettingsStorageHelper<in TKey>
    where TKey : notnull
{
    /// <summary>
    /// 通过键检索单个项目。
    /// </summary>
    /// <typeparam name="TValue">检索对象的类型。</typeparam>
    /// <param name="key">对象的键。</param>
    /// <param name="value">对应于键的<see typeparamref="TValue"/>对象。</param>
    /// <returns>表示操作是否成功的布尔值指示器。</returns>
    bool TryRead<TValue>(TKey key, out TValue? value);

    /// <summary>
    /// 通过键保存单个项目。
    /// </summary>
    /// <typeparam name="TValue">保存对象的类型。</typeparam>
    /// <param name="key">保存值的键。</param>
    /// <param name="value">要保存的对象。</param>
    void Save<TValue>(TKey key, TValue value);

    /// <summary>
    /// 通过键删除单个项目。
    /// </summary>
    /// <param name="key">对象的键。</param>
    /// <returns>表示操作是否成功的布尔值指示器。</returns>
    bool TryDelete(TKey key);

    /// <summary>
    /// 清空设置存储中的所有键和值。
    /// </summary>
    void Clear();
}