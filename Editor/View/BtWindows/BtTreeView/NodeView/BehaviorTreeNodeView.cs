using System;
using BehaviorTree.BehaviorTrees;
using BehaviorTree.Nodes;
using Editor.View.BTWindows.BtTreeView.NodeView.NodeViewEdge;
using ExTools.Utillties;
using LogManager.Core;
using LogManager.LogManagerFactory;
using Script.BehaviorTree;
using Script.BehaviorTree.Save;
using Script.Utillties;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.View.BTWindows.BtTreeView.NodeView
{
    /// <summary>
    /// Represents a view for a Behavior Tree node in the Unity Editor.
    /// </summary>
    public class BehaviorTreeNodeView : BaseNodeView
    {
        #region 所属日志空间

        private static LogSpaceNode kNodeSpace =>
            new LogSpaceNode("BehaviourTreeWindows").AddChild("TreeView").AddChild("NodeView");

        #endregion

        #region 节点样式

        // 背景
        private VisualElement node_border_bar_;

        // 标题背景
        private VisualElement node_title_bar_;

        // 标题
        private Label title_label_;

        #endregion

        /// <summary>
        /// 构建基于<see cref="BaseNodeView"/>的行为树类
        /// </summary>
        /// <param name="node_data">行为树数据</param>
        /// <param name="new_rect">行为树大小以及位置，其会覆盖行为树数据当中的position和size属性</param>
        public BehaviorTreeNodeView(BtNodeBase node_data, Rect new_rect)
        {
            node_data_ = node_data;
            InitWindowSize(new_rect);
            InitializeView();
        }

        protected sealed override void InitializeView()
        {
            // 初始化端口和节点样式
            InitializePorts();

            // 仅修复内容填充问题，不改变整体外观
            FixContentFilling();

            // 将在被行为树显示面板添加之后才触发添加，防止直接添加而导致的查询错误
            schedule.Execute(() =>
            {
                var tree_view = GetFirstAncestorOfType<BehaviorTreeView>();
                InitNodeStyle();
                if (tree_view != null)
                {
                    // 从正确的 treeView 实例获取其对应的 connection_manager
                    var connection_manager = tree_view.GetPortConnectionManager();
                    input_port_?.AddManipulator(new PortDragHandler(input_port_, connection_manager));

                    output_port_?.AddManipulator(new PortDragHandler(output_port_, connection_manager));
                }
            });
        }

        // 温和的修复方法：只修复内容填充，不改变外观
        private void FixContentFilling()
        {
            // 只设置必要的flex属性，不改变width/height
            if (contentContainer != null) contentContainer.style.flexGrow = 1;

            // 确保主容器能够正确伸展（通常默认已设置，但确保一下）
            if (mainContainer != null) mainContainer.style.flexGrow = 1;
        }

        public override void UpdateView()
        {
            title = node_data_.NodeName;
            SetPosition(new Rect(node_data_.Position, node_data_.Size));

            // 更新子节点顺序
            if (node_data_ is BtComposite composite)
                composite.ChildNodes.Sort((x, y) => x.Position.x.CompareTo(y.Position.x));

            // 如果是Player模式并且有连接则开启效果
            if (Application.isPlaying && input_port_ is { connected: true })
            {
                foreach (var connection in input_port_.connections)
                    if (connection is FlowingEdge flowing_edge)
                    {
                        var should_flow = NodeData.NodeState != BehaviorState.kNonExecuting;

                        flowing_edge.EnableFlow = should_flow;

                        if (!should_flow) return;

                        switch (NodeData.NodeState)
                        {
                            case BehaviorState.kExecuting:
                                flowing_edge.CurrentState = EdgeState.kRunning;
                                break;
                            case BehaviorState.kFailure:
                                flowing_edge.CurrentState = EdgeState.kFailure;
                                break;
                            case BehaviorState.kSucceed:
                                flowing_edge.CurrentState = EdgeState.kSuccess;
                                break;
                        }
                    }
            }
            // 如果当前不是Player模式则默认是不开启效果
            else if (!Application.isPlaying && input_port_ is { connected: true })
            {
                foreach (var connection in input_port_.connections)
                    if (connection is FlowingEdge flowing_edge)
                        flowing_edge.EnableFlow = false;
            }
        }

        /// <summary>
        /// Configures the input and output ports for the behavior tree node view based on its data type.
        /// It initializes ports for node connections, updating the visual representation and ensuring proper functionality.
        /// </summary>
        private void InitializePorts()
        {
            title = node_data_.NodeName;
            if (node_data_ is BtMainNode)
            {
                output_port_ = CreateOutputPort(true);
            }
            else
            {
                input_port_ = CreatePort(Direction.Input, Port.Capacity.Single, typeof(BaseNodeView));
                output_port_ = node_data_ switch
                {
                    BtComposite => CreateOutputPort(false),
                    BtPrecondition => CreateOutputPort(true),
                    _ => output_port_
                };
            }

            if (input_port_ != null) inputContainer.Add(input_port_);

            if (output_port_ != null) outputContainer.Add(output_port_);

            RefreshExpandedState();
            RefreshPorts();
        }

        /// <summary>
        /// Asynchronously initializes the style for the behavior tree node view, including the border, title bar,
        /// and title label styles, based on the current node's style configuration.
        /// </summary>
        /// <remarks>
        /// This method applies the appropriate visual styles to the node's UI elements by retrieving the node style
        /// configuration and updating specific UI components. It is designed to ensure consistent visual representation
        /// while handling potential errors during the process.
        /// </remarks>
        /// <exception cref="System.Exception">
        /// Thrown when an exception occurs during the asynchronous operation.
        /// </exception>
        private void InitNodeStyle()
        {
            try
            {
                node_border_bar_ = this.Q<VisualElement>("node-border");
                node_title_bar_ = this.Q<VisualElement>("title");
                title_label_ = this.Q<Label>("title-label");

                // 获取当前承载的树
                var tree_view = GetFirstAncestorOfType<BehaviorTreeView>();
                if (tree_view == null) return;

                var node_style = tree_view.NodeStyleManager.TryGetNodeStyle(NodeData);
                ChangeMainStyle(node_style);
                ChangeTitleStyle(node_style);
                ChangeTitleLabelStyle(node_style);
            }
            catch (Exception e)
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(kNodeSpace,
                    new LogEntry(LogLevel.kWarning, $"协程错误，错误原因为: {e.Message}"));
            }
        }

        private void ChangeMainStyle(BtNodeStyle node_style)
        {
            if (node_style == null)
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogFileName).AddLog(kNodeSpace,
                    new LogEntry(LogLevel.kWarning, "" +
                                                    "节点风格为空，请先通过节点管理器进行查找"));
                return;
            }

            mainContainer.style.backgroundColor = node_style.BackgroundColor;
        }

        private void ChangeTitleStyle(BtNodeStyle node_style)
        {
            if (node_title_bar_ == null)
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogFileName)
                    .AddLog(kNodeSpace, new LogEntry(LogLevel.kWarning, "寻找标题栏失败，请先初始化标题栏"));
                return;
            }

            node_title_bar_.style.color = node_style.TitleBackgroundColor;
        }

        /// <summary>
        /// Initializes the window size of the Behavior Tree node view.
        /// </summary>
        /// <param name="new_rect">The size and position of the node view. The dimensions will be clamped to the minimum allowable width and height defined in <see cref="Script.Utillties.FixedValues"/>.</param>
        private void InitWindowSize(Rect new_rect)
        {
            capabilities |= Capabilities.Resizable;

            var now_height = Mathf.Max(FixedValues.kMinNodeHeight, new_rect.height);
            var now_width = Mathf.Max(FixedValues.kMinNodeWidth, new_rect.width);

            SetPosition(new Rect(new_rect.position, new Vector2(now_width, now_height)));

            style.minWidth = FixedValues.kMinNodeWidth;
            style.minHeight = FixedValues.kMinNodeHeight;

            RegisterCallback<GeometryChangedEvent>(GeometryChangedCallback);
        }

        public override void SetPosition(Rect newPos)
        {
            NodeData.Position = new Vector2(newPos.xMin, newPos.yMin);
            // 对节点的大小进行固定
            var node_width = Mathf.Max(FixedValues.kMinNodeWidth, newPos.width);
            var node_height = Mathf.Max(FixedValues.kMinNodeHeight, newPos.height);
            NodeData.Size = new Vector2(node_width, node_height);
            // 应用节点更改
            style.width = NodeData.Size.x;
            style.height = NodeData.Size.y;

            base.SetPosition(newPos);
        }

        /// <summary>
        /// Updates the style of the title label of the node based on the given node style settings.
        /// Adjusts the text color, font size, and optionally applies a text shadow if enabled.
        /// </summary>
        /// <param name="node_style">The style settings to apply to the title label. Contains properties for text color, font size, and shadow styling.</param>
        private void ChangeTitleLabelStyle(BtNodeStyle node_style)
        {
            if (title_label_ == null || node_style == null || node_title_bar_ == null) return;

            title_label_.style.color = node_style.TextColor;
            title_label_.style.fontSize = node_style.FontSize;
            if (node_style.EnableShadow)
                title_label_.style.textShadow = new TextShadow
                {
                    color = node_style.ShadowColor,
                    blurRadius = node_style.BlurRadius,
                    offset = node_style.ShadowOffset
                };
        }

        /// <summary>
        /// Links the node's output ports to the corresponding input ports
        /// of its connected child nodes or a single child node if applicable.
        /// </summary>
        public void LinkLine()
        {
            var graph_view = GetFirstAncestorOfType<BehaviorTreeView>();
            if (graph_view == null) return;
            switch (NodeData)
            {
                case BtComposite composite:
                    composite.ChildNodes.ForEach(n =>
                    {
                        graph_view.AddElement(LinkPort(output_port_,
                            graph_view.NodeViewManager.NodeViews[n.Guild].InputPort));
                    });
                    break;
                case BtPrecondition precondition:
                    if (precondition.ChildNode == null) return;
                    graph_view.AddElement(LinkPort(output_port_,
                        graph_view.NodeViewManager.NodeViews[precondition.ChildNode.Guild].InputPort
                    ));
                    break;
            }
        }

        private static Edge LinkPort(Port output_socket, Port input_socket)
        {
            var temp_edge = new FlowingEdge
            {
                output = output_socket,
                input = input_socket
            };
            temp_edge.AddEdgeEffect(new EdgeFlowIndicator());
            temp_edge.AddEdgeEffect(new EdgeGradientLine());
            temp_edge.input.Connect(temp_edge);
            temp_edge.output.Connect(temp_edge);
            return temp_edge;
        }

        public void SetRoot()
        {
            BehaviorTreeManagers.instance
                .GetTreeByWindowId(GetFirstAncestorOfType<BehaviorTreeView>().Windows.WindowInstanceId)
                .SetRoot(NodeData);
        }

        /// <summary>
        /// 构建节点端口
        /// </summary>
        /// <param name="direction">端口的类型</param>
        /// <param name="capacity">节点是否可以有多个连接。Port.Capacity.Multi为多个连接，Port.Capacity.Single为单个连接</param>
        /// <param name="type">端口类型数据</param>
        /// <returns>新的节点端口</returns>
        private static Port CreatePort(Direction direction, Port.Capacity capacity, Type type)
        {
            return Port.Create<FlowingEdge>(Orientation.Horizontal, direction, capacity, type);
        }

        /// <summary>
        /// 构建输出节点端口
        /// </summary>
        /// <param name="is_single">节点是否有多个连接</param>
        /// <returns>新的输出节点端口</returns>
        private static Port CreateOutputPort(bool is_single)
        {
            var port = Port.Create<FlowingEdge>(Orientation.Horizontal, Direction.Output,
                is_single ? Port.Capacity.Single : Port.Capacity.Multi, typeof(BaseNodeView));

            return port;
        }

        /// <summary>
        /// 节点几何回调函数，如果节点的大小发生了变化则会回调该函数，其保证了节点的大小有一个最小值.
        /// </summary>
        /// <param name="evt"></param>
        private void GeometryChangedCallback(GeometryChangedEvent evt)
        {
            var node_layout = layout;
            var width = node_layout.width;
            var height = node_layout.height;

            // 检查并纠正宽度
            if (width < FixedValues.kMinNodeWidth) width = FixedValues.kMinNodeWidth;

            // 检查并纠正高度
            if (height < FixedValues.kMinNodeHeight) height = FixedValues.kMinNodeHeight;

            SetPosition(
                new Rect(
                    node_layout.x, node_layout.y, width, height));
        }

        public override void ApplyStyle()
        {
            var node_style = GetFirstAncestorOfType<BehaviorTreeView>().NodeStyleManager.TryGetNodeStyle(NodeData);
            if (node_style == null) return;

            ChangeMainStyle(node_style);
            ChangeTitleStyle(node_style);
            ChangeTitleLabelStyle(node_style);
        }
    }
}