// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace CommunityToolkit.Common.Helpers;

/// <summary>
/// 一个简单的序列化器，只能处理基本类型和字符串。
/// 建议在更复杂的场景中实现自己的 <see cref="IObjectSerializer"/>，基于 System.Text.Json、Newtonsoft.Json 或 DataContractJsonSerializer，参见 https://aka.ms/wct/storagehelper-migration
/// </summary>
public class SystemSerializer : IObjectSerializer
{
    /// <summary>
    /// 从存储中获取基本类型值并使用 <see cref="Convert.ChangeType(object, Type)"/> API 转换为请求的类型
    /// </summary>
    /// <typeparam name="T">要将值转换为的类型</typeparam>
    /// <param name="value">要转换的存储中的值</param>
    /// <returns>反序列化的值或默认值</returns>
    public T Deserialize<T>(string value)
    {
        // 检查类型是否为基本类型或字符串
        if (typeof(T).IsPrimitive || typeof(T) == typeof(string))
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }

        // 如果不是基本类型或字符串，抛出不支持异常
        throw new NotSupportedException("This serializer can only handle primitive types and strings. Please implement your own IObjectSerializer for more complex scenarios.");
    }

    /// <summary>
    /// 返回值以便可以直接序列化
    /// </summary>
    /// <typeparam name="T">要序列化的类型</typeparam>
    /// <param name="value">要序列化的值</param>
    /// <returns>值的字符串表示形式</returns>
    public string? Serialize<T>(T value)
    {
        // 将值转换为字符串表示形式
        return value?.ToString();
    }
}