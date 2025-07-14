using System;
using System.Collections.Generic;
using BehaviorTree.BehaviorTreeBlackboard;
using BehaviorTree.BehaviorTreeBlackboard.Core;
using BehaviorTree.Nodes.ChildNode;
using ExTools;
using ExTools.Utillties;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;


namespace BehaviorTree.Nodes
{
    /// <summary>
    /// 对象节点
    /// </summary>
    [Serializable]
    [NodeLabel("启用节点")]
    public class SetObjectActive : BtActionNode
    {
        [LabelText("是否启用")] [FoldoutGroup("@node_name_")]
        public bool is_active_;

        [LabelText("启用对象")] [FoldoutGroup("@node_name_")]
        public GameObject particle_;

        public override BehaviorState Tick(IBlackboardStorage blackboard)
        {
            particle_.SetActive(is_active_);
            return BehaviorState.kSucceed;
        }
    }

    /// <summary>
    /// 延时节点
    /// </summary>
    [NodeLabel("延时节点")]
    [Serializable]
    public class Delay : BtPrecondition
    {
        [LabelText("延迟时间")] 
        [FoldoutGroup("@node_name_")]
        [MinValue(0)]
        [Unit(Units.Second)]
        public float timer_;

        private float current_timer_;

        public override BehaviorState Tick(IBlackboardStorage blackboard)
        {
            current_timer_ += Time.deltaTime;
            if (current_timer_ >= timer_)
            {
                current_timer_ = 0.0f;
                ChildNode.Tick(blackboard);
                return BehaviorState.kSucceed;
            }

            return BehaviorState.kExecuting;
        }
    }

    [Serializable]
    public class ConditionalNode : BtPrecondition
    {
        [LabelText("是否活动")] [FoldoutGroup("@node_name_")]
        public bool IsActive;

        public override BehaviorState Tick(IBlackboardStorage blackboard)
        {
            if (IsActive)
            {
                ChildNode.Tick(blackboard);
                return BehaviorState.kSucceed;
            }

            return BehaviorState.kFailure;
        }
    }
    
    public abstract class BtSelector : BtComposite
    {
        protected int current_child_index;

        protected BtSelector(IChildNodeStorage storage) : base(storage)
        {
        }

        public override BehaviorState Tick(IBlackboardStorage blackboard)
        {
            if (ChildNodes.Count == 0) return BehaviorState.kFailure;

            if (NodeState != BehaviorState.kExecuting) current_child_index = 0;

            while (current_child_index < ChildNodes.Count)
            {
                var current_node = ChildNodes[current_child_index];
                var child_state = current_node.Tick(blackboard);

                if (child_state == BehaviorState.kSucceed)
                {
                    NodeState = BehaviorState.kSucceed;
                    return BehaviorState.kSucceed;
                }

                else if (child_state == BehaviorState.kExecuting)
                {
                    NodeState = BehaviorState.kExecuting;
                    return NodeState;
                }

                // 如果子节点失败尝试下一个节点
                current_child_index++;
            }

            // 所有子节点全部失败，则返回失败
            NodeState = BehaviorState.kFailure;
            return NodeState;
        }
    }

    [ShowInInspector]
    [NodeLabel("不带优先级选择节点")]
    [NodeFoldoutGroup("选择节点")]
    public class BtNonePrioritySelector : BtSelector
    {
        public BtNonePrioritySelector() : base(new BasicChildStorage())
        {
        }

        // 重置当前选择的子节点索引
        private int last_child_index = -1;

        public override BehaviorState Tick(IBlackboardStorage blackboard)
        {
            if (ChildNodes.Count == 0) return BehaviorState.kFailure;

            // 如果节点不在执行状态,重置当前选择的子节点
            if (NodeState != BehaviorState.kExecuting)
                last_child_index = 0; // 从第一个节点开始，或者使用你想要的初始值
        
            // 保存原始的child_nodes_.Count，避免在循环中可能的变化
            int childCount = ChildNodes.Count;
        
            // 确保索引有效
            if (last_child_index >= childCount || last_child_index < 0)
                last_child_index = 0;

            // 遍历子节点
            int startIndex = last_child_index;
            do
            {
                // 检查索引是否有效（可能在遍历过程中发生改变）
                if (last_child_index >= ChildNodes.Count)
                    break;
                
                var current_node = ChildNodes[last_child_index];
                var child_state = current_node.Tick(blackboard);

                if (child_state == BehaviorState.kSucceed)
                {
                    NodeState = BehaviorState.kSucceed;
                    return BehaviorState.kSucceed;
                }
                else if (child_state == BehaviorState.kExecuting)
                {
                    NodeState = BehaviorState.kExecuting;
                    return NodeState;
                }

                // 如果子节点失败，尝试下一个节点
                last_child_index = (last_child_index + 1) % ChildNodes.Count;
            
            } while (last_child_index != startIndex); // 确保不会无限循环

            // 所有子节点失败，则返回失败
            NodeState = BehaviorState.kFailure;
            return BehaviorState.kFailure;
        }
    }

