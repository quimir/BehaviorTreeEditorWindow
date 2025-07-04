using BehaviorTree.Nodes;
using Editor.View.BTWindows.BtTreeView.NodeView;
using ExTools.Singleton;
using ExTools.Utillties;
using LogManager.Core;
using LogManager.LogManagerFactory;
using Script.BehaviorTree;
using Script.BehaviorTree.Save;
using Script.LogManager;
using Script.Save.Serialization;
using Script.Save.Serialization.Factory;
using Script.Utillties;
using Edge = UnityEditor.Experimental.GraphView.Edge;

namespace Editor.EditorToolExs
{
    public class EditorExTools : SingletonWithLazy<EditorExTools>
    {
        /// <summary>
        /// Updates the connection data between nodes in the behavior tree based on the provided edge.
        /// </summary>
        /// <param name="edge">The edge representing the connection between two nodes.</param>
        public void LinkLineAddData(Edge edge)
        {
            var inputBtNode = edge.input.node as BaseNodeView;
            if (edge.output.node is not BaseNodeView output_node) return;
            switch (output_node.NodeData)
            {
                case BtWeightSelector weightSelector:
                    if (inputBtNode != null) weightSelector.AddChildWithWeight(inputBtNode.NodeData, 0.1f);
                    break;
                case BtPrioritySelector prioritySelector:
                    if (inputBtNode != null) prioritySelector.AddChildWithPriority(inputBtNode.NodeData, 1);
                    break;
                case BtComposite composite:
                    if (inputBtNode != null) composite.ChildNodes.Add(inputBtNode.NodeData);
                    break;
                case BtPrecondition precondition:
                    if (inputBtNode != null) precondition.ChildNode = inputBtNode.NodeData;

                    break;
            }
        }

        /// <summary>
        /// Deletes the connection data between nodes in the behavior tree based on the provided edge.
        /// </summary>
        /// <param name="edge">The edge representing the connection to be removed between two nodes.</param>
        public void UnLinkLineDelete(Edge edge)
        {
            var inputBtNode = edge.input.node as BaseNodeView;
            if (edge.output.node is not BaseNodeView output_node) return;
            switch (output_node.NodeData)
            {
                case BtPrioritySelector prioritySelector:
                    if (inputBtNode != null) prioritySelector.RemoveNode(inputBtNode.NodeData);

                    break;
                case BtWeightSelector weightSelector:
                    if (inputBtNode != null) weightSelector.RemoveNode(inputBtNode.NodeData);

                    break;
                case BtComposite composite:
                    if (inputBtNode != null)
                    {
                        var edge_delete_log = composite.ChildNodes.Remove(inputBtNode.NodeData);
                        if (!edge_delete_log)
                            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(
                                new LogSpaceNode("Editor").AddChild("ExTool"),
                                new LogEntry(LogLevel.kWarning,
                                    $"{composite.NodeName} deletion failure child node: {inputBtNode.name}"));
                    }

                    break;
                case BtPrecondition precondition:
                    if (inputBtNode != null) precondition.ChildNode = null;

                    break;
            }
        }

        /// <summary>
        /// Clones the specified <c>BtNodeStyle</c> object by serializing and deserializing it.
        /// </summary>
        /// <param name="style">The <c>BtNodeStyle</c> instance to clone.</param>
        /// <returns>A deep copy of the provided <c>BtNodeStyle</c> object.</returns>
        public BtNodeStyle CloneBtNodeStyle(BtNodeStyle style)
        {
            if (style==null)
            {
                return null;
            }
            
            var serializer = SerializerCreator.Instance.Create(SerializerType.kJson, new SerializationSettings
            {
                IgnoreNullValues = true,
                TypeNameHandling = SerializationTypeNameHandling.kAuto,
                PreserveReferences = true
            });

            var data = serializer.Serialize(style);
            return serializer.Deserialize<BtNodeStyle>(data);
        }
    }
}