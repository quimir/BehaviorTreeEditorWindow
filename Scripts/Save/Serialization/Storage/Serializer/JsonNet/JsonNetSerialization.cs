using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Save.Serialization.Core.TypeConverter;
using UnityEngine;

namespace Save.Serialization.Storage.Serializer.JsonNet
{
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