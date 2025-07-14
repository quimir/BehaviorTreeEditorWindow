using System;
using System.IO;
using BehaviorTree.BehaviorTreeBlackboard;
using BehaviorTree.BehaviorTreeBlackboard.Core;
using BehaviorTree.Core;
using BehaviorTree.Core.WindowData;
using BehaviorTree.Nodes;
using ExTools.Utillties;
using LogManager.Core;
using LogManager.LogManagerFactory;
using Save.Serialization.Core.TypeConverter;
using Save.Serialization.Factory;
using Save.Serialization.Storage.Serializer;
using Save.Serialization.Storage.Serializer.JsonNet;
using Script.BehaviorTree.Save;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace BehaviorTree.BehaviorTrees
{
    [DisallowMultipleComponent]
    [TypeInfoBox("行为树组件 - 用于定义和执行AI行为")]
    public class ExtendableBehaviorTree : MonoBehaviour, IBehaviorTrees
    {
        // 资产文件
        public BtWindowAsset BtWindowAsset;
        
        private BehaviorTreeWindowData BtNodeBase = new();

        // 行为树唯一标识符
        [SerializeField] 
        [ReadOnly]
        private string tree_id_ = Guid.NewGuid().ToString();

        /// <summary>
        /// 这个类型只是要检查其是否初始化了也即当程序开始的时候进行检查是否有数据了，而不需要管全局的不需要将其进行序列化。
        /// </summary>
        [NonSerialized] private bool has_been_set_up_;

        [SerializeField] 
        [HideInInspector] 
        private string loaded_asset_guid_=string.Empty;

        [NonSerialized] private static readonly LogSpaceNode bt_space_ =new("BehaviorTree");

        protected virtual void Awake()
        {
            InitializeIfNeeded();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResetWindowAsset();
        }
#endif

        private void ClearTreeData()
        {
            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(bt_space_,
                new LogEntry(LogLevel.kInfo, $"[ClearTreeData] 清理行为树数据和注册信息 (Tree ID: {tree_id_})"));

            if (!string.IsNullOrEmpty(tree_id_))
            {
                BehaviorTreeManagers.instance.UnRegisterTree(tree_id_);
            }

            BtNodeBase = new BehaviorTreeWindowData();

            has_been_set_up_ = false;
        }

        /// <summary>
        /// Ensures the behavior tree is initialized if it has not already been set up.
        /// This method performs necessary setup for the behavior tree, including generating unique identifiers,
        /// loading tree data, and registering the tree with the internal registry. If initialization has already
        /// occurred or critical assets are missing, the method will log warnings and skip re-initialization.
        /// Potential scenarios logged during initialization:
        /// - The tree has already been initialized, and further setup is skipped.
        /// - Missing critical assets, such as `BtWindowAsset`.
        /// - The initialization process succeeded, and log entries provide detailed information,
        /// including the new Tree ID and registration completion.
        /// - If initialization fails, relevant errors are logged, and the state is partially reset to allow retries.
        /// This method is typically invoked automatically during the `Awake` lifecycle of the `ExtendableBehaviorTree`
        /// but can also be called explicitly when required.
        /// Exceptions during initialization are caught, and any partial processes are rolled back where appropriate to
        /// ensure the component remains functional.
        /// Notes:
        /// - During Unity Editor mode, changes to the behavior tree are marked dirty to persist them in the editor.
        /// - The component requires a valid `BtWindowAsset` and other associated assets to complete the setup successfully.
        /// - If initialization fails, the state is reset partially, retaining some information like the Tree ID while
        /// resetting node data.
        /// Common usage includes scenarios like ensuring the behavior tree is correctly prepared before being executed or
        /// manipulated by other systems.
        /// </summary>
        public void InitializeIfNeeded()
        {
            if (has_been_set_up_)
            {
                if (has_been_set_up_)
                    ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(bt_space_,
                        new LogEntry(LogLevel.kDebug, $"[InitializeIfNeeded] 已经初始化过 (Tree ID: {tree_id_})，跳过。"));
                return;
            }

            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(bt_space_,
                new LogEntry(LogLevel.kInfo, $"[InitializeIfNeeded] 开始初始化行为树..."));

            if (string.IsNullOrEmpty(tree_id_))
            {
                tree_id_ = Guid.NewGuid().ToString();

#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(bt_space_,
                    new LogEntry(LogLevel.kInfo, $"[InitializeIfNeeded] 生成了新的 Tree ID: {tree_id_}"));
            }

            try
            {
                LoadTreeData();

                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(bt_space_,
                    new LogEntry(LogLevel.kInfo, $"[InitializeIfNeeded] 行为树注册成功 (Tree ID: {tree_id_})"));

                loaded_asset_guid_ = BtWindowAsset ? BtWindowAsset.AssetGuid : string.Empty;
                has_been_set_up_ = true;

                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(bt_space_,
                    new LogEntry(LogLevel.kInfo, $"[InitializeIfNeeded] 行为树初始化完成。"));
                
#if UNITY_EDITOR
                // 标记 dirty 以便保存 loaded_asset_guid_
                EditorUtility.SetDirty(this);
#endif
            }
            catch (Exception e)
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(bt_space_,
                    new LogEntry(LogLevel.kError,
                        $"[InitializeIfNeeded] 行为树初始化失败 (Tree ID: {tree_id_}): {e.Message}\n{e.StackTrace}"));
                // 初始化失败，重置状态？或者保留部分状态？根据需求决定
                // 这里选择重置 BtNodeBase 但保留 tree_id_
                BtNodeBase = new BehaviorTreeWindowData();
                has_been_set_up_ = false; // 标记为未成功初始化
                loaded_asset_guid_ = null; // 清除已加载的 GUID 记录
            }
        }

        private void LoadTreeData()
        {
            // Log 开始加载
            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(bt_space_,
                new LogEntry(LogLevel.kInfo, $"[LoadTreeData] 开始加载行为树数据..."));

            if (!BtWindowAsset)
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(bt_space_,
                    new LogEntry(LogLevel.kWarning, $"[LoadTreeData] BtWindowAsset 为空，无法加载数据。将使用默认空数据。"));
                BtNodeBase = new BehaviorTreeWindowData(); // 使用空数据
                return;
            }

            var file_path = BtWindowAsset.GetAbsoluteExternalDatePath();

            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(bt_space_,
                new LogEntry(LogLevel.kInfo,
                    $"[LoadTreeData] 尝试从 BtWindowAsset '{BtWindowAsset.name}' 加载数据，路径: {file_path ?? "null"}"));

            if (!string.IsNullOrEmpty(file_path) && File.Exists(file_path))
            {
                try
                {
                    var selector_json = SerializerCreator.Instance.Create<JsonSerializerWithStorage>
                    (SerializerType.kJson,new SerializationSettings
                    {
                        PreserveReferences = true,
                        PrettyPrint = true,
                        TypeNameHandling = SerializationTypeNameHandling.kAuto
                    });

                    BtNodeBase = selector_json.LoadFromFile<BehaviorTreeWindowData>(file_path);

                    // 如果 BtNodeBase 加载后为 null，也视为失败，创建一个空的
                    if (BtNodeBase == null)
                    {
                        ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(bt_space_,
                            new LogEntry(LogLevel.kWarning, $"[LoadTreeData] 从文件 '{file_path}' " +
                                                            $"反序列化得到 null，将使用默认空数据。"));
                        BtNodeBase = new BehaviorTreeWindowData();
                    }
                    else
                    {
                        BtNodeBase.Blackboard ??= new LayeredBlackboard(null);
                        ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(bt_space_,
                            new LogEntry(LogLevel.kInfo, $"[LoadTreeData] 成功从 '{file_path}' 加载数据。"));
                    }
                }
                catch (Exception e)
                {
                    ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).
                        AddLog(bt_space_,new LogEntry(LogLevel.kError,$"[LoadTreeData] 从文件 '{file_path}' " +
                                                                      $"加载行为树数据失败: {e.Message}\n{e.StackTrace}"),true);
                    BtNodeBase = new BehaviorTreeWindowData(); // 加载失败，使用空数据
                }
            }
            else
            {
                var reason = string.IsNullOrEmpty(file_path) ? "无法获取有效的数据文件路径" : $"数据文件不存在: {file_path}";
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(bt_space_,
                    new LogEntry(LogLevel.kWarning, $"[LoadTreeData] {reason}。将使用默认空数据。"));
                BtNodeBase = new BehaviorTreeWindowData(); // 文件无效或不存在，使用空数据
            }
        }

        public BtNodeBase GetRoot()
        {
            return BtNodeBase?.RootNode;
        }

        public void SetRoot(BtNodeBase node_base)
        {
            if (node_base == null) return;
            BtNodeBase.RootNode = node_base;
        }

        public void DeleteRoot()
        {
            BtNodeBase.RootNode = null;
        }

        public BehaviorTreeWindowData GetNodeWindow()
        {
            return BtNodeBase;
        }

        public void SetNodeWindow(BehaviorTreeWindowData node_window_data)
        {
            if (node_window_data != null) BtNodeBase = node_window_data;
        }

        public void DeleteNodeWindow()
        {
            BtNodeBase = null;
        }

        public void SaveBtWindow(string file_path = null)
        {
            // 如果没有提供file_path，使用BtWindowAsset
            if (file_path.StringEmpty())
            {
                // 先检查是否有数据，如果没有数据则不保存
                if (BtNodeBase == null) return;

                if (!BtWindowAsset)
                    if (!CreateBtWindowAsset())
                    {
                        ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(bt_space_,
                            new LogEntry(LogLevel.kError, "当前没有放置行为树资产，不允许进行保存！"));
                        return;
                    }

                if (BtWindowAsset.ExternalDataFileExists()) file_path = BtWindowAsset.GetAbsoluteExternalDatePath();
            }

            var selector = SerializerCreator.Instance.Create<JsonSerializerWithStorage>
            (SerializerType.kJson, new SerializationSettings
            {
                PreserveReferences = true,
                PrettyPrint = true,
                TypeNameHandling = SerializationTypeNameHandling.kAuto
            });

            selector.SaveToFile(BtNodeBase, file_path);

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

        public BehaviorTreeWindowData LoadBtWindow(string file_path)
        {
            // 如果没有提供file_path，使用BtWindowAsset
            if (file_path.StringEmpty())
            {
                if (!BtWindowAsset)
                    if (!CreateBtWindowAsset())
                    {
                        ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(bt_space_,
                            new LogEntry(LogLevel.kError, "当前没有放置行为树资产，不允许进行保存！"));
                        return new BehaviorTreeWindowData();
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

        public string GetTreeId()
        {
            // 确保在返回之前初始化
            if (!has_been_set_up_) InitializeIfNeeded();

            return tree_id_;
        }

        public IBlackboardStorage GetBlackboard()
        {
            return BtNodeBase?.Blackboard;
        }

        public BtWindowAsset GetWindowAsset()
        {
            return BtWindowAsset;
        }

        protected virtual void Update()
        {
            BtNodeBase?.RootNode?.Tick(BtNodeBase.Blackboard);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Creates a new BtWindowAsset and associates it with the behavior tree component.
        /// This process involves prompting the user to specify a save location and file name, creating a new ScriptableObject
        /// instance of type BtWindowAsset, and saving it to the specified location using Unity's serialized project asset format.
        /// The creation method ensures that the asset is saved immediately to disk and is properly configured by its
        /// corresponding AssetPostprocessor. This includes setting internal paths, initializing metadata, and creating auxiliary
        /// files required for extended behavior tree functionality. Additionally, the component's BtWindowAsset field is updated
        /// to reference the newly created asset.
        /// Key steps involved in this method:
        /// - Prompting the user with a save file dialog for the asset location.
        /// - Initializing and saving a new BtWindowAsset instance.
        /// - Updating the component's reference to the newly created asset.
        /// - Allowing AssetPostprocessor custom processing (e.g., initializing additional paths and files).
        /// - Ensuring changes are persisted to disk by marking assets as dirty and saving the project.
        /// If the user cancels the save dialog, the asset creation is aborted, and a log entry is made to indicate the cancellation.
        /// Once successfully created, the created asset is reloaded to ensure that the latest state, including modifications by
        /// the AssetPostprocessor, is correctly applied.
        /// Returns <c>true</c> if the asset creation process completes successfully; otherwise, <c>false</c> if the process was
        /// interrupted (e.g., the user cancels the creation dialog or an error occurs during creation).
        /// </summary>
        /// <returns>
        /// A <c>bool</c> value indicating whether the BtWindowAsset was successfully created and associated with the component.
        /// </returns>
        private bool CreateBtWindowAsset()
        {
            // 1.让用户选择保存路径和文件名（使用Unity的方法）
            var suggested_name = gameObject.name + "_BtWindowData"; // 基于 GameObject 给个默认名
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

            // 不等于空需要立马写
            if (!BtWindowAsset)
            {
                var created_asset = AssetDatabase.LoadAssetAtPath<BtWindowAsset>(asset_path);
                BtWindowAsset = created_asset;
                EditorUtility.SetDirty(this);
            }
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
                var created_asset = AssetDatabase.LoadAssetAtPath<BtWindowAsset>(asset_path);

                if (created_asset)
                {
                    BtWindowAsset = created_asset; // 赋值给组件字段
                    EditorUtility.SetDirty(this); // 标记这个组件已更改
                    ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(bt_space_,
                        new LogEntry(LogLevel.kInfo, $"Successfully created and assigned BtWindowAsset: {asset_path}"));

                    // 让新创建的资源在Project窗口中高亮显示
                    EditorGUIUtility.PingObject(created_asset);
                }
                else
                {
                    ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(bt_space_,
                        new LogEntry(LogLevel.kError,
                            $"Failed to load the created BtWindowAsset after creation/postprocessing at path: " +
                            $"{asset_path}. Check console for other errors."));
                }
            };

            return true;
        }
#endif

        [Button("打开行为树窗口")]
        public void OpenView()
        {
            // 只提醒不强制要求其需要数据，因为可以通过后面来保存
            if (!BtWindowAsset)
                if (EditorUtility.DisplayDialog("创建行为树资产", "当前行为树资产为空，是否创建行为树资产", "是", "否"))
                    CreateBtWindowAsset();
        
            if (BtWindowAsset)
            {
                if (!BtWindowAsset.ExternalDataFileExists())
                {
                    if (BtNodeBase.RootNode != null) SaveBtWindow();
                }
            }
            
            EditorBridge.Instance.OnOpenBehaviorTreeWindowRequested?.Invoke(tree_id_);
        }

        /// <summary>
        /// Associates a new behavior tree window asset with the current behavior tree instance
        /// and performs any necessary reset or re-initialization of the associated data.
        /// The method updates the internal reference to the provided `BtWindowAsset` and ensures
        /// the system correctly reflects the new asset.
        /// </summary>
        /// <param name="windowAsset">The new `BtWindowAsset` containing data paths and metadata
        /// required by the behavior tree system. The parameter must not be null, as valid assets
        /// are necessary for proper operation.</param>
        public virtual void SetBtWindowAsset(BtWindowAsset windowAsset)
        {
            if (windowAsset) BtWindowAsset = windowAsset;
            
            ResetWindowAsset();
        }

        /// <summary>
        /// Resets the behavior tree's current window asset and handles necessary data synchronization.
        /// This method ensures that the loaded behavior tree configuration corresponds to the specified asset
        /// and performs the following tasks:
        /// - Clears the existing tree data if the new asset differs from the previously loaded one.
        /// - Marks the tree as not initialized to allow for reinitialization with the new data.
        /// - Reloads data from the specified `BtWindowAsset` during Unity Editor mode for inspection or preview.
        /// - Updates the internal reference for the loaded asset's GUID to keep track of changes.
        /// - If a valid tree ID is present, unregisters the existing tree and registers it again with the behavior tree manager.
        /// - Marks the Unity component as "dirty," prompting the editor to persist any changes made to the component.
        /// This method is typically invoked when a new window asset is assigned or during object validation.
        /// </summary>
        private void ResetWindowAsset()
        {
            var current_asset_guild = BtWindowAsset ? BtWindowAsset.AssetGuid : string.Empty;
            
            // 如果当前指定的资产与已加载的资产不一致
            if (!current_asset_guild.Equals(loaded_asset_guid_))
            {
                // 清理旧数据
                ClearTreeData();
                
                // 标记为需要重新初始化
                has_been_set_up_ = false;

                if (BtWindowAsset)
                {
                    // 在编辑器模式下，立即加载新数据以供预览或检查
                    LoadTreeData();
                }
                
                // 更新已加载的资产GUID记录
                loaded_asset_guid_ = current_asset_guild;

                if (string.IsNullOrWhiteSpace(tree_id_))
                {
                    tree_id_=Guid.NewGuid().ToString();
                }
                // 重新注册
                if (BehaviorTreeManagers.instance.GetTree(tree_id_)!=null)
                {
                    BehaviorTreeManagers.instance.UnRegisterTree(tree_id_);
                    BehaviorTreeManagers.instance.RegisterTree(tree_id_, this);
                }
                else
                {
                    BehaviorTreeManagers.instance.RegisterTree(tree_id_, this);
                }
                
                // 标记此组件“脏”，以便Unity保存更改
                EditorUtility.SetDirty(this);
            }
        }

        public void SetTreeId(string tree_id)
        {
            // 没有进行初始化或者没注册就只进行赋值
            if (string.IsNullOrEmpty(tree_id_))
            {
                tree_id_ = tree_id;
                return;
            }
            
            if (!string.IsNullOrEmpty(tree_id))
            {
                // 取消原本注册的ID
                if (BehaviorTreeManagers.instance.GetTree(tree_id_)!=null)
                {
                    BehaviorTreeManagers.instance.UnRegisterTree(tree_id_);
                }
                
                // 对当前的ID进行注册
                tree_id_ = tree_id;
                BehaviorTreeManagers.instance.RegisterTree(tree_id_, this);
            }
        }

#if UNITY_EDITOR
        [Button("刷新行为树数据")]
        private void RefreshTreeData()
        {
            LoadTreeData();
            has_been_set_up_ = true;
        }
#endif
    }
}