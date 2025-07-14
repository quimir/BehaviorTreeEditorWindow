using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExTools.Utillties;
using LogManager.Core;
using LogManager.LogManagerFactory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Save.Serialization.Core.SerializerWithStorage;
using Save.Serialization.Core.TypeConverter;
using Save.Serialization.Core.TypeConverter.SerializerAttribute;
using Save.Serialization.Storage.TypeConverter;

namespace Save.Serialization.Storage.Serializer.JsonNet
{
    /// <summary>
    /// Provides JSON-based serialization and storage functionality using Newtonsoft.Json.
    /// This class extends <see cref="TextSerializerWithStorage{TConverter, TOptions}"/> to
    /// support JSON serialization, allowing for configuration via <see cref="SerializationSettings"/>
    /// and enabling integration with custom type conversion and specific file handling capabilities.
    /// </summary>
    [SerializerType(SerializerType.kJson)]
    public class JsonSerializerWithStorage : TextSerializerWithStorage<JsonConverter, JsonSerializerSettings>
    {
        private static readonly LogSpaceNode space_node = new LogSpaceNode("Serialization").AddChild("SerializerBase")
            .AddChild("FormatSpecificSerializer").AddChild("JsonNetSerializer");
        
        public JsonSerializerWithStorage(SerializationSettings shared_settings = null) : base(shared_settings)
        {
            ApplySharedSettingsToOptions();
        }

        public override string SerializerName => "Json";

        protected override void InitializeContextInternal()
        {
            Context = new JsonNetSerializationContext(this, shared_settings_);
        }

        protected override JsonConverter CreateSpecificAdapter(ITypeConverter typeConverter, Type supportedType,
            ISerializationContext context)
        {
            try
            {
                var adapter_generic_type = typeof(JsonNetTypeConverterAdapter<>);
                var adapter_specific_type = adapter_generic_type.MakeGenericType(supportedType);
                var converter_interface_type = typeof(ITypeConverter<>).MakeGenericType(supportedType);

                if (!converter_interface_type.IsInstanceOfType(typeConverter))
                {
                    ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(space_node,
                        new LogEntry(LogLevel.kError,
                            $"{GetType().Name}: {SerializerName}: TypeConverter {typeConverter.GetType().Name} " +
                            $"does not implement {converter_interface_type.Name}"));
                    return null;
                }

                // Pass the specific context instance
                return (JsonConverter)Activator.CreateInstance(adapter_specific_type,
                    new object[] { typeConverter, context });
            }
            catch (Exception e)
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(space_node,
                    new LogEntry(LogLevel.kError,
                        $"{GetType().Name}: {SerializerName}: Failed to create JsonNet adapter for " +
                        $"{typeConverter.GetType().Name} and type {supportedType.Name}: {e}"));
                return null;
            }
        }

        protected sealed override void ApplySharedSettingsToOptions()
        {
            if (Options != null) return;
            Options = new JsonSerializerSettings
            {
                ContractResolver = new JsonNetContractResolver(),
                Formatting = shared_settings_.PrettyPrint ? Formatting.Indented : Formatting.None,
                NullValueHandling = shared_settings_.IgnoreNullValues
                    ? NullValueHandling.Ignore
                    : NullValueHandling.Include,
                MaxDepth = shared_settings_.MaxDepth > 0 ? shared_settings_.MaxDepth : 64, // MaxDepth is nullable int
                PreserveReferencesHandling = shared_settings_.PreserveReferences
                    ? PreserveReferencesHandling.Objects
                    : PreserveReferencesHandling.None,
                ReferenceLoopHandling = shared_settings_.PreserveReferences
                    ? ReferenceLoopHandling.Serialize
                    : ReferenceLoopHandling.Ignore, // Adjust based on PreserveReferences
                TypeNameHandling = MapJsonTypeNameHandling(shared_settings_.TypeNameHandling),
                DateFormatHandling = MapJsonDateFormatHandling(shared_settings_.DateTimeHandling),
                // Use custom format ONLY if handling is Custom, otherwise let Newtonsoft handle ISO/MicrosoftDateFormat
                DateFormatString = (shared_settings_.DateTimeHandling == SerializationDateTimeHandling.kCustom &&
                                    !string.IsNullOrEmpty(shared_settings_.CustomDateFormat)
                    ? shared_settings_.CustomDateFormat
                    : null) ?? string.Empty // Use null for default handling based on DateFormatHandling
            };

            ConvertersManager.AddConverter(new GameObjectTypeConverter());
            ConvertersManager.AddConverter(new Vector2TypeConverter());
            ConvertersManager.AddConverter(new Vector3TypeConverter());
            ConvertersManager.AddConverter(new ColorTypeConverter());
            ConvertersManager.AddConverter(new QuaternionTypeConverter());
            ConvertersManager.AddConverter(new Matrix4x4TypeConverter());
            ConvertersManager.AddConverter(new LayerMaskTypeConverter());
            ConvertersManager.AddConverter(new CustomSerializeTypeConverter<object>());
        }

