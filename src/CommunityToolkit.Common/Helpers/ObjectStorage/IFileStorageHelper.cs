// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace CommunityToolkit.Common.Helpers;

/// <summary>
/// 用于通过文件和文件夹在目录/文件系统中存储数据的服务接口。
///
/// 此接口旨在帮助在库中抽象跨平台的文件存储操作，
/// 但实际行为将由实现者决定。例如，我们不提供当前目录的概念，
/// 因此实现者应考虑使用完整路径来支持任何文件操作。否则，"目录感知"
/// 的实现可以通过当前目录字段和遍历函数来实现，在这种情况下相对路径是适用的。
/// </summary>
public interface IFileStorageHelper
{
    /// <summary>
    /// 从文件中检索对象。
    /// </summary>
    /// <typeparam name="T">检索对象的类型。</typeparam>
    /// <param name="filePath">包含对象的文件路径。</param>
    /// <param name="default">对象的默认值。</param>
    /// <returns>完成前的等待任务，包含文件中的对象。</returns>
    Task<T?> ReadFileAsync<T>(string filePath, T? @default = default);

    /// <summary>
    /// 检索文件夹的列表和项目类型。
    /// </summary>
    /// <param name="folderPath">目标文件夹的路径。</param>
    /// <returns>目标文件夹中项目类型和名称的列表。</returns>
    Task<IEnumerable<(DirectoryItemType ItemType, string Name)>> ReadFolderAsync(string folderPath);

    /// <summary>
    /// 在文件中保存对象。
    /// </summary>
    /// <typeparam name="T">保存对象的类型。</typeparam>
    /// <param name="filePath">将包含对象的文件路径。</param>
    /// <param name="value">要保存的对象。</param>
    /// <returns>完成前的等待任务。</returns>
    Task CreateFileAsync<T>(string filePath, T value);

    /// <summary>
    /// 确保在指定的文件夹路径存在文件夹。
    /// </summary>
    /// <param name="folderPath">目标文件夹的路径和名称。</param>
    /// <returns>完成前的等待任务。</returns>
    Task CreateFolderAsync(string folderPath);

    /// <summary>
    /// 删除文件或文件夹项目。
    /// </summary>
    /// <param name="itemPath">要删除项目的路径。</param>
    /// <returns>完成前的等待任务。</returns>
    Task<bool> TryDeleteItemAsync(string itemPath);

    /// <summary>
    /// 重命名项目。
    /// </summary>
    /// <param name="itemPath">目标项目的路径。</param>
    /// <param name="newName">目标项目的新名称。</param>
    /// <returns>完成前的等待任务。</returns>
    Task<bool> TryRenameItemAsync(string itemPath, string newName);
}