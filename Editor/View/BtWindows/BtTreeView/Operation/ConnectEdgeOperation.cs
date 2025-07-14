using Editor.EditorToolExs;
using Editor.EditorToolExs.Operation;
using Editor.EditorToolExs.Operation.Core;
using Editor.View.BTWindows.BtTreeView.NodeView;
using Editor.View.BtWindows.BtTreeView.NodeView.Core;
using Editor.View.BTWindows.BtTreeView.NodeView.NodeViewEdge;
using ExTools.Utillties;
using LogManager.Core;
using LogManager.LogManagerFactory;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.View.BTWindows.BtTreeView.Operation
{
    public class ConnectEdgeOperation : IOperation
    {
        private readonly BehaviorTreeView tree_view_;

        private readonly string output_node_guid_;
        private readonly string input_node_guid_;

        private Edge runtime_edge_;

        public ConnectEdgeOperation(Edge edge, BehaviorTreeView tree_view)
        {
            tree_view_ = tree_view;

            if (edge?.input?.node is BaseNodeView input_node_view &&
                edge?.output?.node is BaseNodeView output_node_view)
            {
                input_node_guid_ = input_node_view.NodeData.Guild;
                output_node_guid_ = output_node_view.NodeData.Guild;
            }
            else
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(
                    new LogSpaceNode("Operations"),
                    new LogEntry(LogLevel.kError, "ConnectEdgeOperation: Failed to initialize with valid node GUIDs."));
            }
        }

        public void Execute()
        {
            // 检查GUID是否有效
            if (string.IsNullOrEmpty(input_node_guid_)||string.IsNullOrEmpty(output_node_guid_))
            {
                return;
            }
            
            // 通过GUID在GraphView中查找当前的节点实例
            var input_node = tree_view_.GetNodeViewByGuid(input_node_guid_);
            var output_node = tree_view_.GetNodeViewByGuid(output_node_guid_);

            // 如果找不到节点节点，则不执行
            if (input_node==null||output_node==null)
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(
                    new LogSpaceNode("Operations"),
                    new LogEntry(LogLevel.kError, "ConnectEdgeOperation: Failed to find node with GUIDs."));
                return;
            }
            
            // 创建一个新的Edge实例并连接
            var edge = new FlowingEdge
            {
                input = input_node.InputPort,
                output = output_node.OutputPort
            };
            
            edge.AddEdgeEffect(new EdgeFlowIndicator());
            edge.AddEdgeEffect(new EdgeGradientLine());
            
            edge.input.Connect(edge);
            edge.output.Connect(edge);
            
            edge.pickingMode = PickingMode.Position;

            runtime_edge_ = edge;
            
            EditorExTools.Instance.LinkLineAddData(edge);
            tree_view_.AddElement(edge);
        }

        public void Undo()
        {
           // 如果没有活动的edge，则直接返回
           if (runtime_edge_==null)
           {
               return;
           }
           
           EditorExTools.Instance.UnLinkLineDelete(runtime_edge_);

           runtime_edge_.pickingMode = PickingMode.Ignore;
           if (runtime_edge_.input!=null)
           {
               runtime_edge_.input.Disconnect(runtime_edge_);
           }

           if (runtime_edge_.output!=null)
           {
               runtime_edge_.output.Disconnect(runtime_edge_);
           }
           
           tree_view_.RemoveElement(runtime_edge_);
           runtime_edge_ = null;
        }

        public void Redo()
        {
            Execute();
        }

        public bool RequireSave => true;
    }
}