using BehaviorTree.BehaviorTreeBlackboard;
using BehaviorTree.Nodes;
using Save.Serialization.Core.TypeConverter.SerializerAttribute;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BehaviorTree.Core.WindowData
{
    /// <summary>
    /// Represents the state of a behavior tree editor window, including its view transform,
    /// root node data, and a collection of node styles.
    /// This class is used to save and restore the behavior tree editor window state.
    /// </summary>
    [BoxGroup]
    [HideReferenceObjectPicker]
    public class BehaviorTreeWindowData
    {
        /// <summary>
        /// Represents the root node of the behavior tree structure.
        /// </summary>
        /// <remarks>
        /// This property serves as the entry point of the behavior tree. It defines the top-level node
        /// from which the behavior tree execution begins. The `RootNode` is essential for managing
        /// hierarchical relationships between nodes and determining the flow of behavior execution.
        /// Modifications to this property impact the root of the behavior tree and thereby the overall behavior
        /// logic of the system.
        /// </remarks>
        public BtNodeBase RootNode { get; set; }

        /// <summary>
        /// Defines configuration data for the editor window associated with behavior tree visualization and management.
        /// </summary>
        /// <remarks>
        /// This property contains details necessary for the editor window's state, layout, and interaction logic.
        /// It serves as a container for editor-specific settings, such as the transform of the graph view,
        /// allowing persistence and restoration of the editor's state across sessions.
        /// Modifications to this property directly influence the behavior of the editor window representation.
        /// </remarks>
        public BtEditorWindowData EditorWindowData { get; set; } = new BtEditorWindowData
        {
            GraphViewTransform = new GraphViewTransform
            {
                scale = new Vector3(1.0f, 1.0f, 1.0f),
            }
        };

        /// <summary>
        /// Represents a mapping of node styles used for customizing and managing visual styles
        /// of nodes within a behavior tree editor window.
        /// </summary>
        /// <remarks>
        /// This collection is used to store and retrieve style-related data for behavior tree nodes.
        /// It allows for loading, saving, and managing the appearance of nodes within the editor.
        /// The underlying implementation utilizes a collection of node style entries, which can be
        /// accessed and converted as needed to/from different data structures, such as dictionaries.
        /// </remarks>
        public BtNodeStyleCollection NodeStyleMap;


        /// <summary>
        /// Represents the blackboard storage for behavior tree data.
        /// </summary>
        /// <remarks>
        /// This variable serves as a centralized storage mechanism for behavior tree-related data.
        /// It encapsulates a key-value storage structure that allows the behavior tree nodes
        /// to share and access common data during runtime. The blackboard concept is widely
        /// used in behavior tree implementations to enable interaction and communication
        /// across different nodes without direct coupling.
        /// Modifications to this property impact the data accessible throughout the behavior tree,
        /// which may affect decision-making or actions performed by various behavior nodes.
        /// Typically, this property is initialized or assigned during the behavior tree setup process.
        /// </remarks>
        public LayeredBlackboard Blackboard = new LayeredBlackboard(null);
    }
}