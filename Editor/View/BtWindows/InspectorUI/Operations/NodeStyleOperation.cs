using BehaviorTree.Nodes;
using Editor.EditorToolExs;
using Editor.EditorToolExs.Operation;
using Editor.EditorToolExs.Operation.Core;
using Editor.View.BtWindows.Core;
using Script.BehaviorTree.Save;

namespace Editor.View.BTWindows.InspectorUI.Operations
{
    public class NodeStyleOperation : IOperation
    {
        private readonly BtNodeBase target_node_;
        private readonly BtNodeStyle old_style_;
        private readonly BtNodeStyle new_style_;
        private readonly string operation_description_;
        
        public NodeStyleOperation(BtNodeBase target_node, BtNodeStyle old_style, BtNodeStyle new_style, string operation_description)
        {
            target_node_ = target_node;
            old_style_ = CloneStyle(old_style);
            new_style_ = CloneStyle(new_style);
            operation_description_ = operation_description;
        }
        
        public void Execute()
        {
            ApplyStyleToNode(target_node_,new_style_);
            RefreshNodeView(target_node_);
        }

        public void Undo()
        {
            ApplyStyleToNode(target_node_,old_style_);
            RefreshNodeView(target_node_);
        }

        public void Redo()
        {
            Execute();
        }

        private void ApplyStyleToNode(BtNodeBase node, BtNodeStyle style)
        {
            if (style == null)
            {
                return;
            }
            
            BehaviorTreeWindows.FocusedWindow.BehaviorTreeView.NodeStyleManager.ApplyStyle(node, style);
        }

        private void RefreshNodeView(BtNodeBase node)
        {
            BehaviorTreeWindows.FocusedWindow.BehaviorTreeView.GetNodeViewByGuid(node.Guild)?.ApplyStyle();
        }

        public bool RequireSave => true;

        private BtNodeStyle CloneStyle(BtNodeStyle original)
        {
            return EditorExTools.Instance.CloneBtNodeStyle(original);
        }
    }
}
