using System;
using BehaviorTree.BehaviorTreeBlackboard;
using BehaviorTree.Core;
using BehaviorTree.Core.WindowData;
using BehaviorTree.Nodes;
using ExTools.Utillties;
using LogManager.Core;
using LogManager.LogManagerFactory;
using Script.BehaviorTree;
using Script.BehaviorTree.Save;
using Script.LogManager;
using Script.Save.Serialization;
using Script.Save.Serialization.Storage;
using Script.Utillties;
using UnityEditor;
using UnityEngine;

namespace BehaviorTree.BehaviorTrees
{
    public abstract class AbstractBehaviorTree : IBehaviorTrees
    {
        /// <summary>
        /// 树的唯一标识符
        /// </summary>
        protected string tree_id_;

        /// <summary>
        /// 行为树资产引用
        /// </summary>
        protected BehaviorTreeWindowData bt_node_base_;

        /// <summary>
        /// 行为树窗口数据
        /// </summary>
        protected BtWindowAsset BtWindowAsset=null;

        // 标记是否已经初始化
        protected bool initialized_ = false;

        public static readonly LogSpaceNode bt_space_ =new LogSpaceNode("BehaviorTree") ;

        /// <summary>
        /// 安全初始化方法
        /// </summary>
        protected virtual void SafeInitialize()
        {
            if (initialized_) return;

            try
            {
                // 生成唯一标识符
                if (tree_id_.StringEmpty()) tree_id_ = Guid.NewGuid().ToString();

                // 标记已经初始化
                initialized_ = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Initialization failed for AbstractBehaviorTree: {e.Message}");
                // 可以选择抛出异常或者使用默认值
                bt_node_base_ = new BehaviorTreeWindowData();
            }
        }

        #region GetBtData接口实现

        public virtual BtNodeBase GetRoot()
        {
            return bt_node_base_?.RootNode;
        }

        public virtual void SetRoot(BtNodeBase node_base)
        {
            if (node_base == null) return;
            bt_node_base_.RootNode = node_base;
        }

        public virtual void DeleteRoot()
        {
            bt_node_base_.RootNode = null;
        }

        public virtual BehaviorTreeWindowData GetNodeWindow()
        {
            return bt_node_base_;
        }

        public virtual void SetNodeWindow(BehaviorTreeWindowData node_window_data)
        {
            if (node_window_data != null) bt_node_base_ = node_window_data;
        }

        public virtual void DeleteNodeWindow()
        {
            bt_node_base_ = null;
        }

        public virtual void SaveBtWindow(string file_path = null)
        {
            // 如果没有提供file_path，使用BtWindowAsset
            if (file_path.StringEmpty())
            {
                // 先检查是否有数据，如果没有数据则不保存
                if (bt_node_base_ == null)
                {
                    return;
                }

                if (!BtWindowAsset)
                {
                    if (!CreateBtWindowAsset())
                    {
                        ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(bt_space_,
                            new LogEntry(LogLevel.kError, "当前没有放置行为树资产，不允许进行保存！"));
                        return;
                    }
                }
                
                if (BtWindowAsset.ExternalDataFileExists()) file_path = BtWindowAsset.GetAbsoluteExternalDatePath();
            }

            var selector = new JsonSerializerWithStorage(new SerializationSettings
            {
                PreserveReferences = true,
                PrettyPrint = true,
                TypeNameHandling = SerializationTypeNameHandling.kAuto
            });
            
            selector.SaveToFile(bt_node_base_, file_path);

            // 更新资产文件路径
            if (BtWindowAsset)
            {
                BtWindowAsset.ExternalDatePath = file_path;
#if UNITY_EDITOR
                EditorUtility.SetDirty(BtWindowAsset);
                AssetDatabase.SaveAssets();
#endif
            }
        }

        public virtual BehaviorTreeWindowData LoadBtWindow(string file_path)
        {
            // 如果没有提供file_path，则使用存储在BtManagement中的file_path
            if (file_path.StringEmpty())
            {
                if (!BtWindowAsset)
                {
                    if (!CreateBtWindowAsset())
                    {
                        ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(bt_space_,
                            new LogEntry(LogLevel.kError, "当前没有放置行为树资产，不允许进行保存！"));
                        return new BehaviorTreeWindowData();
                    }
                }
                
                if (BtWindowAsset.ExternalDataFileExists()) file_path = BtWindowAsset.GetAbsoluteExternalDatePath();
            }

            var selector = new JsonSerializerWithStorage(new SerializationSettings
            {
                PreserveReferences = true,
                PrettyPrint = true,
                TypeNameHandling = SerializationTypeNameHandling.kAuto
            });

            return selector.LoadFromFile<BehaviorTreeWindowData>(file_path);
        }

