using System;

namespace BehaviorTree.BehaviorTreeBlackboard
{
    /// <summary>
    /// Specifies modifiers for blackboard keys in a behavior tree system,
    /// enabling customization of key behavior and characteristics.
    /// </summary>
    [Flags]
    public enum BlackboardKeyModifiers
    {
        /// <summary>
        /// 默认选项，不进行任何操作
        /// </summary>
        kNone=0,
        /// <summary>
        /// 私有数据，不允许除了本黑板之外的其他黑板访问
        /// </summary>
        kPrivate=1,
        /// <summary>
        /// 只读数据，设置后不可修改
        /// </summary>
        kReadonly=2,
        /// <summary>
        /// 易变数据，变化时触发同步
        /// </summary>
        kVolatile=4,
        /// <summary>
        /// 本地缓存，不同步到远程
        /// </summary>
        kLocalCache=8,
        /// <summary>
        /// 启用值比较
        /// </summary>
        kEnableEquals=16
    }

    /// <summary>
    /// Represents a unique key interface for use within a blackboard system
    /// in a behavior tree architecture, enabling identification, storage,
    /// and retrieval of values associated with specific keys.
    /// </summary>
    public interface IBlackboardKey :IEquatable<IBlackboardKey>,IComparable<IBlackboardKey>
    {
        /// <summary>
        /// Gets the name associated with the blackboard key.
        /// Represents a unique string identifier for this key within the blackboard system.
        /// Used for comparisons and organization of blackboard entries.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the unique identifier associated with the blackboard key.
        /// This identifier is an integer, typically generated to ensure distinctness among keys.
        /// Used to differentiate keys even if they share the same name within the blackboard system.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Gets the modifiers associated with the blackboard key.
        /// Represents a bitwise combination of flags defining the behavior or attributes of the key.
        /// These modifiers can include attributes such as private, readonly, volatile, local cache, or enable equals.
        /// Used to configure and enforce rules for the key's behavior and access within the blackboard system.
        /// </summary>
        public int Modifiers { get; }
    }
}