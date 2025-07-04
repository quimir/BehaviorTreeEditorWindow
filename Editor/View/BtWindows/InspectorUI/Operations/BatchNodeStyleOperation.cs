using System;
using System.Collections.Generic;
using BehaviorTree.Nodes;
using Editor.EditorToolEx.Operation;
using Editor.EditorToolExs;
using Editor.View.BtWindows.Core;
using Script.BehaviorTree;
using Script.BehaviorTree.Save;
using Script.Save.Serialization;
using Script.Save.Serialization.Factory;
using Script.Utillties;
using Unity.VisualScripting;

namespace Editor.View.BTWindows.InspectorUI.Operations
{
    public class BatchNodeStyleOperation : IOperation
    {
        private readonly HashSet<BtNodeBase> target_nodes_;
        private readonly Dictionary<BtNodeBase,BtNodeStyle> old_styles_;
        private readonly BtNodeStyle new_style_;
        private readonly string operation_description_;

        public BatchNodeStyleOperation(HashSet<BtNodeBase> target_nodes, BtNodeStyle new_style,
            string operation_description = "批量处理")
        {
            target_nodes_ =
                new HashSet<BtNodeBase>(target_nodes ?? throw new ArgumentNullException(nameof(target_nodes)));
            new_style_ = CloneStyle(new_style);
            operation_description_ = operation_description;
            
            // 保存所有节点的原始样式
            old_styles_ = new Dictionary<BtNodeBase, BtNodeStyle>();
            foreach (var node in target_nodes_)
            {
                BtNodeStyle current_style = null;
                current_style=BehaviorTreeWindows.FocusedWindow.BehaviorTreeView.NodeStyleManager.TryGetNodeStyle(node);
                old_styles_[node] = CloneStyle(current_style);
            }
        }
        public void Execute()
        {
            foreach (var node in target_nodes_)
            {
                BehaviorTreeWindows.FocusedWindow.BehaviorTreeView.NodeStyleManager.ApplyStyle(node, new_style_);
                RefreshNodeView(node);
            }
        }

        public void Undo()
        {
            foreach (var node in target_nodes_)
            {
                if (old_styles_.TryGetValue(node,out var old_style))
                {
                    if (old_style!=null)
                    {
                        BehaviorTreeWindows.FocusedWindow.BehaviorTreeView.NodeStyleManager.ApplyStyle(node,old_style);
                    }
                    else
                    {
                        BehaviorTreeWindows.FocusedWindow.BehaviorTreeView.NodeStyleManager.ResetNodeStyle(node);
                    }
                    
                    RefreshNodeView(node);
                }
            }
        }

        public void Redo()
        {
            Execute();
        }

        private void RefreshNodeView(BtNodeBase node)
        {
            BehaviorTreeWindows.FocusedWindow.BehaviorTreeView.GetNodeViewByGuid(node.Guild)?.ApplyStyle();
        }

        private BtNodeStyle CloneStyle(BtNodeStyle original)
        {
            
            if (original==null)
            {
                return null;
            }
            
            return EditorExTools.Instance.CloneBtNodeStyle(original);
        }

        public bool RequireSave => true;
    }
}
