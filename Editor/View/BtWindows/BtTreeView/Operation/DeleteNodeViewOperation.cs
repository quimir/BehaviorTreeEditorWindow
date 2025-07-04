using System.Collections.Generic;
using System.Linq;
using BehaviorTree.Nodes;
using Editor.EditorToolEx.Operation;
using Editor.View.BTWindows.BtTreeView.NodeView;
using Editor.View.BTWindows.BtTreeView.NodeView.NodeViewEdge;
using NUnit.Framework;
using Script.BehaviorTree;
using Sirenix.Utilities;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Editor.View.BTWindows.BtTreeView.Operation
{
    public class DeleteNodeViewOperation : IOperation
    {
        public class EdgeConnectionInfo
        {
            public string OutputNodeGuid;
            public string InputNodeGuid;
            public int OutputPortIndex;
            public int InputPortIndex;
        }
        
        private readonly List<BtNodeBase> deleted_nodes_;
        private readonly Dictionary<string, List<EdgeConnectionInfo>> connections_;
        private readonly BehaviorTreeView _behaviorTreeView;

        public DeleteNodeViewOperation(List<BtNodeBase> deleted_nodes,
            Dictionary<string, List<Edge>> connections, BehaviorTreeView behaviorTreeView)
        {
            deleted_nodes_ = deleted_nodes;
            _behaviorTreeView = behaviorTreeView;
            connections_ = new Dictionary<string, List<EdgeConnectionInfo>>();
            
            // 保存连接信息
            foreach (var kvp in connections)
            {
                var connection_infos = new List<EdgeConnectionInfo>();
                foreach (var edge in kvp.Value)
                {
                    connection_infos.Add(new EdgeConnectionInfo
                    {
                        OutputNodeGuid = (edge.output.node as BaseNodeView)?.NodeData.Guild,
                        InputNodeGuid = (edge.input.node as BaseNodeView)?.NodeData.Guild,
                        OutputPortIndex = 0,
                        InputPortIndex = 0
                    });
                }

                connections_[kvp.Key] = connection_infos;
            }
        }
        public void Execute()
        {
            
        }

        public void Undo()
        {
            // 恢复所有删除的节点
            foreach (var node_data in deleted_nodes_)
            {
                var node_view = new BehaviorTreeNodeView(node_data, new Rect(node_data.Position, node_data.Size));
                _behaviorTreeView.AddElement(node_view);
                _behaviorTreeView.NodeViewManager.NodeViews[node_data.Guild] = node_view;
            }
            
            // 恢复所有连接
            foreach (var node_data in deleted_nodes_)
            {
                if (connections_.TryGetValue(node_data.Guild,out var connection_infos))
                {
                    foreach (var connection in connection_infos)
                    {
                        var output_node = _behaviorTreeView.GetNodeViewByGuid(connection.OutputNodeGuid);
                        var input_node=_behaviorTreeView.GetNodeViewByGuid(connection.InputNodeGuid);

                        if (output_node!=null&&input_node!=null)
                        {
                            var edge = new FlowingEdge
                            {
                                output = output_node.OutputPort,
                                input = output_node.InputPort
                            };
                            
                            edge.input.Connect(edge);
                            edge.output.Connect(edge);
                            _behaviorTreeView.AddElement(edge);
                        }
                    }
                }
            }
            
            // 成功新建立数据连接
            _behaviorTreeView.nodes.OfType<BehaviorTreeNodeView>().ForEach(n => n.LinkLine());
        }

        public void Redo()
        {
            // 重新删除节点
            var nodes_to_delete = deleted_nodes_.Select(data => _behaviorTreeView.GetNodeViewByGuid(data.Guild))
                .Where(view => view != null).Cast<BaseNodeView>().ToList();
            
            _behaviorTreeView.DeleteNodeData(nodes_to_delete);
        }

        public bool RequireSave => true;
    }
}
