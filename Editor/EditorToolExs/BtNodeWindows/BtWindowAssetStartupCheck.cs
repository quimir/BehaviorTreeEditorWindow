using System;
using System.IO;
using BehaviorTree.BehaviorTrees;
using BehaviorTree.Core;
using Editor.View.BtWindows.Core;
using ExTools.Utillties;
using LogManager.Core;
using LogManager.LogManagerFactory;
using Script.BehaviorTree.Save;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Editor.EditorToolExs.BtNodeWindows
{
    [InitializeOnLoad]
    public class BtWindowAssetStartupCheck
    {
        private const string DateFileExtension = FixedValues.kBtDateFileExtension;

        private static readonly LogSpaceNode log_space_ = new LogSpaceNode("BtWindowAsset").AddChild("StartUpCheck");

        static BtWindowAssetStartupCheck()
        {
            EditorApplication.delayCall += CheckAllBtWindowAssets; // 延迟调用以确保 AssetDatabase 完全加载
        }

        /// <summary>
        /// Performs a startup check for all assets of type BtWindowAsset in the project.
        /// Validates and corrects the external data paths of BtWindowAsset objects by comparing the stored paths
        /// with the expected paths based on the asset's location in the project.
        /// If discrepancies are found, the paths are corrected, and changes are saved to the AssetDatabase.
        /// Log entries are generated to report the progress and results of the check.
        /// </summary>
        private static void CheckAllBtWindowAssets()
        {
            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                new LogEntry(LogLevel.kInfo, "[Startup Check] Checking all BtWindowAsset external data paths..."));
            bool need_refresh = false;
            var assets_need_saving = false;

            // 查找项目中所有的BtWindowAsset
            var guids = AssetDatabase.FindAssets("t:BtWindowData");

            foreach (var guid in guids)
            {
                var asset_path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<BtWindowAsset>(asset_path);

                if (asset == null)
                {
                    ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                        new LogEntry(LogLevel.kWarning,
                            $"[Startup Check] Failed to load BtWindowAsset at path: {asset_path}"));
                    continue;
                }

                var stored_relative_path = asset.ExternalDatePath;
                var expected_relative_path = Path.ChangeExtension(asset_path, DateFileExtension);
                var expected_absolute_path =
                    Path.GetFullPath(Path.Combine(Application.dataPath, "..", expected_relative_path));

                var path_is_correct = stored_relative_path == expected_absolute_path;
                var file_exists_at_path = !string.IsNullOrEmpty(stored_relative_path) &&
                                          File.Exists(asset.GetAbsoluteExternalDatePath()); // 检查存储路径的文件是否存在
                var expected_file_exists = File.Exists(expected_absolute_path); // 检查预期路径的文件是否存在

                // 情况1:路径为空或不正确
                if (string.IsNullOrEmpty(stored_relative_path) || !path_is_correct)
                {
                    ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                        new LogEntry(LogLevel.kWarning,
                            $"[Startup Check] Asset '{asset_path}' has missing or incorrect external data path ('{stored_relative_path}'). Expecting '{expected_relative_path}'. Attempting to fix."));
                    asset.ExternalDatePath = expected_absolute_path;
                    assets_need_saving = true;
                    // 更新检查状态
                    stored_relative_path = expected_relative_path;
                    path_is_correct = true;
                    file_exists_at_path = expected_file_exists; // 如果修正了路径，文件存在性现在取决于预期文件
                }

                // 情况2.路径正确，但指向的文件不存在
                if (!file_exists_at_path)
                {
                    ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,new LogEntry(LogLevel.kWarning
                    ,$"[Startup Check] Data file missing for '{asset_path}'. Creating a new one at '{expected_relative_path}'."));
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(expected_absolute_path) ?? string.Empty);
                        File.WriteAllText(expected_absolute_path, "{}");
                        need_refresh = true;
                    }
                    catch (Exception e)
                    {
                        ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                            new LogEntry(LogLevel.kError,
                                $"[Startup Check] Failed to create data file for '{asset_path}': {e.Message}"));
                    }
                }
            }

            if (assets_need_saving)
            {
                AssetDatabase.SaveAssets();
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                    new LogEntry(LogLevel.kInfo, "[Startup Check] Saved corrected BtWindowAsset paths."));
            }

            if (need_refresh)
            {
                AssetDatabase.Refresh();
            }

            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                new LogEntry(LogLevel.kInfo, "[Startup Check] Finished checking BtWindowAsset paths."));
        }

        [OnOpenAsset(1)]
        public static bool OnOpenAsset(int instance, int line)
        {
            // 让其的id自动转换
            var obj = EditorUtility.InstanceIDToObject(instance);

            if (obj is BtWindowAsset window_asset)
            {
                var tree_id = BehaviorTreeManagers.instance.GetTree(BehaviorTreeManagers.instance
                    .FindTreeByFilePath(window_asset.GetAbsoluteExternalDatePath())?.GetTreeId())?.GetTreeId();
                if (string.IsNullOrEmpty(tree_id))
                {
                    var temp_tree = new BehaviorTreeTemp(window_asset);
                    BehaviorTreeManagers.instance.RegisterTree(temp_tree.GetTreeId(), temp_tree);
                    tree_id = temp_tree.GetTreeId();
                }

                BehaviorTreeWindowsBase.CreateWindowForTrees<BehaviorTreeWindows>(tree_id);

                return true;
            }

            return false;
        }
    }
}