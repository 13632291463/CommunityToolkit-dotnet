// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CommunityToolkit.Common.Collections;

/// <summary>
/// 这个接口表示一个可以进行增量加载的数据源。
/// </summary>
/// <typeparam name="TSource">集合元素的类型。</typeparam>
public interface IIncrementalSource<TSource>
{
    /// <summary>
    /// 这个方法在视图需要显示更多项目时被调用。根据 <paramref name="pageIndex"/> 和 <paramref name="pageSize"/> 参数检索项目。
    /// </summary>
    /// <param name="pageIndex">
    /// 对应要检索项目的页面的从零开始的索引。
    /// </param>
    /// <param name="pageSize">
    /// 为指定的 <paramref name="pageIndex"/> 检索的 <typeparamref name="TSource"/> 项的数量。
    /// </param>
    /// <param name="cancellationToken">
    /// 用于传播操作应被取消的通知。
    /// </param>
    /// <returns>
    /// 返回 <typeparamref name="TSource"/> 的集合。
    /// </returns>
    Task<IEnumerable<TSource>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default);
}