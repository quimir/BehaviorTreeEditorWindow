using System.Collections.Generic;
using System.Linq;
using BehaviorTree.BehaviorTreeBlackboard;
using BehaviorTree.BehaviorTreeBlackboard.Core;
using BehaviorTree.Nodes.ChildNode;
using ExTools;
using ExTools.Utillties;
using Save.Serialization.Core.TypeConverter.SerializerAttribute;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BehaviorTree.Nodes
{
    /// <summary>
    /// 行为树节点的基础类，用于定义行为树节点的基本属性和通用操作接口。
    /// </summary>
    [BoxGroup]
    [HideReferenceObjectPicker]
    public abstract class BtNodeBase
    {
        /// <summary>
        /// 节点的唯一标识符
        /// </summary>
        [FoldoutGroup("基本属性"), LabelText("唯一标识"),HideInInspector]
        public string Guild;
        
        /// <summary>
        /// 节点在视图当中的位置
        /// </summary>
        [FoldoutGroup("基础数据")]
        [LabelText("节点位置")]
        public Vector2 Position;

        [FoldoutGroup("基本属性")] 
        [LabelText("节点大小")]
        public Vector2 Size;
        
        /// <summary>
        /// 节点的名称
        /// </summary>
        [FoldoutGroup("基础数据")]
        [LabelText("名称")]
        public string NodeName;

        /// <summary>
        /// 节点的状态
        /// </summary>
        [FoldoutGroup("基础数据")]
        [LabelText("状态")]
        [ReadOnly]
        public BehaviorState NodeState;

        /// <summary>
        /// 行为树的核心更新函数。
        /// </summary>
        /// <param name="blackboard">由上层传递下来的黑板上下文。</param>
        /// <returns>节点的执行状态。</returns>
        public abstract BehaviorState Tick(IBlackboardStorage blackboard);
    }

    /// <summary>
    /// Represents a composite node in a behavior tree, which can hold and manage multiple child nodes.
    /// Composite nodes are responsible for controlling the flow of execution among their child nodes.
    /// </summary>
    public abstract class BtComposite : BtNodeBase
    {
        /// <summary>
        /// Represents the storage mechanism for managing child nodes within a composite behavior tree node.
        /// Encapsulates functionalities for adding, removing, and retrieving child nodes.
        /// </summary>
        [SerializeReference, HideReferenceObjectPicker]
        [PanelDelegatedProperty(PropertyPanelType.kChildNodes)]
        [ShowInInspector]
        [LabelText("子节点")]
        [PersistField(Required = true)]
        protected IChildNodeStorage storage_;

        /// <summary>
        /// Represents a composite node in a behavior tree, which can contain and manage multiple child nodes.
        /// Composite nodes are responsible for organizing and controlling the flow of execution among their child nodes.
        /// </summary>
        protected BtComposite(IChildNodeStorage storage)
        {
            storage_ = storage;
        }

        protected BtComposite()
        {
            storage_ = new BasicChildStorage();
        }

        /// <summary>
        /// Adds a child node to the composite node.
        /// </summary>
        /// <param name="node">The child node to be added.</param>
        public virtual void AddChild(BtNodeBase node)
        {
            storage_.AddChild(node);
        }

        /// <summary>
        /// Removes a specified child node from the composite node's storage.
        /// </summary>
        /// <param name="node">The child node to be removed from the storage.</param>
        /// <returns>True if the child node was successfully removed; otherwise, false.</returns>
        public virtual bool RemoveChildNode(BtNodeBase node)
        {
            return storage_?.RemoveChildNode(node) ?? false;
        }

        /// <summary>
        /// Relink the cloned nodes within the behavior tree composite structure.
        /// Ensures the internal and child node references are updated correctly during cloning operations.
        /// </summary>
        /// <param name="allClonedNodes">A collection of all nodes that have been cloned and may require relinking.</param>
        public virtual void PostCloneRelink(IReadOnlyCollection<BtNodeBase> allClonedNodes)
        {
            storage_.PostCloneRelink(allClonedNodes);
        }

        /// <summary>
        /// Represents a list of child nodes associated with a behavior tree composite node.
        /// Used to define and manage hierarchical relationships between nodes in a behavior tree structure.
        /// </summary>
        [NonSerialize]
        //[JsonIgnore]
        public List<BtNodeBase> ChildNodes => storage_?.GetChildNodesAsBase() ?? new List<BtNodeBase>();
    }
    
    /// <summary>
    /// 行为树中的先决条件节点，用于判断某些前置条件是否满足，以便决定行为树的执行逻辑。
    /// </summary>
    public abstract class BtPrecondition : BtNodeBase
    {
        /// <summary>
        /// Represents a child node of the current behavior tree node.
        /// It is used to establish hierarchical relationships within the behavior tree structure.
        /// </summary>
        [FoldoutGroup("@node_name_")]
        [LabelText("子节点")]
        [PanelDelegatedProperty(PropertyPanelType.kChildNodes)]
        [ShowInInspector]
        [HideReferenceObjectPicker]
        public BtNodeBase ChildNode;

        /// <summary>
        /// Updates references for all cloned nodes after a deep copy operation to ensure the correct linkage.
        /// </summary>
        /// <param name="allClonedNodes">A read-only collection containing all nodes that were cloned during the deep
        /// copy process.</param>
        public virtual void PostCloneRelink(IReadOnlyCollection<BtNodeBase> allClonedNodes)
        {
            if (ChildNode!=null && !allClonedNodes.Contains(ChildNode))
            {
                ChildNode = null;
            }
        }
    }

    /// <summary>
    /// 行为树的动作节点，用于执行具体的操作或动作。
    /// 作为所有动作类型节点的基类，定义了动作节点的基本类型和行为。
    /// </summary>
    public abstract class BtActionNode : BtNodeBase
    {
    }
}