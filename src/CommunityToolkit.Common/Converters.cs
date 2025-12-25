// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Common;

/// <summary>
/// Set of helpers to convert between data types and notations.
/// </summary>
public static class Converters
{
    /// <summary>
    /// Translate numeric file size in bytes to a human-readable shorter string format.
    /// </summary>
    /// <param name="size">File size in bytes.</param>
    /// <returns>Returns file size short string.</returns>
    public static string ToFileSizeString(long size)
    {
        // 检查文件大小是否小于1KB，如果是则直接返回字节数
        if (size < 1024)
        {
            return size.ToString("F0") + " bytes";
        }
        // 检查文件大小是否小于1MB，如果是则返回KB单位
        else if ((size >> 10) < 1024)
        {
            return (size / 1024F).ToString("F1") + " KB";
        }
        // 检查文件大小是否小于1GB，如果是则返回MB单位
        else if ((size >> 20) < 1024)
        {
            return ((size >> 10) / 1024F).ToString("F1") + " MB";
        }
        // 检查文件大小是否小于1TB，如果是则返回GB单位
        else if ((size >> 30) < 1024)
        {
            return ((size >> 20) / 1024F).ToString("F1") + " GB";
        }
        // 检查文件大小是否小于1PB，如果是则返回TB单位
        else if ((size >> 40) < 1024)
        {
            return ((size >> 30) / 1024F).ToString("F1") + " TB";
        }
        // 检查文件大小是否小于1EB，如果是则返回PB单位
        else if ((size >> 50) < 1024)
        {
            return ((size >> 40) / 1024F).ToString("F1") + " PB";
        }
        // 文件大小大于或等于1EB，返回EB单位
        else
        {
            return ((size >> 50) / 1024F).ToString("F1") + " EB";
        }
    }
}