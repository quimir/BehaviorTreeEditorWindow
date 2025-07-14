using System.Collections.Generic;

namespace BehaviorTree.BehaviorTreeBlackboard.Core
{
    /// <summary>
    /// Represents a storage mechanism for managing key-value pairs in a blackboard system,
    /// providing functionality to retrieve, add, remove, and verify the existence of data.
    /// </summary>
    public interface IBlackboardStorage
    {
        /// <summary>
        /// Attempts to retrieve the value associated with the specified key in the blackboard.
        /// </summary>
        /// <typeparam name="T">The type of the value associated with the key.</typeparam>
        /// <param name="key">The blackboard key used to locate the value.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key if the
        /// key is found; otherwise, the default value for the type of the <typeparamref name="T"/> parameter. This
        /// parameter is passed uninitialized.</param>
        /// <returns>true if the key is found and its value is successfully retrieved; otherwise, false.</returns>
        bool TryGetValue<T>(BlackboardKey<T> key, out T value);

        /// <summary>
        /// Sets the specified value associated with the given key in the blackboard.
        /// </summary>
        /// <typeparam name="T">The type of the value to be stored in the blackboard.</typeparam>
        /// <param name="key">The blackboard key used to identify the value.</param>
        /// <param name="value">The value to be stored associated with the specified key.</param>
        void SetValue<T>(BlackboardKey<T> key, T value);

        /// <summary>
        /// Removes the value associated with the specified key from the blackboard.
        /// </summary>
        /// <typeparam name="T">The type of the value associated with the key.</typeparam>
        /// <param name="key">The blackboard key used to identify the value to be removed.</param>
        /// <returns>true if the value associated with the key was successfully removed; otherwise, false.</returns>
        bool RemoveValue<T>(BlackboardKey<T> key);

        /// <summary>
        /// Determines whether the blackboard contains a value associated with the specified key.
        /// </summary>
        /// <typeparam name="T">The type of the value associated with the key.</typeparam>
        /// <param name="key">The blackboard key used to check for the existence of an associated value.</param>
        /// <returns>true if the key exists in the blackboard; otherwise, false.</returns>
        bool ContainsKey<T>(BlackboardKey<T> key);

        /// <summary>
        /// Retrieves all keys currently stored in the blackboard.
        /// </summary>
        /// <returns>An enumerable collection containing all keys present in the blackboard.</returns>
        IEnumerable<IBlackboardKey> GetAllKeys();
    }
}
