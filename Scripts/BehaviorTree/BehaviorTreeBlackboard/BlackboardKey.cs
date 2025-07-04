using System;
using UnityEngine;

namespace BehaviorTree.BehaviorTreeBlackboard
{
    /// <summary>
    /// Represents a key that uniquely identifies a value in a blackboard system.
    /// </summary>
    /// <typeparam name="T">The type of the value associated with this key.</typeparam>
    public abstract class BlackboardKey<T> : IBlackboardKey
    {
        public string Name { get; }
        [HideInInspector]
        public int Id { get; }
        public int Modifiers { get; }

        public BlackboardKey(string name, BlackboardKeyModifiers modifiers)
        {
            Name = name??throw new ArgumentNullException(nameof(name));
            Id = Guid.NewGuid().GetHashCode();
            Modifiers = (int)modifiers;
        }

        /// <summary>
        /// 私有数据，子黑板无法访问
        /// </summary>
        public bool IsPrivate=>(Modifiers & (int)BlackboardKeyModifiers.kPrivate)!=0;
        /// <summary>
        /// 只读数据，设置后不可修改
        /// </summary>
        public bool IsReadonly=>(Modifiers & (int)BlackboardKeyModifiers.kReadonly)!=0;
        /// <summary>
        /// 易变数据，变化时触发同步
        /// </summary>
        public bool IsVolatile=>(Modifiers & (int)BlackboardKeyModifiers.kVolatile)!=0;
        /// <summary>
        /// 本地缓存，不同步到远程
        /// </summary>
        public bool IsLocalCache=>(Modifiers & (int)BlackboardKeyModifiers.kLocalCache)!=0;
        /// <summary>
        /// 启用值比较
        /// </summary>
        public bool IsEnableEquals=>(Modifiers & (int)BlackboardKeyModifiers.kEnableEquals)!=0;

        /// <summary>
        /// Encapsulates a value into a BlackboardValue object based on the specific type,
        /// enabling storage within the behavior tree blackboard.
        /// </summary>
        /// <param name="value">The value of type T to be encapsulated into a BlackboardValue structure.</param>
        /// <returns>A BlackboardValue containing the encapsulated value of type T.</returns>
        public abstract BlackboardValue Box(T value);

        /// <summary>
        /// Extracts and returns a value of type T from the provided BlackboardValue object,
        /// based on the specific key implementation.
        /// </summary>
        /// <param name="value">The BlackboardValue instance containing the encapsulated data to be unboxed.</param>
        /// <returns>The unboxed value of type T extracted from the BlackboardValue.</returns>
        public abstract T Unbox(in BlackboardValue value);

        public bool Equals(IBlackboardKey other)
        {
            if (ReferenceEquals(this,other))
            {
                return true;
            }

            if (other is null)
            {
                return false;
            }

            return Name == other.Name && Id.Equals(other.Id);
        }

        public int CompareTo(IBlackboardKey other)
        {
            if (other==null)
            {
                return 1;
            }
            
            var name_comparison=string.Compare(Name,other.Name,StringComparison.Ordinal);
            return name_comparison!=0?name_comparison:Id.CompareTo(other.Id);
        }

        public override bool Equals(object obj)
        {
            return obj is IBlackboardKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, GetType());
        }
    }

    public sealed class IntBlackboardKey : BlackboardKey<int>
    {
        public IntBlackboardKey(string name, BlackboardKeyModifiers modifiers) : base(name, modifiers)
        {
        }

        public override BlackboardValue Box(int value)
        {
            return BlackboardValue.From(value);
        }

        public override int Unbox(in BlackboardValue value) => value.IntValue;
    }
    
    public sealed class FloatBlackboardKey : BlackboardKey<float>
    {
        public FloatBlackboardKey(string name, BlackboardKeyModifiers modifiers) : base(name, modifiers)
        {
        }
        
        public override BlackboardValue Box(float value) => BlackboardValue.From(value);

        public override float Unbox(in BlackboardValue value) => value.FloatValue;
    }

    public sealed class BoolBlackboardKey : BlackboardKey<bool>
    {
        public BoolBlackboardKey(string name, BlackboardKeyModifiers modifiers) : base(name, modifiers)
        {
        }

        public override BlackboardValue Box(bool value)=>BlackboardValue.From(value);

        public override bool Unbox(in BlackboardValue value)=>value.BoolValue;
    }

    public class DefaultBlackboardKey<T> : BlackboardKey<T>
    {
        private readonly Func<T, BlackboardValue> boxer_;
        private readonly Func<BlackboardValue,T> unboxer_;
        
        public DefaultBlackboardKey(string name, BlackboardKeyModifiers modifiers) : base(name, modifiers)
        {
            boxer_=CreateBoxer();
            unboxer_=CreateUnboxer();
        }

        public DefaultBlackboardKey(string name) : base(name, BlackboardKeyModifiers.kNone)
        {
            boxer_=CreateBoxer();
            unboxer_=CreateUnboxer();       
        }

        public override BlackboardValue Box(T value) => boxer_(value);

        public override T Unbox(in BlackboardValue value)=>unboxer_(value);

        private Func<T, BlackboardValue> CreateBoxer()
        {
            var type = typeof(T);

            if (type==typeof(int))
            {
                return (Func<T, BlackboardValue>)(object)new Func<int, BlackboardValue>(BlackboardValue.From);
            }
            if (type==typeof(long))
            {
                return (Func<T, BlackboardValue>)(object)new Func<long, BlackboardValue>(BlackboardValue.From);
            }if (type==typeof(float))
            {
                return (Func<T, BlackboardValue>)(object)new Func<float, BlackboardValue>(BlackboardValue.From);
            }if (type==typeof(double))
            {
                return (Func<T, BlackboardValue>)(object)new Func<double, BlackboardValue>(BlackboardValue.From);
            }if (type==typeof(bool))
            {
                return (Func<T, BlackboardValue>)(object)new Func<bool, BlackboardValue>(BlackboardValue.From);
            }

            if (type == typeof(string))
            {
                return (Func<T, BlackboardValue>)(object)new Func<string, BlackboardValue>(BlackboardValue.From);
            }
            return BlackboardValue.From;
        }
        
        private Func<BlackboardValue,T> CreateUnboxer()
        {
            return value => value.Get<T>();
        }

        public static implicit operator DefaultBlackboardKey<T>(string name) =>
            new DefaultBlackboardKey<T>(name, BlackboardKeyModifiers.kNone);

        public static DefaultBlackboardKey<T> Create(string name)
        {
            return new DefaultBlackboardKey<T>(name, BlackboardKeyModifiers.kNone);
        }

        public static DefaultBlackboardKey<T> Create(string name, BlackboardKeyModifiers modifiers)
        {
            return new DefaultBlackboardKey<T>(name, modifiers);
        }
    }

    public class Vector3BlackboardKey : BlackboardKey<Vector3>
    {
        public Vector3BlackboardKey(string name, BlackboardKeyModifiers modifiers) : base(name, modifiers)
        {
        }

        public override BlackboardValue Box(Vector3 value)=>BlackboardValue.From(value);

        public override Vector3 Unbox(in BlackboardValue value)=>value.Vector3Value;
    }
}