        public BehaviorTreeWindowData GetTreeWindowData()
        {
            return bt_node_base_;
        }

        public virtual string GetTreeId()
        {
            // 确保在返回之前初始化
            if (!initialized_) SafeInitialize();

            return tree_id_;
        }

        public IBlackboardStorage GetBlackboard()
        {
            return bt_node_base_.Blackboard;
        }

        public BtWindowAsset GetWindowAsset()
        {
            return BtWindowAsset;
        }

        #endregion


        public virtual void SetTreeId(string tree_id)
        {
            if (!tree_id.StringEmpty()) tree_id_ = tree_id;
        }

        public virtual void SetBtWindowAsset(BtWindowAsset window_asset)
        {
            if (window_asset != null) BtWindowAsset = window_asset;
        }

        // 强制重新初始化
        public virtual void ForceReInitialize()
        {
            initialized_ = false;
            SafeInitialize();
        }

        public abstract ExtendableBehaviorTree AttachToGameObject(GameObject game_object);

        protected virtual bool CreateBtWindowAsset()
        {
            // 1.让用户选择保存路径和文件名（使用Unity的方法）
            var suggested_name = GetType() + "_BtWindowData"; // 基于 GameObject 给个默认名
            var asset_path = EditorUtility.SaveFilePanelInProject(
                "Create New BtWindow Asset", suggested_name + ".asset", "asset",
                "Please select a location to save the BtWindowAsset."); // 提示信息

            // 如果用户取消了对话框
            if (string.IsNullOrEmpty(asset_path))
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(bt_space_,
                    new LogEntry(LogLevel.kInfo, "BtWindowAsset creation cancelled by user."));
                return false;
            }

            //  assetPath现在是相对于项目的路径,例如"Assets/.../??.asset"

            // 2.创建ScriptableObject实例(在内存中)
            var instance = ScriptableObject.CreateInstance<BtWindowAsset>();

            // 3.将内存中的示例保存为项目的资源文件，CreateAsset会自动处理正确的Unity序列化格式(YAML)
            AssetDatabase.CreateAsset(instance, asset_path);

            // 4.立即保存更改到磁盘，确保文件写入
            AssetDatabase.SaveAssets();
            
            // 5.有区别于ExtendableBehaviorTree这里需要立马读取，因为这时候必须要立马拿取数据存储中心
            var created_asset = AssetDatabase.LoadAssetAtPath<BtWindowAsset>(asset_path);

            BtWindowAsset = created_asset;

            // --- 让 AssetPostprocessor 处理后续 ---
            // AssetDatabase.CreateAsset 会触发 BtWindowAssetProcessor 的 OnPostprocessAllAssets。
            // Postprocessor 会负责：
            //   - 加载刚创建的 'instance'。
            //   - 设置其 internal ExternalDataPath 指向对应的 .btwindowtemp 文件。
            //   - 创建 .btwindowtemp 文件。
            //   - 标记 'instance' 为 Dirty 并调用 SaveAssets。

            // 5. 将新创建并（理论上已被 Postprocessor 处理过的）资源赋值给组件
            // 最好在 Postprocessor 处理完成后再赋值，但 CreateAsset 通常是同步的
            // 为了确保获取到的是最新状态（包含 Postprocessor 设置的路径），可以稍微延迟或重新加载
            // 但通常直接使用 instance 也可以，因为它是同一个对象引用
            // 我们重新加载一次以确保获取最新状态
            EditorApplication.delayCall += () =>
            {
                if (created_asset != null)
                {
                    BtWindowAsset = created_asset; // 赋值给组件字段
                    ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(bt_space_,
                        new LogEntry(LogLevel.kInfo, $"Successfully created and assigned BtWindowAsset: {asset_path}"));

                    // 让新创建的资源在Project窗口中高亮显示
                    EditorGUIUtility.PingObject(created_asset);
                }
                else
                {
                    ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(bt_space_,
                        new LogEntry(LogLevel.kError,
                            $"Failed to load the created BtWindowAsset after creation/postprocessing at path: {asset_path}. Check console for other errors."));
                    // 如果 Postprocessor 或 CreateAsset 环节有问题，这里可能会失败
                }
            };

            return true;
        }
    }
}