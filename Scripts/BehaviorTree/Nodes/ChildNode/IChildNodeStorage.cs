using System.Collections.Generic;

namespace BehaviorTree.Nodes.ChildNode
{
    /// <summary>
    /// Represents a storage mechanism for managing child nodes within a behavior tree structure.
    /// </summary>
    public interface IChildNodeStorage
    {
        /// <summary>
        /// Retrieves the list of child nodes as instances of the base node type (BtNodeBase).
        /// </summary>
        /// <returns>
        /// A list of BtNodeBase objects representing the child nodes of the implementing storage.
        /// </returns>
        List<BtNodeBase> GetChildNodesAsBase();

        /// <summary>
        /// Adds a child node to the current storage implementation.
        /// </summary>
        /// <param name="node">The child node to be added. Must be of type BtNodeBase.</param>
        void AddChild(BtNodeBase node);

        /// <summary>
        /// Inserts a child node at the specified index within the storage.
        /// </summary>
        /// <param name="node">The child node to insert.</param>
        /// <param name="index">The zero-based index at which the child node should be inserted.</param>
        void InsertChildNode(BtNodeBase node, int index);

        /// <summary>
        /// Removes the specified child node from the collection of child nodes.
        /// </summary>
        /// <param name="node">The child node to be removed.</param>
        /// <returns>
        /// A boolean value indicating whether the removal was successful. Returns true if the child node was found and removed, otherwise false.
        /// </returns>
        bool RemoveChildNode(BtNodeBase node);

        /// <summary>
        /// Removes all child nodes from the current storage, effectively clearing its contents.
        /// </summary>
        void Clear();

        /// <summary>
        /// Gets the number of child nodes managed by the current storage implementation.
        /// </summary>
        /// <value>
        /// An integer representing the total count of child nodes stored.
        /// </value>
        int Count { get; }

        /// <summary>
        /// Relink child nodes within the storage after a cloning operation by validating them against a provided
        /// collection of cloned nodes. Any invalid or missing references are removed to ensure the integrity of the
        /// storage.
        /// </summary>
        /// <param name="all_clone_nodes">A read-only collection of newly cloned nodes used to validate and relink the
        /// child nodes in the storage.</param>
        void PostCloneRelink(IReadOnlyCollection<BtNodeBase> all_clone_nodes);
    }

    /// <summary>
    /// Defines a contract for a generic storage mechanism handling child nodes within a behavior tree,
    /// associated with a specific type of node.
    /// </summary>
    /// <typeparam name="T">The type representing the child nodes managed by the storage.</typeparam>
    public interface IChildNodeStorage<T> : IChildNodeStorage
    {
        /// <summary>
        /// Retrieves the list of child nodes managed by the storage.
        /// </summary>
        /// <returns>
        /// A list of child nodes specific to the implementing storage, represented by the generic type.
        /// </returns>
        List<T> GetChildren();

        /// <summary>
        /// Adds a child node to the current storage.
        /// </summary>
        /// <param name="node">The child node to be added to the storage.</param>
        void AddChild(T node);

        /// <summary>
        /// Inserts a child node into the collection at the specified index.
        /// </summary>
        /// <param name="node">The child node to be inserted.</param>
        /// <param name="index">The zero-based index at which the child node should be inserted.</param>
        void InsertChild(T node, int index);

        /// <summary>
        /// Removes the specified child node from the current storage.
        /// </summary>
        /// <param name="node">The child node to be removed from the storage.</param>
        /// <returns>
        /// A boolean value indicating whether the specified child node was successfully removed.
        /// Returns true if the node was found and removed; otherwise, false.
        /// </returns>
        bool RemoveChild(T node);
    }
}
