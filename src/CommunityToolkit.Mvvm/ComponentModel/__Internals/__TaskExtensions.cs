// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CommunityToolkit.Mvvm.ComponentModel.__Internals;

/// <summary>
/// 一个内部帮助类，用于支持 <see cref="ObservableObject"/> 和从其模板生成的代码。
/// 此类型不打算由用户代码直接使用。
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[Obsolete("This type is not intended to be used directly by user code")]
public static class __TaskExtensions
{
    /// <summary>
    /// 获取一个跳过结束验证的可等待对象。
    /// </summary>
    /// <param name="task">要为其获取可等待对象的输入 <see cref="Task"/>。</param>
    /// <returns>包装 <paramref name="task"/> 的 <see cref="TaskAwaitableWithoutEndValidation"/> 对象。</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("This method is not intended to be called directly by user code")]
    public static TaskAwaitableWithoutEndValidation GetAwaitableWithoutEndValidation(this Task task)
    {
        return new(task);
    }

    /// <summary>
    /// 一个跳过结束验证的自定义任务可等待对象。
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("This type is not intended to be called directly by user code")]
    public readonly struct TaskAwaitableWithoutEndValidation
    {
        /// <summary>
        /// 要为其创建等待器的包装 <see cref="Task"/> 实例。
        /// </summary>
        private readonly Task task;

        /// <summary>
        /// 使用指定参数创建新的 <see cref="TaskAwaitableWithoutEndValidation"/> 实例。
        /// </summary>
        /// <param name="task">要为其创建等待器的包装 <see cref="Task"/> 实例。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TaskAwaitableWithoutEndValidation(Task task)
        {
            this.task = task;
        }

        /// <summary>
        /// 为当前底层任务获取 <see cref="Awaiter"/> 实例。
        /// </summary>
        /// <returns>当前底层任务的 <see cref="Awaiter"/> 实例。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Awaiter GetAwaiter()
        {
            return new(this.task);
        }

        /// <summary>
        /// <see cref="TaskAwaitableWithoutEndValidation"/> 的等待器对象。
        /// </summary>
        public readonly struct Awaiter : ICriticalNotifyCompletion
        {
            /// <summary>
            /// 底层 <see cref="TaskAwaiter"/> 实例。
            /// </summary>
            private readonly TaskAwaiter taskAwaiter;

            /// <summary>
            /// 使用指定参数创建新的 <see cref="Awaiter"/> 实例。
            /// </summary>
            /// <param name="task">要为其创建等待器的包装 <see cref="Task"/> 实例。</param>
            public Awaiter(Task task)
            {
                this.taskAwaiter = task.GetAwaiter();
            }

            /// <summary>
            /// 获取操作是否已完成。
            /// </summary>
            /// <remarks>此属性供编译器用户使用，而不是在代码中直接使用。</remarks>
            public bool IsCompleted
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => this.taskAwaiter.IsCompleted;
            }

            /// <summary>
            /// 结束等待操作。
            /// </summary>
            /// <remarks>此方法供编译器用户使用，而不是在代码中直接使用。</remarks>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void GetResult()
            {
            }

            /// <inheritdoc/>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnCompleted(Action continuation)
            {
                this.taskAwaiter.OnCompleted(continuation);
            }

            /// <inheritdoc/>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void UnsafeOnCompleted(Action continuation)
            {
                this.taskAwaiter.UnsafeOnCompleted(continuation);
            }
        }
    }
}