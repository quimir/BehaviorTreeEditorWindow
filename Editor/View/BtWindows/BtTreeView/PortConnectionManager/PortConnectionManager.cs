using System;
using Editor.View.BTWindows.BtTreeView;
using Editor.View.BTWindows.BtTreeView.NodeView.NodeViewEdge;
using ExTools.Utillties;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.View.BtWindows.BtTreeView.PortConnectionManager
{
    /// <summary>
    /// Manages the connection between ports in a graph view.
    /// Provides functionality for handling the creation and cancellation of connections,
    /// and facilitates interaction with the graph view during port dragging.
    /// </summary>
    public class PortConnectionManager:IPortConnectionManager
    {
        /// <summary>
        /// Reference to the <see cref="GraphView"/> instance used for managing
        /// and interacting with the visual representation of a behavior tree.
        /// This variable is utilized within the <see cref="PortConnectionManager"/>
        /// class to add elements, manage ports, and to facilitate various operations
        /// related to graph view manipulation.
        /// </summary>
        private readonly GraphView tree_view_;
        
        private Port active_port_;
        
        private FlowingEdge temp_edge_;
        
        public event Action<Vector2> OnMenuRequested;
        
        public event Action<Edge> OnEdgeCreated;
        
        /// <summary>
        /// Manages the connection between ports in a graph view.
        /// Provides functionality for handling the creation and cancellation of connections,
        /// and facilitates interaction with the graph view during port dragging.
        /// </summary>
        /// <param name="tree_view">Reference to the <see cref="GraphView"/> instance used for managing
        /// and interacting with the visual representation of a behavior tree.</param>
        public PortConnectionManager(GraphView tree_view)
        {
            tree_view_ = tree_view;
        }
        
        public Port GetActivePort()
        {
            return active_port_;
        }

        public void StartPortDrag(Port port)
        {
            active_port_ = port;
            
            // 创建临时连接线
            temp_edge_ = new FlowingEdge()
            {
                output = port.direction == Direction.Output ? port : null,
                input = port.direction == Direction.Input ? port : null,
            };
            temp_edge_.AddEdgeEffect(new EdgeFlowIndicator());
            temp_edge_.AddEdgeEffect(new EdgeGradientLine());
        }

        public void UpdatePortDrag(Vector2 position)
        {
            if (temp_edge_==null||active_port_==null)
            {
                return;
            }
            
            // 将鼠标位置转换为图表空间坐标
            var mouse_position=tree_view_.contentViewContainer.WorldToLocal(position);
            
            // 更新临时连接的端点位置
            if (active_port_.direction==Direction.Output)
            {
                // 更新目标点位置
                var closest_port = FindClosestPort(mouse_position, Direction.Input);
                if (closest_port!=null)
                {
                    temp_edge_.input = closest_port;
                }
                else
                {
                    temp_edge_.input?.Disconnect(temp_edge_);
                    temp_edge_.input = null;
                }
            }
            else
            {
                var closest_port = FindClosestPort(mouse_position, Direction.Output);
                if (closest_port!=null)
                {
                    temp_edge_.output = closest_port;
                }
                else
                {
                    temp_edge_.output?.Disconnect(temp_edge_);
                    temp_edge_.output = null;
                }
            }
            
            temp_edge_.MarkDirtyRepaint();
        }

        public void EndPortDrag(Vector2 position)
        {
            if (temp_edge_==null||active_port_==null)
            {
                return;
            }
            
            // 获取鼠标位置下的可连接端口
            var target_port = GetCompatiblePortAtPosition(position);
            
            if (target_port!=null)
            {
                // 找到了兼容端口，创建实际的连接
                var edge = new FlowingEdge()
                {
                    output = active_port_.direction == Direction.Output ? active_port_ : target_port,
                    input = active_port_.direction == Direction.Input ? active_port_ : target_port
                };
                edge.AddEdgeEffect(new EdgeFlowIndicator());
                edge.AddEdgeEffect(new EdgeGradientLine());
                 
                edge.input.Connect(edge);
                edge.output.Connect(edge);
                tree_view_.AddElement(edge);
                 
                OnEdgeCreated?.Invoke(edge);
            }
            else
            {
                // 没有找到兼容端口，请求显示节点创建菜单
                OnMenuRequested?.Invoke(position);
            }
            
            CleanupTempEdge();
        }

        /// <summary>
        /// Finds the closest port to the specified position within a certain distance threshold.
        /// The search is filtered by the specified port direction (Input or Output).
        /// </summary>
        /// <param name="position">The position used as a reference to find the closest port.</param>
        /// <param name="direction">The direction of the port to be filtered, either Input or Output.</param>
        /// <returns>The closest port matching the specified direction within the threshold, or null if no port is found.</returns>
        private Port FindClosestPort(Vector2 position, Direction direction)
        {
            Port closet_port = null;
            var min_distance = float.MaxValue;

            foreach (var port in tree_view_.ports.ToList())
            {
                if (port.direction != direction) continue;

                var port_world_position = port.worldBound.center;
                var distance = Vector2.Distance(position, tree_view_.contentContainer.WorldToLocal(port_world_position));

                // 设置一个最大距离阈值
                if (distance < min_distance && distance < 30)
                {
                    min_distance = distance;
                    closet_port = port;
                }
            }

            return closet_port;
        }

        /// <summary>
        /// Cleans up the temporary edge created during a port drag operation.
        /// Removes the temporary edge from the graph view and resets references
        /// to the active port and temporary edge, ensuring the internal state is updated accordingly.
        /// </summary>
        private void CleanupTempEdge()
        {
            if (temp_edge_!=null)
            {
                tree_view_.RemoveElement(temp_edge_);
                temp_edge_ = null;
            }
            
            active_port_ = null;
        }

        /// <summary>
        /// 获取所有兼容端口的位置，遍历该端口下的所有兼容端口，如果找到兼容的则可以进行连接
        /// </summary>
        /// <param name="position">现在端口的位置</param>
        /// <returns>连接端口</returns>
        private Port GetCompatiblePortAtPosition(Vector2 position)
        {
            // 将position转换为正确的坐标空间
            var mouse_position = tree_view_.contentViewContainer.WorldToLocal(position);

            // 遍历所有端口，找到鼠标位置下的兼容接口
            foreach (var port in tree_view_.ports.ToList())
            {
                // 跳过源端口自身
                if (port == active_port_) continue;

                // 检查端口方向是否兼容
                if (port.direction == active_port_.direction) continue;

                // 检查端口是否在鼠标位置附近
                var port_world_position = port.worldBound.center;
                var port_position = tree_view_.contentViewContainer.WorldToLocal(port_world_position);

                var distance = Vector2.Distance(mouse_position, port_position);
                if (distance <= FixedValues.kNodeBetweenLineDistance) return port;
            }

            return null;
        }

        public void CancelPortDrag()
        {
            CleanupTempEdge();
        }
    }
}
