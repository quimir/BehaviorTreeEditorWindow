using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using BehaviorTree.BehaviorTrees;
using BehaviorTree.Nodes;
using ExTools;
using LogManager.Core;
using LogManager.LogManagerFactory;
using Script.BehaviorTree;
using Script.BehaviorTree.Save;
using Script.LogManager;
using Script.Tool;
using Script.Utillties;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Vector2 = UnityEngine.Vector2;

namespace Editor.View.BTWindows.BtTreeView.NodeView
{
    /// <summary>
    /// Provides management functionality for handling behavior tree node views within an editor.
    /// </summary>
    public class BtNodeViewManager
    {
        private static readonly LogSpaceNode log_space_ = 
            new LogSpaceNode("BehaviourTreeWindows").AddChild("TreeView").AddChild("BtNodeViewManager");
        
        private readonly Dictionary<string, BaseNodeView> node_views_ = new();
        
        public Dictionary<string,BaseNodeView> NodeViews => node_views_;

        private readonly BehaviorTreeView tree_view_;

        /// <summary>
        /// Manages the creation, deletion, and retrieval of node views within a behavior tree editor.
        /// </summary>
        public BtNodeViewManager(BehaviorTreeView treeView)
        {
            tree_view_ = treeView;
        }

        #region 删除节点

        /// <summary>
        /// Deletes a node view based on the specified unique identifier (GUID).
        /// </summary>
        /// <param name="guid">The unique identifier of the node view to be deleted.</param>
        /// <returns>Returns true if the node view was successfully deleted; otherwise, false.</returns>
        public bool DeleteNodeViewForGuid(string guid)
        {
            if (node_views_.Count < 0 || node_views_ == null)
            {
                return false;
            }

            var node_view = node_views_.Values.FirstOrDefault(n => n.NodeData.Guild == guid);

            if (node_view!=null)
            {
                DeleteNodeData(new List<BaseNodeView>{node_view});
            }

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
                    edge.input.Disconnect(edge);
                    edge.output.Disconnect(edge);
                    tree_view_.RemoveElement(edge);
                }
            
            BehaviorTreeManagers.instance.GetTreeByWindowId(tree_view_.Windows.WindowInstanceId).DeleteRoot();
            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                new LogEntry(LogLevel.kInfo,
                    $"已经成功移除根节点 名字: {selectedBtNode.NodeData.NodeName} ID :{selectedBtNode.NodeData.Guild}"));

