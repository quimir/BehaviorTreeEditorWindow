using System;
using ExTools.Utillties;
using Script.Utillties;

namespace Script.Save.Serialization.Factory
{
    /// <summary>
    /// Defines a factory for creating and managing serializers, including the ability to register custom serializer creators.
    /// </summary>
    public interface ISerializerFactory
    {
        /// <summary>
        /// 创建一个序列化器实例
        /// </summary>
        /// <param name="type">序列化器类型</param>
        /// <param name="settings">序列化设置</param>
        /// <returns>序列化器实例</returns>
        ISerializer CreateSerializer(SerializerType type, SerializationSettings settings = null);
    
        /// <summary>
        /// 注册自定义序列化器创建器
        /// </summary>
        /// <param name="type">序列化器类型</param>
        /// <param name="creator">创建器委托</param>
        void RegisterSerializerCreator(SerializerType type, Func<SerializationSettings, ISerializer> creator);
    }
}
