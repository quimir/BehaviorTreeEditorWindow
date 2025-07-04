using BehaviorTree.BehaviorTreeBlackboard;
using UnityEngine;

namespace BehaviorTree.Nodes.Monster
{
    public sealed class MonsterGameObjectKey : DefaultBlackboardKey<GameObject>
    {
        public MonsterGameObjectKey(string name, BlackboardKeyModifiers modifiers) : base(name, modifiers)
        {
        }

        public MonsterGameObjectKey(string name) : base(name)
        {
        }
    }
    
    public sealed class MonsterTransformKey:DefaultBlackboardKey<Transform>
    {
        public MonsterTransformKey(string name, BlackboardKeyModifiers modifiers) : base(name, modifiers)
        {
        }

        public MonsterTransformKey(string name) : base(name)
        {
        }
    }

    public sealed class MonsterMovementSpeedKey : DefaultBlackboardKey<float>
    {
        public MonsterMovementSpeedKey(string name, BlackboardKeyModifiers modifiers) : base(name, modifiers)
        {
        }

        public MonsterMovementSpeedKey(string name) : base(name)
        {
        }
    }
    
    public sealed class TargetGameObjectKey:DefaultBlackboardKey<GameObject>
    {
        public TargetGameObjectKey(string name, BlackboardKeyModifiers modifiers) : base(name, modifiers)
        {
        }
    }

    public sealed class TargetTransformKey : DefaultBlackboardKey<Transform>
    {
        public TargetTransformKey(string name, BlackboardKeyModifiers modifiers) : base(name, modifiers)
        {
        }
    }

    public sealed class HasTargetKey : DefaultBlackboardKey<bool>
    {
        public HasTargetKey(string name, BlackboardKeyModifiers modifiers) : base(name, modifiers)
        {
        }
    }

    public sealed class DistanceToTargetKey : DefaultBlackboardKey<float>
    {
        public DistanceToTargetKey(string name, BlackboardKeyModifiers modifiers) : base(name, modifiers)
        {
        }
    }

    public sealed class AttackRangeKey : DefaultBlackboardKey<float>
    {
        public AttackRangeKey(string name, BlackboardKeyModifiers modifiers) : base(name, modifiers)
        {
        }

        public AttackRangeKey(string name) : base(name)
        {
        }
    }

    public sealed class AttackDamageKey : DefaultBlackboardKey<float>
    {
        public AttackDamageKey(string name, BlackboardKeyModifiers modifiers) : base(name, modifiers){}
    }
    
    public sealed class AnimatorKey:DefaultBlackboardKey<Animator>
    {
        public AnimatorKey(string name, BlackboardKeyModifiers modifiers) : base(name, modifiers){}
    }
    
    public sealed class PatrolPointsKey:DefaultBlackboardKey<Vector3[]>
    {
        public PatrolPointsKey(string name, BlackboardKeyModifiers modifiers) : base(name, modifiers){}
    }
    
    public sealed class CurrentPatrolPointIndexKey:DefaultBlackboardKey<int>
    {
        public CurrentPatrolPointIndexKey(string name, BlackboardKeyModifiers modifiers) : base(name, modifiers){}
    }
}
