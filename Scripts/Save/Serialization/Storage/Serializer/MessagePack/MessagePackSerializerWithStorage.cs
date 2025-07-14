using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using ExTools.Utillties;
using LogManager.Core;
using LogManager.LogManagerFactory;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Save.Serialization.Core.SerializerWithStorage;
using Save.Serialization.Core.TypeConverter;

namespace Save.Serialization.Storage.Serializer.MessagePack
{
    [Obsolete("该类暂时未完成，该类为实验性质的类，带后续更新完成")]
    public class
        MessagePackSerializerWithStorage : BinarySerializerWithStorage<IMessagePackFormatter,
        MessagePackSerializerOptions>
    {
        private static readonly LogSpaceNode space_node = new LogSpaceNode("Serialization").AddChild("SerializerBase")
            .AddChild("FormatSpecificSerializer").AddChild("MessagePackSerializer");
        public override string SerializerName => "MessagePack";

        private IFormatterResolver current_resolver_;
        
        public MessagePackSerializerWithStorage(SerializationSettings shared_settings = null) : base(shared_settings)
        {
            ApplySharedSettingsToOptions();
        }

        protected override IMessagePackFormatter CreateSpecificAdapter(ITypeConverter typeConverter, Type supportedType,
            ISerializationContext context)
        {
            if (typeConverter==null||supportedType==null||context==null)
            {
                return null;
            }

            try
            {
                var adapter_type=typeof(MessagePackTypeConverterAdapter<>).MakeGenericType(supportedType);
                
                // 创建适配器实例
                return (IMessagePackFormatter)Activator.CreateInstance(adapter_type, new object[] { typeConverter, context });
            }
            catch (Exception e)
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(space_node,
                    new LogEntry(LogLevel.kError,
                        $"{GetType().Name}: {SerializerName}: Failed to create MessagePack adapter for {typeConverter.GetType().Name} and type {supportedType.Name}: {e}"));
                return null;
            }
        }

        protected override void InitializeContextInternal()
        {
           Context=new MessagePackSerializationContext(this, shared_settings_);
        }

        protected sealed override void ApplySharedSettingsToOptions()
        {
            var builder = MessagePackSerializerOptions.Standard;
            
            // 引用共享设置到MessagePack选项
            if (shared_settings_.PreserveReferences)
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(space_node,new LogEntry(LogLevel.kWarning,"MessagePack does not natively support reference preservation. Consider using custom formatters for circular references."));
            }
            
            // 设置序列化选项
            var resolvers = new List<IFormatterResolver> { current_resolver_ };
            
            // 添加适配器转换器
            var adapted_converters = ConvertersManager.GetAdaptedConverters(Context);
            var adaptedConverters = adapted_converters as IMessagePackFormatter[] ?? adapted_converters.ToArray();
            var messagePackFormatters = adapted_converters as IMessagePackFormatter[] ?? adaptedConverters.ToArray();
            if (messagePackFormatters.Any())
            {
                var custom_resolver=MessagePackFormatterResolver.Instance.Create(messagePackFormatters.ToArray());
                resolvers.Insert(0,custom_resolver);
            }

            var final_resolvers = CompositeResolver.Create(resolvers.ToArray());
            Options=builder.WithResolver(final_resolvers);
            
            // 更新上下文
            if (Context is MessagePackSerializationContext context)
            {
                context.UpdateSettings(shared_settings_);
            }
        }

        protected override void RebuildOptions()
        {
            ApplySharedSettingsToOptions();
        }

        public override byte[] SerializeToBinary<T>(T obj)
        {
            try
            {
                return global::MessagePack.MessagePackSerializer.Serialize(obj, Options);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Failed to serialize object of type {typeof(T).Name} to MessagePack binary", e);
            }
        }

        public override T DeserializeFromBinary<T>(byte[] data)
        {
            try
            {
                if (data==null||data.Length==0)
                {
                    return default(T);
                }
                
                return global::MessagePack.MessagePackSerializer.Deserialize<T>(data, Options);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Failed to deserialize MessagePack binary to type {typeof(T).Name}", e);
            }
        }

        public override string[] GetSupportedFileExtensions()
        {
            return new[] { ".msgpack", ".mp", ".bin" };
        }

        protected override bool CanHandleFileContent(byte[] fileHeader)
        {
            if (fileHeader==null||fileHeader.Length==0)
            {
                return false;
            }
            
            var first_byte = fileHeader[0];
            
            return first_byte is >= 0x80 and <= 0x8f ||  // fixmap
                   first_byte is >= 0x90 and <= 0x9f ||  // fixarray
                   first_byte is >= 0xa0 and <= 0xbf ||  // fixstr
                   first_byte == 0xc0 ||                          // nil
                   first_byte == 0xc2 || first_byte == 0xc3 ||    // boolean
                   first_byte is >= 0xcc and <= 0xd3 ||    // number formats
                   first_byte is >= 0xd4 and <= 0xd8 ||    // fixext
                   first_byte is >= 0xda and <= 0xdb ||    // str formats
                   first_byte is >= 0xdc and <= 0xdd ||    // array formats
                   first_byte is >= 0xde and <= 0xdf;      // map formats
        }

        public override async UniTask<object> SerializeAsync<T>(T obj, CancellationToken cancellation_token = default)
        {
            return await UniTask.RunOnThreadPool(()=>SerializeToBinary(obj),cancellationToken:cancellation_token);
        }

        public override async UniTask<T> DeserializeAsync<T>(object data, CancellationToken cancellation_token = default)
        {
            if (data is byte[] binary_data)
            {
                return await UniTask.RunOnThreadPool(()=>DeserializeFromBinary<T>(binary_data),cancellationToken:cancellation_token);
            }
            
            throw new ArgumentException($"Expected byte[] data for MessagePack deserialization, got {data?.GetType().Name ?? "null"}");
        }
    }
}