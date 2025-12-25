// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CommunityToolkit.Common;

/// <summary>
/// 用于处理任务的帮助类。
/// </summary>
public static class TaskExtensions
{
    /// <summary>
    /// 如果可用，则获取 <see cref="Task"/> 的结果，否则返回 <see langword="null"/>。
    /// </summary>
    /// <param name="task">要获取结果的输入 <see cref="Task"/> 实例。</param>
    /// <returns>如果 task 成功完成，则返回 <paramref name="task"/> 的结果，否则返回 <see langword="default"/>。</returns>
    /// <remarks>
    /// 此方法不会在 <paramref name="task"/> 尚未完成时阻塞。此外，它不是泛型的，
    /// 并使用反射访问 <see cref="Task{TResult}.Result"/> 属性，如果结果是值类型，则会装箱，这会增加开销。
    /// 只有在无法使用泛型时才应使用它。
    /// </remarks>
#if NET6_0_OR_GREATER
	// "此方法使用反射来尝试访问输入 Task 实例的 Task<T>.Result 属性。"
    [RequiresUnreferencedCode("This method uses reflection to try to access the Task<T>.Result property of the input Task instance.")]
#endif
    public static object? GetResultOrDefault(this Task task)
    {
        // 检查实例是否为已完成的 Task
        if (
#if NETSTANDARD2_1
            task.IsCompletedSuccessfully
#else
            task.Status == TaskStatus.RanToCompletion
#endif
            )
        {
            // 我们需要显式检查以确保输入任务不是缓存的
            // Task.CompletedTask 实例，因为这在内部可以存储为
            // Task<T>（例如，在 .NET 6 上是 VoidTaskResult），这
            // 会导致以下代码返回该结果而不是 null。
            if (task != Task.CompletedTask)
            {
                // 尝试获取 Task<T>.Result 属性。此方法无论如何
                // 都会在类型检查后被调用，但使用它
                // 来验证输入类型可以节省一些额外的反射调用。
                // 此外，这样做还使该方法足够灵活
                // 以处理输入 Task<T> 实际上是某些
                // 继承自 Task<T> 的特定运行时类型实例的情况。
                PropertyInfo? propertyInfo = task.GetType().GetProperty(nameof(Task<object>.Result));

                // 返回结果（如果可能）
                return propertyInfo?.GetValue(task);
            }
        }

        return null;
    }

    /// <summary>
    /// 如果可用，则获取 <see cref="Task{TResult}"/> 的结果，否则返回 <see langword="default"/>。
    /// </summary>
    /// <typeparam name="T">要获取结果的 <see cref="Task{TResult}"/> 类型。</typeparam>
    /// <param name="task">要获取结果的输入 <see cref="Task{TResult}"/> 实例。</param>
    /// <returns>如果 task 成功完成，则返回 <paramref name="task"/> 的结果，否则返回 <see langword="default"/>。</returns>
    /// <remarks>此方法不会在 <paramref name="task"/> 尚未完成时阻塞。</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? GetResultOrDefault<T>(this Task<T?> task)
    {
#if NETSTANDARD2_1
        return task.IsCompletedSuccessfully ? task.Result : default;
#else
        return task.Status == TaskStatus.RanToCompletion ? task.Result : default;
#endif
    }
}