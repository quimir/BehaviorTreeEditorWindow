using Editor.View.BtWindows.BtTreeView.PortConnectionManager;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.View.BtWindows.BtTreeView.NodeView.Core
{
    /// <summary>
    /// Handles drag-and-drop interactions for a specific port within a graph view.
    /// Provides handlers for pointer events such as down, move, up, and cancel, enabling connections between ports.
    /// </summary>
    public class PortDragHandler : PointerManipulator
    {
        /// <summary>
        /// Represents the specific port associated with the drag-and-drop operation managed by the PortDragHandler.
        /// Serves as the target for pointer events such as down, move, up, and cancel.
        /// </summary>
        private readonly Port port_;

        /// <summary>
        /// Manages the port connection behavior during drag-and-drop operations within the PortDragHandler.
        /// Responsible for beginning, updating, finalizing, and canceling port drag interactions, as well as
        /// handling associated connection events.
        /// </summary>
        private readonly IPortConnectionManager connection_manager_;

        /// <summary>
        /// Stores the initial position of the pointer when a drag operation begins on a port.
        /// Used to calculate changes in pointer position during the dragging process.
        /// </summary>
        private Vector2 drag_start_position_;

        /// <summary>
        /// A boolean flag indicating whether the port dragging operation is currently in progress.
        /// </summary>
        private bool dragging_;

        public PortDragHandler(Port port, IPortConnectionManager connectionManager)
        {
            port_ = port;
            connection_manager_ = connectionManager;
            target = port_;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(PointerDownHandler);
            target.RegisterCallback<PointerMoveEvent>(PointerMoveHandler);
            target.RegisterCallback<PointerUpEvent>(PointUpHandler);
            target.RegisterCallback<PointerCancelEvent>(PointerCancelHandler);
        }
        
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(PointerDownHandler);
            target.UnregisterCallback<PointerMoveEvent>(PointerMoveHandler);
            target.UnregisterCallback<PointerUpEvent>(PointUpHandler);
            target.UnregisterCallback<PointerCancelEvent>(PointerCancelHandler);
        }

        /// <summary>
        /// Handles the pointer cancel event during a port drag operation.
        /// Cancels the ongoing drag operation if active and releases the pointer capture.
        /// </summary>
        /// <param name="evt">The pointer cancel event containing details about the event.</param>
        private void PointerCancelHandler(PointerCancelEvent evt)
        {
            if (dragging_)
            {
                connection_manager_.CancelPortDrag();
                dragging_ = false;
            }
            
            target.ReleasePointer(evt.pointerId);
            evt.StopPropagation();
        }

        /// <summary>
        /// Handles the pointer up event during a port drag operation.
        /// Ends the drag operation if active, releases the pointer capture, and stops further event propagation.
        /// </summary>
        /// <param name="evt">The pointer up event containing details about the pointer interaction.</param>
        private void PointUpHandler(PointerUpEvent evt)
        {
            if (!target.HasPointerCapture(evt.pointerId))
            {
                return;
            }

            if (dragging_)
            {
                connection_manager_.EndPortDrag(evt.position);
                dragging_ = false;
            }
            
            target.ReleasePointer(evt.pointerId);
            evt.StopPropagation();
        }

        /// <summary>
        /// Handles the pointer move event during a port drag operation.
        /// Updates the drag state and connection line as the pointer moves across the graph view.
        /// </summary>
        /// <param name="evt">The pointer move event containing details about the pointer's position and movement.</param>
        private void PointerMoveHandler(PointerMoveEvent evt)
        {
            if (!target.HasPointerCapture(evt.pointerId))
            {
                return;
            }

            Vector2 diff = evt.position - (Vector3)drag_start_position_;
            
            // 如果移动距离超过阈值，开始拖拽
            if (!dragging_&&diff.magnitude>10)
            {
                dragging_ = true;
                connection_manager_.StartPortDrag(port_);
            }

            if (dragging_)
            {
                // 更新连接线位置
                connection_manager_.UpdatePortDrag(evt.position);
            }
            
            evt.StopPropagation();
        }

        /// <summary>
        /// Handles the pointer down event during a port drag operation.
        /// Initiates the drag operation and captures the pointer for further drag-related events.
        /// </summary>
        /// <param name="evt">The pointer down event containing details about the event, such as the position and button pressed.</param>
        private void PointerDownHandler(PointerDownEvent evt)
        {
            // 只处理左键
            if (evt.button!=0)
            {
                return;
            }

            drag_start_position_ = evt.position;
            target.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }
    }
}
