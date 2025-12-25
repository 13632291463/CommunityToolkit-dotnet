// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommunityToolkit.Common;

/// <summary>
/// 数组操作辅助类
/// </summary>
public static class ArrayExtensions
{
    /// <summary>
    /// 从锯齿数组中获取指定列的元素
    /// 如果列索引超出范围，将抛出异常；对于没有元素的位置返回默认值
    /// 注意：没有等效的 GetRow 方法，因为可以使用 array[row] 来获取行
    /// </summary>
    /// <typeparam name="T">数组元素类型</typeparam>
    /// <param name="rectarray">源数组</param>
    /// <param name="column">要获取的列记录，从0开始的索引</param>
    /// <returns>给定列的元素枚举，对于较小的内部数组使用默认值</returns>
    public static IEnumerable<T?> GetColumn<T>(this T?[][] rectarray, int column)
    {
        if (column < 0 || column >= rectarray.Max(array => array.Length))
        {
            throw new ArgumentOutOfRangeException(nameof(column));
        }

        for (int r = 0; r < rectarray.GetLength(0); r++)
        {
            if (column >= rectarray[r].Length)
            {
                yield return default;

                continue;
            }

            yield return rectarray[r][column];
        }
    }

    /// <summary>
    /// 返回数组的字符串表示
    /// </summary>
    /// <typeparam name="T">数组元素类型</typeparam>
    /// <param name="array">源数组</param>
    /// <returns>数组的字符串表示</returns>
    public static string ToArrayString<T>(this T?[] array)
    {
        // 返回的字符串将采用以下格式:
        // [1, 2, 3]
        StringBuilder builder = new();

        _ = builder.Append('[');

        for (int i = 0; i < array.Length; i++)
        {
            if (i != 0)
            {
                _ = builder.Append(",\t");
            }

            _ = builder.Append(array[i]?.ToString());
        }

        _ = builder.Append(']');

        return builder.ToString();
    }

    /// <summary>
    /// 返回锯齿数组的字符串表示
    /// </summary>
    /// <typeparam name="T">数组元素类型</typeparam>
    /// <param name="mdarray">源数组</param>
    /// <returns>数组的字符串表示</returns>
    public static string ToArrayString<T>(this T?[][] mdarray)
    {
        // 返回的字符串使用与2D数组重载相同的格式
        StringBuilder builder = new();

        _ = builder.Append('[');

        for (int i = 0; i < mdarray.Length; i++)
        {
            if (i != 0)
            {
                _ = builder.Append(',');
                _ = builder.Append(Environment.NewLine);
                _ = builder.Append(' ');
            }

            _ = builder.Append('[');

            T?[] row = mdarray[i];

            for (int j = 0; j < row.Length; j++)
            {
                if (j != 0)
                {
                    _ = builder.Append(",\t");
                }

                _ = builder.Append(row[j]?.ToString());
            }

            _ = builder.Append(']');
        }

        _ = builder.Append(']');

        return builder.ToString();
    }

    /// <summary>
    /// 返回二维数组的字符串表示
    /// </summary>
    /// <typeparam name="T">数组元素类型</typeparam>
    /// <param name="array">源数组</param>
    /// <returns>数组的字符串表示</returns>
    public static string ToArrayString<T>(this T?[,] array)
    {
        // 返回的字符串将采用以下格式:
        // [[1, 2,  3],
        //  [4, 5,  6],
        //  [7, 8,  9]]
        StringBuilder builder = new();

        _ = builder.Append('[');

        int height = array.GetLength(0);
        int width = array.GetLength(1);

        for (int i = 0; i < height; i++)
        {
            if (i != 0)
            {
                _ = builder.Append(',');
                _ = builder.Append(Environment.NewLine);
                _ = builder.Append(' ');
            }

            _ = builder.Append('[');

            for (int j = 0; j < width; j++)
            {
                if (j != 0)
                {
                    _ = builder.Append(",\t");
                }

                _ = builder.Append(array[i, j]?.ToString());
            }

            _ = builder.Append(']');
        }

        _ = builder.Append(']');

        return builder.ToString();
    }
}