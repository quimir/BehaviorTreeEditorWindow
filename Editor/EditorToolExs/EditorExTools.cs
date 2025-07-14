using BehaviorTree.Nodes;
using Editor.View.BTWindows.BtTreeView.NodeView;
using Editor.View.BtWindows.BtTreeView.NodeView.Core;
using ExTools.Singleton;
using ExTools.Utillties;
using Save.Serialization.Core.TypeConverter;
using Save.Serialization.Factory;
using Script.BehaviorTree.Save;
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
                case BtComposite composite:
                    if (inputBtNode != null) composite.AddChild(inputBtNode.NodeData);
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
                case BtComposite composite:
                    if (inputBtNode!=null)
                    {
                        composite.RemoveChildNode(inputBtNode.NodeData);
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