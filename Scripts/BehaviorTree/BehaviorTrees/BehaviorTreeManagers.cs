using System;
using System.Collections.Generic;
using System.Linq;
using BehaviorTree.Core;
using LogManager.Core;
using LogManager.LogManagerFactory;
using Save.CustomSerialize;
using Script.Utillties;
using UnityEditor;
using UnityEngine;

namespace BehaviorTree.BehaviorTrees
{
    /// <summary>
    /// The BehaviourTreeManagers is a singleton class used to manage the registration,
    /// lookup, and lifecycle of behavior trees and their associated windows.
    /// It provides methods to register and unregister behavior trees, manage
    /// their corresponding window instances, and query related data.
    /// </summary>
    [FilePath("BehaviourTreeManagers.asset", FilePathAttribute.Location.PreferencesFolder)]
    public class BehaviorTreeManagers : ScriptableSingleton<BehaviorTreeManagers>
    {
        /// <summary>
        /// A private dictionary that maintains a mapping of behavior tree identifiers (strings)
        /// to their respective behavior tree instances (`IBehaviorTrees`).
        /// This dictionary serves as the central repository for registered behavior trees
        /// within the `BehaviorTreeManagers` class.
        /// </summary>
        [SerializeField] private CustomSerializableDictionary<string, IBehaviorTrees> registered_trees_ = new();

        /// <summary>
        /// A private serialized dictionary that maps behavior tree identifiers (strings)
        /// to their corresponding window identifiers (strings). This dictionary is used
        /// internally by the `BehaviorTreeManagers` class to track the association between
        /// behavior trees and their respective open editor windows.
        /// </summary>
        [SerializeField] private CustomSerializableDictionary<string, string> open_window_map_ = new();

        /// <summary>
        /// A private field that stores a mapping between file paths (represented as strings)
        /// and their corresponding behavior tree instances (`IBehaviorTrees`).
        /// This dictionary serves as a lookup table for associating behavior trees with their respective
        /// external data file paths, enabling quick retrieval based on file location.
        /// </summary>
        [SerializeField] private CustomSerializableDictionary<string, IBehaviorTrees> file_path_to_tree_ = new();

        [NonSerialized] private static readonly LogSpaceNode log_space_ =
            new LogSpaceNode("BehaviorTree").AddChild("BehaviourTreeManagers");

        /// <summary>
        /// Registers a behavior tree in memory by associating the specified tree ID
        /// with the given `IBehaviorTrees` instance. Optionally allows overwriting
        /// an existing tree with the same ID.
        /// </summary>
        /// <param name="tree_id">
        /// The unique identifier of the behavior tree to be registered.
        /// </param>
        /// <param name="trees">
        /// The instance of the `IBehaviorTrees` representing the behavior tree to be registered.
        /// </param>
        /// <returns>
        /// True if the tree was successfully registered; otherwise, false.
        /// </returns>
        private bool RegisterTreeMemory(string tree_id, IBehaviorTrees trees)
        {
            if (string.IsNullOrEmpty(tree_id)) return false;

            if (registered_trees_.ContainsKey(tree_id) && registered_trees_[tree_id] == trees) return false;

            registered_trees_.Add(tree_id, trees);

            // 对文件路径进行注册
            if (!trees.GetWindowAsset() || !trees.GetWindowAsset().ExternalDataFileExists()) return true;
            if (file_path_to_tree_.ContainsKey(trees.GetWindowAsset().GetAbsoluteExternalDatePath()) &&
                file_path_to_tree_[trees.GetWindowAsset().GetAbsoluteExternalDatePath()] == trees)
                return true;

            file_path_to_tree_.Add(trees.GetWindowAsset().GetAbsoluteExternalDatePath(), trees);

            return true;
        }

