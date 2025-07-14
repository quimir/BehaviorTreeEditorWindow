using BehaviorTree.BehaviorTreeBlackboard;
using BehaviorTree.BehaviorTreeBlackboard.Core;
using BehaviorTree.Core.WindowData;
using BehaviorTree.Nodes;
using Script.BehaviorTree.Save;

namespace BehaviorTree.Core
{
    /// <summary>
    /// Represents the core interface for behavior trees, providing methods and properties
    /// to manage the structure, execution, and associated data of a behavior tree.
    /// </summary>
    public interface IBehaviorTrees
    {
        /// <summary>
        /// Retrieves the root node of the behavior tree.
        /// </summary>
        /// <returns>The root node of the behavior tree as an instance of BtNodeBase.</returns>
        BtNodeBase GetRoot();

        /// <summary>
        /// Sets the root node of the behavior tree to the specified node.
        /// </summary>
        /// <param name="node_base">The node to be set as the root of the behavior tree. Must be an instance of BtNodeBase.</param>
        void SetRoot(BtNodeBase node_base);

        /// <summary>
        /// Removes the root node of the behavior tree, effectively detaching it from the structure.
        /// After invoking this method, the root node reference will be set to null.
        /// </summary>
        void DeleteRoot();

        /// <summary>
        /// Retrieves the window data for the behavior tree, which includes the state and layout of the UI.
        /// </summary>
        /// <returns>An instance of BehaviorTreeWindowData containing the current window data of the behavior tree.</returns>
        BehaviorTreeWindowData GetNodeWindow();

        /// <summary>
        /// Sets the window data for the behavior tree, including the root node and graphical view transformation data.
        /// </summary>
        /// <param name="node_window_data">An instance of BehaviorTreeWindowData containing the new window configuration and root node.</param>
        void SetNodeWindow(BehaviorTreeWindowData node_window_data);

        /// <summary>
        /// Deletes the node window data associated with the behavior tree.
        /// This operation clears or detaches any references to the node window,
        /// resetting its state within the behavior tree structure.
        /// </summary>
        void DeleteNodeWindow();

        /// <summary>
        /// Saves the current state of the behavior tree window to a file.
        /// </summary>
        /// <param name="file_path">The file path where the behavior tree window data will be saved. If null or empty, a default path will be used.</param>
        void SaveBtWindow(string file_path = null);

        /// <summary>
        /// Loads the behavior tree window data from the specified file path.
        /// </summary>
        /// <param name="file_path">The path of the file to load the behavior tree window data from. If null or empty, a default path may be used.</param>
        /// <returns>An instance of BehaviorTreeWindowData containing the deserialized behavior tree window data.</returns>
        BehaviorTreeWindowData LoadBtWindow(string file_path);

        /// <summary>
        /// Retrieves the unique identifier of the behavior tree.
        /// </summary>
        /// <returns>A string representing the unique identifier of the behavior tree.</returns>
        string GetTreeId();

        /// <summary>
        /// Retrieves the blackboard storage associated with the behavior tree,
        /// which manages key-value pairs for runtime data sharing and manipulation.
        /// </summary>
        /// <returns>The blackboard storage instance as an implementation of IBlackboardStorage.</returns>
        IBlackboardStorage GetBlackboard();

        /// <summary>
        /// Retrieves the window asset associated with the behavior tree structure.
        /// </summary>
        /// <returns>The window asset as an instance of BtWindowAsset.</returns>
        BtWindowAsset GetWindowAsset();
    }
}
