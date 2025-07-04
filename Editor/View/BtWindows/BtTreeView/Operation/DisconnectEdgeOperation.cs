using System.Linq;
using Editor.EditorToolEx;
using Editor.EditorToolEx.Operation;
using Editor.EditorToolExs;
using Editor.View.BTWindows.BtTreeView.NodeView;
using Editor.View.BTWindows.BtTreeView.NodeView.NodeViewEdge;
using UnityEditor.Experimental.GraphView;

namespace Editor.View.BTWindows.BtTreeView.Operation
{
    public class DisconnectEdgeOperation :IOperation
    {
        private readonly Edge edge_;
        private readonly BehaviorTreeView _behaviorTreeView;
        private readonly string output_node_guid_;
        private readonly string input_node_guid_;

        public DisconnectEdgeOperation(Edge edge, BehaviorTreeView behaviorTreeView)
        {
            edge_ = edge;
            _behaviorTreeView = behaviorTreeView;
            output_node_guid_ = (edge.output.node as BaseNodeView)?.NodeData.Guild;
            input_node_guid_ = (edge.input.node as BaseNodeView)?.NodeData.Guild;
        }
        
        public void Execute()
        {
        }

        public void Undo()
        {
            // 重新连接
            var output_node = _behaviorTreeView.GetNodeViewByGuid(output_node_guid_) as BehaviorTreeNodeView;
            var input_node=_behaviorTreeView.GetNodeViewByGuid(input_node_guid_) as BehaviorTreeNodeView;

            if (output_node!=null&&input_node!=null)
            {
                var new_edge = new FlowingEdge
                {
                    output = output_node.OutputPort,
                    input = input_node.InputPort
                };
                
                new_edge.input.Connect(new_edge);
                new_edge.output.Connect(new_edge);
                _behaviorTreeView.AddElement(new_edge);
                
                // 更新数据模型
                EditorExTools.Instance.LinkLineAddData(new_edge);
            }
        }

        public void Redo()
        {
            // 重新断开连接
            var output_node=_behaviorTreeView.GetNodeViewByGuid(output_node_guid_) as BehaviorTreeNodeView;
            var input_node=_behaviorTreeView.GetNodeViewByGuid(input_node_guid_) as BehaviorTreeNodeView;

            if (output_node!=null&&input_node!=null)
            {
                // 查找并断开连接
                var edge_to_remove = output_node.OutputPort.connections.FirstOrDefault(e => e.input.node == input_node);

                if (edge_to_remove!=null)
                {
                    edge_to_remove.input?.Disconnect(edge_to_remove);
                    edge_to_remove.output?.Disconnect(edge_to_remove);
                    _behaviorTreeView.RemoveElement(edge_to_remove);
                    
                    // 更新数据模型
                    EditorExTools.Instance.UnLinkLineDelete(edge_to_remove);
                }
            }
        }

        public bool RequireSave => true;
    }
}