            node_views_.Remove(selectedBtNode.NodeData.Guild);
            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                new LogEntry(LogLevel.kInfo,
                    $"已经成功从字典当中移除根节点 名字: {selectedBtNode.NodeData.NodeName} ID :{selectedBtNode.NodeData.Guild}"));

            tree_view_.RemoveElement(selectedBtNode);
            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                new LogEntry(LogLevel.kInfo,
                    $"已经成功从视图当中移除根节点 名字: {selectedBtNode.NodeData.NodeName} ID :{selectedBtNode.NodeData.Guild}"));
        }

        /// <summary>
        /// Removes the specified node views from the behavior tree editor, including their connections and associated data.
        /// </summary>
        /// <param name="delete_node_views">A list of node views to be removed. Each node view's associated data and graphical elements will also be handled accordingly.</param>
        public void DeleteNodeData(List<BaseNodeView> delete_node_views)
        {
            if (delete_node_views==null||delete_node_views.Count<0)
            {
                return;
            }
            
            // 逆向遍历避免修改集合时的迭代问题
            foreach (var node_view in delete_node_views.Reverse<BaseNodeView>())
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                    new LogEntry(LogLevel.kInfo,
                        $"节点视图： {node_view.NodeData.NodeName} ID: {node_view.NodeData.Guild}尝试进行移除"));
                
                // 如果该节点是子节点，则先从起父节点中移除（更新数据结构）
                var parent_view=FindParentNodeView(node_view);
                if (parent_view!=null)
                {
                    // 从父节点的数据移除此节点
                    RemoveFromParentNodes(parent_view, node_view.NodeData);
                }
                
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                    new LogEntry(LogLevel.kInfo,
                        $"节点视图检查完毕已尝试移除节点： {node_view.NodeData.NodeName} ID: {node_view.NodeData.Guild}"));
                
                // 断开所有连接线
                DisconnectAllEdges(node_view);
                
                node_views_.Remove(node_view.NodeData.Guild);

                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                    new LogEntry(LogLevel.kInfo,
                        $"已触发从字典当中移除： {node_view.NodeData.NodeName} ID: {node_view.NodeData.Guild}"));
                
                tree_view_.RemoveElement(node_view);
                
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                    new LogEntry(LogLevel.kInfo,
                        $"触发从视图当中移除： {node_view.NodeData.NodeName} ID: {node_view.NodeData.Guild}"));
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
            if (parentView == null || nodeData == null)
            {
                return false;
            }
            
            var parent_data = parentView.NodeData;

            switch (parent_data)
            {
                case BtWeightSelector weight_selector:
                    weight_selector.RemoveNode(nodeData);
                    break;
                case BtPrioritySelector priority_selector:
                    priority_selector.RemoveNode(nodeData);
                    break;
                case BtComposite composite:
                    composite.ChildNodes.RemoveAll(n => n.Guild == nodeData.Guild);
                    break;
                case BtPrecondition precondition:
                    if (precondition.ChildNode?.Guild==nodeData.Guild)
                    {
                        precondition.ChildNode = null;
                    }
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
            if (nodeView?.InputPort==null)
            {
                return null;
            }
            
            // 获取输入端口的边
            var connect_edge = nodeView.InputPort.connections.FirstOrDefault();
            
            // 获取边的源节点
            var parent_port = connect_edge?.output;
            return parent_port?.node as BaseNodeView;
        }

        #endregion

        #region 移除节点连接线

        public void DeleteEdgeAndData(List<Edge> edges_to_delete)
        {
            if (edges_to_delete==null||edges_to_delete.Count<0)
            {
                return;
            }
            
            // 更新数据模型，假设边地输入端表示子节点，输出端表示父节点
            foreach (var edge in edges_to_delete)
            {
                if (edge.input.node is BaseNodeView child_node_view && edge.output.node is BaseNodeView parent_node_view)
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

        private void DisconnectAllEdges(BaseNodeView nodeView)
        {
            // 处理输入端口
            if (nodeView.InputPort!=null)
            {
                foreach (var edge in nodeView.InputPort.connections.ToList())
                {
                    edge.input.Disconnect(edge);
                    edge.output.Disconnect(edge);
                    tree_view_.RemoveElement(edge);
                }
            }
            
            // 处理输出端口
            if (nodeView.OutputPort!=null)
            {
                foreach (var edge in nodeView.OutputPort.connections.ToList())
                {
                    edge.input.Disconnect(edge);
                    edge.output.Disconnect(edge);
                    tree_view_.RemoveElement(edge);
                }
            }
        }

        #endregion

        /// <summary>
        /// Creates a new node view for a given behavior tree node and adds it to the internal collection of node views.
        /// </summary>
        /// <param name="node_data">The behavior tree node data to create the node view from.</param>
        /// <returns>A new instance of <see cref="BehaviorTreeNodeView"/> representing the specified behavior tree node, or null if the provided node data is null.</returns>
        public BehaviorTreeNodeView CreateNodeView(BtNodeBase node_data)
        {
            if (node_data == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(node_data.Guild))
            {
                node_data.Guild=Guid.NewGuid().ToString();
            }
            
            var node_view = new BehaviorTreeNodeView(node_data,
                new Rect(node_data.Position, node_data.Size));
            
            node_views_.Add(node_data.Guild,node_view);

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
            if (Activator.CreateInstance(type) is not BtNodeBase node_data)
            {
                return null;
            }
            
            // 设置名称（先从特性例获取，如果为空则使用类型名）
            var node_label = node_data.GetType().GetCustomAttribute(typeof(NodeLabelAttribute));
            if (node_label is NodeLabelAttribute node_labels && !string.IsNullOrEmpty(node_labels.label_))
            {
                node_data.NodeName = node_labels.label_;
            }
            else
            {
                node_data.NodeName = type.Name;
            }
            
            // 为每一个节点生成唯一标识符
            if (string.IsNullOrEmpty(node_data.Guild))
            {
                node_data.Guild=Guid.NewGuid().ToString();
            }
            
            // 检查是否已经有相同名称的节点，如果有则加上自增后缀
            var base_name = node_data.NodeName;
            var new_name = base_name;
            var index = 1;
            // 循环判断，直到生成唯一的名称为止
            while (node_views_.Values.Any(nv=>nv.NodeData.NodeName==new_name))
            {
                new_name = base_name + index;
                index++;
            }

            node_data.NodeName = new_name;
            var node_view = new BehaviorTreeNodeView(node_data, new Rect(position, Vector2.zero));
            node_views_.Add(node_data.Guild,node_view);
            return node_view;
        }

        public BaseNodeView GetNodeViewByGuid(string guid)
        {
            node_views_.TryGetValue(guid, out var view);
            return view;
        }
    }
}
