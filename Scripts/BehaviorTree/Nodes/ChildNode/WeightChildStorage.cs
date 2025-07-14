using System;
using System.Collections.Generic;
using System.Linq;
using ExTools;
using ExTools.Utillties;
using Save.Serialization.Core.TypeConverter.SerializerAttribute;
using Sirenix.OdinInspector;

namespace BehaviorTree.Nodes.ChildNode
{
    /// <summary>
    /// Represents a node with an associated weight, typically used in behavior trees or similar structures.
    /// </summary>
    [CustomSerialize]
    [Serializable]
    public class NodeWeight
    {
        /// <summary>
        /// Represents a behavior tree node that serves as the primary unit of processing
        /// and decision-making within the tree structure.
        /// </summary>
        /// <remarks>
        /// A <c>Node</c> is a core component of behavior trees, often encapsulating logic
        /// or composites for hierarchical decision-making. It may be associated with specific
        /// behaviors, conditional checks, or other operational units within a tree-based
        /// system. Nodes can also serve as children or parents of other nodes, enabling
        /// complex behavior patterns.
        /// </remarks>
        [LabelText("子节点")] public BtNodeBase Node;

        /// <summary>
        /// Represents the weight assigned to a specific behavior tree node.
        /// This value is used to determine the likelihood of selecting this node
        /// during certain operations, such as weighted random selection.
        /// </summary>
        /// <remarks>
        /// The <c>Weight</c> value must be non-negative and usually works in conjunction
        /// with other nodes' weights to compute probabilities for node selection.
        /// </remarks>
        [MinValue(0)] [LabelText("节点权重")] public float Weight;

        /// <summary>
        /// Indicates whether the corresponding node has already been tested.
        /// </summary>
        /// <remarks>
        /// The <c>HasBeenTested</c> variable is a boolean flag that serves as a
        /// marker to track if a specific operation or validation has been performed
        /// on the current node. This helps prevent redundant processing and can be
        /// reset as needed during the node's lifecycle.
        /// </remarks>
        [LabelText("是否已经测试过")] [ReadOnly] public bool HasBeenTested;

        public NodeWeight(BtNodeBase node, float weight)
        {
            Node = node;
            Weight = weight;
            HasBeenTested = false;
        }
    }
    
    public class WeightChildStorage : IChildNodeStorage<NodeWeight>
    {
        /// <summary>
        /// Represents a collection of <c>NodeWeight</c> instances associated with child nodes
        /// in a behavior tree structure.
        /// </summary>
        /// <remarks>
        /// The <c>node_weights_</c> list is responsible for storing the weights assigned to child nodes
        /// in a behavior tree, aiding in the selection or prioritization of these child nodes during
        /// tree traversal. This variable encapsulates the behavior tree's logic for managing child node
        /// relationships and their respective weights.
        /// </remarks>
        [LabelText("节点权重列表"), PanelDelegatedProperty(PropertyPanelType.kChildNodes)]
        [ShowInInspector]
        [HideReferenceObjectPicker]
        [PersistField]
        private List<NodeWeight> node_weights_ = new List<NodeWeight>();
        
        public List<NodeWeight> GetChildren()
        {
            return node_weights_;
        }

        public void AddChild(NodeWeight node)
        {
            if (node.HasBeenTested)
            {
                node.HasBeenTested = false;
            }
            node_weights_.Add(node);
        }

        public void InsertChild(NodeWeight node, int index)
        {
            if (node.HasBeenTested)
            {
                node.HasBeenTested = false;
            }
            
            node_weights_.Insert(index,node);
        }

        public bool RemoveChild(NodeWeight node)
        {
            return node_weights_.Remove(node);
        }

        public List<BtNodeBase> GetChildNodesAsBase()=>node_weights_.Select(p=>p.Node).ToList();

        public void AddChild(BtNodeBase node)=>AddChild(new NodeWeight(node,0.1f));

        public void InsertChildNode(BtNodeBase node, int index)=>InsertChild(new NodeWeight(node,0.1f),index);

        public bool RemoveChildNode(BtNodeBase node)
        {
            var item_to_remove=node_weights_.FirstOrDefault(p=>p.Node==node);
            if (item_to_remove!=null)
            {
                return RemoveChild(item_to_remove);
            }

            return false;
        }

        public void Clear()
        {
            node_weights_.Clear();
        }

        public int Count=>node_weights_.Count;

        public void PostCloneRelink(IReadOnlyCollection<BtNodeBase> all_clone_nodes)
        {
            var valid_nodes_set = new HashSet<BtNodeBase>(all_clone_nodes);

            for (int i = node_weights_.Count - 1; i >= 0; i--)
            {
                var weight_node = node_weights_[i];

                if (weight_node.Node!=null && !valid_nodes_set.Contains(weight_node.Node))
                {
                    node_weights_.RemoveAt(i);
                }
            }
        }
    }
}
