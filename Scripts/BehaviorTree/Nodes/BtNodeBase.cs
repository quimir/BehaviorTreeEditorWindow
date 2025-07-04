using System.Collections.Generic;
using BehaviorTree.BehaviorTreeBlackboard;
using ExTools;
using ExTools.Utillties;
using Script.Utillties;
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
        /// Represents a list of child nodes associated with a behavior tree composite node.
        /// Used to define and manage hierarchical relationships between nodes in a behavior tree structure.
        /// </summary>
        [FoldoutGroup("@node_name_")]
        [LabelText("子节点")]
        [ShowInInspector,ShowIf("ShowChildNodes")]
        [PanelDelegatedProperty(PropertyPanelType.kChildNodes)]
        [HideReferenceObjectPicker]
        public List<BtNodeBase> ChildNodes = new List<BtNodeBase>();

        /// <summary>
        /// 是否需要在UI面板显示子节点(如果你有自己管理的子节点就将其设置为false，如果是用BtComposite管理的子节点那么推荐为true，
        /// 但是推荐只使用一个子节点作为面板显示)
        /// </summary>
        protected abstract bool ShowChildNodes { get; }
    }
    
    /// <summary>
    /// 行为树中的先决条件节点，用于判断某些前置条件是否满足，以便决定行为树的执行逻辑。
    /// </summary>
    public abstract class BtPrecondition : BtNodeBase
    {
        /// <summary>
        /// 子节点
        /// </summary>
        [FoldoutGroup("@node_name_")]
        [LabelText("子节点")]
        [PanelDelegatedProperty(PropertyPanelType.kChildNodes)]
        [ShowInInspector]
        [HideReferenceObjectPicker]
        public BtNodeBase ChildNode;
    }

    /// <summary>
    /// 行为树的动作节点，用于执行具体的操作或动作。
    /// 作为所有动作类型节点的基类，定义了动作节点的基本类型和行为。
    /// </summary>
    public abstract class BtActionNode : BtNodeBase
    {
    }
}