        /// <summary>
        /// Registers multiple behavior trees in memory by associating their respective tree IDs
        /// with the given `IBehaviorTrees` instances. Optionally allows overwriting existing
        /// trees with the same IDs. Trigger a single save operation if any changes occur.
        /// </summary>
        /// <param name="trees_to_register">
        /// A dictionary containing the tree IDs as keys and the corresponding `IBehaviorTrees`
        /// instances as values to be registered.
        /// </param>
        public void RegisterTrees(Dictionary<string, IBehaviorTrees> trees_to_register)
        {
            var has_changed = false;
            foreach (var pair in trees_to_register)
                // 使用只操作内存的方法
                if (RegisterTreeMemory(pair.Key, pair.Value))
                    has_changed = true;

            // 如果在整个批量操作中有任何实际的更改，
            // 则在所有操作完成后，只调用一次 SetDirty！
            if (has_changed) EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Registers a behavior tree with the specified tree ID and instance of `IBehaviorTrees`.
        /// Optionally allows overwriting an existing tree with the same ID.
        /// </summary>
        /// <param name="tree_id">
        /// The unique identifier associated with the behavior tree to register. Cannot be null or empty.
        /// </param>
        /// <param name="trees">
        /// The instance of the `IBehaviorTrees` that represents the behavior tree to be registered.
        /// </param>
        public void RegisterTree(string tree_id, IBehaviorTrees trees)
        {
            if (string.IsNullOrEmpty(tree_id))
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                    new LogEntry(LogLevel.kError, "Cannot register tree with empty ID"), true);
                return;
            }

            var has_changed = false;
            if (RegisterTreeMemory(tree_id, trees))
            {
                has_changed = true;
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                    new LogEntry(LogLevel.kInfo,
                        $"The ID of the behavior tree is successfully registered. The ID of the behavior tree " +
                        $"is: {trees.GetTreeId()}"));
            }

