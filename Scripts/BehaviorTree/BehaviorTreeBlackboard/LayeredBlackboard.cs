using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;

namespace BehaviorTree.BehaviorTreeBlackboard
{
    /// <summary>
    /// Represents a layered blackboard storage implementation for behavior trees, combining both private
    /// and shared storage. A layered blackboard enables hierarchical organization of keys and values,
    /// allowing for separation between private and shared data while optionally synchronizing shared data.
    /// </summary>
    [HideReferenceObjectPicker]
    public class LayeredBlackboard : IBlackboardStorage
    {
        private readonly IBlackboardStorage shared_storage_;
        private readonly IBlackboardStorage private_storage_;
        
        public BlackboardStorage PrivateStorage=>private_storage_ as BlackboardStorage;

        /// <summary>
        /// Represents a layered blackboard storage that combines a shared storage with a private storage.
        /// This allows for separation of shared data and instance-specific data within the blackboard system.
        /// </summary>
        public LayeredBlackboard(IBlackboardStorage shared_storage)
        {
            shared_storage_ = shared_storage ?? new BlackboardStorage();
            private_storage_ = new BlackboardStorage();
        }
        
        public bool TryGetValue<T>(BlackboardKey<T> key, out T value)
        {
            if (private_storage_.TryGetValue(key,out value))
            {
                return true;
            }

            if (key.IsPrivate)
            {
                value = default;
                return false;
            }

            if (shared_storage_.TryGetValue(key,out value))
            {
                return true;
            }

            value = default;
            return false;
        }

        public void SetValue<T>(BlackboardKey<T> key, T value)
        {
            private_storage_.SetValue(key,value);

            if (!key.IsLocalCache)
            {
                shared_storage_.SetValue(key,value);
            }
        }

        public bool RemoveValue<T>(BlackboardKey<T> key)
        {
            return private_storage_.RemoveValue(key);
        }

        public bool RemoveValue<T>(BlackboardKey<T> key, bool remove_shared)
        {
            var private_removed = private_storage_.RemoveValue(key);
            if (remove_shared)
            {
                return private_removed&&shared_storage_.RemoveValue(key);
            }

            return false;
        }

        public bool ContainsKey<T>(BlackboardKey<T> key)
        {
            return private_storage_.ContainsKey(key)||shared_storage_.ContainsKey(key);
        }

        public IEnumerable<IBlackboardKey> GetAllKeys()
        {
            return private_storage_.GetAllKeys().Union(shared_storage_.GetAllKeys());
        }
    }
}
