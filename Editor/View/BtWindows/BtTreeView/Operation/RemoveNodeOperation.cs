using System.Linq;
using BehaviorTree.Nodes;
using Editor.EditorToolExs.Operation;
using Editor.EditorToolExs.Operation.Core;
using Editor.View.BTWindows.BtTreeView.NodeView;
using Editor.View.BtWindows.BtTreeView.NodeView.Core;
using Script.BehaviorTree;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.View.BtWindows.BtTreeView.Operation
{
    /// <summary>
    /// Represents an operation to remove a node from a node tree structure in the editor.
    /// </summary>
    /// <remarks>
    /// This class encapsulates the functionality to remove a node view from its parent node, update
    /// the node view manager, and visually adjust the node's state in the user interface. The operation
    /// can also be undone or redone as part of an undo-redo system.
    /// </remarks>
    public class RemoveNodeOperation : IOperation
    {
        private readonly BaseNodeView node_view_;
        private readonly BaseNodeView parent_node_;
        private readonly BtNodeViewManager node_manager_;

        private readonly float node_value_;

        public RemoveNodeOperation(BaseNodeView node_view, BtNodeViewManager node_manager)
        {
            node_view_ = node_view;
            node_manager_ = node_manager;
            parent_node_ = node_manager_?.FindParentNodeView(node_view);
            node_value_ = parent_node_?.NodeData switch
            {
                
                BtWeightSelector weightSelector => weightSelector.GetChildren().FirstOrDefault(s =>
                    s.Node == node_view_.NodeData)!.Weight,
                BtPrioritySelector prioritySelector => prioritySelector.GetChildren().FirstOrDefault(s =>
                    s.Node == node_view_.NodeData)!.Priority,
                _ => node_value_
            };
        }
        public void Execute()
        {
            // 从父节点数据中移除
            if (parent_node_!=null)
            {
                node_manager_.RemoveFromParentNodes(parent_node_, node_view_.NodeData);
            }
            
            node_manager_.RemoveNodeViewFromDictionary(node_view_.NodeData.Guild);
            node_view_.style.opacity = 0.3f;
            node_view_.pickingMode = PickingMode.Ignore;
            node_manager_.RemoveElementFromView(node_view_);
        }

        public void Undo()
        {
            node_manager_.AddNodeViewToDictionary(node_view_);

            if (parent_node_!=null)
            {
                AddToParentNodes(parent_node_,node_view_.NodeData);
            }

            node_view_.style.opacity = 1;
            node_view_.pickingMode = PickingMode.Position;
            node_manager_.AddElementToView(node_view_);
        }

        public void Redo()
        {
            Execute();
        }

        public bool RequireSave => true;

        private void AddToParentNodes(BaseNodeView parent_view, BtNodeBase node_data)
        {
            var parent_node = parent_view.NodeData;

            switch (parent_node)
            {
                case BtWeightSelector weightSelector:
                    weightSelector.AddChildWithWeight(node_data, node_value_);
                    break;
                case BtPrioritySelector priority_selector:
                    priority_selector.AddChildWithPriority(node_data,(int)node_value_);
                    return;
                case BtComposite composite:
                    composite.ChildNodes.Add(node_data);
                    break;
                case BtPrecondition precondition:
                    precondition.ChildNode = node_data;
                    break;
            }
        }
    }
}
