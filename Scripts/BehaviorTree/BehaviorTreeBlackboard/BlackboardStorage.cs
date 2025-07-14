using System;
using System.Collections.Generic;
using BehaviorTree.BehaviorTreeBlackboard.Core;
using Save.CustomSerialize;
using Save.Serialization.Core.TypeConverter.SerializerAttribute;
using UnityEngine;

namespace BehaviorTree.BehaviorTreeBlackboard
{
    /// <summary>
    /// Represents a storage system for blackboard keys and values used in behavior trees.
    /// </summary>
    [Serializable]
    public class BlackboardStorage : IBlackboardStorage
    {
        [PersistField]
        private readonly CustomSerializableDictionary<IBlackboardKey,BlackboardValue> storage_ = new();
        
        public bool TryGetValue<T>(BlackboardKey<T> key, out T value)
        {
            if (storage_.TryGetValue(key,out var union_value))
            {
                value = key.Unbox(union_value);
                return true;
            }

            value = default(T);
            return false;
        }
        
        public void SetValue<T>(BlackboardKey<T> key, T value)
        {
            if (key.IsReadonly && storage_.ContainsKey(key))
            {
                throw new InvalidOperationException($"Key '{key.Name}' is readonly and has already been set.");
            }

            var new_value = key.Box(value);

            storage_.TryGetValue(key, out var old_value);

            storage_.TryAdd(key, new_value);

            if (key.IsVolatile)
            {
                bool has_changed = true;
                if (key.IsEnableEquals)
                {
                    has_changed = !new_value.Equals(old_value);
                }

                if (has_changed)
                {
                    OnValueChanged?.Invoke(key,old_value,new_value);
                }
            }
        }

        public bool RemoveValue<T>(BlackboardKey<T> key)
        {
            if (key.IsReadonly && storage_.ContainsKey(key))
            {
                return false;
            }

            return storage_.Remove(key);
        }

        public bool ContainsKey<T>(BlackboardKey<T> key)=>storage_.ContainsKey(key);

        public IEnumerable<IBlackboardKey> GetAllKeys()=>storage_.Keys;
        
        public event Action<IBlackboardKey,BlackboardValue,BlackboardValue> OnValueChanged; 
        
        public int Count=>storage_.Count;

        /// <summary>
        /// Retrieves a read-only view of the storage containing the blackboard keys and their associated values.
        /// </summary>
        /// <returns>A read-only dictionary with keys of type <see cref="IBlackboardKey"/> and values of type
        /// <see cref="BlackboardValue"/>.</returns>
        public IReadOnlyDictionary<IBlackboardKey, BlackboardValue> GetStorageView() => storage_;
    }
}
