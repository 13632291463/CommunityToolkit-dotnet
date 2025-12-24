// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Mvvm.Input.Internals;

/// <summary>
/// 一个接口，用于标识命令是否支持取消操作
/// </summary>
internal interface ICancellationAwareCommand
{
    /// <summary>
    /// 获取当前命令是否支持取消操作
    /// </summary>
    bool IsCancellationSupported { get; }
}