using System.Collections.Generic;
using System.Linq;
using BehaviorTree.BehaviorTrees;
using Editor.EditorToolExs.Operation.Core;
using Editor.View.BTWindows.BtTreeView;
using Editor.View.BTWindows.BtTreeView.NodeView;
using Editor.View.BtWindows.BtTreeView.NodeView.Core;
using UnityEditor.Experimental.GraphView;

namespace Editor.View.BtWindows.BtTreeView.Operation
{
    /// <summary>
    /// Represents a composite operation that handles the deletion of multiple nodes in a behavior tree view.
    /// </summary>
    /// <remarks>
    /// This class implements a composite pattern to group and execute multiple delete operations
    /// as a single unit. Each delete operation corresponds to an individual node to be removed from the tree.
    /// It supports execution, undo, redo functionality, and save state checks.
    /// </remarks>
    public class DeleteNodesCompositeOperation :IOperation
    {
        private readonly List<BaseNodeView> nodes_to_delete_;
        private readonly BtNodeViewManager node_manager_;
        private readonly List<IOperation> sub_operations_;
        private readonly BehaviorTreeView tree_view_;

        /// <summary>
        /// A composite operation that encapsulates the deletion of multiple nodes in a behavior tree view.
        /// </summary>
        public DeleteNodesCompositeOperation(List<BaseNodeView> nodes_to_delete, BehaviorTreeView tree_view)
        {
            nodes_to_delete_ = nodes_to_delete;
            tree_view_ = tree_view;
            node_manager_ = tree_view_.NodeViewManager;
            sub_operations_ = new List<IOperation>();

            BuildSubOperations();
        }

        private void BuildSubOperations()
        {
            // 收集所有需要删除的边
            var edges_to_remove = new HashSet<Edge>();

            foreach (var node in nodes_to_delete_)
            {
                var connections = node_manager_.GetNodeConnections(node);
                foreach (var edge in connections)
                {
                    edges_to_remove.Add(edge);
                }
            }
            
            // 先创建删除边的操作
            foreach (var edge in edges_to_remove)
            {
                sub_operations_.Add(new RemoveEdgeOperation(edge,node_manager_));
            }
            
            // 再创建删除节点的操作
            foreach (var node in nodes_to_delete_)
            {
                var tree = BehaviorTreeManagers.instance.GetTreeByWindowId(tree_view_.Windows.WindowInstanceId);
                if (tree!=null&&tree.GetRoot()?.Guild==node.NodeData.Guild)
                {
                    sub_operations_.Add(new RemoveRootNodeOperation(node,node_manager_));
                }
                else
                {
                    sub_operations_.Add(new RemoveNodeOperation(node,node_manager_));
                }
            }
        }

        public void Execute()
        {
            foreach (var operation in sub_operations_)
            {
                operation.Execute();
            }
            
            tree_view_.ClearSelection();
        }

        public void Undo()
        {
            foreach (var operation in sub_operations_.AsEnumerable().Reverse())
            {
                operation.Undo();
            }
        }

        public void Redo()
        {
            Execute();
        }

        public bool RequireSave => sub_operations_.Any(x=>x.RequireSave);
    }
}
