using System;
using Save.Serialization.Core.TypeConverter.SerializerAttribute;
using UnityEngine;

namespace Save.CustomSerialize
{
    /// <summary>
    /// Represents a custom serializable key-value pair structure.
    /// </summary>
    /// <typeparam name="K">The type of the key.</typeparam>
    /// <typeparam name="V">The type of the value.</typeparam>
    [Serializable]
    public class CustomSerializableKVP<K, V>
    {
        /// <summary>
        /// Represents the private field storing the key within the custom serializable key-value pair.
        /// </summary>
        /// <remarks>
        /// This field is marked with <see cref="UnityEngine.SerializeField"/> to make it visible for Unity's serialization system,
        /// and with <see cref="Save.Serialization.Core.TypeConverter.SerializerAttribute.PersistFieldAttribute"/> to ensure it is persisted
        /// during serialization. The field is of generic type <typeparamref name="K"/>, which is defined by the class.
        /// </remarks>
        [SerializeField] [PersistField] private K key_ = default;
        public K Key=>key_;

        /// <summary>
        /// Represents the private field storing the value within the custom serializable key-value pair.
        /// </summary>
        /// <remarks>
        /// This field is marked with <see cref="UnityEngine.SerializeField"/> to ensure it is serialized in Unity's ecosystem,
        /// and with <see cref="Save.Serialization.Core.TypeConverter.SerializerAttribute.PersistFieldAttribute"/> to guarantee its persistence
        /// during the custom serialization process. The field's type is generic <typeparamref name="V"/>, defined by the enclosing class.
        /// </remarks>
        [SerializeField] [PersistField] private V value_ = default;
        public V Value=>value_;

        public CustomSerializableKVP(K key, V value)
        {
            key_ = key;
            value_ = value;
        }
    }
}
