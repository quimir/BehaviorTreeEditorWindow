using System;
using System.Collections.Generic;
using System.Linq;
using BehaviorTree;
using BehaviorTree.Nodes;
using Editor.EditorToolExs;
using Editor.EditorToolExs.BtNodeWindows;
using Editor.EditorToolExs.Operation;
using Editor.EditorToolExs.Operation.Storage;
using Editor.View.BtWindows.BtTreeView;
using Editor.View.BtWindows.BtTreeView.NodeView;
using Editor.View.BTWindows.BtTreeView.NodeView;
using Editor.View.BtWindows.BtTreeView.NodeView.Core;
using Editor.View.BTWindows.BtTreeView.NodeView.NodeViewEdge;
using Editor.View.BtWindows.BtTreeView.Operation;
using Editor.View.BTWindows.BtTreeView.Operation;
using Editor.View.BtWindows.BtTreeView.PortConnectionManager;
using Editor.View.BtWindows.Core;
using Editor.View.BtWindows.NodeMenuProvider;
using Editor.View.BTWindows.NodeMenuProvider;
using ExTools.Utillties;
using LogManager.Core;
using LogManager.LogManagerFactory;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.View.BTWindows.BtTreeView
{
    /// <summary>
    /// Represents a custom graph view for creating and managing behavior trees in the Unity editor.
    /// </summary>
    /// <remarks>
    /// This class is responsible for managing the visual representation of a behavior tree, including
    /// nodes, edges, and various user operations like copy, paste, cut, and delete. It provides an
    /// interface for handling node view management, styling, and operations, such as connecting or
    /// deleting nodes.
    /// </remarks>
    public class BehaviorTreeView : GraphView,IDisposable
    {
        #region 数据储存

        private BtNodeViewManager node_view_manager_;

        public BtNodeViewManager NodeViewManager => node_view_manager_;

        private BtNodeStyleManager node_style_manager_;

        public BtNodeStyleManager NodeStyleManager
        {
            get => node_style_manager_;
            set => node_style_manager_ = value;
        }

        #region 检查是否需要更新显示面板

        /// <summary>
        /// Tracks the previously selected elements in the graphical user interface
        /// to detect and respond to selection changes within the tree view.
        /// </summary>
        private HashSet<ISelectable> previous_selection_;

        /// <summary>
        /// Indicates whether a selection change check has been scheduled in the tree view.
        /// Prevents multiple checks from being scheduled concurrently by ensuring that the
        /// selection change logic is only executed when necessary.
        /// </summary>
        private bool selection_check_scheduled_ = false;

        #endregion

        #endregion

        #region 历史操作

        /// <summary>
        /// Stores the starting positions of nodes during a drag operation
        /// to track and process movement within the behavior tree view.
        /// </summary>
        private readonly Dictionary<BehaviorTreeNodeView, Vector2> node_drag_start_positions = new();

        private OperationManager operation_manager_;

        public OperationManager OperationManager => operation_manager_;

        public const bool kOutConsole = false;

        public enum ViewState
        {
            /// <summary>
            /// 初始化状态
            /// </summary>
            kInitializing,

            /// <summary>
            /// 用户编辑状态
            /// </summary>
            kUserEditing
        }

        private ViewState current_view_state_ = ViewState.kInitializing;

        public ViewState CurrentViewState
        {
            get => current_view_state_;
            set => current_view_state_ = value;
        }

        #endregion

        private BehaviorTreeWindows windows_;

        public BehaviorTreeWindows Windows => windows_;

        #region 右键菜单及端点管理器

        /// <summary>
        /// Handles the management and display of the node creation menu within the tree view.
        /// Implements the <see cref="INodeMenuProvider"/> interface to provide menu options
        /// for node creation and interaction during graph editing.
        /// </summary>
        private INodeMenuProvider node_menu_provider_;

        /// <summary>
        /// Manages the connections between node ports in the tree view. Handles actions such as
        /// starting, updating, and ending port dragging, as well as handling menu requests
        /// and the creation of new edges (connections). Implements the <see cref="IPortConnectionManager"/> interface.
        /// </summary>
        private IPortConnectionManager port_connection_manager_;

        #endregion

        #region Debug日志空间

        private static readonly LogSpaceNode log_space_ = new LogSpaceNode("BehaviourTreeWindows").AddChild("TreeView");

        #endregion

        public class uxml_factory : UxmlFactory<BehaviorTreeView, UxmlTraits>
        {
        }

        #region 初始化部分

        /// <summary>
        /// Sets up the behavior tree view by initializing its components, applying style configurations,
        /// and registering necessary callbacks for user interactions and updates. It also prepares the
        /// view for integration with the provided BehaviorTreeWindows instance.
        /// </summary>
        /// <param name="windows">The BehaviorTreeWindows instance that manages and interacts with the behavior
        /// tree view.</param>
        public void Initialize(BehaviorTreeWindows windows)
        {
            windows_ = windows;

            Insert(0, new GridBackground());
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var style_sheet =
                AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/Resource/BtWindows/TreeView.uss");

            styleSheets.Add(style_sheet);

            InitializeManager();

            graphViewChanged -= OnGraphViewChanged;
            graphViewChanged += OnGraphViewChanged;

            RegisterCallback<MouseDownEvent>(OnMouseDownControl, TrickleDown.TrickleDown);
            RegisterCallback<MouseUpEvent>(OnGraphViewMouseUp);

            RegisterCallback<PointerUpEvent>(OnPotentialSelectionChange);

            // 首先收集现在是否已经添加了面板
            previous_selection_ = new HashSet<ISelectable>(selection);
            if (previous_selection_.Count > 0) windows_.BehaviorTreeInspectorView?.UpdateViewData(previous_selection_);
        }

        public BehaviorTreeView()
        {
            var style_sheet =
                AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/Resource/BtWindows/TreeView.uss");

            styleSheets.Add(style_sheet);
        }

        private void OnPotentialSelectionChange<T>(T evt) where T : EventBase
        {
            // 如果检查尚未安排
            if (!selection_check_scheduled_)
            {
                selection_check_scheduled_ = true;

                schedule.Execute(() =>
                {
                    CheckSelectionChanged();
                    selection_check_scheduled_ = false;
                }).ExecuteLater(0);
            }
        }

        /// <summary>
        /// Compares the current selection in the tree view with the previously recorded selection.
        /// If the selection has changed, it triggers an update to the inspector view to reflect
        /// the new selection state and updates the internal record of the selection.
        /// </summary>
        private void CheckSelectionChanged()
        {
            var current_selection = new HashSet<ISelectable>(selection);

            if (current_selection.SetEquals(previous_selection_)) return;

            windows_.BehaviorTreeInspectorView?.UpdateViewData(current_selection);

            previous_selection_ = current_selection;
        }

        private void InitializeManager()
        {
            node_view_manager_ = new BtNodeViewManager(this);

            node_style_manager_ = new BtNodeStyleManager();

            port_connection_manager_ = new PortConnectionManager(this);

            // 连接事件
            port_connection_manager_.OnMenuRequested += position =>
            {
                // 当拖拽结束且需要显示菜单时，显示节点创建菜单
                var pending_port = port_connection_manager_.GetActivePort();
                position = windows_.position.position + position;
                node_menu_provider_.ShowMenu(position, pending_port);
            };

            port_connection_manager_.OnEdgeCreated += edge =>
            {
                // 当创建新连接时，通知图表变更
                var changes = new GraphViewChange
                {
                    edgesToCreate = new List<Edge> { edge }
                };

                graphViewChanged?.Invoke(changes);
            };

            node_menu_provider_ = ScriptableObject.CreateInstance<SearchWindowNodeMenuProviders>();
            if (node_menu_provider_ is SearchWindowNodeMenuProviders providers)
                providers.Initialize(windows_, this, CreateNodeAtPosition);

            nodeCreationRequest += context => { node_menu_provider_.ShowMenu(context.screenMousePosition); };

            operation_manager_ = new OperationManager();
        }

        #endregion

        #region 键盘操作

        public void HandleCutNodeData()
        {
            if (selection.Count == 0) return;
            
            CutNodeData();
        }

        public void CutNodeData()
        {
            CopyNodeData();
            var selected_nodes = selection.OfType<BaseNodeView>().ToList();
            DeleteNodeDataWithOperation(selected_nodes);
        }

        public void HandleCopyNode()
        {
            if (selection.Count==0)
            {
                return;
            }
            
            CopyNodeData();
        }

        public void HandlePasteNode()
        {
            if (!CopyNodeDataManager.Instance.IsCopyNode)
            {
                return;
            }
            
            PasteNodeData();
        }

        public void AddSelectedNode(List<BehaviorTreeNodeView> node_views)
        {
            if (node_views == null || node_views.Count < 0) return;

            ClearSelection();

            // 将新创建的节点视图加入到GraphView的选择中
            foreach (var node_view in node_views)
                // GraphView的selection集合会自动更新视图的选中状态
                AddToSelection(node_view);

            FrameSelection();
        }

        public bool HandleDeleteSelection()
        {
            if (selection.Count==0)
            {
                return false;
            }
            
            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace)
                .AddLog(log_space_, new LogEntry(LogLevel.kInfo, "触发移除操作"), kOutConsole);
            
            var selected_nodes = selection.OfType<BaseNodeView>().ToList();
            var selected_nodes_edge = selection.OfType<Edge>().ToList();
            
            selection.Clear();
            ClearSelection();
            
            schedule.Execute(() =>
            {
                try
                {
                    operation_manager_.BeginOperationGroup();
                    var deleted_nodes = new DeleteNodesCompositeOperation(selected_nodes, this);
                    operation_manager_.ExecuteOperation(deleted_nodes);
                    foreach (var edge in selected_nodes_edge)
                    {
                        var disconnect_operation = new RemoveEdgeOperation(edge, node_view_manager_);
                        operation_manager_.ExecuteOperation(disconnect_operation);
                    }
                    operation_manager_.EndOperationGroup();
                }
                catch (Exception ex)
                {
                    ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                        new LogEntry(LogLevel.kError,
                            $"删除节点时发生错误: {ex.Message}\n{ex.StackTrace}"), true);
                }
            }).ExecuteLater(10); // 尽可能小的延迟

            return true;
        }

        #endregion

        private void OnMouseDownControl(MouseDownEvent evt)
        {
            // 记录所有选中节点的起始位置
            if (evt.button == 0)
                foreach (var selected_element in selection)
                    if (selected_element is BehaviorTreeNodeView node_view)
                        node_drag_start_positions[node_view] = node_view.GetPosition().position;
        }

        private void OnGraphViewMouseUp(MouseUpEvent evt)
        {
            // 左键单击
            if (evt.button == 0)
            {
                // 检查是否有节点被移动
                var any_node_moved = false;
                var move_nodes = new List<(BehaviorTreeNodeView node, Vector2 old_pos, Vector2 new_pos)>();

                foreach (var kvp in node_drag_start_positions)
                {
                    var node_view = kvp.Key;
                    var start_pos = kvp.Value;
                    var current_pos = node_view.GetPosition().position;

                    if (start_pos != current_pos)
                    {
                        any_node_moved = true;
                        move_nodes.Add((node_view, start_pos, current_pos));
                    }
                }

                if (any_node_moved && current_view_state_ == ViewState.kUserEditing)
                {
                    var move_operation = new MoveNodeViewOperation(move_nodes);
                    operation_manager_.ExecuteOperation(move_operation);
                    ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                        new LogEntry(LogLevel.kInfo, "成功将移动节点操作添加至消息中心"), kOutConsole);
                }

                // 请除起始位置记录
                node_drag_start_positions.Clear();
            }
        }

        /// <summary>
        /// Retrieves a node view by its unique identifier (GUID). Searches through
        /// all existing node views to locate the one matching the specified GUID.
        /// </summary>
        /// <param name="guid">The unique identifier of the node view to retrieve.</param>
        /// <returns>The <see cref="BaseNodeView"/> object corresponding to the provided GUID, or null if no match is found.</returns>
        public BaseNodeView GetNodeViewByGuid(string guid)
        {
            // 先使用缓冲机制查找，实在查找不到再使用本地查找
            if (node_view_manager_.NodeViews.TryGetValue(guid, out var node_view)) return node_view;

            return nodes.ToList().OfType<BaseNodeView>().FirstOrDefault(n => n.NodeData.Guild == guid);
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange graph_view_change)
        {
            if (operation_manager_.IsOperationInProgress)
            {
                return graph_view_change;
            }
            
            // 只在用户变价状态下记录操作
            if (current_view_state_ == ViewState.kUserEditing)
            {
                // 处理边的创建
                if (graph_view_change.edgesToCreate != null)
                    foreach (var edge in graph_view_change.edgesToCreate)
                    {
                        var connect_operation = new ConnectEdgeOperation(edge, this);
                        operation_manager_.ExecuteOperation(connect_operation);
                    }

                // 处理元素删除
                if (graph_view_change.elementsToRemove != null)
                {
                    // 分别处理边删除和节点删除
                    var edges_to_remove=graph_view_change.elementsToRemove.OfType<Edge>().ToList();
                    var nodes_to_remove = graph_view_change.elementsToRemove.OfType<BaseNodeView>().ToList();
                    
                    operation_manager_.BeginOperationGroup();

                    try
                    {
                        // 处理单独的边删除（不属于节点删除的边）
                        var standalone_edges = edges_to_remove.Where(edge => !nodes_to_remove.Any(node =>
                            (node.InputPort != null && node.InputPort.connections.Contains(edge)) || (node.OutputPort
                                != null && node.OutputPort.connections.Contains(edge)))).ToList();

                        foreach (var edge in standalone_edges)
                        {
                            var disconnect_operation = new RemoveEdgeOperation(edge, node_view_manager_);
                            operation_manager_.ExecuteOperation(disconnect_operation);
                        }

                        // 处理节点删除（包括相关的边）
                        if (nodes_to_remove.Any())
                        {
                            var delete_operation =
                                new DeleteNodesCompositeOperation(nodes_to_remove.Cast<BaseNodeView>().ToList(), this);
                            operation_manager_.ExecuteOperation(delete_operation);
                        }
                    }
                    finally
                    {
                        operation_manager_.EndOperationGroup();
                    }
                }
            }
            else
            {
                // 初始化状态下，只执行操作不记录
                graph_view_change.edgesToCreate?.ForEach(edge => { EditorExTools.Instance.LinkLineAddData(edge); });

                graph_view_change.elementsToRemove?.ForEach(element =>
                {
                    if (element is Edge edge) EditorExTools.Instance.UnLinkLineDelete(edge);
                });
            }

            return graph_view_change;
        }

        /// <summary>
        /// 复制行为树的节点，原理是通过序列化和反序列将数据独立出来
        /// </summary>
        public void CopyNodeData()
        {
            if (selection.OfType<BehaviorTreeNodeView>().ToList().Count == 0) return;
            var node_data = selection.OfType<BehaviorTreeNodeView>().Select(view => view.NodeData).ToList();

            CopyNodeDataManager.Instance.AddNodeData(node_data);
        }

        public void PasteNodeData()
        {
            if (!CopyNodeDataManager.Instance.IsCopyNode)
            {
                return;
            }

            Vector2 mouse_position = Event.current.mousePosition;

            List<BtNodeBase> node_to_paste = CopyNodeDataManager.Instance.GetNodeDataAndResetGuid();

            var paste_operation = new PasteNodeOperation(node_to_paste, mouse_position, this);
            
            operation_manager_.ExecuteOperation(paste_operation);
        }

        #region 删除节点

        public void DeleteNodeData(List<BaseNodeView> nodes_to_delete)
        {
            if (nodes_to_delete == null || !nodes_to_delete.Any())
                return;
            
            NodeViewManager.DeleteNodeData(nodes_to_delete);
        }

        public void DeleteNodeDataWithOperation(List<BaseNodeView> nodes_to_delete)
        {
            if (nodes_to_delete == null || !nodes_to_delete.Any())
                return;

            var delete_operation = new DeleteNodesCompositeOperation(nodes_to_delete, this);
            operation_manager_.ExecuteOperation(delete_operation);
        }

        #endregion

        #region 构建节点

        /// <summary>
        /// Creates a new node of the specified type at a given position within the graph view.
        /// If a pending port is provided, establishes a connection between the created node
        /// and the pending port. Updates the operation history if the view is in user editing mode.
        /// </summary>
        /// <param name="nodeType">The type of the node to be created. Must not be null.</param>
        /// <param name="position">The position in the graph where the new node will be placed.</param>
        /// <param name="pendingPort">An optional port that this node will connect to upon creation.</param>
        private void CreateNodeAtPosition(Type nodeType, Vector2 position, Port pendingPort = null)
        {
            if (nodeType == null) return;

            var node = node_view_manager_.CreateNodeView(nodeType, position);
            if (node == null) return;

            // 添加节点到图表
            AddElement(node);
            FlowingEdge edge = null;

            // 如果有待连接的端口，创建连接
            if (pendingPort != null)
            {
                var nodeView = node as BehaviorTreeNodeView;

                // 检查是否为单一输出如果为单一输出则删除之前的所有的连接
                if (pendingPort.capacity == Port.Capacity.Single)
                {
                    // 断开现有的输出连接
                    var existingConnections = pendingPort.connections.ToList();
                    foreach (var existingEdge in existingConnections)
                    {
                        // 断开现有的连接
                        existingEdge.input.Disconnect(existingEdge);
                        existingEdge.output.Disconnect(existingEdge);
                        RemoveElement(existingEdge);
                    }
                }

                if (pendingPort.direction == Direction.Output)
                    edge = new FlowingEdge
                    {
                        output = pendingPort,
                        input = nodeView?.InputPort
                    };
                else
                    edge = new FlowingEdge
                    {
                        output = nodeView?.OutputPort,
                        input = pendingPort
                    };

                if (edge.input != null && edge.output != null)
                {
                    edge.AddEdgeEffect(new EdgeFlowIndicator());
                    edge.AddEdgeEffect(new EdgeGradientLine());
                }
            }

            // 只在用户编辑状态下记录操作
            if (current_view_state_ == ViewState.kUserEditing)
            {
                operation_manager_.BeginOperationGroup();
                var create_operation = new CreateNodeViewOperation(node.NodeData, this);
                operation_manager_.ExecuteOperation(create_operation);
                var create_edge_operation = new ConnectEdgeOperation(edge, this);
                operation_manager_.ExecuteOperation(create_edge_operation);
                operation_manager_.EndOperationGroup();
            }
        }

        #endregion

        #region 节点判断

        private bool IsPortCompatible(Port source_port, Port target_port)
        {
            // 如果是单一端口，确保没有已存在的连接
            if (source_port.capacity == Port.Capacity.Single && source_port.connected) return false;

            return true;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports
                .Where(end_ports =>
                    // 方向不同（输入和输出）
                    end_ports.direction != startPort.direction &&
                    // 不是同一个节点的端口
                    end_ports.node != startPort.node &&
                    // 对于单一连接的端口，检查是否已经有连接
                    (end_ports.capacity != Port.Capacity.Single || !end_ports.connected) &&
                    // 自定义端口检查
                    IsPortCompatible(startPort, end_ports))
                .ToList();
        }

        #endregion

        /// <summary>
        /// Retrieves the manager responsible for handling port connections within the behavior tree view.
        /// The manager facilitates operations such as dragging, connecting, and managing ports between nodes.
        /// </summary>
        /// <returns>An instance of <see cref="IPortConnectionManager"/> that manages port connection operations.</returns>
        public IPortConnectionManager GetPortConnectionManager()
        {
            return port_connection_manager_;
        }

        public void MarkAsSaved()
        {
            operation_manager_.MarkAsSaved();
        }

        #region 自定义右键菜单

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("创建节点", a =>
            {
                if (!BehaviorTreeWindows.FocusedWindow) return;

                node_menu_provider_.ShowMenu(windows_.position.position + a.eventInfo.localMousePosition);
            });
            evt.menu.AppendSeparator();

            var selection_list = selection.OfType<BaseNodeView>().ToList();
            if (selection_list.Count == 1)
            {
                var selection_node_data = selection_list[0];
                var selection_node_view = selection_node_data as BehaviorTreeNodeView;
                evt.menu.AppendAction("设置根节点", _ =>
                {
                    if (selection_node_view != null) selection_node_view.SetRoot();
                });
                evt.menu.AppendSeparator();
            }

            evt.menu.AppendAction("全选节点", _ =>
            {
                var node_views = this.Query<BehaviorTreeNodeView>().ToList();
                AddSelectedNode(node_views);
            }, _ => selection_list.Count > 0 ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);

            evt.menu.AppendAction("复制节点", _ => CopyNodeData(),
                _ => selection_list.Count > 0
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("剪切节点", _ =>
            {
                CopyNodeData();
                DeleteNodeDataWithOperation(selection_list);
            }, _ => selection_list.Count > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("粘贴节点", _ => PasteNodeData(),
                _ => CopyNodeDataManager.Instance.IsCopyNode ? 
                    DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendSeparator();

            evt.menu.AppendAction("删除节点", _ => DeleteNodeDataWithOperation(selection_list),
                _ => selection_list.Count > 0
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendSeparator();

            var selection_list_edge = selection.OfType<Edge>().ToList();
            if (selection_list_edge.Count > 0)
                evt.menu.AppendAction("解除两者连线", _ => node_view_manager_.DeleteEdgeAndData(selection_list_edge));
        }

        #endregion


        public void Dispose()
        {
            node_view_manager_?.Dispose();
        }
    }
}