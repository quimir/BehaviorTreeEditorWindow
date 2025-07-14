using Editor.EditorToolExs;
using Editor.EditorToolExs.Operation;
using Editor.EditorToolExs.Operation.Core;
using Editor.View.BTWindows.BtTreeView;
using Editor.View.BTWindows.BtTreeView.NodeView;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.View.BtWindows.BtTreeView.Operation
{
    /// <summary>
    /// Represents an operation to remove an edge from a graph-based view, including
    /// disconnecting the edge from associated nodes, removing it from the visual
    /// representation, and managing associated data changes. Implements the <see cref="IOperation"/>
    /// interface to support execution, undo, and redo behavior.
    /// </summary>
    public class RemoveEdgeOperation : IOperation
    {
        private readonly Edge edge_;
        private readonly BtNodeViewManager node_manager_;
    
        public RemoveEdgeOperation(Edge edge,BtNodeViewManager tree_view)
        {
            node_manager_ = tree_view;
            edge_ = edge;
        }
    
        public void Execute()
        {
            // 先移除数据连接
            EditorExTools.Instance.UnLinkLineDelete(edge_);

            edge_.pickingMode = PickingMode.Ignore;
        
            // 断开视图连接
            node_manager_.DisconnectEdge(edge_);
        
            // 从视图中移除
            node_manager_.RemoveElementFromView(edge_);
        }

        public void Undo()
        {
            // 重新添加到视图
            node_manager_.AddElementToView(edge_);
            edge_.pickingMode = PickingMode.Position;
            node_manager_.ConnectEdge(edge_);
            EditorExTools.Instance.LinkLineAddData(edge_);
        }

        public void Redo()
        {
            Execute();
        }

        public bool RequireSave => true;
    }
}