    /// <summary>
    /// 带优先级的选择节点（Priority Selector）：这种选择节点每次都是自左向右依次选择，当发现找到一个可执行的子节点后就停止搜索后续子节点。
    /// 这样的选择方式，就存在一个优先级的问题，也就是说最左边的节点优先级最高，因为它是被最先判断的。对于这种选择节点来说，它的子节点的前提设定，
    /// 必须是“从窄到宽”的方式，否则后续节点都会发生“饿死”的情况，也就是说永远不会被执行到.
    /// </summary>
    [NodeLabel("带优先级选择节点")]
    [NodeFoldoutGroup("选择节点")]
    public class BtPrioritySelector : BtSelector
    {
        private PriorityChildStorage TypeStorage=>storage_ as PriorityChildStorage;

        public BtPrioritySelector() : base(new PriorityChildStorage()){}

        public List<NodePriority> GetChildren()
        {
            return TypeStorage.GetChildren();
        }

        public void AddChildWithPriority(BtNodeBase node, int priority)
        {
            if (node == null) return;
            
            TypeStorage.AddChild(new NodePriority{Node = node,Priority = priority});
        }

        public bool RemoveNode(BtNodeBase node)
        {
            return TypeStorage.RemoveChildNode(node);
        }

        public override BehaviorState Tick(IBlackboardStorage blackboard)
        {
            if (ChildNodes.Count == 0) return BehaviorState.kFailure;

            // 依次检查每个子节点，不再重新排序
            foreach (var child in ChildNodes)
            {
                var child_state = child.Tick(blackboard);

                if (child_state == BehaviorState.kSucceed)
                {
                    NodeState = BehaviorState.kSucceed;
                    return BehaviorState.kSucceed;
                }
                else if (child_state == BehaviorState.kExecuting)
                {
                    NodeState = BehaviorState.kExecuting;
                    return NodeState;
                }
            }

            // 所有子节点没有成功，则返回失败
            NodeState = BehaviorState.kFailure;
            return NodeState;
        }

        public void Clear()
        {
            TypeStorage.Clear();
        }
    }

    /// <summary>
    /// 带权值的选择节点（Weighted Selector）：对于这种选择节点，我们会预先为每一个分支标注一个“权值”（Weight Value），然后当我们选择的时候，
    /// 采用随机选择的方式来选，随机时会参考权值，并且保证已经被测试过的节点的不会再被测试，直到有一个节点的前提被满足，或者测试完所有的节点。
    /// 带权值的选择节点对于子节点前提由于随机的存在，所以子节点的前提可以任意，而不会发生“饿死”的情况，一般来说，我们通常会把所以子节点的前提
    /// 设为相同，以更好的表现出权值带来的概率上的效果。
    /// </summary>
    [NodeLabel("带权值选择节点")]
    [NodeFoldoutGroup("选择节点")]
    public class BtWeightSelector : BtSelector
    {
        public BtWeightSelector() : base(new WeightChildStorage())
        {
        }

        private WeightChildStorage TypedStorage => storage_ as WeightChildStorage;

        /// <summary>
        /// Retrieves a list of child nodes wrapped with their associated weights.
        /// </summary>
        /// <returns>A list of <c>NodeWeight</c> instances representing the child nodes and their corresponding weights.</returns>
        public List<NodeWeight> GetChildren()
        {
            return TypedStorage.GetChildren();
        }

        public void AddChildWithWeight(BtNodeBase node, float weight)
        {
            if (node == null || weight < 0)
                return;
            
            TypedStorage.AddChild(new NodeWeight(node, weight));
        }

