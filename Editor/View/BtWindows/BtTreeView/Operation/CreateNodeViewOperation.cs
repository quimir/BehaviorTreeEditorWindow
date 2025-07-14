using System.Collections.Generic;
using BehaviorTree.Nodes;
using Editor.EditorToolExs.Operation;
using Editor.EditorToolExs.Operation.Core;
using Editor.View.BTWindows.BtTreeView.NodeView;
using Editor.View.BtWindows.BtTreeView.NodeView.Core;
using UnityEngine;

namespace Editor.View.BTWindows.BtTreeView.Operation
{
    /// <summary>
    /// Represents an operation for creating a node view within a behavior tree editor.
    /// This operation supports execution, undo, and redo functionality, and can be used
    /// to manage changes to the visual representation of behavior tree nodes.
    /// </summary>
    public class CreateNodeViewOperation : IOperation
    {
        private readonly BehaviorTreeView tree_view_;
        private readonly BtNodeBase node_data_;

        public CreateNodeViewOperation(BtNodeBase node_data, BehaviorTreeView tree_view)
        {
            tree_view_ = tree_view;
            node_data_ = node_data;
        }
        
        public void Execute()
        {
            if (tree_view_.GetNodeViewByGuid(node_data_.Guild)==null)
            {
                var node_view=tree_view_.NodeViewManager.CreateNodeView(node_data_);
                if (node_view!=null)
                {
                    tree_view_.AddElement(node_view);
                }
            }
        }

        public void Undo()
        {
            var node_view = tree_view_.GetNodeViewByGuid(node_data_.Guild);
            if (node_view==null)
            {
                return;
            }
            
            tree_view_.NodeViewManager.DeleteNodeData(new List<BaseNodeView>{node_view});
        }

        public void Redo()
        {
            Execute();
        }

        public bool RequireSave => true;
    }
}