        private DateFormatHandling MapJsonDateFormatHandling(SerializationDateTimeHandling handling)
        {
            switch (handling) // Same as before
            {
                case SerializationDateTimeHandling.kIsoFormat: return DateFormatHandling.IsoDateFormat;
                case SerializationDateTimeHandling.kUnixTimestamp:
                    ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(space_node,
                        new LogEntry(LogLevel.kWarning,
                            $"{GetType().Name}: {SerializerName}: kTimestamp requires a custom JsonConverter<DateTime>."));
                    return DateFormatHandling.IsoDateFormat; // Fallback
                case SerializationDateTimeHandling.kCustom:
                    // Let DateFormatString handle the format with IsoDateFormat handling type
                    return DateFormatHandling.IsoDateFormat;
                default: return DateFormatHandling.IsoDateFormat;
            }
        }

        private TypeNameHandling MapJsonTypeNameHandling(SerializationTypeNameHandling handling)
        {
            switch (handling)
            {
                case SerializationTypeNameHandling.kNone:
                    return TypeNameHandling.None;
                case SerializationTypeNameHandling.kAuto:
                    return TypeNameHandling.Auto;
                case SerializationTypeNameHandling.kAll:
                    return TypeNameHandling.All;
                case SerializationTypeNameHandling.kObjects:
                    return TypeNameHandling.Objects;
                case SerializationTypeNameHandling.kArrays:
                    return TypeNameHandling.Arrays;

                default:
                    return TypeNameHandling.None;
            }
        }

        protected override void RebuildOptions()
        {
            // 1. Apply base settings (like MaxDepth, Formatting etc.)
            ApplySharedSettingsToOptions();

            // 2. Get converters from the manager
            var primary_converters = ConvertersManager.GetPrimaryConverters();
            var adapted_converters = ConvertersManager.GetAdaptedConverters(Context);

            var final_converters = new List<JsonConverter>();

            final_converters.AddRange(primary_converters);
            final_converters.AddRange(adapted_converters);

            Options.Converters = final_converters.Distinct().ToList();
        }

        public override string SerializeToText<T>(T obj)
        {
            return JsonConvert.SerializeObject(obj, Options);
        }

        public override T DeserializeFromText<T>(string text)
        {
            return JsonConvert.DeserializeObject<T>(text, Options);
        }

        public override string[] GetSupportedFileExtensions()
        {
            return new[] { ".json", ".txt", ".btwindowdata" };
        }

        protected override bool CanHandleFileContent(byte[] fileHeader)
        {
            if (!base.CanHandleFileContent(fileHeader)) return false;

            // 尝试将字节数据转换为字符串
            string content;
            try
            {
                // 尝试以UTF-8解码
                content = Encoding.UTF8.GetString(fileHeader);
            }
            catch
            {
                return false;
            }

            // 尝试解析Json
            try
            {
                // 移除前后空白
                content = content.Trim();

                // 检查是否以{或[开始(Json或数组)
                if (!(content.StartsWith("{") && content.EndsWith("}")) &&
                    !(content.StartsWith("[") && content.EndsWith("]")))
                    return false;

                // 使用System.Text.Json尝试解析
                JToken.Parse(content);
                return true;
            }
            catch (JsonReaderException)
            {
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}