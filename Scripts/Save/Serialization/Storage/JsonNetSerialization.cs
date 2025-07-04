using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Script.Save.Serialization;
using UnityEngine;

namespace Save.Serialization.Storage
{
    public class JsonNetSerializationContext : ISerializationContext
    {
        public JsonNetSerializationContext(ISerializer serializer, SerializationSettings settings = null)
        {
            Serializer = serializer;
            Settings = settings ?? new SerializationSettings();
        }

        public void UpdateSettings(SerializationSettings settings)
        {
            Settings = settings ?? new SerializationSettings();
        }

        public ISerializer Serializer { get; }
        public SerializationSettings Settings { get; private set; }
    }

    /// <summary>
    /// Provides custom logic for serialization and deserialization of property values by utilizing a specified
    /// <see cref="JsonConverter"/>.
    /// </summary>
    /// <remarks>
    /// This class implements the <see cref="IValueProvider"/> interface to define custom behavior for getting
    /// and setting property values during JSON serialization and deserialization. It allows a specific
    /// converter to handle the transformation of property values to and from JSON tokens.
    /// </remarks>
    /// <threadsafety>
    /// Instances of this class are not guaranteed to be thread-safe.
    /// </threadsafety>
    public class CustomPropertyValueProvider : IValueProvider
    {
        private readonly PropertyInfo property_;
        private readonly JsonConverter converter_;

        public CustomPropertyValueProvider(PropertyInfo property, JsonConverter converter)
        {
            property_ = property;
            converter_ = converter;
        }

        public void SetValue(object target, object value)
        {
            // 使用转换器进行自定义反序列化
            if (converter_ != null && value is JToken token)
            {
                using var reader = token.CreateReader();
                var context = JsonSerializer.CreateDefault().Context;
                var serializer = new JsonSerializer { Context = context };
                var converted_value = converter_.ReadJson(reader, property_.PropertyType,
                    property_.GetValue(target), serializer);
                property_.SetValue(target, converted_value);
                return;
            }

            // 默认设置
            property_.SetValue(target, value);
        }

        public object GetValue(object target)
        {
            var value = property_.GetValue(target);

            // 使用转换器进行自定义序列化
            if (converter_ != null && converter_.CanConvert(property_.PropertyType))
            {
                // 需要在这里使用间接方法，因为GetValue需要返回值而非写入
                var token_writer = new JTokenWriter();
                using var writer = token_writer;
                converter_.WriteJson(writer, value, new JsonSerializer());
                return token_writer.Token;
            }

            return value;
        }
    }

    /// <summary>
    /// A custom contract resolver for handling specific serialization and deserialization behavior
    /// within a behavior tree structure, extending the <see cref="DefaultContractResolver"/>.
    /// </summary>
    /// <remarks>
    /// This class customizes JSON serialization by performing the following actions:
    /// - Skipping serialization for fields marked with the <see cref="NonSerializedAttribute"/>.
    /// - Handling properties with custom serialization attributes by utilizing specified JSON converters.
    /// </remarks>
    /// <threadsafety>
    /// Instances of this class are not guaranteed to be thread-safe.
    /// </threadsafety>
    public class BehaviorTreeContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            // 跳过被标记为[NonSerialized]的字段
            if (member is FieldInfo { IsNotSerialized: true })
            {
                property.ShouldDeserialize = _ => false;
                property.Ignored = true;
            }

            // 处理自定义序列化属性
            var custom_serialize_attr = member.GetCustomAttribute<CustomSerializeAttribute>();
            if (custom_serialize_attr is { ConverterType: not null })
                // 创建转换器实例
                if (Activator.CreateInstance(custom_serialize_attr.ConverterType) is JsonConverter converter)
                    if (member is PropertyInfo propertyInfo)
                        property.ValueProvider = new CustomPropertyValueProvider(propertyInfo, converter);

