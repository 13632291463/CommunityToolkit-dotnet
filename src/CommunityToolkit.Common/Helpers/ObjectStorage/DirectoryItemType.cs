// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Common.Helpers;

/// <summary>
/// 表示目录中可用的项目类型。
/// </summary>
public enum DirectoryItemType
{
    /// <summary>
    /// 该项目既不是文件也不是文件夹。
    /// </summary>
    None,

    /// <summary>
    /// 表示文件类型的项目。
    /// </summary>
    File,

    /// <summary>
    /// 表示文件夹类型的项目。
    /// </summary>
    Folder
}