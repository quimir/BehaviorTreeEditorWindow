using System;
using System.Collections.Generic;
using Save.Serialization.Core.TypeConverter.SerializerAttribute;
using UnityEngine;

namespace Save.CustomSerialize
{
    /// <summary>
    /// A custom implementation of a dictionary that supports serialization in Unity.
    /// This class extends the <see cref="Dictionary{TKey, TValue}"/> and implements the
    /// <see cref="UnityEngine.ISerializationCallbackReceiver"/> interface to handle serialization
    /// and deserialization processes for Unity's editor and runtime.
    /// </summary>
    /// <typeparam name="K">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="V">The type of the values in the dictionary.</typeparam>
    [Serializable]
    [CustomSerialize]
    public class CustomSerializableDictionary<K, V> : Dictionary<K, V>, ISerializationCallbackReceiver
    {
        [SerializeField] 
        [PersistField]
        private List<CustomSerializableKVP<K,V>> _keys = new List<CustomSerializableKVP<K,V>>();

        /// <summary>
        /// Removes all elements from the dictionary and clears the internal list used for serialization support.
        /// </summary>
        public new void Clear()
        {
            base.Clear();
            _keys.Clear();
        }

        /// <summary>
        /// Removes the specified key and its associated value from the dictionary.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>True if the element is successfully removed; otherwise, false. This method also returns false if
        /// the specified key is not found in the dictionary.</returns>
        public new bool Remove(K key)
        {
            if (base.Remove(key))
            {
                var index=_keys.FindIndex(kvp=>EqualityComparer<K>.Default.Equals(kvp.Key,key));
                if (index!=-1)
                {
                    _keys.RemoveAt(index);
                }

                return true;
            }

            return false;
        }
        
        public void OnBeforeSerialize()
        {
            if (!(Count>_keys.Count))
            {
                return;
            }
            _keys.Clear();
            foreach (var pair in this)
            {
                _keys.Add(new CustomSerializableKVP<K,V>(pair.Key,pair.Value));;
            }
        }

        public void OnAfterDeserialize()
        {
            UpdateDictionaryInternal();
        }

        /// <summary>
        /// Updates the internal state of the dictionary by rebuilding its key-value pairs
        /// from the serialized key-value list. Ensures that duplicate or null entries
        /// are not added to the dictionary to maintain consistency during deserialization.
        /// </summary>
        private void UpdateDictionaryInternal()
        {
            base.Clear();
            foreach (var kvp in _keys)
            {
                if(kvp==null)continue;
                if(kvp.Key==null)continue;
                if(kvp.Value==null)continue;
                if (ContainsKey(kvp.Key)) continue;
                
                base.Add(kvp.Key,kvp.Value);
            }
        }
    }
}
