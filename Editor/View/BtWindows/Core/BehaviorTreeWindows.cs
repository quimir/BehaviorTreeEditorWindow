using System;
using System.Collections.Generic;
using System.Linq;
using BehaviorTree.BehaviorTrees;
using BehaviorTree.Core.WindowData;
using BehaviorTree.Nodes;
using Editor.EditorToolExs.BtNodeWindows;
using Editor.EditorToolExs.Operation.Storage;
using Editor.View.BTWindows.BtTreeView;
using Editor.View.BTWindows.BtTreeView.NodeView;
using Editor.View.BtWindows.BtTreeView.NodeView.Core;
using Editor.View.BtWindows.Core.EditorRestore;
using Editor.View.BtWindows.InspectorUI.Core;
using Editor.View.BtWindows.SearchBar;
using Editor.View.BtWindows.SearchBar.Core;
using Editor.View.BtWindows.SearchBar.Storage;
using ExTools.Utillties;
using LogManager.Core;
using LogManager.LogManagerFactory;
using Save.FileStorage;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.View.BtWindows.Core
{
    /// <summary>
    /// Represents a specialized editor window for managing and interacting with Behavior Trees.
    /// </summary>
    public class BehaviorTreeWindows : BehaviorTreeWindowsBase
    {
        /// <summary>
        /// Gets the currently focused BehaviorTreeWindows instance.
        /// </summary>
        /// <remarks>
        /// This static property holds a reference to the BehaviorTreeWindows object
        /// that is currently focused. It is set when the window gains focus and reset
        /// to null when the window loses focus. This allows other components to access
        /// the instance of the focused window as needed.
        /// </remarks>
        public static BehaviorTreeWindows FocusedWindow { get; private set; }

        public BehaviorTreeView BehaviorTreeView { get; private set; }
        public BehaviorTreeInspectorView BehaviorTreeInspectorView { get; private set; }
        private SplitView split_view_;

        private CompositeOperationManager operation_manager_;

        [NonSerialized] private static readonly LogSpaceNode log_space_ = new("BehaviourTreeWindows");

        [SerializeField] private VisualTreeAsset m_VisualTreeAsset;

        private VisualElement root_;

        private bool has_unsaved_changes_ = false;
        private bool is_loaded_from_file_ = false;
        private bool use_saved_ = false;
        
        private SearchView search_view_;
        private VisualElement background_veil_; // 背景遮罩，用于实现模糊效果

        [MenuItem("Window/UI Toolkit/BehaviourTreeWindows")]
        public static void ShowExample()
        {
            var temp_tree = CreateTempTree();
            BehaviorTreeManagers.instance.RegisterTree(temp_tree.GetTreeId(), temp_tree);

            CreateWindowForTrees<BehaviorTreeWindows>(temp_tree.GetTreeId());
        }

        protected override void InitWindow(string trees_id)
        {
            WindowInstanceId = string.IsNullOrEmpty(WindowInstanceId) ? Guid.NewGuid().ToString() : WindowInstanceId;
            var current_tree = BehaviorTreeManagers.instance.GetTree(trees_id);

            // 没有值或者没有注册行为树的时候禁止访问
            if (current_tree == null)
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                    new LogEntry(LogLevel.kError, "Cannot open window for an empty tree ID."), true);
                Close();
                return;
            }

            // 进行注册，防止莫名其妙的原因被另外的虚假窗口保存
            if (!BehaviorTreeManagers.instance.RegisterWindow(trees_id, WindowInstanceId))
            {
                // 如果不能注册，则说明注册了一个虚假的窗口，取消虚假窗口的绑定并且重新将窗口绑定回来
                BehaviorTreeManagers.instance.UnRegisterWindowFormTreeId(trees_id);

                BehaviorTreeManagers.instance.RegisterWindow(trees_id, WindowInstanceId);
            }

            if (current_tree.GetWindowAsset() != null)
                FileRecordManager.Instance.FilePathStorage.SetCurrentFile(
                    AssetDatabase.GetAssetPath(current_tree.GetWindowAsset()));

            CheckIfLoadedFromFile();

            BuildWindowUI();
        }

        private void CheckIfLoadedFromFile()
        {
            var behavior_tree = BehaviorTreeManagers.instance.GetTreeByWindowId(WindowInstanceId);
            if (behavior_tree == null) return;

            is_loaded_from_file_ = behavior_tree.GetWindowAsset() != null;
        }

        /// <summary>
        /// Constructs and initializes the UI elements for the behavior tree editor window.
        /// </summary>
        /// <remarks>
        /// This method is responsible for setting up the various components and views within
        /// the behavior tree editor window, including the tree view, inspector panels, and any
        /// associated state or operations manager. It also restores the window's state based on
        /// the relevant tree's stored data, if available.
        /// </remarks>
        /// <seealso cref="BehaviorTreeView"/>
        /// <seealso cref="BehaviorTreeInspectorView"/>
        /// <seealso cref="CompositeOperationManager"/>
        private void BuildWindowUI()
        {
            BehaviorTreeView = root_.Q<BehaviorTreeView>();
            BehaviorTreeView.Initialize(this);
            BehaviorTreeInspectorView = root_.Q<BehaviorTreeInspectorView>();
            BehaviorTreeInspectorView.AddInspectorPanel(new NodePropertiesPanel(BehaviorTreeView, new Foldout
                {
                    text = "节点属性",
                    value = true,
                    style =
                    {
                        display = DisplayStyle.None
                    }
                })
            );
            BehaviorTreeInspectorView.AddInspectorPanel(new NodeStylePanels(BehaviorTreeView, new Foldout
            {
                text = "节点样式", value = true, style = { display = DisplayStyle.None }
            }));
            split_view_ = root_.Q<SplitView>();

            BehaviorTreeView.CurrentViewState = BehaviorTreeView.ViewState.kInitializing;

            // 初始化操作历史器
            operation_manager_ = new CompositeOperationManager();
            operation_manager_.AddSubManagerOperation(BehaviorTreeView.OperationManager);
            operation_manager_.AddSubManagerOperation(BehaviorTreeInspectorView.OperationManager);
            operation_manager_.OnSaveStateChanged += OnSaveStateChanged;

            var current_tree =
                BehaviorTreeManagers.instance.GetTree(
                    BehaviorTreeManagers.instance.GetTreeIdByWindowId(WindowInstanceId));

            if (current_tree?.GetNodeWindow()?.NodeStyleMap != null)
                BehaviorTreeView.NodeStyleManager =
                    new BtNodeStyleManager(current_tree.GetNodeWindow().NodeStyleMap.ToDictionary());

            if (current_tree?.GetRoot() != null) BuildNodeViewFromRoot(current_tree.GetRoot());

            if (BehaviorTreeView.NodeViewManager.NodeViews.Any())
                BehaviorTreeView.nodes.OfType<BehaviorTreeNodeView>().ForEach(n => n.LinkLine());

            var tree_window_data = current_tree?.GetNodeWindow();

            if (tree_window_data?.EditorWindowData?.SplitViewWidth > 0)
                split_view_.FixedPaneInitialDimension = tree_window_data.EditorWindowData.SplitViewWidth;

            if (tree_window_data?.EditorWindowData?.WindowRect is { width: > 0 })
                position = tree_window_data.EditorWindowData.WindowRect;

            if (tree_window_data?.EditorWindowData?.GraphViewTransform != null)
            {
                BehaviorTreeView.viewTransform.position = tree_window_data.EditorWindowData.GraphViewTransform.position;
                BehaviorTreeView.viewTransform.scale = tree_window_data.EditorWindowData.GraphViewTransform.scale;
            }

            BehaviorTreeView.CurrentViewState = BehaviorTreeView.ViewState.kUserEditing;
        }

        private void OnSaveStateChanged()
        {
            var require_save = operation_manager_.RequireSave;
            if (has_unsaved_changes_ != require_save)
            {
                has_unsaved_changes_ = require_save;

                titleContent.text = titleContent.text.Replace("*", "");
                titleContent.text = require_save ? $"*{titleContent.text}" : titleContent.text;

                Repaint();
            }
        }

        private void BuildNodeViewFromRoot(BtNodeBase root_node)
        {
            if (root_node == null) return;

            var node_view = BehaviorTreeView.NodeViewManager.CreateNodeView(root_node);
            BehaviorTreeView.AddElement(node_view);
            switch (root_node)
            {
                case BtComposite composite:
                    composite.ChildNodes.ForEach(BuildNodeView);
                    break;
                case BtPrecondition precondition:
                    BuildNodeView(precondition.ChildNode);
                    break;
            }
        }

        private void BuildNodeView(BtNodeBase node_data)
        {
            if (node_data == null) return;

            var node_view = BehaviorTreeView.NodeViewManager.CreateNodeView(node_data);
            BehaviorTreeView.AddElement(node_view);

            switch (node_data)
            {
                case BtComposite composite:
                    composite.ChildNodes.ForEach(BuildNodeView);
                    break;
                case BtPrecondition precondition:
                    BuildNodeView(precondition.ChildNode);
                    break;
            }
        }

        public void InitializeForRestoration(string windowId, string treeId)
        {
            WindowInstanceId = windowId;

            // 在管理器中重新绑定于树的绑定关系
            BehaviorTreeManagers.instance.RegisterWindow(treeId, windowId);

            // 进行刷新
            RefreshWindow();
        }

        private void OnDestroy()
        {
            // 检查是否是编辑器退出
            if (BehaviorTreeEditorLifecycleManager.IsQuitting) return;

            SaveWindow();

            var behavior_tree = BehaviorTreeManagers.instance.GetTreeByWindowId(WindowInstanceId);

            if (is_loaded_from_file_)
            {
                // 关闭之后移除当前还存在于内存当中的颜色信息，防止记录文件无限膨胀
                var nodes = BehaviorTreeView.NodeViewManager.NodeViews.Keys.ToList();
                var node_styles = BehaviorTreeView.NodeStyleManager.NodeStyles.Keys.ToList();
                var result = node_styles.Except(nodes).ToList();
                behavior_tree.GetNodeWindow().NodeStyleMap.Entries.RemoveAll(e => result.Contains(e.NodeGuid));
                behavior_tree.SaveBtWindow();
            }

            if (behavior_tree is BehaviorTreeTemp)
                BehaviorTreeManagers.instance.UnRegisterTree(behavior_tree.GetTreeId());
            BehaviorTreeManagers.instance.UnRegisterWindowFromWindowId(WindowInstanceId);

            BehaviorTreeManagers.instance.SaveAllData();

            FileRecordManager.Instance.SaveFilePathStorage();

            MenuBar?.OnDestroy();
            BehaviorTreeInspectorView.Dispose();
            BehaviorTreeView.Dispose();
            operation_manager_.OnSaveStateChanged -= OnSaveStateChanged;
            operation_manager_.Dispose();
        }

        protected override void OnFocus()
        {
            base.OnFocus();
            FocusedWindow = this;

            if (root_ != null) root_.Focus();
        }

        protected override void OnLostFocus()
        {
            base.OnLostFocus();
            if (FocusedWindow == this) FocusedWindow = null;
        }

        protected override void CreateGUI()
        {
            base.CreateGUI();
            
            root_ = rootVisualElement;
            root_.focusable = true;
            var visual_tree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/Editor/Resource/BtWindows/BehaviourTreeWindows.uxml");
            var visual_uss =
                AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/Resource/BtWindows/BehaviourTreeWindows.uss");

            visual_tree.CloneTree(root_);
            root_.styleSheets.Add(visual_uss);

            var available_filters = new List<ISearchFilter>
            {
                new FuzzyScoreFilter(),
                new RegexFilter(),
                new CaseSensitiveFilter()
            };
            
            var search_controller=new SearchController(DataProvider, available_filters);
            
            search_view_=new SearchView(search_controller);

            root_.RegisterCallback<KeyDownEvent>(OnEditorWindowKeyDown, TrickleDown.TrickleDown);
        }
        
        private IEnumerable<ISearchableItem> DataProvider()
        {
            if (BehaviorTreeView == null)
            {
                return Enumerable.Empty<ISearchableItem>();
            }

            return BehaviorTreeView.Query<BehaviorTreeNodeView>()
                .ToList()
                .Select(node_view => new NodeSearchableItem(node_view,BehaviorTreeView));
        }

        private void OnEditorWindowKeyDown(KeyDownEvent evt)
        {
            // 检查当前窗口是否获得聚焦，或者事件是否来自当前窗口.
            if (focusedWindow != this) return;

            if (BehaviorTreeView == null) return;

            var event_handled = false;

            switch (evt.keyCode)
            {
                case KeyCode.Delete:
                    BehaviorTreeView?.HandleDeleteSelection();
                    event_handled = true;
                    break;
            }

            var is_modifier =
                Application.platform == RuntimePlatform.OSXEditor ? evt.commandKey : evt.ctrlKey;
            
            if (search_view_!=null&&search_view_.style.display==DisplayStyle.Flex)
            {
                if (is_modifier && evt.keyCode == KeyCode.F)
                {
                    search_view_.Toggle(root_);
                    evt.StopPropagation();
                    evt.PreventDefault();
                    return;
                }
            }

            if (is_modifier)
                switch (evt.keyCode)
                {
                    case KeyCode.F:
                    {
                        search_view_?.Toggle(root_);
                        event_handled = true;
                        break;
                    }
                    case KeyCode.S:
                        use_saved_ = true;
                        SaveWindow();
                        MarkAsSaved();
                        titleContent.text = titleContent.text.Replace("*", "");
                        use_saved_ = false;
                        event_handled = true;
                        break;
                    case KeyCode.C:
                        BehaviorTreeView?.HandleCopyNode();
                        event_handled = true;
                        break;
                    case KeyCode.V:
                        BehaviorTreeView?.HandlePasteNode();
                        event_handled = true;
                        break;
                    case KeyCode.X:
                        BehaviorTreeView?.HandleCutNodeData();
                        event_handled = true;
                        break;
                    case KeyCode.A:
                        var node_views = BehaviorTreeView.Query<BehaviorTreeNodeView>().ToList();
                        BehaviorTreeView?.AddSelectedNode(node_views);
                        event_handled = true;
                        break;
                    case KeyCode.D:
                        BehaviorTreeView?.HandleCopyNode();
                        BehaviorTreeView?.HandlePasteNode();
                        event_handled = true;
                        break;
                    case KeyCode.Z:
                        if (evt.shiftKey)
                        {
                            if (operation_manager_.CanRedo) operation_manager_.Redo();
                        }
                        else
                        {
                            if (operation_manager_.CanUndo) operation_manager_.Undo();
                        }

                        event_handled = true;
                        break;
                    case KeyCode.Y:
                        if (operation_manager_.CanRedo) operation_manager_.Redo();
                        event_handled = true;
                        break;
                }

            // 如果事件被处理了，阻止它继续传播到Unity Editor
            if (event_handled)
            {
                evt.StopPropagation();
                evt.PreventDefault();
            }
        }

        private void MarkAsSaved()
        {
            operation_manager_.MarkAsSaved();
        }

        protected override void OnInspectorUpdate()
        {
            base.OnInspectorUpdate();
            if (BehaviorTreeView == null) return;

            BehaviorTreeView.nodes.OfType<BaseNodeView>().ForEach(n => n.UpdateView());

            Repaint();
        }

        public override void RefreshWindow()
        {
            base.RefreshWindow();
            var tree_id = BehaviorTreeManagers.instance.GetTreeIdByWindowId(WindowInstanceId);
            if (string.IsNullOrEmpty(tree_id))
            {
                // 如果找不到关联数据，则关闭窗口
                Close();
                return;
            }

            if (rootVisualElement == null) return;

            // 清空并构建UI
            rootVisualElement.Clear();
            // 重新构建UI防止出现未知错误
            CreateGUI();
            BuildWindowUI();
        }

        public override void SaveWindow()
        {
            if (BehaviorTreeView == null) return;

            if (use_saved_)
            {
                SaveNodeWindowData();
                return;
            }

            if (operation_manager_.RequireSave)
            {
                var is_open = EditorUtility.DisplayDialog("保存提示", "检查到当前文件进行变动没有保存，是否需要进行保存？",
                    "是", "否");

                if (!is_open) return;

                SaveNodeWindowData();

                return;
            }

            if (is_loaded_from_file_) SaveNodeWindowData();
        }

        private void SaveNodeWindowData()
        {
            var active_tree_window = BehaviorTreeManagers.instance.GetTreeByWindowId(WindowInstanceId);

            if (active_tree_window == null) return;

            var active_tree_window_data = active_tree_window.GetNodeWindow();

            var transform_view = new GraphViewTransform
            {
                position = BehaviorTreeView.viewTransform.position,
                scale = BehaviorTreeView.viewTransform.scale,
                rotation = BehaviorTreeView.viewTransform.rotation
            };

            if (active_tree_window_data != null)
            {
                active_tree_window_data.NodeStyleMap =
                    BtNodeStyleCollection.FromDictionary(BehaviorTreeView.NodeStyleManager.NodeStyles);
                // 防止出现为空的情况
                if (active_tree_window_data.EditorWindowData == null)
                    active_tree_window_data = new BehaviorTreeWindowData();

                active_tree_window_data.EditorWindowData.GraphViewTransform = transform_view;
                active_tree_window_data.EditorWindowData.WindowRect = position;
                active_tree_window_data.EditorWindowData.SplitViewWidth = split_view_.FixedPaneInitialDimension;
            }

            active_tree_window.SaveBtWindow();
            is_loaded_from_file_ = active_tree_window.GetWindowAsset();

            if (active_tree_window is BehaviorTreeTemp tree_temp)
                if (EditorUtility.DisplayDialog("附着对象", "是否选择将当前的行为树数据附着到游戏对象当中", "是", "否"))
                    BehaviorTreeTransferWindow.ShowWindow((selected_game_object) =>
                        {
                            var result = tree_temp.AttachToGameObject(selected_game_object);
                            if (result != null)
                                EditorUtility.DisplayDialog("成功", $"行为树已经成功附着到 {selected_game_object.name}", "确定");
                            BehaviorTreeManagers.instance.UnRegisterWindowFormTreeId(result.GetTreeId());
                            BehaviorTreeManagers.instance.RegisterWindow(result.GetTreeId(), WindowInstanceId);
                        },
                        () =>
                        {
                            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace)
                                .AddLog(log_space_, new LogEntry(LogLevel.kInfo, "用户取消了临时树的附着"));
                        });

            FileRecordManager.Instance.FilePathStorage.AddOrUpdateFile(
                AssetDatabase.GetAssetPath(active_tree_window.GetWindowAsset()));
        }
    }
}