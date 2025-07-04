using Editor.EditorToolEx;
using Editor.EditorToolEx.Operation;
using Editor.EditorToolExs;
using Editor.View.BTWindows.BtTreeView.NodeView;
using UnityEditor.Experimental.GraphView;

namespace Editor.View.BTWindows.BtTreeView.Operation
{
    public class ConnectEdgeOperation : IOperation
    {
        private readonly Edge edge_;
        private readonly BehaviorTreeView _behaviorTreeView;
        private readonly BehaviorTreeNodeView parent_node_;
        private readonly BehaviorTreeNodeView child_node_;

        public ConnectEdgeOperation(Edge edge, BehaviorTreeView behaviorTreeView)
        {
            edge_ = edge;
            _behaviorTreeView = behaviorTreeView;
            parent_node_=edge.output.node as BehaviorTreeNodeView;
            child_node_=edge.input.node as BehaviorTreeNodeView;
        }
        public void Execute()
        {
        }

        public void Undo()
        {
            // 断开连接
            edge_.input?.Disconnect(edge_);
            edge_.output?.Disconnect(edge_);
            _behaviorTreeView.RemoveElement(edge_);
            
            // 更新数据模型
            if (parent_node_!=null&&child_node_!=null)
            {
                _behaviorTreeView.NodeViewManager.RemoveFromParentNodes(parent_node_,child_node_.NodeData);
            }
        }

        public void Redo()
        {
            // 重新连接
            edge_.input?.Connect(edge_);
            edge_.output?.Connect(edge_);
            _behaviorTreeView.AddElement(edge_);
            
            // 更新数据模型
            EditorExTools.Instance.LinkLineAddData(edge_);
        }

        public bool RequireSave => true;
    }
}
