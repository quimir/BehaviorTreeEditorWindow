using System;
using System.Collections.Generic;
using UnityEngine;

namespace Save.CustomSerialize
{
    [Serializable]
    public class CustomSerializableDictionary<K, V>:Dictionary<K,V>,ISerializationCallbackReceiver
    {
        [SerializeField] private List<CustomSerializableKVP<K,V>> _keys = new List<CustomSerializableKVP<K,V>>();

        public new void Clear()
        {
            base.Clear();
            _keys.Clear();
        }

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
