using System;
using System.IO;
using LogManager.Core;
using LogManager.LogManagerFactory;
using Script.LogManager;
using Script.Utillties;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace BehaviorTree.Core
{
    /// <summary>
    /// BtWindowAsset is a ScriptableObject class that acts as a container for external data
    /// paths related to the Behavior Tree system. This asset represents associated metadata
    /// and utilities for working with external behavior tree configuration files.
    /// </summary>
    public class BtWindowAsset : ScriptableObject
    {
        [SerializeField] [ReadOnly] private string external_date_path_;

        [SerializeField] [ReadOnly] private string asset_guid_ = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the external data path associated with this asset.
        /// This property represents the relative or absolute file path for
        /// external Behavior Tree data or configuration files.
        /// Changes to this property trigger logging and mark the asset as dirty
        /// in the Unity Editor, ensuring project consistency and traceability.
        /// </summary>
        public string ExternalDatePath
        {
            get => external_date_path_;
            // 内部设置，通过 AssetPostprocessor 或特定方法修改
            set
            {
                if (external_date_path_ != value)
                {
                    // 添加日志确认 Setter 被调用
                    ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(new LogSpaceNode("BtWindowAsset"),
                        new LogEntry(LogLevel.kInfo,
                            $"[BtWindowAsset Setter] Setting ExternalDataPath on '{name}' from '{external_date_path_}' to '{value}'"));
                    external_date_path_ = value;

#if UNITY_EDITOR
                    EditorUtility.SetDirty(this);
#endif
                }
            }
        }

        /// <summary>
        /// Gets the unique identifier (GUID) associated with this asset.
        /// This property ensures that every asset has a valid, unique GUID
        /// which is automatically generated and assigned if missing.
        /// The GUID is used to track and link assets persistently across
        /// the system, ensuring integrity and consistency, even when the
        /// asset is moved or renamed.
        /// </summary>
        public string AssetGuid
        {
            get
            {
                // 如果GUID为空或无效，则生成一个新的并保存
                if (string.IsNullOrEmpty(asset_guid_))
                {
#if UNITY_EDITOR
                    asset_guid_ = Guid.NewGuid().ToString();
                    ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(new LogSpaceNode("BtWindowAsset"),
                        new LogEntry(LogLevel.kInfo,
                            $"[BtWindowAsset Getter] Generated new AssetGuid for '{name}': {asset_guid_}"));
                    EditorUtility.SetDirty(this); // 标记为已修改，以便 Unity 保存新的 GUID
#else
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog("BtWindowAsset",
                     new LogEntry(LogLevel.kWarning, $"[BtWindowAsset Getter] AssetGuid for '{name}' is missing in runtime."));
                  return string.Empty; // 或者一个固定的无效值
#endif
                }

                return asset_guid_;
            }
        }

        /// <summary>
        /// Returns the absolute path to the external data file associated with this BtWindowAsset.
        /// If the provided external data path is already an absolute path, it will be returned as is.
        /// If it is a relative path, it will compute and return the equivalent absolute path relative
        /// to the root project directory (the parent of the Assets folder).
        /// </summary>
        /// <returns>
        /// The absolute path to the external data file if the path is valid, otherwise null if the
        /// external data path is null or empty.
        /// </returns>
        public string GetAbsoluteExternalDatePath()
        {
            if (string.IsNullOrEmpty(external_date_path_)) return null;

            // 如果ExternalDatePath已经是绝对路径
            if (Path.IsPathRooted(external_date_path_)) return ExternalDatePath;

            // 假设他是相对于项目根目录(Assets文件夹外一层)
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", ExternalDatePath));
        }

        /// <summary>
        /// Checks whether the external data file associated with this BtWindowAsset exists.
        /// It retrieves the absolute path of the external data file and verifies if the file is present
        /// in the file system. Returns true only when the path is valid and the file is found; otherwise, false.
        /// </summary>
        /// <returns>
        /// True if the external data file exists at the resolved absolute path; false if the path is null,
        /// empty, or the file is not found.
        /// </returns>
        public bool ExternalDataFileExists()
        {
            var path = GetAbsoluteExternalDatePath();
            return !string.IsNullOrEmpty(path) && File.Exists(path);
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(asset_guid_))
                EditorApplication.delayCall += () =>
                {
                    if (this && string.IsNullOrEmpty(asset_guid_))
                    {
                        asset_guid_ = Guid.NewGuid().ToString();
                        ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(
                            new LogSpaceNode("BtWindowAsset"),
                            new LogEntry(LogLevel.kInfo,
                                $"[BtWindowAsset OnEnable] Generated missing AssetGuid for '{name}': {asset_guid_}"));
                        EditorUtility.SetDirty(this);
                    }
                };
#endif
        }
    }
}