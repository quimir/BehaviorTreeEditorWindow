using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BehaviorTree;
using BehaviorTree.Nodes;
using ExTools.Singleton;
using ExTools.Utillties;
using LogManager.Core;
using LogManager.LogManagerFactory;
using Save.Serialization.Core.FileStorage;
using Save.Serialization.Core.TypeConverter;
using Save.Serialization.Core.TypeConverter.SerializerAttribute;
using Save.Serialization.Factory;
using UnityEngine;

namespace Editor.View.BtWindows.BtTreeView.NodeView
{
    public class CopyNodeDataManager : SingletonWithLazy<CopyNodeDataManager>, IDisposable
    {
        /// <summary>
        /// Stores a collection of lists, where each internal list contains behavior tree nodes that have been copied.
        /// </summary>
        /// <remarks>
        /// This field serves as a repository for maintaining multiple sets of copied nodes, supporting scenarios where
        /// more complex clipboard operations or multiple layers of stored data are needed. Each inner list represents a
        /// group of `BtNodeBase` instances copied as a single operation.
        /// The data in `copy_node_list_` is marked with the `PersistField` attribute for potential serialization and
        /// deserialization purposes, allowing its content to be saved and loaded, for example, during editor sessions.
        /// Typically used in the behavior tree editor to track sets of copied nodes and facilitate actions like paste
        /// operations.
        /// </remarks>
        [PersistField] private List<List<BtNodeBase>> copy_node_list_ = new();
        
        [NonSerialize] private List<BtNodeBase> copy_node_ = new();

        /// <summary>
        /// Gets a value indicating whether there are any nodes currently copied to the clipboard in the behavior tree editing context.
        /// </summary>
        /// <remarks>
        /// This property checks if the internal list of copied nodes contains any items.
        /// Returns true if at least one node is present in the clipboard; otherwise, returns false.
        /// Typically used to enable or disable paste operations in the editor.
        /// </remarks>
        [NonSerialize]
        public bool IsCopyNode => copy_node_ is { Count: > 0 };

        /// <summary>
        /// Indicates whether the serialization state of copied node data should be saved to a file.
        /// </summary>
        /// <remarks>
        /// This static property determines if the current data of copied nodes within the behavior tree editor
        /// is written to a file during specific operations. If set to true, the copied data is serialized and saved
        /// when the internal saving logic is triggered, such as during resource disposal or other predefined events.
        /// The file is stored at a persistent data path with a predefined filename.
        /// </remarks>
        [NonSerialize] public static bool SaveToFile = false;

        protected override void InitializationInternal()
        {
            LoadCopyNodeFromFile();
        }

        /// <summary>
        /// Loads copied node data from a file located in the persistent data path.
        /// </summary>
        /// <remarks>
        /// This method attempts to deserialize copied node data from a file named "copy_node.txt."
        /// If the deserialization fails or the file does not exist, appropriate warnings are logged.
        /// Additionally, it updates the internal copy node list with the deserialized data if available.
        /// </remarks>
        private void LoadCopyNodeFromFile()
        {
            var path = Path.Combine(Application.persistentDataPath, "copy_node.txt");
            if (File.Exists(path))
            {
                var serializer = SerializerFactory.Instance.CreateSerializer(SerializerType.kJson,
                    new SerializationSettings
                    {
                        PrettyPrint = true,
                        PreserveReferences = true,
                        TypeNameHandling = SerializationTypeNameHandling.kAuto
                    });

                if (serializer is ISerializerFileStorage serializerFile)
                    serializerFile.LoadFromFile<CopyNodeDataManager>(path);

                if (copy_node_list_ == null || copy_node_list_.Count == 0)
                {
                    ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(
                        new LogSpaceNode("BehaviorTreeWindows").AddChild("BehaviorTreeView")
                            .AddChild("CopyNodeViewManager"), new LogEntry(LogLevel.kWarning, "当前复制管理器的反序列失败"));
                    return;
                }

                copy_node_ ??= new List<BtNodeBase>();
                
                copy_node_ = copy_node_list_.LastOrDefault();
            }
        }

        /// <summary>
        /// Adds node data to the internal copy node list.
        /// </summary>
        /// <param name="node_data">List of <see cref="BtNodeBase"/> nodes to be added.</param>
        /// <param name="reset_guid">Indicates whether to reset GUIDs of the nodes. Defaults to true.</param>
        public void AddNodeData(List<BtNodeBase> node_data, bool reset_guid = true)
        {
            if (node_data is not { Count: > 0 }) return;
            copy_node_ = reset_guid ? node_data.CloneData() : node_data;
            copy_node_list_.Add(copy_node_);
        }

        /// <summary>
        /// Retrieves the list of copied node data from the internal storage.
        /// </summary>
        /// <returns>A list of <see cref="BtNodeBase"/> instances representing the copied nodes.</returns>
        public List<BtNodeBase> GetNodeData()
        {
            return copy_node_;
        }

        /// <summary>
        /// Retrieves a copy of the node data list and resets the GUIDs of the nodes to ensure uniqueness.
        /// </summary>
        /// <returns>A list of <see cref="BtNodeBase"/> with updated GUIDs.</returns>
        public List<BtNodeBase> GetNodeDataAndResetGuid()
        {
            return copy_node_.CloneData();
        }

        private void Clear()
        {
            copy_node_list_.Clear();
            copy_node_.Clear();
        }

        /// <summary>
        /// Saves the current copy node data to a file if the SaveToFile flag is enabled.
        /// </summary>
        /// <remarks>
        /// This method serializes the internal state of the copy node data using a JSON serializer
        /// with specific serialization settings, including pretty printing and preserving references.
        /// The output file is saved to the application’s persistent data path with the filename "copy_node.txt".
        /// </remarks>
        private void SaveCopyNodeToFile()
        {
            if (SaveToFile)
            {
                var path = Path.Combine(Application.persistentDataPath, "copy_node.txt");
                var serializer = SerializerFactory.Instance.CreateSerializer(SerializerType.kJson,
                    new SerializationSettings
                    {
                        PrettyPrint = true,
                        PreserveReferences = true,
                        TypeNameHandling = SerializationTypeNameHandling.kAuto
                    });

                if (serializer is ISerializerFileStorage serializerFile) serializerFile.SaveToFile(this, path);
            }
        }

        public void Dispose()
        {
            if (SaveToFile) SaveCopyNodeToFile();
        }
    }
}