using System.Collections.Generic;
using BehaviorTree.Nodes;
using Editor.EditorToolExs.Operation.Core;
using Editor.View.BTWindows.BtTreeView;
using Editor.View.BTWindows.BtTreeView.NodeView;
using Editor.View.BtWindows.BtTreeView.NodeView.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.View.BtWindows.BtTreeView.Operation
{
    public class PasteNodeOperation : IOperation
    {
        private readonly BehaviorTreeView tree_view_;
        private readonly BtNodeViewManager node_manager_;
        private readonly List<BtNodeBase> pasted_node_data_;// 存储即将或已粘贴的节点数据（已是深度拷贝且GUID已更新）
        private readonly List<BehaviorTreeNodeView> pasted_node_views_;// 实际粘贴后创建的节点视图
        private readonly Vector2 mouse_position_;// 粘贴时的鼠标位置

        public PasteNodeOperation(List<BtNodeBase> pasted_node_data, Vector2 mouse_position, BehaviorTreeView tree_view)
        {
            pasted_node_data_ = pasted_node_data;
            mouse_position_ = mouse_position;
            pasted_node_views_=new List<BehaviorTreeNodeView>();
            tree_view_ = tree_view;
            node_manager_ = tree_view_.NodeViewManager;
        }
        
        public void Execute()
        {
            // 在执行前清空，确保是干净的重做/首次执行
            pasted_node_views_.Clear();

            // 计算鼠标在 GraphView 内容容器内的本地坐标
            // 这是最常用的将屏幕鼠标位置转换为 GraphView 内容区域本地坐标的方法
            Vector2 graph_paste_position = tree_view_.contentViewContainer.WorldToLocal(mouse_position_);
            
            // 保留第一个点的位置以进行构建的时候可以将其进行还原
            var first_node_position=pasted_node_data_[0].Position;

            // 进行小范围的偏移防止节点进行重叠
            const int node_position_offset = 10;

            foreach (var node_data_to_paste in pasted_node_data_)
            {
                var node_position = node_data_to_paste.Position;
                // 使用计算出的 GraphView 内容容器本地坐标作为基准位置
                Vector2 new_node_position = node_position-first_node_position+graph_paste_position;

                // 对每个节点应用偏移，使其不完全重叠
                new_node_position += new Vector2(node_position_offset, node_position_offset);

                node_data_to_paste.Position = new_node_position; // 更新节点数据的位置

                var node_view = node_manager_.CreateNodeView(node_data_to_paste);
                if (node_view != null)
                {
                    tree_view_.AddElement(node_view);
                    pasted_node_views_.Add(node_view);
                }
            }

            tree_view_.AddSelectedNode(pasted_node_views_);

            // 重新链接节点视图的连接线，基于它们的数据模型
            pasted_node_views_.ForEach(n => n.LinkLine());
        }

        public void Undo()
        {
            // 清除选择，避免删除时选中状态干扰
            tree_view_.ClearSelection();

            var node_data = new List<BaseNodeView>();

            foreach (var node_view in pasted_node_views_)
            {
                if (node_view is BaseNodeView base_node_view)
                {
                    node_data.Add(base_node_view);
                }
            }
            
            // 移除所有粘贴的节点视图及其关联的数据和连接
            node_manager_.DeleteNodeData(node_data);
        }

        public void Redo()
        {
            Execute();
        }

        public bool RequireSave => true;
    }
}
