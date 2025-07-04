using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Script.Save.Serialization
{
    /// <summary>
    /// 抽象基类，提供序列化器实现的基础结构，用于定义序列化和反序列化逻辑。
    /// 实现了通用序列化器接口 <see cref="ISerializer"/>。
    /// 具体序列化器需要从此类派生并实现抽象方法。
    /// </summary>
    public abstract class SerializerBase : ISerializer
    {
        public abstract string SerializerName { get; }

        public abstract Type GetSerializedDataType();

        public abstract object Serialize<T>(T obj);

        public abstract T Deserialize<T>(object data);

        public abstract ITypeConverterMessage GetTypeConverterManager();

        /// <summary>
        /// 验证指定的转换器类型是否为有效的类型。
        /// </summary>
        /// <param name="converter">需要验证的转换器对象。</param>
        /// <returns>如果转换器是有效类型则返回 true，否则返回 false。</returns>
        protected abstract bool ValidateConverterType(object converter);
    }
}