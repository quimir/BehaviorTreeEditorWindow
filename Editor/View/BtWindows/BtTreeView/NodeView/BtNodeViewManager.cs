using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BehaviorTree.BehaviorTrees;
using BehaviorTree.Nodes;
using Editor.View.BtWindows.BtTreeView.NodeView.Core;
using ExTools;
using ExTools.Utillties;
using LogManager.Core;
using LogManager.LogManagerFactory;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

namespace Editor.View.BTWindows.BtTreeView.NodeView
{
    /// <summary>
    /// Provides management functionality for handling behavior tree node views within an editor.
    /// </summary>
    public class BtNodeViewManager : IDisposable
    {
        private static readonly LogSpaceNode log_space_ =
            new LogSpaceNode("BehaviourTreeWindows").AddChild("TreeView").AddChild("BtNodeViewManager");

        private readonly Dictionary<string, BaseNodeView> node_views_ = new();

        /// <summary>
        /// Provides access to a dictionary containing node views, allowing management and interaction
        /// with various node views within the behavior tree editor framework.
        /// </summary>
        /// <remarks>
        /// The dictionary is keyed by a unique string identifier for each node and maps to instances
        /// of <see cref="BaseNodeView"/>, which represent the visual and logical aspects of the nodes
        /// in the behavior tree editor.
        /// </remarks>
        public Dictionary<string, BaseNodeView> NodeViews => node_views_;

        private readonly BehaviorTreeView tree_view_;

        /// <summary>
        /// Manages the creation, deletion, and retrieval of node views within a behavior tree editor.
        /// </summary>
        public BtNodeViewManager(BehaviorTreeView treeView)
        {
            tree_view_ = treeView;
        }

        /// <summary>
        /// Removes a node view from the internal dictionary using the specified unique identifier.
        /// </summary>
        /// <param name="guid">The unique identifier of the node view to be removed from the dictionary.</param>
        public void RemoveNodeViewFromDictionary(string guid)
        {
            node_views_.Remove(guid);
            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                new LogEntry(LogLevel.kInfo, $"从字典中移除节点视图: {guid}"));
        }

