using System;
using BehaviorTree.BehaviorTreeBlackboard;
using BehaviorTree.Nodes.Monster;
using ExTools;
using ExTools.Utillties;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

namespace BehaviorTree.Nodes
{
    [NodeLabel("是否黑板中存在目标")]
    public class CheckHasTarget : BtPrecondition
    {
        [LabelText("目标对象黑板键")]
        public DefaultBlackboardKey<GameObject> TargetKey = new("TargetGameObject", BlackboardKeyModifiers.kNone);

        public override BehaviorState Tick(IBlackboardStorage blackboard)
        {
            if (blackboard.ContainsKey(TargetKey))
                if (blackboard.TryGetValue(TargetKey, out var target) && target != null)
                {
                    NodeState = BehaviorState.kSucceed;
                    return NodeState;
                }

            NodeState = BehaviorState.kFailure;
            return NodeState;
        }
    }

    [NodeLabel("检查与目标之间的距离是否满足条件")]
    public class CheckDistanceToTarget : BtPrecondition
    {
        [LabelText("怪物位置黑板键")] public DefaultBlackboardKey<Transform> MonsterTransformKey =
            new("MonsterTransform", BlackboardKeyModifiers.kNone);

        [LabelText("目标位置黑板键")]
        public DefaultBlackboardKey<Transform>
            TargetTransformKey = new("TargetTransform", BlackboardKeyModifiers.kNone);

        [LabelText("所需最小距离")] public float MinDistance = 0f;
        [LabelText("所需最大距离")] public float MaxDistance = 0f;
        [LabelText("是否将距离写入黑板")] public bool WriteDistanceToBlackboard = false;

        [ShowIf("WriteDistanceToBlackboard")] [LabelText("距离黑板键")]
        public FloatBlackboardKey DistanceKey = new("DistanceToTarget", BlackboardKeyModifiers.kNone);

        public override BehaviorState Tick(IBlackboardStorage blackboard)
        {
            if (!blackboard.TryGetValue(MonsterTransformKey, out var monster_transform) ||
                monster_transform == null)
            {
                NodeState = BehaviorState.kFailure;
                return NodeState;
            }

            if (!blackboard.TryGetValue(TargetTransformKey, out var target_transform) || target_transform == null)
            {
                NodeState = BehaviorState.kFailure;
                return NodeState;
            }

            var distance = Vector3.Distance(monster_transform.position, target_transform.position);

            if (WriteDistanceToBlackboard) blackboard.SetValue(DistanceKey, distance);

            if (distance >= MinDistance && distance <= MaxDistance)
            {
                NodeState = BehaviorState.kSucceed;
                return NodeState;
            }

            NodeState = BehaviorState.kFailure;
            return NodeState;
        }
    }

    [NodeLabel("怪物寻找目标的行动节点")]
    public class MonsterFindTarget : BtActionNode
    {
        public DefaultBlackboardKey<GameObject> MonsterKey = new("MonsterGameObject", BlackboardKeyModifiers.kNone);
        public DefaultBlackboardKey<GameObject> TargetKey = new("TargetGameObject", BlackboardKeyModifiers.kNone);

        public DefaultBlackboardKey<Transform>
            TargetTransformKey = new("TargetTransform", BlackboardKeyModifiers.kNone);

        public BoolBlackboardKey HasTargetKey = new("HasTarget", BlackboardKeyModifiers.kNone);

        [LabelText("寻敌范围")] public float SearchRadius = 20f;
        [LabelText("目标标签")] public string TargetTag = "Player";

        public override BehaviorState Tick(IBlackboardStorage blackboard)
        {
            if (!blackboard.TryGetValue(MonsterKey, out var monster_object) || monster_object == null)
            {
                NodeState = BehaviorState.kFailure;
                return NodeState;
            }

            // 寻找最近的具有目标标签的GameObject
            var potential_targets = GameObject.FindGameObjectsWithTag(TargetTag);
            GameObject closest_target = null;
            var min_distance_sq = Mathf.Pow(SearchRadius, 2);

            foreach (var target in potential_targets)
                // 确保怪物不是自己
                if (target != monster_object)
                {
                    float distance_sq = (target.transform.position - monster_object.transform.position).sqrMagnitude;
                    if (distance_sq<min_distance_sq)
                    {
                        min_distance_sq=distance_sq;
                        closest_target = target;
                    }
                }

            if (closest_target!=null)
            {
                blackboard.SetValue(TargetKey,closest_target);
                blackboard.SetValue(TargetTransformKey,closest_target.transform);
                blackboard.SetValue(HasTargetKey,true);
                NodeState = BehaviorState.kSucceed;
            }
            else
            {
                blackboard.SetValue(TargetKey,null);
                blackboard.SetValue(TargetTransformKey,null);
                blackboard.SetValue(HasTargetKey,false);
                NodeState = BehaviorState.kFailure;
            }

            return NodeState;
        }
    }

