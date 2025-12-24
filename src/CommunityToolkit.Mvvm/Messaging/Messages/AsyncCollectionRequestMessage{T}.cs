// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CA1001

namespace CommunityToolkit.Mvvm.Messaging.Messages;

/// <summary>
/// 一个用于可以接收多个回复的请求消息的类，可以直接使用或通过派生类使用。
/// </summary>
/// <typeparam name="T">请求的类型。</typeparam>
public class AsyncCollectionRequestMessage<T> : IAsyncEnumerable<T>
{
    /// <summary>
    /// 接收到的回复集合。我们接受 <see cref="Task{TResult}"/> 实例，代表已经运行的
    /// 可以并行执行的操作，或 <see cref="Func{T,TResult}"/> 实例，可用于从 <see cref="GetAsyncEnumerator"/> 
    /// 中顺序启动多个异步操作，且不会在时间上重叠。
    /// </summary>
    private readonly List<(Task<T>?, Func<CancellationToken, Task<T>>?)> responses = new();

    /// <summary>
    /// 用于将传递给 <see cref="GetAsyncEnumerator"/> 的令牌与传递给消息所有订阅者的令牌链接的
    /// <see cref="CancellationTokenSource"/> 实例。
    /// </summary>
    private readonly CancellationTokenSource cancellationTokenSource = new();

    /// <summary>
    /// 获取将与异步枚举接收到的响应的令牌链接的 <see cref="System.Threading.CancellationToken"/> 实例。
    /// 这可用于取消仍在处理的异步回复，如果此请求消息不再需要新项目。
    /// 请考虑以下示例，我们定义一个消息来检索当前打开的文档：
    /// <code>
    /// public class OpenDocumentsRequestMessage : AsyncCollectionRequestMessage&lt;XmlDocument&gt; { }
    /// </code>
    /// 然后我们可以请求并枚举结果，如下所示：
    /// <code>
    /// await foreach (var document in Messenger.Default.Send&lt;OpenDocumentsRequestMessage&gt;())
    /// {
    ///     // 在这里处理每个文档...
    /// }
    /// </code>
    /// 如果我们还想控制传递给消息订阅者的令牌，我们可以通过在开始枚举之前向返回的消息传递我们控制的令牌来实现
    /// (<see cref="TaskAsyncEnumerableExtensions.WithCancellation{T}(IAsyncEnumerable{T},CancellationToken)"/>)。
    /// 带有此额外更改的前面的代码片段如下所示：
    /// <code>
    /// await foreach (var document in Messenger.Default.Send&lt;OpenDocumentsRequestMessage&gt;().WithCancellation(cts.Token))
    /// {
    ///     // 在这里处理每个文档...
    /// }
    /// </code>
    /// 当不再需要新项目（或根据情况的任何其他原因）时，可以取消传递给枚举器的令牌（通过调用 <see cref="CancellationTokenSource.Cancel()"/>），
    /// 这也将通知请求消息中的剩余任务。消息本身公开的令牌将自动链接并与传递给枚举器的令牌一起取消。
    /// </summary>
    public CancellationToken CancellationToken => this.cancellationTokenSource.Token;

    /// <summary>
    /// 回复当前请求消息。
    /// </summary>
    /// <param name="response">用于回复请求消息的响应。</param>
    public void Reply(T response)
    {
        Reply(Task.FromResult(response));
    }

    /// <summary>
    /// 回复当前请求消息。
    /// </summary>
    /// <param name="response">用于回复请求消息的响应。</param>
    /// <exception cref="System.ArgumentNullException">当 <paramref name="response"/> 为 <see langword="null"/> 时抛出。</exception>
    public void Reply(Task<T> response)
    {
        ArgumentNullException.ThrowIfNull(response);

        this.responses.Add((response, null));
    }

    /// <summary>
    /// 回复当前请求消息。
    /// </summary>
    /// <param name="response">用于回复请求消息的响应。</param>
    /// <exception cref="System.ArgumentNullException">当 <paramref name="response"/> 为 <see langword="null"/> 时抛出。</exception>
    public void Reply(Func<CancellationToken, Task<T>> response)
    {
        ArgumentNullException.ThrowIfNull(response);

        this.responses.Add((null, response));
    }

    /// <summary>
    /// 获取接收到的响应项集合。
    /// </summary>
    /// <param name="cancellationToken">用于停止操作的 <see cref="System.Threading.CancellationToken"/> 值。</param>
    /// <returns>接收到的响应项集合。</returns>
    public async Task<IReadOnlyCollection<T>> GetResponsesAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.CanBeCanceled)
        {
            _ = cancellationToken.Register(this.cancellationTokenSource.Cancel);
        }

        List<T> results = new(this.responses.Count);

        await foreach (T? response in this.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            results.Add(response);
        }

        return results;
    }

    /// <summary>
    /// 获取异步枚举器以遍历响应集合。
    /// </summary>
    /// <param name="cancellationToken">用于控制操作取消的 <see cref="CancellationToken"/>。</param>
    /// <returns>返回响应集合的异步枚举器。</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.CanBeCanceled)
        {
            _ = cancellationToken.Register(this.cancellationTokenSource.Cancel);
        }

        // 遍历所有响应，根据响应类型执行不同的处理
        foreach ((Task<T>? task, Func<CancellationToken, Task<T>>? func) in this.responses)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            // 如果是Task类型的响应，等待其完成
            if (task is not null)
            {
                yield return await task.ConfigureAwait(false);
            }
            // 如果是Func类型的响应，使用提供的取消令牌执行函数
            else
            {
                yield return await func!(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}