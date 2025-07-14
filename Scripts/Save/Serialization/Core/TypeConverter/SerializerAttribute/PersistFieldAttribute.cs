using System;

namespace Save.Serialization.Core.TypeConverter.SerializerAttribute
{
    /// <summary>
    /// An attribute used to indicate that a field or property should be persisted during serialization.
    /// </summary>
    /// <remarks>
    /// This attribute is applied to fields or properties to specify their inclusion in the serialization process.
    /// It allows optional configuration through properties such as <c>PropertyName</c>, <c>Required</c>, and <c>DefaultValue</c>.
    /// The <c>PropertyName</c> allows renaming the serialized member.
    /// The <c>Required</c> property enforces mandatory presence of the field or property in deserialization.
    /// The <c>DefaultValue</c> can be used to specify a fallback value if the field or property is not explicitly set.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field|AttributeTargets.Property,AllowMultiple = false)]
    public class PersistFieldAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the custom property name to be used in serialization or deserialization
        /// when the associated attribute is applied to a field or property. This name overrides
        /// the default property name determined by the runtime.
        /// When specified, this value is used to map the field or property to an alternative
        /// serialized key or identifier.
        /// If left as null or empty, the runtime will use the default field or property name for serialization.
        /// Example use case includes renaming fields for specific formats or adhering to naming conventions.
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the associated field or property is required
        /// during serialization or deserialization. When set to true, the field or property
        /// must be provided to fulfill the requirements for data integrity.
        /// Typically used for enforcing the presence of certain values in the serialized or deserialized
        /// object structure, ensuring completeness during data transformations.
        /// If set to false, the field or property is considered optional and may be omitted in serialization
        /// or deserialization processes.
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// Gets or sets the default value to be used for a field or property during serialization or deserialization
        /// when no explicit value is assigned. This value ensures consistency in cases where data is missing or undefined.
        /// If specified, the serializer will automatically substitute this value for fields or properties that lack a
        /// concrete assignment. This can simplify handling null values and ensure default application behavior.
        /// </summary>
        public object DefaultValue { get; set; }
        
        public PersistFieldAttribute(string propertyName=null)
        {
            PropertyName=propertyName;
        }
    }

    /// <summary>
    /// An attribute used to indicate that a field or property should not be serialized during the serialization process.
    /// </summary>
    /// <remarks>
    /// This attribute is intended to explicitly exclude specific fields or properties from being serialized.
    /// It is typically utilized in scenarios where certain members are not meant to be persisted
    /// or included in the output of the serialization logic.
    /// Applying this attribute ensures that the associated member is omitted during serialization.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field|AttributeTargets.Property,AllowMultiple = false)]
    public class NonSerializeAttribute : System.Attribute
    {
    }
}
