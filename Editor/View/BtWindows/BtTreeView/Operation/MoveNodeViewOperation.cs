using System.Collections.Generic;
using Editor.EditorToolEx.Operation;
using Editor.View.BTWindows.BtTreeView.NodeView;
using Script.BehaviorTree;
using UnityEngine;

namespace Editor.View.BTWindows.BtTreeView.Operation
{
    public class MoveNodeViewOperation : IOperation
    {
        private readonly List<(BehaviorTreeNodeView node, Vector2 old_position, Vector2 new_position)> move_data_;

        public MoveNodeViewOperation(List<(BehaviorTreeNodeView node, Vector2 old_position, Vector2 new_position)> move_data)
        {
            this.move_data_ = move_data;
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
