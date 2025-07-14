using ExTools.Utillties;
using Script.Save.Serialization;

namespace Save.Serialization.Core.TypeConverter
{
    /// <summary>
    /// Represents the settings used for controlling the behavior of serialization processes.
    /// </summary>
    public class SerializationSettings
    {
        /// <summary>
        /// 是否忽略空值
        /// </summary>
        public bool IgnoreNullValues { get; set; } = false;
        
        /// <summary>
        /// 是否使用美化格式输出
        /// </summary>
        public bool PrettyPrint { get; set; } = false;

        /// <summary>
        /// 序列化时的最大递归深度
        /// </summary>
        public int MaxDepth { get; set; } = 64;
        
        /// <summary>
        /// 是否保留对象引用，用于处理循环引用
        /// </summary>
        public bool PreserveReferences { get; set; } = false;

        /// <summary>
        /// 自定义类型解析策略
        /// </summary>
        public SerializationTypeNameHandling TypeNameHandling { get; set; } = SerializationTypeNameHandling.kNone;

        /// <summary>
        /// 日期处理格式
        /// </summary>
        public SerializationDateTimeHandling DateTimeHandling { get; set; } = SerializationDateTimeHandling.kIsoFormat;

        /// <summary>
        /// Custom date format used for serializing and deserializing DateTime values when the DateTimeHandling option
        /// is set to Custom.
        /// </summary>
        public string CustomDateFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
    }

    /// <summary>
    /// Defines a context used during serialization and deserialization processes,
    /// providing the necessary settings and serializer implementation.
    /// </summary>
    public interface ISerializationContext
    {
        ISerializer Serializer { get; }
        
        SerializationSettings Settings { get; }
        
        void UpdateSettings(SerializationSettings settings);
    }
}