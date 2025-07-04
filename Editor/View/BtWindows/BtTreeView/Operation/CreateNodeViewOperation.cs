using System.Collections.Generic;
using Editor.EditorToolEx.Operation;
using Editor.View.BTWindows.BtTreeView.NodeView;
using UnityEditor.Experimental.GraphView;

namespace Editor.View.BTWindows.BtTreeView.Operation
{
    public class CreateNodeViewOperation : IOperation
    {
        private readonly BaseNodeView node_view_;
        private readonly BehaviorTreeView _behaviorTreeView;
        private readonly List<Edge> connections_;

        public CreateNodeViewOperation(BaseNodeView nodeView, BehaviorTreeView behaviorTreeView, List<Edge> connections=null)
        {
            node_view_ = nodeView;
            _behaviorTreeView = behaviorTreeView;
            connections_ = connections??new List<Edge>();
        }

        public void Execute()
        {
        }

        public void Undo()
        {
            foreach (var edge in connections_)
            {
                edge.input?.Disconnect(edge);
                edge.output?.Disconnect(edge);
                _behaviorTreeView.RemoveElement(edge);
            }
            
            _behaviorTreeView.DeleteNodeData(new List<BaseNodeView>{node_view_});
        }

        public void Redo()
        {
            _behaviorTreeView.AddElement(node_view_);
            _behaviorTreeView.NodeViewManager.NodeViews[node_view_.NodeData.Guild] = node_view_;
            // // 重新添加节点
            // _behaviourTreeView.AddElement(node_view_);
            // _behaviourTreeView.NodeViews[node_view_.NodeData.Guild] = node_view_;

            foreach (var edge in connections_)
            {
                edge.input?.Connect(edge);
                edge.output?.Connect(edge);
                _behaviorTreeView.AddElement(edge);
            }
            
        }

        public bool RequireSave => true;
    }
}
