// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace System;

/// <summary>
/// 每次第2代GC时调度回调（您可能会看到第0代和第1代，但只有一次）。
/// 来自 https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Gen2GcCallback.cs.
/// </summary>
internal sealed class Gen2GcCallback : CriticalFinalizerObject
{
    /// <summary>
    /// 在每次GC时调用的回调。
    /// </summary>
    private readonly Action<object> callback;

    /// <summary>
    /// 对要传递给<see cref="callback"/>的目标对象的弱<see cref="GCHandle"/>引用。
    /// </summary>
    private GCHandle handle;

    /// <summary>
    /// 初始化<see cref="Gen2GcCallback"/>类的新实例。
    /// </summary>
    /// <param name="callback">在每次GC时调用的回调。</param>
    /// <param name="target">作为参数传递给<paramref name="callback"/>的目标对象。</param>
    private Gen2GcCallback(Action<object> callback, object target)
    {
        this.callback = callback;
        this.handle = GCHandle.Alloc(target, GCHandleType.Weak);
    }

    /// <summary>
    /// 注册一个回调，在每次GC时调用，直到目标对象被回收。
    /// </summary>
    /// <param name="callback">在每次GC时调用的回调。</param>
    /// <param name="target">作为参数传递给<paramref name="callback"/>的目标对象。</param>
    public static void Register(Action<object> callback, object target)
    {
#if NETSTANDARD2_0
        // 检测是否为.NET Framework运行时
        if (RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework"))
        {
            // 在.NET Framework上使用GC回调会导致应用程序域卸载问题，
            // 因此如果检测到该运行时，则不注册回调并忽略。
            // .NET Framework上的用户需要手动清理消息传递器。
            return;
        }
#endif

        _ = new Gen2GcCallback(callback, target);
    }

    /// <summary>
    /// 析构<see cref="Gen2GcCallback"/>类的实例。
    /// 当目标对象仍然存活时，此终结器会使用<see cref="GC.ReRegisterForFinalize(object)"/>重新注册，
    /// 这意味着只要触发第2代垃圾回收就会再次执行（因为<see cref="Gen2GcCallback"/>实例本身
    /// 在第一次经过第0代和第1代回收后仍然存活，将被移到第2代）。
    /// </summary>
    ~Gen2GcCallback()
    {
        // 检查目标对象是否仍然存活
        if (this.handle.Target is object target)
        {
            try
            {
                // 调用回调函数，传入目标对象
                this.callback(target);
            }
            catch
            {
                // 忽略回调中可能发生的异常
            }

            // 重新注册当前实例以在下次GC时再次触发终结器
            GC.ReRegisterForFinalize(this);
        }
        else
        {
            // 目标对象已被回收，释放句柄
            this.handle.Free();
        }
    }
}