        /// <summary>
        /// Removes a specified child node from the current node.
        /// </summary>
        /// <param name="node">The child node to remove.</param>
        /// <returns><c>true</c> if the node was successfully removed; otherwise, <c>false</c>.</returns>
        public bool RemoveNode(BtNodeBase node)
        {
            return TypedStorage.RemoveChildNode(node);
        }

        public override BehaviorState Tick(IBlackboardStorage blackboard)
        {
            if (ChildNodes.Count == 0 || TypedStorage.Count == 0) return BehaviorState.kFailure;

            if (NodeState != BehaviorState.kExecuting)
            {
                var node_weights = TypedStorage.GetChildren();
                // 每次tick开始时，重置当前子节点索引
                current_child_index = -1;

                float total_weight = 0;
                foreach (var nw in node_weights)
                    if (!nw.HasBeenTested) // 只计算未测试的节点
                        total_weight += nw.Weight;

                if (total_weight == 0)
                {
                    // 如果所有节点都已经被测试过，则重置它们的状态
                    foreach (var nw in node_weights) nw.HasBeenTested = false;

                    total_weight = 0;
                }

                var random_value = Random.Range(0, total_weight);
                float current_sum = 0;

                
                for (var i = 0; i < node_weights.Count; i++)
                {
                    var node_weight = node_weights[i];

                    if (node_weight.HasBeenTested) continue; // 跳过已经测试的节点

                    current_sum += node_weight.Weight;
                    if (random_value <= current_sum)
                    {
                        current_child_index = i;
                        break;
                    }
                }

                if (current_child_index != -1) node_weights[current_child_index].HasBeenTested = true; // 标记已测试完
            }

            var current_node = ChildNodes[current_child_index];
            NodeState = current_node.Tick(blackboard);
            return NodeState;
        }

        public void Clear()
        {
            TypedStorage.Clear();
        }
    }

    [NodeLabel("顺序节点")]
    public class BtSequence : BtComposite
    {
        private int current_child_index;

        public override BehaviorState Tick(IBlackboardStorage blackboard)
        {
            if (ChildNodes.Count == 0) return BehaviorState.kFailure;

            if (NodeState != BehaviorState.kExecuting) current_child_index = 0;

            while (current_child_index < ChildNodes.Count)
            {
                var current_node = ChildNodes[current_child_index];
                var child_state = current_node.Tick(blackboard);

                if (child_state == BehaviorState.kFailure)
                {
                    NodeState = BehaviorState.kFailure;
                    return NodeState;
                }

                else if (child_state == BehaviorState.kExecuting)
                {
                    NodeState = BehaviorState.kExecuting;
                    return NodeState;
                }

                current_child_index++;
            }

            NodeState = BehaviorState.kSucceed;
            return NodeState;
        }
    }

    [NodeLabel("并行节点")]
    public class BtParallel : BtComposite
    {
        [FoldoutGroup("并行运行条件")] [LabelText("成功所需数量")][MinValue(0)]
        public int success_threshold_ = 1;

        [FoldoutGroup("并行运行条件")] [LabelText("失败所需数量")][MinValue(0)]
        public int failure_threshold_ = 1;

        public override BehaviorState Tick(IBlackboardStorage blackboard)
        {
            var success_count = 0;
            var failure_count = 0;
            var executing_count = 0;

            foreach (var child in ChildNodes)
            {
                var child_state = child.Tick(blackboard);

                switch (child_state)
                {
                    case BehaviorState.kSucceed:
                        success_count++;
                        break;
                    case BehaviorState.kFailure:
                        failure_count++;
                        break;
                    case BehaviorState.kNonExecuting:
                        executing_count++;
                        break;
                }

                // 检查是否达到成功/失败的阈值
                if (success_count >= success_threshold_)
                {
                    NodeState = BehaviorState.kSucceed;
                    return NodeState;
                }

                if (failure_count >= failure_threshold_)
                {
                    NodeState = BehaviorState.kFailure;
                    return NodeState;
                }
            }

            // 如果有节点正在执行且未达到成功或失败阈值，则并行节点处于执行状态
            if (executing_count > 0)
            {
                NodeState = BehaviorState.kExecuting;
                return NodeState;
            }

            NodeState = BehaviorState.kFailure;
            return NodeState;
        }
    }

    [NodeLabel("程序总输入口")]
    public class BtMainNode : BtPrecondition
    {
        public override BehaviorState Tick(IBlackboardStorage blackboard)
        {
            return ChildNode.Tick(blackboard);
        }
    }
}