using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExTools.Singleton;
using ExTools.Utillties;
using LogManager.Core;
using LogManager.LogManagerFactory;
using Save.Serialization;
using Script.LogManager;
using Script.Save.Serialization.Storage;
using Script.Utillties;

namespace Script.Save.Serialization.Factory
{
    /// <summary>
    /// Provides methods for creating serializer instances based on the specified serializer type or custom settings.
    /// </summary>
    public class SerializerCreator:SingletonWithLazy<SerializerCreator>
    {
        /// <summary>
        /// Creates a new instance of a serializer based on the provided type and optional settings.
        /// </summary>
        /// <param name="type">The type of serializer to create.</param>
        /// <param name="settings">Optional serialization settings to configure the serializer.</param>
        /// <returns>An instance of a serializer corresponding to the specified type.</returns>
        public ISerializer Create(SerializerType type, SerializationSettings settings = null)
        {
            return SerializerFactory.Instance.CreateSerializer(type, settings);
        }

        /// <summary>
        /// Creates a JSON serializer instance with optional serialization settings.
        /// </summary>
        /// <param name="settings">Optional settings to configure the JSON serializer.</param>
        /// <returns>An instance of a JSON serializer.</returns>
        public ISerializer CreateJson(SerializationSettings settings = null)
        {
            return Create(SerializerType.kJson, settings);
        }

        /// <summary>
        /// Creates a new instance of a serializer of the specified type using the optional serialization settings.
        /// </summary>
        /// <param name="type">The type of serializer to create, as defined in the SerializerType enumeration.</param>
        /// <param name="settings">Optional settings to configure the behavior of the serializer during its creation.</param>
        /// <typeparam name="T">The specific serializer type that implements the ISerializer interface.</typeparam>
        /// <returns>A new instance of the desired serializer type cast as <typeparamref name="T"/>.</returns>
        public T Create<T>(SerializerType type, SerializationSettings settings = null) where T : ISerializer
        {
            return (T)Create(type, settings);
        }
    }

    /// <summary>
    /// Manages the creation and registration of serializer instances, enabling the production of serializers
    /// for specified types and settings. This factory also supports the dynamic discovery and registration
    /// of serializers from assemblies.
    /// </summary>
    public class SerializerFactory : SingletonWithLazy<SerializerFactory>, ISerializerFactory
    {
        private readonly Dictionary<SerializerType, Func<SerializationSettings, ISerializer>> creator_map_ =
            new Dictionary<SerializerType, Func<SerializationSettings, ISerializer>>();

        protected override void InitializationInternal()
        {
            RegisterSerializerCreator(SerializerType.kJson,settings=>new JsonSerializerWithStorage(settings));
        }

        public ISerializer CreateSerializer(SerializerType type, SerializationSettings settings = null)
        {
            if (creator_map_.TryGetValue(type,out var creator))
            {
                return creator(settings);
            }
            
            throw new ArgumentException($"未注册的序列化器类型：{type}");
        }

        public void RegisterSerializerCreator(SerializerType type, Func<SerializationSettings, ISerializer> creator)
        {
            creator_map_[type] = creator ?? throw new ArgumentNullException(nameof(creator));
        }

        /// <summary>
        /// Automatically registers serializers from the specified assembly or all loaded assemblies,
        /// identifying classes that implement the ISerializer interface and are associated with
        /// the SerializerTypeAttribute. This method facilitates the dynamic discovery and configuration of serializers.
        /// </summary>
        /// <param name="assembly">The assembly to inspect for serializers. If null, all loaded assemblies are examined.</param>
        public void AutoRegisterSerializers(Assembly assembly)
        {
            IEnumerable<Type> types;

            if (assembly!=null)
            {
                types = assembly.GetTypes();
            }
            else
            {
                types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a =>
                {
                    try
                    {
                        return a.GetTypes();
                    }
                    catch (Exception e)
                    {
                        ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(new LogSpaceNode("SerializerFactory"),new LogEntry(LogLevel.kWarning,e.Message));
                        return Type.EmptyTypes;
                    }
                });
            }

            foreach (var type in types)
            {
                if (!type.IsClass||type.IsAbstract||type.IsGenericTypeDefinition)
                {
                    continue;
                }
                
                // 查找SerializerTypeAttribute
                var attr = type.GetCustomAttribute<SerializerTypeAttribute>();
                if (attr==null)
                {
                    continue;
                }
                
                // 确保类实现了ISerializer
                if (!typeof(ISerializer).IsAssignableFrom(type))
                {
                    continue;
                }
                
                // 查找接受SerializationSettings参数的构造函数
                var ctor = type.GetConstructor(new[] { typeof(SerializationSettings) });

                if (ctor!=null)
                {
                    RegisterSerializerCreator(attr.Type,settings=>(ISerializer)Activator.CreateInstance(type,settings));
                }
                else
                {
                    // 尝试使用无参构造函数
                    var default_ctor=type.GetConstructor(Type.EmptyTypes);
                    if (default_ctor!=null)
                    {
                        RegisterSerializerCreator(attr.Type,_=>(ISerializer)Activator.CreateInstance(type));
                    }
                }
            }
        }
    }
}
