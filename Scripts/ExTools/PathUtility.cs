using System.IO;
using ExTools.Singleton;
using ExTools.Utillties;
using LogManager.Core;
using LogManager.LogManagerFactory;
using UnityEditor;
using UnityEngine;

namespace ExTools
{
    /// <summary>
    /// Provides utility methods for handling file paths within a Unity project.
    /// </summary>
    public class PathUtility : SingletonWithLazy<PathUtility>
    {
        /// <summary>
        /// Converts an absolute file path to a relative path based on the Unity project's directory.
        /// </summary>
        /// <param name="absolute_path">The absolute file path to be converted to a relative path.</param>
        /// <returns>
        /// The relative file path if the conversion is successful, or <c>null</c> if the provided absolute path is invalid
        /// or does not belong to the Unity project's directory.
        /// </returns>
        public string AbsoluteToRelativePath(string absolute_path)
        {
            if (string.IsNullOrEmpty(absolute_path)) return null;

            var project_path = Directory.GetParent(Application.dataPath)?.FullName;

            // 标准化路径分隔符
            absolute_path = absolute_path.Replace('\\', '/');
            project_path = project_path?.Replace('\\', '/');

            if (project_path != null && absolute_path.StartsWith(project_path))
            {
                var relative_path = absolute_path.Substring(project_path.Length);
                if (relative_path.StartsWith("/")) relative_path = relative_path.Substring(1);
                return relative_path;
            }

            return null;
        }

        /// <summary>
        /// Converts a relative file path to an absolute path based on the Unity project's directory.
        /// </summary>
        /// <param name="relative_path">The relative file path to be converted to an absolute path.</param>
        /// <returns>
        /// The absolute file path if the conversion is successful, or <c>null</c> if the provided relative path is invalid
        /// or the project's directory cannot be determined.
        /// </returns>
        public string RelativeToAbsolutePath(string relative_path)
        {
            if (string.IsNullOrEmpty(relative_path)) return null;

            var project_path = Directory.GetParent(Application.dataPath)?.FullName;
            if (project_path != null)
            {
                var absolute_path = Path.GetFullPath(Path.Combine(project_path, relative_path));
                return absolute_path?.Replace('\\', '/');
            }

            return null;
        }

        /// <summary>
        /// Checks if a file exists at the specified relative path based on the Unity project's directory.
        /// </summary>
        /// <param name="relative_path">The relative file path to check for existence.</param>
        /// <returns>
        /// <c>true</c> if the file exists at the specified relative path, otherwise <c>false</c>.
        /// </returns>
        public bool RelativePathExists(string relative_path)
        {
            var absolute_path = RelativeToAbsolutePath(relative_path);
            return !string.IsNullOrEmpty(absolute_path) && File.Exists(absolute_path);
        }

        /// <summary>
        /// Determines whether the given relative path is located within the Unity project's Assets folder.
        /// </summary>
        /// <param name="relative_path">The relative file path to check for being within the Assets folder.</param>
        /// <returns>
        /// <c>true</c> if the given relative path starts with "Assets/"; otherwise, <c>false</c>.
        /// </returns>
        public bool IsInAssetsFolder(string relative_path)
        {
            return !string.IsNullOrEmpty(relative_path) && relative_path.StartsWith("Assets/");
        }

        /// <summary>
        /// Retrieves the absolute path of the Unity project directory.
        /// </summary>
        /// <returns>
        /// The full path of the Unity project directory, with standardized path separators,
        /// or <c>null</c> if the directory cannot be determined.
        /// </returns>
        public string GetProjectPath()
        {
            return Directory.GetParent(Application.dataPath)?.FullName.Replace('\\', '/');
        }

        /// <summary>
        /// Retrieves the absolute path to the Unity project's Assets folder.
        /// </summary>
        /// <returns>
        /// The absolute file path to the Assets folder of the Unity project with standardized path separators.
        /// </returns>
        public string GetAssetsPath()
        {
            return Application.dataPath.Replace('\\', '/');
        }

        /// <summary>
        /// Maps a relative file path intended for the Unity Editor to its corresponding runtime file path,
        /// based on the runtime file storage strategy.
        /// </summary>
        /// <param name="editor_relative_path">The relative path of the file as used in the Unity Editor.</param>
        /// <returns>The full runtime file path constructed based on the provided relative path.</returns>
        public string GetRuntimeFilePath(string editor_relative_path)
        {
            // 根据运行时文件存储策略实现
            var file_name = Path.GetFileName(editor_relative_path);

            // 如果文件在StreamingAssets中
            return Path.Combine(Application.streamingAssetsPath, file_name);
        }

        public bool MoveAsset(string source_path, string target_path)
        {
            if (!AssetDatabase.LoadAssetAtPath<Object>(source_path))
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(
                    new LogSpaceNode("ExTools").AddChild("PathUtility"),
                    new LogEntry(LogLevel.kError, $"源文件不存在: {source_path}"), true);
                return false;
            }

            // 确保目标目录存在
            var target_directory = Path.GetDirectoryName(target_path);
            if (!Directory.Exists(target_directory))
            {
                if (target_directory != null) Directory.CreateDirectory(target_directory);
                AssetDatabase.Refresh(); // 刷新数据库
            }

            // 移动资源
            var result = AssetDatabase.MoveAsset(source_path, target_path);

            if (string.IsNullOrEmpty(result))
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(
                    new LogSpaceNode("ExTools").AddChild("PathUtility"),
                    new LogEntry(LogLevel.kInfo, $"文件移动成功: {source_path} -> {target_path}"));
                AssetDatabase.Refresh();
                return true;
            }
            else
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(
                    new LogSpaceNode("ExTools").AddChild("PathUtility"),
                    new LogEntry(LogLevel.kError, $"文件移动失败: {result}"),true);
                return false;
            }
        }
    }
}