    [LabelText("怪物移动到目标的行动节点")]
    public class MonsterMoveToTarget : BtActionNode
    {
        [LabelText("怪物Transform黑板键")]
        private MonsterTransformKey MonsterTransformBlackboard=new MonsterTransformKey("MonsterTransform", BlackboardKeyModifiers.kNone);
        [LabelText("目标Transform黑板键")]
        private TargetTransformKey TargetTransformBlackboard=new TargetTransformKey("TargetTransform", BlackboardKeyModifiers.kNone);
        [LabelText("移动速度黑板键")]
        private MonsterMovementSpeedKey MovementSpeedBlackboard=new("MonsterMovementSpeed", BlackboardKeyModifiers.kNone);
        [LabelText("NavMeshAgent黑板键")]
        private DefaultBlackboardKey<NavMeshAgent> NavMeshAgentKey=new("MonsterNavMeshAgent", BlackboardKeyModifiers.kNone);
        public override BehaviorState Tick(IBlackboardStorage blackboard)
        {
            if (!blackboard.TryGetValue(MonsterTransformBlackboard,out var monster_transform) || monster_transform==null)
            {
                NodeState = BehaviorState.kFailure;
                return NodeState;
            }

            if (!blackboard.TryGetValue(TargetTransformBlackboard,out var target_transform)|| target_transform==null)
            {
                NodeState=BehaviorState.kFailure;
                return NodeState;           
            }

            if (!blackboard.TryGetValue(MovementSpeedBlackboard,out float move_speed))
            {
                move_speed = 5f;// 默认速度
            }
            
            // 使用NavMeshAgent进行移动
            if (blackboard.TryGetValue(NavMeshAgentKey,out var nav_mesh_agent) && nav_mesh_agent!=null)
            {
                nav_mesh_agent.speed = move_speed;
                nav_mesh_agent.SetDestination(target_transform.position);
                
                // 判断是否到达目标
                if (!nav_mesh_agent.pathPending && nav_mesh_agent.remainingDistance<=nav_mesh_agent.stoppingDistance)
                {
                    NodeState = BehaviorState.kSucceed;
                }
                else if (nav_mesh_agent.hasPath && nav_mesh_agent.velocity.sqrMagnitude>0.01f)
                {
                    NodeState = BehaviorState.kExecuting;
                }
                else
                {
                    NodeState = BehaviorState.kExecuting;
                }
            }
            else
            {
                Vector3 direction = (target_transform.position - monster_transform.position).normalized;
                monster_transform.position = Vector3.MoveTowards(monster_transform.position, target_transform.position,
                    move_speed * Time.deltaTime);
                
                // 接近目标
                if (Vector3.Distance(monster_transform.position,target_transform.position)<0.1f)
                {
                    NodeState = BehaviorState.kSucceed;
                }
                else
                {
                    NodeState = BehaviorState.kExecuting;
                }
            }

            return NodeState;
        }
    }
    
    [NodeLabel("怪物攻击目标的行为树节点")]
    public class MonsterAttackTarget : BtActionNode
    {
        [LabelText("怪物Transform黑板键")]
        public MonsterTransformKey MonsterTransformKey=new MonsterTransformKey("MonsterTransform", BlackboardKeyModifiers.kNone);
        [LabelText("目标黑板Transform黑板键")]
        public TargetTransformKey TargetTransformKey=new TargetTransformKey("TargetTransform", BlackboardKeyModifiers.kNone);