            if (has_changed) EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Unregisters a behavior tree from the manager using the specified tree ID.
        /// </summary>
        /// <param name="tree_id">The unique identifier of the behavior tree to unregister.</param>
        public void UnRegisterTree(string tree_id)
        {
            if (string.IsNullOrEmpty(tree_id))
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                    new LogEntry(LogLevel.kError, "Cannot unregister tree with empty ID"), true);
                return;
            }

            if (registered_trees_.Remove(tree_id, out var trees))
            {
                if (trees.GetWindowAsset() && trees.GetWindowAsset().ExternalDataFileExists())
                    file_path_to_tree_.Remove(trees.GetWindowAsset().GetAbsoluteExternalDatePath());

                EditorUtility.SetDirty(this);
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                    new LogEntry(LogLevel.kInfo,
                        $"The ID of the behavior tree is successfully unregistered. The ID of the behavior " +
                        $"tree is: {trees.GetTreeId()}"));
            }
        }

        /// <summary>
        /// Retrieves the behavior tree associated with the specified tree ID.
        /// </summary>
        /// <param name="tree_id">The unique identifier of the behavior tree to retrieve.</param>
        /// <returns>
        /// The `IBehaviorTrees` instance associated with the given tree ID if it exists;
        /// otherwise, `null` if the tree ID is not registered.
        /// </returns>
        public IBehaviorTrees GetTree(string tree_id)
        {
            if (string.IsNullOrEmpty(tree_id)) return null;

            if (registered_trees_.TryGetValue(tree_id, out var tree)) return tree;

            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                new LogEntry(LogLevel.kWarning, $"Behavior tree not found: {tree_id}"));
            return null;
        }

        /// <summary>
        /// Searches for a behavior tree instance based on the provided file path and returns the corresponding
        /// `IBehaviorTrees` instance if found.
        /// </summary>
        /// <param name="file_path">The absolute file path used to locate the associated behavior tree. This must not
        /// be null or empty.</param>
        /// <returns>
        /// Returns the `IBehaviorTrees` instance associated with the given file path if it exists.
        /// If no matching tree is found or the file path is invalid, returns `null`.
        /// </returns>
        public IBehaviorTrees FindTreeByFilePath(string file_path)
        {
            if (string.IsNullOrEmpty(file_path))
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                    new LogEntry(LogLevel.kWarning, "File path is empty"));
                return null;
            }

            // 首先尝试从文件路径映射中查找（只查找非临时树）
            if (file_path_to_tree_.TryGetValue(file_path, out var tree)) return tree;

            foreach (var registered_tree in registered_trees_.Values)
                if (registered_tree.GetWindowAsset() && registered_tree.GetWindowAsset().ExternalDataFileExists())
                    if (registered_tree.GetWindowAsset().GetAbsoluteExternalDatePath().Equals(file_path))
                    {
                        registered_trees_.Add(file_path, registered_tree);
                        return registered_tree;
                    }

            return null;
        }

        /// <summary>
        /// Associates a behavior tree identified by tree ID with a specific editor window,
        /// identified by its window ID. This ensures that the tree is bound to the window instance
        /// for further management and operational tracking.
        /// </summary>
        /// <param name="tree_id">
        /// The unique identifier of the behavior tree to be registered.
        /// </param>
        /// <param name="window_id">
        /// The unique identifier of the editor window to associate with the behavior tree.
        /// </param>
        /// <returns>
        /// True if the registration is successful, or false if the tree or window ID is invalid
        /// or if the registration fails due to an existing association.
        /// </returns>
        public bool RegisterWindow(string tree_id, string window_id)
        {
            if (string.IsNullOrWhiteSpace(window_id) || string.IsNullOrWhiteSpace(tree_id)) return false;

            if (!open_window_map_.TryAdd(tree_id, window_id)) return false;

            EditorUtility.SetDirty(this);
            return true;
        }

        /// <summary>
        /// Unregisters a previously registered window from the tree ID association map.
        /// </summary>
        /// <param name="tree_id">The unique identifier of the behavior tree whose associated window should be
        /// unregistered.</param>
        /// <returns>
        /// Returns `true` if the window associated with the given tree ID was successfully unregistered; otherwise,
        /// returns `false`if no association existed for the specified tree ID.
        /// </returns>
        public bool UnRegisterWindowFormTreeId(string tree_id)
        {
            var register_value = open_window_map_.Remove(tree_id);
            if (register_value) EditorUtility.SetDirty(this);

            return register_value;
        }

        /// <summary>
        /// Unregisters a window from the internal mapping using its unique window ID.
        /// </summary>
        /// <param name="window_id">The unique identifier of the window to be unregistered.</param>
        /// <returns>
        /// Returns `true` if the window was successfully unregistered; otherwise, returns `false`
        /// if the window ID does not exist in the mapping.
        /// </returns>
        public bool UnRegisterWindowFromWindowId(string window_id)
        {
            var key = open_window_map_.FirstOrDefault(n => n.Value == window_id).Key;
            if (string.IsNullOrEmpty(key)) return false;

            var register_value =
                open_window_map_.Remove(key);
            if (register_value) EditorUtility.SetDirty(this);

            return register_value;
        }

        /// <summary>
        /// Retrieves the `IBehaviorTrees` instance associated with the specified window ID, if it exists.
        /// </summary>
        /// <param name="window_id">The unique identifier of the window for which to retrieve the associated
        /// behavior tree.</param>
        /// <returns>
        /// Returns the `IBehaviorTrees` instance if a behavior tree is associated with the specified window ID;
        /// otherwise, returns `null` if no match is found or if the input is null or empty.
        /// </returns>
        public IBehaviorTrees GetTreeByWindowId(string window_id)
        {
            return string.IsNullOrWhiteSpace(window_id)
                ? null
                : GetTree(open_window_map_.FirstOrDefault(n => n.Value == window_id).Key);
        }

        /// <summary>
        /// Retrieves the tree ID associated with the given window ID.
        /// </summary>
        /// <param name="window_id">The unique identifier of the window for which the tree ID is being retrieved.</param>
        /// <returns>
        /// Returns the tree ID associated with the provided window ID. If no association exists or the input is
        /// invalid, an empty string is returned.
        /// </returns>
        public string GetTreeIdByWindowId(string window_id)
        {
            if (string.IsNullOrWhiteSpace(window_id)) return string.Empty;

            var tree_id = open_window_map_.FirstOrDefault(n => n.Value == window_id).Key;

            return tree_id;
        }

        /// <summary>
        /// Retrieves the window ID associated with the specified tree ID from the open window map.
        /// Returns an empty string if the tree ID is null, empty, or not found in the map.
        /// </summary>
        /// <param name="tree_id">
        /// The unique identifier of the behavior tree whose associated window ID is to be retrieved.
        /// </param>
        /// <returns>
        /// The window ID associated with the specified tree ID, or an empty string if the association is not found.
        /// </returns>
        public string GetWindowIdByTreeId(string tree_id)
        {
            if (string.IsNullOrWhiteSpace(tree_id) || string.IsNullOrEmpty(tree_id)) return string.Empty;

            return open_window_map_.FirstOrDefault(n => n.Key == tree_id).Value;
        }

        public void SaveAllData()
        {
            Save(true);
        }
    }
}