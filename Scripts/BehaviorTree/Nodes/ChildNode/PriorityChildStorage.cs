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
    /// Represents a behavior tree node with an associated priority level.
    /// This is used to prioritize the execution order of child nodes in a behavior tree.
    /// </summary>
    [CustomSerialize]
    [Serializable]
    public class NodePriority
    {
        /// <summary>
        /// Represents a behavior tree node from the base type <see cref="BtNodeBase"/>.
        /// </summary>
        [LabelText("子节点")] public BtNodeBase Node;

        /// <summary>
        /// Represents the priority level of a behavior tree child node, used for prioritizing execution order.
        /// </summary>
        [MinValue(0)] [LabelText("节点优先级")]
        public float Priority;
    }
    
    public class PriorityChildStorage : IChildNodeStorage<NodePriority>
    {
        [ShowInInspector]
        [FoldoutGroup("节点优先级配置")]
        [LabelText("优先级列表"),PanelDelegatedProperty(PropertyPanelType.kChildNodes)]
        [HideReferenceObjectPicker]
        [PersistField]
        private List<NodePriority> node_priorities_ = new List<NodePriority>();
        
        public List<NodePriority> GetChildren()
        {
            SortNodePriorities();
            return node_priorities_;
        }

        public void AddChild(NodePriority node)
        {
            node_priorities_.Add(node);
            SortNodePriorities();
        }

        public void InsertChild(NodePriority node, int index)
        {
            node_priorities_.Insert(index,node);
        }

        public bool RemoveChild(NodePriority node)
        {
            return node_priorities_.Remove(node);
        }

        public List<BtNodeBase> GetChildNodesAsBase()=>node_priorities_.Select(p=>p.Node).ToList();

        public void AddChild(BtNodeBase node)=>node_priorities_.Add(new NodePriority { Node = node, Priority = 1 });

        public void InsertChildNode(BtNodeBase node, int index)=>node_priorities_.Insert(index,new NodePriority 
            { Node = node, Priority = 1 });

        public bool RemoveChildNode(BtNodeBase node)
        {
            var item_to_remove=node_priorities_.FirstOrDefault(p=>p.Node==node);
            if (item_to_remove != null)
                return RemoveChild(item_to_remove);
            return false;
        }

        public void Clear()
        {
            node_priorities_.Clear();
        }

        public int Count=>node_priorities_.Count;

        public void PostCloneRelink(IReadOnlyCollection<BtNodeBase> all_clone_nodes)
        {
            // 高效做法：将列表转换为 HashSet 以进行 O(1) 复杂度的快速查找。
            var validNodesSet = new HashSet<BtNodeBase>(all_clone_nodes);

            // 从后往前遍历列表以安全地移除元素
            for (int i = node_priorities_.Count - 1; i >= 0; i--)
            {
                var priorityNode = node_priorities_[i];
            
                // 如果当前子节点不在本次克隆的集合中，说明它是一个无效的引用（可能指向原始树），移除它。
                if (priorityNode.Node != null && !validNodesSet.Contains(priorityNode.Node))
                {
                    node_priorities_.RemoveAt(i);
                }
            }
        }

        private void SortNodePriorities()
        {
            node_priorities_.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }
    }
}