        [LabelText("攻击范围黑板键")] public AttackRangeKey AttackRangeKey = new AttackRangeKey("AttackRange");
        [LabelText("攻击力黑板键")] public AttackDamageKey AttackDamageKey = new AttackDamageKey("AttackDamage",BlackboardKeyModifiers.kNone);
        [LabelText("动画控制器黑板键")]public AnimatorKey AnimatorKey=new AnimatorKey("Animator",BlackboardKeyModifiers.kNone);
        [LabelText("攻击动画触发器名称")] public string AttackAnimationTrigger = "Attack";
        
        public override BehaviorState Tick(IBlackboardStorage blackboard)
        {
            if (!blackboard.TryGetValue(MonsterTransformKey,out var monster_transform)|| monster_transform==null)
            {
                NodeState = BehaviorState.kFailure;
                return NodeState;
            }

            if (!blackboard.TryGetValue(TargetTransformKey,out var target_transform)|| target_transform==null)
            {
                NodeState = BehaviorState.kFailure;
                return NodeState;
            }

            if (!blackboard.TryGetValue(AttackRangeKey,out float attack_range))
            {
                attack_range = 2.0f;// 默认攻击范围
            }

            if (!blackboard.TryGetValue(AttackDamageKey,out float attack_damage))
            {
                attack_damage = 10.0f;// 默认攻击力
            }

            float distance = Vector3.Distance(monster_transform.position, target_transform.position);

            if (distance<=attack_range)
            {
                // 播放攻击动画
                if (blackboard.TryGetValue(AnimatorKey,out var animator)&&animator!=null)
                {
                    animator.SetTrigger(AttackAnimationTrigger);
                }

                NodeState = BehaviorState.kSucceed;
            }
            else
            {
                NodeState = BehaviorState.kFailure;
            }

            return NodeState;
        }
    }

    [NodeLabel("怪物巡逻行动的节点")]
    public class MonsterPatrol : BtActionNode
    {
        [LabelText("怪物Transform黑板键")]
        public MonsterTransformKey MonsterTransformKey=new MonsterTransformKey("MonsterTransform", BlackboardKeyModifiers.kNone);
        [LabelText("巡逻点黑板键")]
        public PatrolPointsKey PatrolPointsKey=new PatrolPointsKey("PatrolPoints", BlackboardKeyModifiers.kNone);
        [LabelText("当前巡逻点索引黑板键")]
        public CurrentPatrolPointIndexKey CurrentPatrolPointIndexKey=new CurrentPatrolPointIndexKey("CurrentPatrolPointIndex", BlackboardKeyModifiers.kNone);
        [LabelText("移动速度黑板键")]
        public MonsterMovementSpeedKey MovementSpeedKey=new("MonsterMovementSpeed", BlackboardKeyModifiers.kNone);

        [LabelText("到达点容忍距离")] public float ArrivalTolerance = 0.5f;
        public override BehaviorState Tick(IBlackboardStorage blackboard)
        {
            if (!blackboard.TryGetValue(MonsterTransformKey,out var monster_transform)|| monster_transform==null)
            {
                NodeState = BehaviorState.kFailure;
                return NodeState;
            }

            if (!blackboard.TryGetValue(PatrolPointsKey,out var patrol_points)|| patrol_points==null)
            {
                NodeState = BehaviorState.kFailure;
                return NodeState;
            }

            if (!blackboard.TryGetValue(CurrentPatrolPointIndexKey,out var current_index))
            {
                current_index = 0;// 默认从第一个巡逻点开始
                blackboard.SetValue(CurrentPatrolPointIndexKey,current_index);
            }

            if (!blackboard.TryGetValue(MovementSpeedKey,out var move_speed))
            {
                move_speed = 3f;// 默认速度
            }

            Vector3 target_point = patrol_points[current_index];
            
            // 移动到目标点
            monster_transform.position=Vector3.MoveTowards(monster_transform.position,target_point,move_speed*Time.deltaTime);
            
            // 如果到达目标点
            if (Vector3.Distance(monster_transform.position,target_point)<ArrivalTolerance)
            {
                current_index = (current_index + 1) % patrol_points.Length;// 移动到下一个点
                blackboard.SetValue(CurrentPatrolPointIndexKey,current_index);
                NodeState = BehaviorState.kSucceed;
            }
            else
            {
                NodeState = BehaviorState.kExecuting;
            }

            return NodeState;
        }
    }
}