using BehaviorTree.Nodes;
using UnityEditor.Experimental.GraphView;

namespace Editor.View.BtWindows.BtTreeView.NodeView.Core
{
    /// <summary>
    /// Represents the foundational class for node views in a behavior tree editor.
    /// Extends the Unity GraphView Node class and provides additional functionality
    /// for working with behavior tree nodes, including data associations, input/output ports,
    /// and customizable styles.
    /// </summary>
    public abstract class BaseNodeView : Node
    {
        /// <summary>
        /// Stores the underlying model data associated with this node view.
        /// Represents the behavior tree node's core data, defining its fundamental properties,
        /// state, and behavior logic. Acts as the primary bridge between the visual editor
        /// representation and the in-memory behavior tree structure.
        /// </summary>
        protected BtNodeBase node_data_;

        /// <summary>
        /// Represents the input port for the node in the behavior tree view.
        /// Used to receive incoming connections from other nodes, typically providing
        /// data or control flow into the current node. Configured with specific direction
        /// and capacity based on the node's type and role in the behavior tree structure.
        /// </summary>
        protected Port input_port_;

        /// <summary>
        /// Represents the output port of a node in the behavior tree editor.
        /// Provides a graphical interface for connecting this node to other nodes in the tree,
        /// enabling data flow and execution linking.
        /// The behavior of the output port may vary based on the type of node it is associated with,
        /// such as composite or precondition nodes.
        /// </summary>
        protected Port output_port_;

        protected BaseNodeView()
        {
        }

        /// <summary>
        /// Serves as the base class for visual representation of behavior tree nodes within a custom editor.
        /// Provides essential functionality for managing ports, node data, and interaction with the behavior tree model.
        /// This class extends Unity's `Node` class and is intended to be inherited by specific node view implementations.
        /// </summary>
        protected BaseNodeView(string uiFile) : base(uiFile)
        {
        }

        /// <summary>
        /// Gets the underlying data node associated with this NodeView instance.
        /// This property serves as the primary data object that the node view interacts with,
        /// encapsulating the core attributes and behaviors of the behavior tree node.
        /// </summary>
        public BtNodeBase NodeData => node_data_;

        /// <summary>
        /// Represents the input port of the node view.
        /// This port is used to establish connections where the node acts as the destination
        /// for incoming edges from other nodes in the behavior tree structure.
        /// </summary>
        public Port InputPort => input_port_;

        /// <summary>
        /// Provides access to the output port of the node view, enabling connection to other nodes in the graph.
        /// This property is used to establish outgoing edges from the current node within the behavior tree structure.
        /// </summary>
        public Port OutputPort => output_port_;

        /// <summary>
        /// Initializes the view of the node, setting up ports, styles, and any required configurations.
        /// This method is abstract in the base class and defines the core setup logic for derived node views.
        /// </summary>
        protected abstract void InitializeView();

        /// <summary>
        /// Updates the view of the node by synchronizing its properties with the underlying data model.
        /// Ensures that node's visual representation (title, position) matches the associated node data.
        /// If in play mode, updates the connection edges' states. For composite nodes, sorts child nodes based on their horizontal positions.
        /// </summary>
        public abstract void UpdateView();

        /// <summary>
        /// Applies styles to the node view by retrieving and using style configurations associated with the node data.
        /// This method customizes the appearance of the node, including main structure, title, and title label,
        /// based on the corresponding style attributes provided by the style manager.
        /// If no style is found for the node, it gracefully exits without applying any changes.
        /// </summary>
        public virtual void ApplyStyle()
        {
        }
    }
}
