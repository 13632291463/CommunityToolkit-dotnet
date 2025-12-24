// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Collections.Generic;

/// <summary>
/// 用于 Dictionary{TKey,TValue} 的辅助类
/// </summary>
internal static class HashHelpers
{
    /// <summary>
    /// 小于最大数组长度的最大质数
    /// </summary>
    private const int MaxPrimeArrayLength = 0x7FFFFFC3;

    /// <summary>
    /// 在 GetPrime 方法中使用的任意质数因子
    /// </summary>
    private const int HashPrime = 101;

    /// <summary>
    /// 用作哈希表大小的质数表
    /// </summary>
    private static readonly int[] primes =
    {
        3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
        1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
        17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
        187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
        1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369
    };

    /// <summary>
    /// 检查一个值是否为质数
    /// </summary>
    /// <param name="candidate">要检查的值</param>
    /// <returns>candidate 是否为质数</returns>
    private static bool IsPrime(int candidate)
    {
        // 检查候选数是否为奇数
        if ((candidate & 1) != 0)
        {
            // 计算平方根作为检查的上限
            int limit = (int)Math.Sqrt(candidate);

            // 从3开始，只检查奇数除数
            for (int divisor = 3; divisor <= limit; divisor += 2)
            {
                if ((candidate % divisor) == 0)
                {
                    return false;
                }
            }

            return true;
        }

        // 唯一的偶数质数是2
        return candidate == 2;
    }

    /// <summary>
    /// 获取比指定值大的最小质数
    /// </summary>
    /// <param name="min">目标最小值</param>
    /// <returns>找到的新质数</returns>
    public static int GetPrime(int min)
    {
        // 首先在预定义的质数表中查找
        foreach (int prime in primes)
        {
            if (prime >= min)
            {
                return prime;
            }
        }

        // 如果质数表中没有足够大的质数，则搜索更大的质数
        for (int i = min | 1; i < int.MaxValue; i += 2)
        {
            // 确保新质数与 HashPrime 相关的特殊条件
            if (IsPrime(i) && ((i - 1) % HashPrime != 0))
            {
                return i;
            }
        }

        return min;
    }

    /// <summary>
    /// 返回哈希表扩展后的大小
    /// </summary>
    /// <param name="oldSize">之前的表大小</param>
    /// <returns>扩展后的表大小</returns>
    public static int ExpandPrime(int oldSize)
    {
        // 将大小翻倍
        int newSize = 2 * oldSize;

        // 检查是否超过最大质数数组长度
        if ((uint)newSize > MaxPrimeArrayLength && MaxPrimeArrayLength > oldSize)
        {
            return MaxPrimeArrayLength;
        }

        return GetPrime(newSize);
    }

    /// <summary>
    /// 返回除数的近似倒数: ceil(2**64 / divisor)
    /// </summary>
    /// <remarks>此方法仅应在64位系统上使用</remarks>
    public static ulong GetFastModMultiplier(uint divisor)
    {
        return ulong.MaxValue / divisor + 1;
    }

    /// <summary>
    /// 使用通过 GetFastModMultiplier 预计算的乘数执行模运算
    /// </summary>
    /// <param name="value">要进行模运算的值</param>
    /// <param name="divisor">除数</param>
    /// <param name="multiplier">通过 GetFastModMultiplier 预计算的乘数</param>
    /// <returns>模运算的结果</returns>
    /// <remarks>此方法仅应在64位系统上使用</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint FastMod(uint value, uint divisor, ulong multiplier)
    {
        // 使用乘法和位移操作来快速计算模运算
        return (uint)(((((multiplier * value) >> 32) + 1) * divisor) >> 32);
    }
}