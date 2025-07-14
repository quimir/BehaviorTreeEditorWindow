using Save.Serialization.Core;
using Save.Serialization.Core.TypeConverter;

namespace Save.Serialization.Storage.Serializer.JsonNet
{
    public class JsonNetSerializationContext : ISerializationContext
    {
        public JsonNetSerializationContext(ISerializer serializer, SerializationSettings settings = null)
        {
            Serializer = serializer;
            Settings = settings ?? new SerializationSettings();
        }

        public void UpdateSettings(SerializationSettings settings)
        {
            Settings = settings ?? new SerializationSettings();
        }

        public ISerializer Serializer { get; }
        public SerializationSettings Settings { get; private set; }
    }
}
