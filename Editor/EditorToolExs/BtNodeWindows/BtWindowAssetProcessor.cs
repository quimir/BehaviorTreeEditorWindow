using System;
using System.IO;
using System.Linq;
using BehaviorTree.Core;
using ExTools;
using LogManager.Core;
using LogManager.LogManagerFactory;
using Script.LogManager;
using Script.Utillties;
using UnityEditor;
using UnityEngine;

namespace Editor.EditorToolExs.BtNodeWindows
{
    public class BtWindowAssetProcessor : AssetPostprocessor
    {
        private const string TargetExtension = ".asset";

        private const string DataFileExtension = FixedValues.kBtDateFileExtension;

        private static readonly LogSpaceNode log_space_ = new LogSpaceNode("Asset").AddChild("BtWindowAssetProcessor");

        /// <summary>
        /// Called after assets are imported, deleted, or moved within the Unity project.
        /// This method processes imported assets, manages associated data files, and ensures
        /// proper updates for assets of specific extensions defined in the processor.
        /// </summary>
        /// <param name="importedAssets">An array of paths to assets that have been imported.</param>
        /// <param name="deletedAssets">An array of paths to assets that have been deleted.</param>
        /// <param name="movedAssets">An array of paths to assets that have been moved to new locations.</param>
        /// <param name="movedFromAssetPaths">An array of paths from which assets have been moved.</param>
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            // 处理移动的资源
            for (var i = 0; i < movedAssets.Length; i++)
            {
                var new_asset_path = movedAssets[i];
                // 我们只关心主资源 .asset文件的移动
                if (!new_asset_path.EndsWith(TargetExtension, StringComparison.OrdinalIgnoreCase)) continue;
                if (!AssetDatabase.LoadAssetAtPath<BtWindowAsset>(new_asset_path)) continue;

                var old_asset_path = movedFromAssetPaths[i];

                var old_data_path = Path.ChangeExtension(old_asset_path, DataFileExtension);
                var new_data_path = Path.ChangeExtension(new_asset_path, DataFileExtension);

                var old_data_full_path = Path.GetFullPath(old_data_path);
                var new_data_full_path = Path.GetFullPath(new_data_path);

                // 如果旧的附属文件存在，就移动它
                if (File.Exists(old_data_full_path))
                {
                    try
                    {
                        // 确保目标目录存在
                        Directory.CreateDirectory(Path.GetDirectoryName(new_data_full_path) ?? string.Empty);

                        // 移动主文件和.meta文件
                        File.Move(old_data_full_path, new_data_full_path);
                        var old_meta_path = old_data_full_path + ".meta";
                        if (File.Exists(old_meta_path)) File.Move(old_meta_path, new_data_full_path + ".meta");

                        ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                            new LogEntry(LogLevel.kInfo,
                                $"[AssetProcessor] Moved associated data file from '{old_data_path}' to '{new_data_path}'."));

                        // 移动成功后，需要刷新AssetDatabase让Unity知道变化
                        AssetDatabase.Refresh();
                    }
                    catch (Exception e)
                    {
                        ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                            new LogEntry(LogLevel.kError,
                                $"[AssetProcessor] Failed to move associated data file from '{old_data_path}' to '{new_data_path}': {e.Message}"));
                    }
                }

                var asset = AssetDatabase.LoadAssetAtPath<BtWindowAsset>(new_asset_path);
                    if (asset != null && asset.ExternalDatePath != new_data_path)
                    {
                        var new_external_path=PathUtility.Instance.AbsoluteToRelativePath(new_data_path);
                        asset.ExternalDatePath = new_external_path;
                    }
            }

            // 处理删除的资源
            foreach (var deleted_asset_path in deletedAssets)
                if (deleted_asset_path.EndsWith(TargetExtension, StringComparison.OrdinalIgnoreCase))
                {
                    var data_path = Path.ChangeExtension(deleted_asset_path, DataFileExtension);
                    var data_full_path = Path.GetFullPath(data_path);

                    if (File.Exists(data_full_path))
                        try
                        {
                            File.Delete(data_full_path);
                            var meta_path = data_full_path + ".meta";
                            if (File.Exists(meta_path)) File.Delete(meta_path);

                            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace)
                                .AddLog(log_space_,
                                    new LogEntry(LogLevel.kInfo,
                                        $"[AssetProcessor] Deleted associated data file '{data_path}'."));
                            // 删除后进行刷新
                            AssetDatabase.Refresh();
                        }
                        catch (Exception e)
                        {
                            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace)
                                .AddLog(log_space_,
                                    new LogEntry(LogLevel.kError,
                                        $"[AssetProcessor] Failed to delete associated data file '{data_path}': {e.Message}"));
                            ;
                        }
                }

            // 处理导入/新创建的资源
            foreach (var imported_asset_path in importedAssets)
            {
                // A. 处理 ScriptableObject (.asset)，确保附属文件存在
                if (imported_asset_path.EndsWith(TargetExtension, StringComparison.OrdinalIgnoreCase))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<BtWindowAsset>(imported_asset_path);
                    if (asset == null) continue;

                    var expected_data_path = Path.ChangeExtension(imported_asset_path, DataFileExtension);
                    var expected_data_full_path = Path.GetFullPath(expected_data_path);

                    // 更新内部路径
                    if (asset.ExternalDatePath != expected_data_path) asset.ExternalDatePath = expected_data_path;

                    // 如果附属文件不存在，则创建
                    if (!File.Exists(expected_data_full_path))
                    {
                        File.WriteAllText(expected_data_full_path, "{}");
                        ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                            new LogEntry(LogLevel.kInfo,
                                $"[AssetProcessor] Created associated data file '{expected_data_path}'."));
                        // 强制刷新触发下面的逻辑
                        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                    }
                }

                // // B. 处理附属文件 (.btwindowtemp)，将其隐藏
                // if (imported_asset_path.EndsWith(DataFileExtension, StringComparison.OrdinalIgnoreCase))
                // {
                //     var importer = AssetImporter.GetAtPath(imported_asset_path);
                //     if (importer != null && (importer.hideFlags & HideFlags.HideInHierarchy) == 0)
                //     {
                //         importer.hideFlags |= HideFlags.HideInHierarchy;
                //         importer.SaveAndReimport();
                //         ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                //             new LogEntry(LogLevel.kInfo,
                //                 $"[AssetProcessor] Hidden associated data file '{imported_asset_path}'."));
                //     }
                // }
            }
        }
    }
}