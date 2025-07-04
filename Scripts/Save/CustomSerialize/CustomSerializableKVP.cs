using System;
using UnityEngine;

namespace Save.CustomSerialize
{
    [Serializable]
    public class CustomSerializableKVP<K,V>
    {
        [SerializeField] private K key_ = default;
        public K Key=>key_;
        [SerializeField] private V value_ = default;
        public V Value=>value_;

        public CustomSerializableKVP(K key, V value)
        {
            key_ = key;
            value_ = value;
        }
    }
}
