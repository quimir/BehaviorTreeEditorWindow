using System;
using System.Collections.Generic;
using ExTools.Singleton;
using MessagePack;
using MessagePack.Formatters;
using Save.Serialization.Core;
using Save.Serialization.Core.TypeConverter;

namespace Save.Serialization.Storage.Serializer.MessagePack
{
    public class MessagePackSerializationContext : ISerializationContext
    {
        public ISerializer Serializer { get; private set; }
        public SerializationSettings Settings { get; private set; }

        public MessagePackSerializationContext(ISerializer serializer, SerializationSettings settings)
        {
            Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }
        
        public void UpdateSettings(SerializationSettings settings)
        {
            Settings=settings??throw new ArgumentNullException(nameof(settings));
        }
    }

    public class MessagePackFormatterResolver : SingletonWithLazy<MessagePackFormatterResolver>
    {
        private class CustomFormatterResolver : IFormatterResolver
        {
            private readonly Dictionary<Type, IMessagePackFormatter> formatters_;

            public CustomFormatterResolver(IMessagePackFormatter[] formatters)
            {
                formatters_ = new Dictionary<Type, IMessagePackFormatter>();

                foreach (var formatter in formatters)
                {
                    if (formatter!=null)
                    {
                        // 通过反射机制获取formatter处理的类型
                        var formatter_type=formatter.GetType();
                        var interfaces = formatter_type.GetInterfaces();

                        foreach (var inter in interfaces)
                        {
                            if (inter.IsGenericType&&inter.GetGenericTypeDefinition()==typeof(IMessagePackFormatter<>))
                            {
                                var target_type=inter.GetGenericArguments()[0];
                                formatters_[target_type] = formatter;
                                break;
                            }
                        }
                    }
                }
            }
            
            public IMessagePackFormatter<T> GetFormatter<T>()
            {
                return formatters_.TryGetValue(typeof(T),out var formatter)?(IMessagePackFormatter<T>)formatter:null;
            }
        }
        
        public IFormatterResolver Create(params IMessagePackFormatter[] formatters)
        {
            return new CustomFormatterResolver(formatters);
        }
    }

    public class MessagePackTypeConverterAdapter<T> : IMessagePackFormatter<T>
    {
        private readonly ITypeConverter<T> type_converter_;
        private readonly ISerializationContext context_;

        public MessagePackTypeConverterAdapter(ITypeConverter<T> type_converter, ISerializationContext context)
        {
            type_converter_=type_converter??throw new ArgumentNullException(nameof(type_converter));
            context_=context??throw new ArgumentNullException(nameof(context));
        }
        
        public void Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options)
        {
            try
            {
                if (!type_converter_.CanConvert(typeof(T)))
                {
                    throw new InvalidOperationException($"Type converter cannot handle type {typeof(T).Name}");
                }

                var serialized_data = type_converter_.Serialize(value, context_);
                
                MessagePackSerializer.Serialize(ref writer, serialized_data, options);
            }
            catch (Exception e)
            {
                throw new MessagePackSerializationException($"Error serializing type {typeof(T).Name} using custom converter", e);
            }
        }

        public T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            try
            {
                if (type_converter_.CanConvert(typeof(T)))
                {
                    throw new InvalidOperationException($"Type converter cannot handle type {typeof(T).Name}");
                }
                
                var serialized_data = MessagePackSerializer.Deserialize<object>(ref reader, options);

                return type_converter_.Deserialize(serialized_data, context_);
            }
            catch (Exception e)
            {
                throw new MessagePackSerializationException($"Error deserializing type {typeof(T).Name} using custom converter", e);
            }
        }
    }
}
