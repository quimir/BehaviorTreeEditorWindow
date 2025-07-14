using System.Collections.Generic;
using Editor.EditorToolExs.Operation;
using Editor.EditorToolExs.Operation.Core;
using Editor.View.BTWindows.BtTreeView.NodeView;
using Script.BehaviorTree;
using UnityEngine;

namespace Editor.View.BTWindows.BtTreeView.Operation
{
    /// <summary>
    /// Handles the operation of moving node views within a behavior tree editor.
    /// This class implements the <see cref="IOperation"/> interface, allowing the movement
    /// of nodes to be executed, undone, and redone. Movement data includes the node view,
    /// its old position, and its new position.
    /// </summary>
    public class MoveNodeViewOperation : IOperation
    {
        private readonly List<(BehaviorTreeNodeView node, Vector2 old_position, Vector2 new_position)> move_data_;

        public MoveNodeViewOperation(List<(BehaviorTreeNodeView node, Vector2 old_position, Vector2 new_position)> move_data)
        {
            move_data_ = move_data;
        }
        
        public void Execute()
        {
        }

        public void Undo()
        {
            foreach (var (node,old_position,_) in move_data_)
            {
                node.SetPosition(new Rect(old_position,node.NodeData.Size));
                node.NodeData.Position = old_position;
            }
        }

        public void Redo()
        {
            foreach (var (node,_,new_position) in move_data_)
            {
                node.SetPosition(new Rect(new_position,node.NodeData.Size));
                node.NodeData.Position = new_position;
            }
        }

        public bool RequireSave => true;
    }
}
