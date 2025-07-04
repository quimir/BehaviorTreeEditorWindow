using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Editor.View.BtWindows.BtTreeView.PortConnectionManager
{
    /// <summary>
    /// Defines an interface for managing the connection of ports within a graph view or tree view context.
    /// Provides methods for starting, updating, and finalizing port drag operations, as well as retrieving
    /// active port information and handling connection events.
    /// </summary>
    public interface IPortConnectionManager
    {
        /// <summary>
        /// Initiates the dragging process for a given port, setting it as the active port
        /// and creating a temporary connection (edge) for visual representation during drag operations.
        /// </summary>
        /// <param name="port">The port from which the drag operation is started.</param>
        void StartPortDrag(Port port);

        /// <summary>
        /// Updates the position of the temporary connection (edge) during a port drag operation.
        /// This method adjusts the endpoint of the temporary edge to align with the current mouse position in the graph view.
        /// </summary>
        /// <param name="position">The current position of the pointer in screen coordinates.</param>
        void UpdatePortDrag(Vector2 position);

        /// <summary>
        /// Finalizes the port dragging process, cleaning up the temporary connection
        /// and attempting to establish a final connection to a compatible target port.
        /// </summary>
        /// <param name="position">The position of the pointer at the end of the drag operation,
        /// used to identify potential target ports for connection.</param>
        void EndPortDrag(Vector2 position);

        /// <summary>
        /// Cancels the ongoing port drag operation, cleaning up the temporary connection (edge)
        /// created during the drag process and resetting the dragging state.
        /// </summary>
        void CancelPortDrag();

        /// <summary>
        /// Retrieves the currently active port in the connection manager, which is typically
        /// the port involved in an ongoing drag operation or the last port interacted with.
        /// </summary>
        /// <returns>The currently active port, or null if no port is active.</returns>
        Port GetActivePort();

        /// <summary>
        /// Event triggered when a context menu is requested. Typically invoked with a position vector indicating
        /// where the menu should be displayed.
        /// </summary>
        event Action<Vector2> OnMenuRequested;

        /// <summary>
        /// Event triggered when a new edge (connection between ports) is created within the graph view.
        /// Typically invoked after successfully connecting two ports.
        /// </summary>
        event Action<Edge> OnEdgeCreated;
    }
}
