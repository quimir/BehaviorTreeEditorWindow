using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Save.Serialization.Core.TypeConverter;
using Save.Serialization.Core.TypeConverter.Manager;

namespace Save.Serialization.Core
{
    /// <summary>
    /// 序列化器接口，此接口用来处理数据如何序列化/反序列化，是否可以处理该类型的数据和什么类型可以序列化/反序列。
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// 将数据序列化为原始数据
        /// </summary>
        /// <param name="obj">序列化数据</param>
        /// <typeparam name="T">数据类型</typeparam>
        /// <returns>原始数据</returns>
        object Serialize<T>(T obj);

        /// <summary>
        /// 将原始数据反序列化
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <typeparam name="T">序列化的类型</typeparam>
        /// <returns>反序列后的数据</returns>
        T Deserialize<T>(object data);

        /// <summary>
        /// 序列化唯一标识符，用来标识现在序列化的类型
        /// </summary>
        string SerializerName { get; }

        /// <summary>
        /// 获取可以序列化/反序列的对象类型
        /// </summary>
        /// <returns>该类可以处理的序列化/反序列化对象类型</returns>
        Type GetSerializedDataType();
        
        /// <summary>
        /// 获取类型规则器管理器
        /// </summary>
        /// <returns>返回类型规则器器管理器实例</returns>
        ITypeConverterMessage GetTypeConverterManager();
    }

    /// <summary>
    /// 多线程序列化/反序列化接口，实现该接口以实现具体的多线程序列化/反序列流程
    /// </summary>
    public interface IAsyncSerializer : ISerializer
    {
        /// <summary>
        /// 异步将对象序列化为原始数据
        /// </summary>
        /// <param name="obj">要序列化的对象</param>
        /// <param name="cancellation_token">用于取消操作的令牌，默认为默认值</param>
        /// <typeparam name="T">对象的类型</typeparam>
        /// <returns>表示序列化结果的异步操作</returns>
        UniTask<object> SerializeAsync<T>(T obj, CancellationToken cancellation_token = default);

        /// <summary>
        /// 异步反序列化数据为指定类型的对象
        /// </summary>
        /// <param name="data">要反序列化的数据</param>
        /// <param name="cancellation_token">用于取消操作的令牌</param>
        /// <typeparam name="T">目标对象的类型</typeparam>
        /// <returns>反序列化后的对象</returns>
        UniTask<T> DeserializeAsync<T>(object data, CancellationToken cancellation_token = default);
    }

    /// <summary>
    /// Interface defining methods for text-based serialization and deserialization.
    /// This interface extends ISerializer for handling text-specific operations
    /// such as converting objects to and from text representations.
    /// </summary>
    public interface ITextSerializer : ISerializer
    {
        /// <summary>
        /// Serializes an object into its text representation.
        /// </summary>
        /// <param name="obj">The object to be serialized.</param>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <returns>A string containing the serialized text representation of the object.</returns>
        string SerializeToText<T>(T obj);

        /// <summary>
        /// Deserializes an object of the specified type from its text representation.
        /// </summary>
        /// <param name="text">The text representation of the object to be deserialized.</param>
        /// <typeparam name="T">The type of the object to deserialize.</typeparam>
        /// <returns>The deserialized object of type <typeparamref name="T"/>.</returns>
        T DeserializeFromText<T>(string text);
    }

    /// <summary>
    /// Interface for binary serialization, designed for serializing objects into binary data
    /// and deserializing binary data back into objects.
    /// </summary>
    public interface IBinarySerializer : ISerializer
    {
        /// <summary>
        /// 将对象序列化为二进制数据
        /// </summary>
        /// <param name="obj">要序列化的对象</param>
        /// <typeparam name="T">对象的类型</typeparam>
        /// <returns>表示对象的二进制数据</returns>
        byte[] SerializeToBinary<T>(T obj);

        /// <summary>
        /// Deserializes binary data into an object of the specified type.
        /// </summary>
        /// <param name="data">The binary data to deserialize.</param>
        /// <typeparam name="T">The type of the object to deserialize to.</typeparam>
        /// <returns>An instance of type <typeparamref name="T"/> deserialized from the binary data.</returns>
        T DeserializeFromBinary<T>(byte[] data);
    }
}