using System;
using System.Collections.Generic;
using System.Linq;
using ExTools.Utillties;
using Script.Utillties;
using UnityEngine;

namespace Script.Save.Serialization
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
        
        public string CustomDateFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
    }

    public interface ISerializationContext
    {
        ISerializer Serializer { get; }
        
        SerializationSettings Settings { get; }
        
        void UpdateSettings(SerializationSettings settings);
    }
    
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

    /// <summary>
    /// Manages a collection of type converters, providing methods for adding, removing,
    /// and querying converters, as well as notifying changes in the registry.
    /// </summary>
    /// <typeparam name="TConverter">
    /// The primary type of converters managed by this class.
    /// </typeparam>
    public class BaseTypeConverterManager<TConverter> : ITypeConverterMessage<TConverter>
    {
        /// <summary>
        /// A private collection that stores the primary converters managed by the system.
        /// These converters are responsible for performing type-specific serialization or
        /// deserialization tasks.
        /// </summary>
        private readonly List<TConverter> primary_converters_ = new List<TConverter>();

        /// <summary>
        /// A collection of secondary type converters used to handle conversion of types
        /// that are not managed as primary converters. This list ensures extensibility by
        /// supporting additional conversion rules through secondary mechanisms.
        /// </summary>
        private readonly List<ITypeConverter> secondary_converters_ = new List<ITypeConverter>();

        /// <summary>
        /// Factory function provided by the specific serializer to create library-specific adapters from ITypeConverter.
        /// </summary>
        private readonly Func<ITypeConverter, Type, ISerializationContext, TConverter> adapter_factory_;

        /// <summary>
        /// Event triggered when a converter is added or removed.
        /// </summary>
        public event Action OnRegistryChanged;
        
        public BaseTypeConverterManager(Func<ITypeConverter, Type, ISerializationContext, TConverter> adapterFactory)
        {
            adapter_factory_ = adapterFactory??throw new ArgumentNullException(nameof(adapterFactory));
        }
        
        public virtual void AddConverter(object converter)
        {
            if (converter==null)
            {
                return;
            }

            bool changed = false;
            if (converter is TConverter primary)
            {
                // Use ReferenceEquals for potentially stateless formatters/converters
                if (!primary_converters_.Any(c=>ReferenceEquals(c,primary)||c.Equals(primary)))
                {
                    primary_converters_.Add(primary);
                    changed = true;
                }
            }
            else if (converter is ITypeConverter secondary)
            {
                if (!secondary_converters_.Any(c=>ReferenceEquals(c,secondary)||c.Equals(secondary)))
                {
                    secondary_converters_.Add(secondary);
                    changed = true;// Adding a secondary converter also requires rebuild
                }
            }
            else
            {
                throw new ArgumentException($"Converter must be of type {typeof(TConverter).Name} or {nameof(ITypeConverter)}. Got: {converter.GetType().Name}");
            }

            if (changed)
            {
                NotifyChange();
            }
        }

        /// <summary>
        /// Invokes the OnRegistryChanged event.
        /// </summary>
        protected virtual void NotifyChange()
        {
            OnRegistryChanged?.Invoke();
        }

        public virtual bool RemoveConverter(object converter)
        {
            if (converter==null)
            {
                return false;
            }

            bool changed = false;
            if (converter is TConverter primary)
            {
                // Find based on reference or equality
                var existing=primary_converters_.FirstOrDefault(c=>ReferenceEquals(c, primary) || c.Equals(primary));
                if (existing!=null)
                {
                    primary_converters_.Remove(existing);
                    changed = true;
                }
            }
            else if (converter is ITypeConverter secondary)
            {
                var existing=secondary_converters_.FirstOrDefault(c=>ReferenceEquals(c, secondary) || c.Equals(secondary));
                if (existing!=null)
                {
                    secondary_converters_.Remove(existing);
                    changed = true;
                }
            }
            else
            {
                // Optionally log a warning or ignore if type doesn't match
                Debug.LogWarning($"Attempted to remove an object that is neither {typeof(TConverter).Name} nor {nameof(ITypeConverter)}: {converter.GetType().Name}");
                return false;
            }

            if (changed)
            {
                NotifyChange();
            }

            return true;
        }

        public virtual bool ContainsConverter(object converter)
        {
            if (converter==null)
            {
                return false;
            }

            if (converter is TConverter primary)
            {
                return primary_converters_.Any(c=>ReferenceEquals(c,primary)||c.Equals(primary));
            }

            if (converter is ITypeConverter secondary)
            {
                return secondary_converters_.Any(c=>ReferenceEquals(c,secondary)||c.Equals(secondary));
            }

            return false;
        }

        public virtual IEnumerable<object> GetAllConverterObjects()
        {
            return primary_converters_.Cast<object>().Concat(secondary_converters_.Cast<object>());
        }

        public IEnumerable<TConverter> GetPrimaryConverters() => primary_converters_.AsReadOnly();

        public IEnumerable<ITypeConverter> GetSecondaryConverters() => secondary_converters_.AsReadOnly();

        /// <summary>
        /// Retrieves the adapted converters from the secondary converters based on the provided serialization context.
        /// </summary>
        /// <param name="context">The serialization context used to adapt and filter the converters.</param>
        /// <returns>An enumerable collection of distinct adapted converters.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided context is null.</exception>
        public virtual IEnumerable<TConverter> GetAdaptedConverters(ISerializationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var adapters = new List<TConverter>();
            foreach (var type_converter in secondary_converters_)
            {
                if (type_converter.SupportedTypes==null)
                {
                    continue;
                }

                foreach (var supported_type in type_converter.SupportedTypes)
                {
                    try
                    {
                        // Use the factory passed in constructor
                        var adapter = adapter_factory_(type_converter, supported_type, context);
                        if (adapter!=null)
                        {
                            adapters.Add(adapter);
                        }
                    }
                    catch (Exception e)
                    {
                        // Log error during adapter creation for a specific type
                        Debug.LogError($"Error creating adapter for {type_converter.GetType().Name} targeting {supported_type.Name}: {e}");
                    }
                }
            }
            
            // Return distinct adapters. Equality check depends on TConverter's implementation. Using a comparer might
            // be necessary if default equality is not sufficient.
            return adapters.Distinct();
        }
    }
}