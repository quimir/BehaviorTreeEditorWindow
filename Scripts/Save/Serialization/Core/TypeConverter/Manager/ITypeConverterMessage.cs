using System;
using System.Collections.Generic;

namespace Save.Serialization.Core.TypeConverter.Manager
{
    /// <summary>
    /// 类型规则器管理器接口，实现该接口以实现类型规则器的统一管理
    /// </summary>
    public interface ITypeConverterMessage
    {
        /// <summary>
        /// 添加类型规则器，在此可以添加关于任意自定义类型的规则
        /// </summary>
        /// <param name="converter">自定义类型规则</param>
        public void AddConverter(object converter);
        
        /// <summary>
        /// 移除指定的类型规则器，如果指定类型规则器被移除则返回true，否则返回false
        /// </summary>
        /// <param name="converter">类型规则器实例</param>
        /// <returns>如果指定类型规则器被正确的移除则返回true，否则返回false</returns>
        public bool RemoveConverter(object converter);
        
        /// <summary>
        /// 判断类型规则器是否存在
        /// </summary>
        /// <param name="converter">类型规则器</param>
        /// <returns>存在返回true，否则返回false</returns>
        public bool ContainsConverter(object converter);

        /// <summary>
        /// 获取所有已添加的类型规则器
        /// </summary>
        /// <returns>如果转换器存在，则返回所有已添加的转换器</returns>
        public IEnumerable<object> GetAllConverterObjects();
    }

    /// <summary>
    /// 类型规则器管理器接口，实现该接口以实现类型规则器的统一管理。
    /// </summary>
    public interface ITypeConverterMessage<out TConverter> : ITypeConverterMessage
    {
        /// <summary>
        /// 获取已加载的序列化器中的类型规则器(比如说.JsonNet当中的JsonConverter)
        /// </summary>
        /// <returns>序列化器中的类型规则器</returns>
        public IEnumerable<TConverter> GetPrimaryConverters();
        
        /// <summary>
        /// 获取已加载的通用类型规则器(一般为ITypeConverter)
        /// </summary>
        /// <returns>通用类型规则器</returns>
        public IEnumerable<ITypeConverter> GetSecondaryConverters();

        /// <summary>
        /// Retrieves the adapted converters based on the provided serialization context.
        /// </summary>
        /// <param name="context">The serialization context used to adapt and filter the converters.</param>
        /// <returns>An enumerable collection of adapted converters.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the provided context is null.</exception>
        public IEnumerable<TConverter> GetAdaptedConverters(ISerializationContext context);
    }
}