            return property;
        }
    }

    public static class JsonDataConverter
    {
        // 将JToken转换为通用对象格式
        public static object ConvertJTokenToGeneric(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    var dict = new Dictionary<string, object>();
                    foreach (var kvp in (JObject)token)
                    {
                        dict[kvp.Key] = ConvertJTokenToGeneric(kvp.Value);
                    }
                    return dict;

                case JTokenType.Array:
                    var list = new List<object>();
                    foreach (var item in (JArray)token)
                    {
                        list.Add(ConvertJTokenToGeneric(item));
                    }
                    return list;

                case JTokenType.Integer:
                    return token.Value<int>();

                case JTokenType.Float:
                    return token.Value<double>();

                case JTokenType.String:
                    return token.Value<string>();

                case JTokenType.Boolean:
                    return token.Value<bool>();

                case JTokenType.Date:
                    return token.Value<DateTime>();

                case JTokenType.Null:
                    return null;
                default:
                    return token.ToString();
            }
        }

        // 将JObject转换为字典
        public static Dictionary<string,object> ConvertJObjectToDictionary(JObject j_object)
        {
            var dict= new Dictionary<string, object>();
            foreach(var property in j_object.Properties())
            {
                dict[property.Name] = ConvertJTokenToGeneric(property.Value);
            }

            return dict;
        }

        // 将JObject转换为列表
        public static List<object> ConvertJArrayToList(JArray j_array)
        {
            var list = new List<object>();
            foreach (var item in j_array)
            {
                list.Add(ConvertJTokenToGeneric(item));
            }
            return list;
        }
    }

    public class JsonNetTypeConverterAdapter<T> : JsonConverter
    {
        private readonly ITypeConverter<T> converter_;
        private readonly ISerializationContext context_;

        private readonly HashSet<object> objects_being_processed_=new HashSet<object>();
        private readonly int MaxDepth = 100;
        private int current_depth_ = 0;

        public JsonNetTypeConverterAdapter(ITypeConverter<T> converter, ISerializationContext context)
        {
            converter_ = converter;
            context_ = context;

            if (context_?.Settings!=null)
            {
                MaxDepth = context_.Settings.MaxDepth;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // 深度检查
            current_depth_++;
            if (current_depth_ > MaxDepth)
            {
                Debug.LogError($"序列化深度超过限制({MaxDepth})，可能存在循环引用");
                writer.WriteNull();
                current_depth_--;
                return;
            }

            // 循环引用检查
            if (value!=null&&!objects_being_processed_.Add(value))
            {
                Debug.LogWarning($"检测到循环引用: {value.GetType().Name}");
                writer.WriteNull();
                current_depth_--;
                return;
            }

            try
            {
                // 确保值类型正确
                if (value is not T typed_value)
                {
                    writer.WriteNull();
                    return;
                }

                // 使用泛型转换器进行序列化
                var result = converter_.Serialize(typed_value, context_);

                // 如果返回null，就写入null
                if (result == null)
                {
                    writer.WriteNull();
                    return;
                }

                // 将中间格式转换为Json
                if (result is Dictionary<string, object> dict)
                {
                    writer.WriteStartObject();
                    foreach (var kvp in dict)
                    {
                        writer.WritePropertyName(kvp.Key);

                        // 直接写入原始值，避免递归调用
                        WriteValue(writer, kvp.Value, serializer);
                    }

                    writer.WriteEndObject();
                }
                else
                {
                    // 直接写入值
                    WriteValue(writer, result, serializer);
                }
            }
            finally
            {
                if (value!=null)
                {
                    objects_being_processed_.Remove(value);
                }
                current_depth_--;
            }
            
        }

        private void WriteValue(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
            }
            else if (value is string strValue)
            {
                writer.WriteValue(strValue);
            }
            else if (value is int intValue)
            {
                writer.WriteValue(intValue);
            }
            else if (value is long longValue)
            {
                writer.WriteValue(longValue);
            }
            else if (value is float floatValue)
            {
                writer.WriteValue(floatValue);
            }
            else if (value is double doubleValue)
            {
                writer.WriteValue(doubleValue);
            }
            else if (value is bool boolValue)
            {
                writer.WriteValue(boolValue);
            }
            else if (value is DateTime dateTimeValue)
            {
                writer.WriteValue(dateTimeValue);
            }
            else if (value is Dictionary<string, object> dictValue)
            {
                writer.WriteStartObject();
                foreach (var kvp in dictValue)
                {
                    writer.WritePropertyName(kvp.Key);
                    WriteValue(writer, kvp.Value, serializer);
                }
                writer.WriteEndObject();
            }
            else if (value is IList listValue)
            {
                writer.WriteStartArray();
                foreach (var item in listValue)
                {
                    WriteValue(writer, item, serializer);
                }
                writer.WriteEndArray();
            }
            else
            {
                // 对于其他类型，尝试使用JToken
                try
                {
                    var token = JToken.FromObject(value, serializer);
                    token.WriteTo(writer);
                }
                catch
                {
                    // 最后的备选方案：转为字符串
                    writer.WriteValue(value.ToString());
                }
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // 深度检查
            current_depth_++;
            if (current_depth_ > MaxDepth)
            {
                Debug.LogError($"反序列化深度超过限制({MaxDepth})，可能存在循环引用");
                current_depth_--;
                return null;
            }

            try
            {
                if (reader.TokenType == JsonToken.Null)
                {
                    return null;
                }

                // 从Json读取数据
                JToken token = JToken.ReadFrom(reader);

                // 将JToken转换为通用对象格式
                object generic_data;
                if (token is JObject j_object)
                {
                    generic_data = JsonDataConverter.ConvertJObjectToDictionary(j_object);
                }
                else if (token is JArray j_array)
                {
                    generic_data = JsonDataConverter.ConvertJArrayToList(j_array);
                }
                else
                {
                    generic_data = JsonDataConverter.ConvertJTokenToGeneric(token);
                }
                
                return converter_.Deserialize(generic_data, context_);
            }
            finally
            {
                current_depth_--;
            }
            
        }

        private object ReadValue(JsonReader reader, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.Boolean:
                    return reader.Value != null && (bool)reader.Value;
                case JsonToken.Integer:
                    return Convert.ToInt64(reader.Value);
                case JsonToken.Float:
                    return Convert.ToDouble(reader.Value);
                case JsonToken.Date:
                case JsonToken.String:
                    return reader.Value;
                case JsonToken.StartObject:
                    return ReadObject(reader, serializer);
                case JsonToken.StartArray:
                    return ReadArray(reader, serializer);
                default:
                    return reader.Value;
            }
        }

        private Dictionary<string,object> ReadObject(JsonReader reader, JsonSerializer serializer)
        {
            var dict = new Dictionary<string, object>();

            while (reader.Read())
            {
                if (reader.TokenType==JsonToken.EndObject)
                {
                    break;
                }

                if (reader.TokenType!=JsonToken.PropertyName)
                {
                    continue;
                }

                string property_name = (string)reader.Value;

                
                // 读取下一个token
                reader.Read();

                // 递归读取值
                var value = ReadValue(reader, serializer);
                if (property_name != null) dict[property_name] = value;
            }

            return dict;
        }

        private List<object> ReadArray(JsonReader reader, JsonSerializer serializer)
        {
            var list = new List<object>();

            while (reader.Read())
            {
                if (reader.TokenType==JsonToken.EndArray)
                {
                    break;
                }

                // 递归读取数组元素
                var value = ReadValue(reader, serializer);
                list.Add(value);
            }

            return list;
        }

        public override bool CanConvert(Type objectType)
        {
            return converter_.CanConvert(objectType);
        }
    }
}