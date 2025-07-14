using System;

namespace Save.Serialization.Core.TypeConverter
{
    /// <summary>
    /// 通用类型规则器接口，实现此接口来规定规则器可以处理什么类型。
    /// </summary>
    public interface ITypeConverter
    {
        /// <summary>
        /// 支持的类型
        /// </summary>
        Type[] SupportedTypes { get; }
        
        /// <summary>
        /// 该类型是否可以由规则器处理
        /// </summary>
        /// <param name="type">需要处理的类型</param>
        /// <returns>如果可以处理则返回true，否则false</returns>
        bool CanConvert(Type type);
    }

    /// <summary>
    /// Defines a base interface for type converters, which are responsible for handling
    /// serialization and deserialization of specific types.
    /// </summary>
    public interface ITypeConverter<T> : ITypeConverter
    {
        /// <summary>
        /// 将目标对象进行序列化，在序列化之前会调用CanConvert进行类型检查，如果检查不通过则不进行序列化。一般来说推荐将数据序列化的形式为
        /// key-value 的格式，这样的格式比较好处理，您也可以不遵循这项规则，但是需要您的类型规则器转换器进行适配，并且也需要反序列化函数进行
        /// 适配。
        /// </summary>
        /// <param name="value">目标对象</param>
        /// <param name="context"></param>
        /// <returns>序列化之后的数据</returns>
        object Serialize(T value, ISerializationContext context);

        /// <summary>
        /// 将序列化的数据转换为目标对象，在反序列化之前会调用CanConvert进行类型检查，如果检查不通过则不进行反序列化。一般需要根据序列化设定的
        /// 形式进行反序列化。因此请参考序列化的形式。
        /// </summary>
        /// <param name="data">序列化的数据</param>
        /// <param name="context"></param>
        /// <returns>目标对象</returns>
        T Deserialize(object data, ISerializationContext context);
    }
}