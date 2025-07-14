using Editor.EditorToolExs.Operation;
using Editor.EditorToolExs.Operation.Core;
using Editor.View.BTWindows.BtTreeView.NodeView;
using Editor.View.BtWindows.BtTreeView.NodeView.Core;

namespace Editor.View.BtWindows.BtTreeView.Operation
{
    /// <summary>
    /// Represents an operation to remove the root node from a behavior tree in the editor.
    /// This operation is part of a modification workflow that supports undo and redo functionality.
    /// </summary>
    public class RemoveRootNodeOperation : IOperation
    {
        private readonly BaseNodeView root_node_;
        private readonly BtNodeViewManager node_manager_;
        
        public RemoveRootNodeOperation(BaseNodeView rootNode,BtNodeViewManager nodeManager)
        {
            root_node_ = rootNode;
            node_manager_ = nodeManager;
        }
        public void Execute()
        {
            node_manager_.RemoveRootFromBehaviorTree();
            node_manager_.RemoveNodeViewFromDictionary(root_node_.NodeData.Guild);
            node_manager_.RemoveElementFromView(root_node_);
        }

        public void Undo()
        {
            node_manager_.AddElementToView(root_node_);
            node_manager_.AddNodeViewToDictionary(root_node_);
            node_manager_.SetRootToBehaviorTree(root_node_.NodeData);
        }

        public void Redo()
        {
            Execute();
        }

        public bool RequireSave => true;
    }
}
