using System.Text;
using LogManager.LogConfigurationManager;
using Script.Save.Serialization;
using Script.Save.Serialization.Storage;
using Script.Utillties;

namespace LogManager.Storage
{
    public class NetJsonLogConfigurationManager : BaseConfigurationManager
    {
        public NetJsonLogConfigurationManager(LogConfiguration configuration = null, 
            string default_file_name = FixedValues.kLogConfigurationFileName, 
            string base_directory = null) : base(configuration, default_file_name, base_directory)
        {
        }

        protected override LogConfiguration Deserialize(byte[] data)
        {
            var serializer = new JsonSerializerWithStorage(new SerializationSettings
            {
                PrettyPrint = true,
            });
            
            return serializer.Deserialize<LogConfiguration>(Encoding.UTF8.GetString(data));
        }

        protected override byte[] Serialize(LogConfiguration configuration)
        {
            var serializer = new JsonSerializerWithStorage(new SerializationSettings
            {
                PrettyPrint = true,
            });
            return Encoding.UTF8.GetBytes(serializer.SerializeToText(configuration));
        }
    }
}