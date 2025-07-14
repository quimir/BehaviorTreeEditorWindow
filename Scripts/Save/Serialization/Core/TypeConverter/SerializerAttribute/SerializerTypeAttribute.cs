using ExTools.Utillties;

namespace Save.Serialization.Core.TypeConverter.SerializerAttribute
{
    /// <summary>
    /// An attribute used to designate the serialization type for a specific class, structure, or enumeration.
    /// This enables associating a serializer implementation with a given <see cref="SerializerType"/>.
    /// </summary>
    public class SerializerTypeAttribute : System.Attribute
    {
        public SerializerType Type { get; }

        public SerializerTypeAttribute(SerializerType type)
        {
            Type = type;
        }
    }
}
