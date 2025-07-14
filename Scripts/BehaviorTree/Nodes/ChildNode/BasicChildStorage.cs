using System.Collections.Generic;
using Save.Serialization.Core.TypeConverter.SerializerAttribute;
using Sirenix.OdinInspector;

namespace BehaviorTree.Nodes.ChildNode
{
    public class BasicChildStorage : IChildNodeStorage<BtNodeBase>
    {
        [ShowInInspector]
        [PersistField]
        private List<BtNodeBase> child_nodes_ = new List<BtNodeBase>();
        
        public List<BtNodeBase> GetChildren()=>child_nodes_;

        public List<BtNodeBase> GetChildNodesAsBase()=>child_nodes_;

        public void AddChild(BtNodeBase node)=>child_nodes_.Add(node);
        public void InsertChildNode(BtNodeBase node, int index)=>child_nodes_.Insert(index,node);

        public bool RemoveChildNode(BtNodeBase node)=>child_nodes_.Remove(node);

        public void InsertChild(BtNodeBase node, int index)=>child_nodes_.Insert(index,node);

        public bool RemoveChild(BtNodeBase node)=>child_nodes_.Remove(node);

        public void Clear()=>child_nodes_.Clear();

        public int Count => child_nodes_.Count;

        public void PostCloneRelink(IReadOnlyCollection<BtNodeBase> all_clone_nodes)
        {
            var valid_nodes_set = new HashSet<BtNodeBase>(all_clone_nodes);
            
            // 从后往前遍历列表以安全地移除元素
            for (int i = child_nodes_.Count-1; i >=0; i--)
            {
                var child_node = child_nodes_[i];

                if (child_node!=null && !valid_nodes_set.Contains(child_node))
                {
                    child_nodes_.RemoveAt(i);
                }
            }
        }
    }
}
