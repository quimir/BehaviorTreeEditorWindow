using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Editor.View.BTWindows.NodeMenuProvider
{
    /// <summary>
    /// Represents a provider for displaying a contextual menu for node creation and interaction
    /// within a graph view editor.
    /// </summary>
    public interface INodeMenuProvider
    {
        /// <summary>
        /// Displays a contextual menu for node creation and interaction in the graph view editor.
        /// </summary>
        /// <param name="menu_position">The screen position at which the menu will be displayed.</param>
        /// <param name="pending_port">The port associated with the menu (optional); typically used to manage connections.</param>
        void ShowMenu(Vector2 menu_position, Port pending_port = null);

        /// <summary>
        /// Event triggered when a node is selected within the graph view editor.
        /// The event provides the selected node type, position, and associated port details.
        /// </summary>
        event Action<Type, Vector2, Port> OnNodeSelected;
    }
}
