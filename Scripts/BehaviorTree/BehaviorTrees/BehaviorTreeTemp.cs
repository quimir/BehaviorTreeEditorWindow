using System;
using System.IO;
using BehaviorTree.BehaviorTreeBlackboard;
using BehaviorTree.Core;
using BehaviorTree.Core.WindowData;
using LogManager.Core;
using LogManager.LogManagerFactory;
using Script.BehaviorTree;
using Script.LogManager;
using Script.Utillties;
using UnityEditor;
using UnityEngine;

namespace BehaviorTree.BehaviorTrees
{
    public sealed class BehaviorTreeTemp : AbstractBehaviorTree
    {
        // 是否为临时行为树标记
        [HideInInspector] public bool is_temporary = true;

        public BehaviorTreeTemp()
        {
            tree_id_ = Guid.NewGuid().ToString();
            bt_node_base_ = new BehaviorTreeWindowData
            {
                Blackboard = new LayeredBlackboard(null)
            };
            
            SafeInitialize();
        }

        public BehaviorTreeTemp(BehaviorTreeWindowData data)
        {
            tree_id_ = tree_id_.StringEmpty() ? Guid.NewGuid().ToString() : tree_id_;
            bt_node_base_ = data ?? new BehaviorTreeWindowData()
            {
                Blackboard = new LayeredBlackboard(null)
            };
            
            SafeInitialize();
        }

        public BehaviorTreeTemp(string file_path)
        {
            if (file_path.StringEmpty())
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(bt_space_,
                    new LogEntry(LogLevel.kError, "[BtTempBehaviorTree] Input file path is null or empty."));
                return;
            }

            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(bt_space_,
                new LogEntry(LogLevel.kInfo, $"[BtTempBehaviorTree] Processing path: {file_path}"));

#if UNITY_EDITOR
            var bt_asset = AssetDatabase.LoadAssetAtPath<BtWindowAsset>(file_path);

            if (bt_asset)
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(bt_space_,
                    new LogEntry(LogLevel.kInfo,
                        $"[BtTempBehaviorTree] Successfully loaded '{file_path}' as BtWindowAsset: {bt_asset.name}"));

                var external_path = bt_asset.ExternalDatePath;

                if (!string.IsNullOrEmpty(external_path))
                {
                    ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(bt_space_,
                        new LogEntry(LogLevel.kInfo,
                            $"[BtTempBehaviorTree] Found ExternalDatePath: '{external_path}'"));
                    var resolved_external_path = bt_asset.GetAbsoluteExternalDatePath();
                    ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(bt_space_,
                        new LogEntry(LogLevel.kInfo,
                            $"[BtTempBehaviorTree] Resolved absolute ExternalDatePath: '{resolved_external_path}'"));

                    if (!string.IsNullOrEmpty(resolved_external_path) && File.Exists(resolved_external_path))
                    {
                        ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(bt_space_,
                            new LogEntry(LogLevel.kInfo,
                                $"[BtTempBehaviorTree] External data file at '{resolved_external_path}' exists. Ready for parsing."));

                        bt_node_base_ = LoadBtWindow(resolved_external_path);

                        if (bt_node_base_ != null)
                            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(
                                bt_space_,
                                new LogEntry(LogLevel.kInfo,
                                    $"[BtTempBehaviorTree] The specific data of the behavior tree has been obtained from the behavior tree resource file"));
                    }
                }
#endif
            }
            
            SafeInitialize();
        }

        public BehaviorTreeTemp(BtWindowAsset asset)
        {
            if (!asset)
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(bt_space_,new LogEntry(LogLevel.kWarning,"未初始化窗口资源，不允许初始化"));
                return;
            }
            
            tree_id_ = Guid.NewGuid().ToString();
            BtWindowAsset = asset;
            bt_node_base_=LoadBtWindow(asset.GetAbsoluteExternalDatePath());
        }

        public override ExtendableBehaviorTree AttachToGameObject(GameObject game_object)
        {
            if (game_object == null)
            {
                Debug.LogError("无法将行为树挂载到空GameObject上");
                return null;
            }

            var base_tree = game_object.AddComponent<ExtendableBehaviorTree>();

            // 设置行为树数据
            base_tree.SetBtWindowAsset(BtWindowAsset);
            base_tree.SetNodeWindow(bt_node_base_);
            base_tree.SetTreeId(tree_id_);
            

            return base_tree;
        }
    }
}