        /// <summary>
        /// Adds a node view to the internal dictionary using its unique identifier as the key.
        /// </summary>
        /// <param name="node_view">The node view to be added to the dictionary. Must contain a unique identifier in
        /// its <c>NodeData.Guild</c> property.</param>
        public void AddNodeViewToDictionary(BaseNodeView node_view)
        {
            node_views_[node_view.NodeData.Guild] = node_view;
            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                new LogEntry(LogLevel.kInfo, $"添加节点视图到字典: {node_view.NodeData.Guild}"));
        }

        /// <summary>
        /// Removes a specified graphical element from the behavior tree view.
        /// </summary>
        /// <param name="element">The graphical element to be removed.</param>
        public void RemoveElementFromView(GraphElement element)
        {
            tree_view_.RemoveElement(element);
            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                new LogEntry(LogLevel.kInfo, $"从视图中移除元素: {element.GetType().Name}"));
        }

        /// <summary>
        /// Adds a graphical element to the behavior tree view and logs the operation.
        /// </summary>
        /// <param name="element">The graphical element that will be added to the tree view.</param>
        public void AddElementToView(GraphElement element)
        {
            tree_view_.AddElement(element);
            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                new LogEntry(LogLevel.kInfo, $"添加元素到视图: {element.GetType().Name}"));
        }

        /// <summary>
        /// Disconnects the specified edge from its input and output ports in the behavior tree node view.
        /// Also logs the disconnection event for monitoring purposes.
        /// </summary>
        /// <param name="edge">The edge to be disconnected from input and output ports.</param>
        public void DisconnectEdge(Edge edge)
        {
            edge.input.Disconnect(edge);
            edge.output.Disconnect(edge);
            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                new LogEntry(LogLevel.kInfo, "断开边连接"));
        }

        /// <summary>
        /// Establishes a connection between the input and output ports of the specified edge in the behavior tree editor.
        /// </summary>
        /// <param name="edge">The edge object representing the connection to be established in the behavior tree graph.</param>
        public void ConnectEdge(Edge edge)
        {
            edge.input.Connect(edge);
            edge.output.Connect(edge);
            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                new LogEntry(LogLevel.kInfo, "连接边"));
        }

        /// <summary>
        /// Removes the root node from the behavior tree associated with the current behavior tree view.
        /// This method ensures that the root node is deleted from the underlying behavior tree data structure.
        /// </summary>
        public void RemoveRootFromBehaviorTree()
        {
            BehaviorTreeManagers.instance.GetTreeByWindowId(tree_view_.Windows.WindowInstanceId).DeleteRoot();
            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                new LogEntry(LogLevel.kInfo, "从行为树数据中删除根节点"));
        }

        /// <summary>
        /// Sets the root node for the behavior tree associated with the current behavior tree view instance.
        /// </summary>
        /// <param name="rootNode">The node to be set as the root of the behavior tree.</param>
        public void SetRootToBehaviorTree(BtNodeBase rootNode)
        {
            BehaviorTreeManagers.instance.GetTreeByWindowId(tree_view_.Windows.WindowInstanceId).SetRoot(rootNode);
            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                new LogEntry(LogLevel.kInfo, $"设置行为树根节点: {rootNode.NodeName}"));
        }

        /// <summary>
        /// Removes the specified child node from the parent node view based on the type of the parent's node data
        /// (composite or precondition).
        /// </summary>
        /// <param name="parent_view">The parent node view from which the child node should be removed.</param>
        /// <param name="node_data">The child node data to be removed from the parent view.</param>
        /// <returns>True if the node was successfully removed, false otherwise.</returns>
        public bool RemoveFromParentNode(BaseNodeView parent_view, BtNodeBase node_data)
        {
            if (parent_view == null || node_data == null) return false;

            var parent_data = parent_view.NodeData;

            switch (parent_data)
            {
                case BtComposite composite:
                    composite.RemoveChildNode(node_data);
                    break;
                case BtPrecondition precondition:
                    if (precondition.ChildNode?.Guild == node_data.Guild) precondition.ChildNode = null;
                    break;
            }

            return true;
        }

        /// <summary>
        /// Retrieves all connections (input and output edges) associated with a specified node view.
        /// </summary>
        /// <param name="node_view">The node view whose connections are to be retrieved.</param>
        /// <returns>A list of edges representing all connections associated with the specified node view.</returns>
        public List<Edge> GetNodeConnections(BaseNodeView node_view)
        {
            var connections = new List<Edge>();
            if (node_view.InputPort != null) connections.AddRange(node_view.InputPort.connections);

            if (node_view.OutputPort != null) connections.AddRange(node_view.OutputPort.connections);

            return connections;
        }

        /// <summary>
        /// Retrieves a list of child node views connected to the specified node view.
        /// </summary>
        /// <param name="node_view">The node view for which to retrieve the connected child node views.</param>
        /// <returns>A list of child node views connected through the output port of the given node view.</returns>
        public List<BaseNodeView> GetChildNodeViews(BaseNodeView node_view)
        {
            var children = new List<BaseNodeView>();

            if (node_view.OutputPort != null)
                foreach (var edge in node_view.OutputPort.connections)
                    if (edge.input.node is BaseNodeView child_node)
                        children.Add(child_node);

            return children;
        }

        #region 删除节点

        /// <summary>
        /// Deletes a node view based on the specified unique identifier (GUID).
        /// </summary>
        /// <param name="guid">The unique identifier of the node view to be deleted.</param>
        /// <returns>Returns true if the node view was successfully deleted; otherwise, false.</returns>
        public bool DeleteNodeViewForGuid(string guid)
        {
            if (node_views_.Count < 0 || node_views_ == null) return false;

            var node_view = node_views_.Values.FirstOrDefault(n => n.NodeData.Guild == guid);

            if (node_view != null) DeleteNodeData(new List<BaseNodeView> { node_view });

            return true;
        }

        /// <summary>
        /// Deletes the root node from the behavior tree and its graphical representation in the editor.
        /// </summary>
        /// <param name="selectedBtNode">The root node view to be deleted.</param>
        public void DeleteRootNode(BaseNodeView selectedBtNode)
        {
            if (selectedBtNode == null) return;

            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                new LogEntry(LogLevel.kInfo,
                    $"尝试移除根节点 名字: {selectedBtNode.NodeData.NodeName} ID :{selectedBtNode.NodeData.Guild}"));

            // 断开所有子节点的连接，但不删除子节点
            if (selectedBtNode.OutputPort != null)
                foreach (var edge in selectedBtNode.OutputPort.connections.ToList())
                {
                    DisconnectEdge(edge);
                    RemoveElementFromView(edge);
                }

            RemoveRootFromBehaviorTree();
            RemoveNodeViewFromDictionary(selectedBtNode.NodeData.Guild);
            RemoveElementFromView(selectedBtNode);
        }

        /// <summary>
        /// Removes the specified node views from the behavior tree editor, including their connections and associated data.
        /// </summary>
        /// <param name="delete_node_views">A list of node views to be removed. Each node view's associated data and
        /// graphical elements will also be handled accordingly.</param>
        public void DeleteNodeData(List<BaseNodeView> delete_node_views)
        {
            if (delete_node_views == null || delete_node_views.Count < 0) return;

            // 逆向遍历避免修改集合时的迭代问题
            foreach (var node_view in delete_node_views.Reverse<BaseNodeView>())
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                    new LogEntry(LogLevel.kInfo,
                        $"节点视图： {node_view.NodeData.NodeName} ID: {node_view.NodeData.Guild}尝试进行移除"));

                var connections = GetNodeConnections(node_view);
                foreach (var edge in connections)
                {
                    DisconnectEdge(edge);
                    RemoveElementFromView(edge);
                }

                RemoveNodeViewFromDictionary(node_view.NodeData.Guild);
                RemoveElementFromView(node_view);
            }

            tree_view_.ClearSelection();
        }

        /// <summary>
        /// Removes a specified node's data from its parent node data.
        /// </summary>
        /// <param name="parentView">The parent node view containing the parent node's data.</param>
        /// <param name="nodeData">The node data to be removed from the parent's child nodes.</param>
        /// <returns>True if the node data has been successfully removed, false otherwise.</returns>
        public bool RemoveFromParentNodes(BaseNodeView parentView, BtNodeBase nodeData)
        {
            if (parentView == null || nodeData == null) return false;

            var parent_data = parentView.NodeData;

            switch (parent_data)
            {
                case BtComposite composite:
                    composite.RemoveChildNode(nodeData);
                    break;
                case BtPrecondition precondition:
                    if (precondition.ChildNode?.Guild == nodeData.Guild) precondition.ChildNode = null;
                    break;
            }

            return true;
        }

        /// <summary>
        /// Finds and returns the parent node view of the specified node view in the behavior tree.
        /// </summary>
        /// <param name="nodeView">The node view whose parent is to be found.</param>
        /// <returns>The parent node view if found; otherwise, null.</returns>
        public BaseNodeView FindParentNodeView(BaseNodeView nodeView)
        {
            // 检查输入端口是否存在
            if (nodeView?.InputPort == null) return null;

            // 获取输入端口的边
            var connect_edge = nodeView.InputPort.connections.FirstOrDefault();

            // 获取边的源节点
            var parent_port = connect_edge?.output;
            return parent_port?.node as BaseNodeView;
        }

        #endregion

        #region 移除节点连接线

        /// <summary>
        /// Deletes the specified edges from the graph, disconnects them from their nodes,
        /// removes associated data, and updates the view and logging accordingly.
        /// </summary>
        /// <param name="edges_to_delete">A list of edges to be deleted and processed.</param>
        public void DeleteEdgeAndData(List<Edge> edges_to_delete)
        {
            if (edges_to_delete == null || edges_to_delete.Count < 0) return;

            // 更新数据模型，假设边地输入端表示子节点，输出端表示父节点
            foreach (var edge in edges_to_delete)
            {
                if (edge.input.node is BaseNodeView child_node_view &&
                    edge.output.node is BaseNodeView parent_node_view)
                {
                    // 删除边相连的数据
                    RemoveFromParentNodes(child_node_view, parent_node_view.NodeData);

                    ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                        new LogEntry(LogLevel.kInfo,
                            $"{child_node_view.NodeData.NodeName} ID: {child_node_view.NodeData.Guild} 成功移除相连接线的数据"));
                }

                // 断开连接：先断开输入端，再断开输出端
                edge.input.Disconnect(edge);
                edge.output.Disconnect(edge);

                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace)
                    .AddLog(log_space_, new LogEntry(LogLevel.kInfo, "成功断开连接"));

                //从视图中移除边
                tree_view_.RemoveElement(edge);

                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace)
                    .AddLog(log_space_, new LogEntry(LogLevel.kInfo, "成功从视图当中移除边"));
            }
        }

        /// <summary>
        /// Disconnects all edges connected to the specified node view, including both input and output connections.
        /// </summary>
        /// <param name="nodeView">The node view from which all connected edges will be disconnected.</param>
        private void DisconnectAllEdges(BaseNodeView nodeView)
        {
            // 处理输入端口
            if (nodeView.InputPort != null)
                foreach (var edge in nodeView.InputPort.connections.ToList())
                {
                    edge.input.Disconnect(edge);
                    edge.output.Disconnect(edge);
                    tree_view_.RemoveElement(edge);
                }

            // 处理输出端口
            if (nodeView.OutputPort != null)
                foreach (var edge in nodeView.OutputPort.connections.ToList())
                {
                    edge.input.Disconnect(edge);
                    edge.output.Disconnect(edge);
                    tree_view_.RemoveElement(edge);
                }
        }

        #endregion

        /// <summary>
        /// Creates a new node view for a given behavior tree node and adds it to the internal collection of node views.
        /// </summary>
        /// <param name="node_data">The behavior tree node data to create the node view from.</param>
        /// <returns>A new instance of <see cref="BehaviorTreeNodeView"/> representing the specified behavior tree
        /// node, or null if the provided node data is null.</returns>
        public BehaviorTreeNodeView CreateNodeView(BtNodeBase node_data)
        {
            if (node_data == null) return null;

            if (node_views_.TryGetValue(node_data.Guild, out var existing_node_view))
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace)
                    .AddLog(log_space_,
                        new LogEntry(LogLevel.kWarning,
                            $"Attempted to create a NodeView for an existing GUID: {node_data.Guild}. " +
                            $"Returning existing view."));
                return existing_node_view as BehaviorTreeNodeView;
            }

            if (string.IsNullOrWhiteSpace(node_data.Guild)) node_data.Guild = Guid.NewGuid().ToString();

            var node_view = new BehaviorTreeNodeView(node_data,
                new Rect(node_data.Position, node_data.Size));
            
            node_views_.Add(node_data.Guild, node_view);

            return node_view;
        }

        /// <summary>
        /// Creates a new node view for the specified node type and initializes its properties.
        /// </summary>
        /// <param name="type">The type of the node to create the view for.</param>
        /// <param name="position">The position within the graph where the node view will be placed.</param>
        /// <returns>
        /// A fully initialized <see cref="BehaviorTreeNodeView"/> instance if the node creation is successful; otherwise, null.
        /// </returns>
        public BehaviorTreeNodeView CreateNodeView(Type type, Vector2 position)
        {
            if (Activator.CreateInstance(type) is not BtNodeBase node_data) return null;

            // 设置名称（先从特性例获取，如果为空则使用类型名）
            var node_label = node_data.GetType().GetCustomAttribute(typeof(NodeLabelAttribute));
            if (node_label is NodeLabelAttribute node_labels && !string.IsNullOrEmpty(node_labels.label_))
                node_data.NodeName = node_labels.label_;
            else
                node_data.NodeName = type.Name;

            // 为每一个节点生成唯一标识符
            if (string.IsNullOrEmpty(node_data.Guild)) node_data.Guild = Guid.NewGuid().ToString();

            // 检查是否已经有相同名称的节点，如果有则加上自增后缀
            var base_name = node_data.NodeName;
            var new_name = base_name;
            var index = 1;
            // 循环判断，直到生成唯一的名称为止
            while (node_views_.Values.Any(nv => nv.NodeData.NodeName == new_name))
            {
                new_name = base_name + index;
                index++;
            }

            node_data.NodeName = new_name;
            var node_view = new BehaviorTreeNodeView(node_data, new Rect(position, Vector2.zero));
            node_views_.Add(node_data.Guild, node_view);
            return node_view;
        }

        public void Dispose()
        {
            node_views_.Clear();
        }
    }
}