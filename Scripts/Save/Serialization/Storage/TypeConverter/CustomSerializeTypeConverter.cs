using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Save.Serialization.Core.TypeConverter;
using Save.Serialization.Core.TypeConverter.SerializerAttribute;

namespace Save.Serialization.Storage.TypeConverter
{
    /// <summary>
    /// A type converter implementing custom serialization and deserialization logic for a specific type.
    /// </summary>
    /// <typeparam name="T">The type that this converter supports for custom serialization and deserialization.</typeparam>
    public class CustomSerializeTypeConverter<T> : ITypeConverter<T>
    {
        public Type[] SupportedTypes => new[] { typeof(T) };

        public bool CanConvert(Type type)
        {
            return type == typeof(T) && type.GetCustomAttribute<CustomSerializeAttribute>() != null;
        }

        public object Serialize(T value, ISerializationContext context)
        {
            if (value == null) return null;

            var type = typeof(T);
            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime))
            {
                // 处理基本类型
                return value;
            }
            else if (type.Namespace != null && type.Namespace.StartsWith("UnityEngine"))
            {
                // 处理 Unity 类型
                var serializer = context.Serializer;
                if (serializer != null)
                {
                    // 尝试查找该 Unity 类型的转换器
                    var converter = serializer.GetTypeConverterManager().GetAllConverterObjects()
                        .FirstOrDefault(c => c is ITypeConverter type_converter && type_converter.CanConvert(type));
                    if (converter != null)
                    {
                        // 交给找到的转换器处理
                        var serializeMethod = converter.GetType().GetMethod("Serialize",
                            new[] { type, typeof(ISerializationContext) });
                        if (serializeMethod != null)
                            return serializeMethod.Invoke(converter, new object[] { value, context });
                        else
                            throw new InvalidOperationException(
                                $"找到类型 {type.Name} 的转换器 {converter.GetType().Name}，但未实现正确的 Serialize 方法。");
                    }
                    else
                    {
                        throw new NotSupportedException(
                            $"Unity 类型 {type.Name} 没有对应的序列化器在格式管理器 {serializer.SerializerName} 中注册。请添加相应的 ITypeConverter。");
                    }
                }
                else
                {
                    throw new InvalidOperationException("SerializationContext 中 Serializer 为空。");
                }
            }
            else if (typeof(IDictionary).IsAssignableFrom(type))
            {
                // 处理字典类型
                var dictionary = (IDictionary)value;
                var result = new Dictionary<object, object>();
                foreach (var key in dictionary.Keys)
                    result[SerializeInternal(key, context, new HashSet<object>())] =
                        SerializeInternal(dictionary[key], context, new HashSet<object>());
                return result;
            }
            else if (typeof(IList).IsAssignableFrom(type))
            {
                // 处理列表类型
                var list = (IList)value;
                var result = new List<object>();
                foreach (var item in list) result.Add(SerializeInternal(item, context, new HashSet<object>()));
                return result;
            }
            else
            {
                // 处理自定义类型
                var result = new Dictionary<string, object>();
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (var field in fields)
                    if (field.GetCustomAttribute<NonSerializedAttribute>() == null)
                        result[field.Name] = SerializeInternal(field.GetValue(value), context, new HashSet<object>());

                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead && p.GetIndexParameters().Length == 0);
                foreach (var property in properties)
                    if (property.GetCustomAttribute<NonSerializedAttribute>() == null)
                        result[property.Name] =
                            SerializeInternal(property.GetValue(value), context, new HashSet<object>());

                return result;
            }
        }

        private object SerializeInternal(object obj, ISerializationContext context, HashSet<object> visited)
        {
            if (obj == null) return null;

            var type = obj.GetType();

            // 防止无限递归和栈溢出
            if (!visited.Add(obj))
            {
                // 可以根据 PreserveReferences 设置来决定是否抛出异常或特殊处理
                if (context.Settings.PreserveReferences)
                    // 这里可以返回一个引用标记，具体实现取决于格式管理器的处理方式
                    return new { ReferenceId = obj.GetHashCode() };
                else
                    throw new InvalidOperationException($"检测到循环引用，类型为 {type.Name}。请启用 PreserveReferences 或检查对象结构。");
            }

            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime))
            {
                return obj;
            }
            else if (type.Namespace != null && type.Namespace.StartsWith("UnityEngine"))
            {
                var serializer = context.Serializer;
                if (serializer != null)
                {
                    var converter = serializer.GetTypeConverterManager().GetAllConverterObjects()
                        .FirstOrDefault(c => c is ITypeConverter type_converter && type_converter.CanConvert(type));
                    if (converter != null)
                    {
                        var serializeMethod = converter.GetType().GetMethod("Serialize",
                            new[] { type, typeof(ISerializationContext) });
                        if (serializeMethod != null)
                            return serializeMethod.Invoke(converter, new[] { obj, context });
                        else
                            throw new InvalidOperationException(
                                $"找到类型 {type.Name} 的转换器 {converter.GetType().Name}，但未实现正确的 Serialize 方法。");
                    }
                    else
                    {
                        throw new NotSupportedException(
                            $"Unity 类型 {type.Name} 没有对应的序列化器在格式管理器 {serializer.SerializerName} 中注册。请添加相应的 ITypeConverter。");
                    }
                }
                else
                {
                    throw new InvalidOperationException("SerializationContext 中 Serializer 为空。");
                }
            }
            else if (typeof(IDictionary).IsAssignableFrom(type))
            {
                var dictionary = (IDictionary)obj;
                var result = new Dictionary<object, object>();
                foreach (var key in dictionary.Keys)
                    result[SerializeInternal(key, context, new HashSet<object>(visited))] =
                        SerializeInternal(dictionary[dictionary[key]], context, new HashSet<object>(visited));
                return result;
            }
            else if (typeof(IList).IsAssignableFrom(type))
            {
                var list = (IList)obj;
                var result = new List<object>();
                foreach (var item in list) result.Add(SerializeInternal(item, context, new HashSet<object>(visited)));
                return result;
            }
            else
            {
                var result = new Dictionary<string, object>();
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (var field in fields)
                    if (field.GetCustomAttribute<NonSerializedAttribute>() == null)
                        result[field.Name] =
                            SerializeInternal(field.GetValue(obj), context, new HashSet<object>(visited));

                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead && p.GetIndexParameters().Length == 0);
                foreach (var property in properties)
                    if (property.GetCustomAttribute<NonSerializedAttribute>() == null)
                        result[property.Name] =
                            SerializeInternal(property.GetValue(obj), context, new HashSet<object>(visited));

                return result;
            }
        }

        public T Deserialize(object data, ISerializationContext context)
        {
            if (data == null) return default;

            var type = typeof(T);
            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime))
            {
                // 处理基本类型
                return (T)Convert.ChangeType(data, type);
            }
            else if (type.Namespace != null && type.Namespace.StartsWith("UnityEngine"))
            {
                // 处理 Unity 类型
                var serializer = context.Serializer;
                if (serializer != null)
                {
                    // 尝试查找该 Unity 类型的转换器
                    var converter = serializer.GetTypeConverterManager().GetAllConverterObjects()
                        .FirstOrDefault(c => c is ITypeConverter type_converter && type_converter.CanConvert(type));
                    if (converter != null)
                    {
                        // 交给找到的转换器处理
                        var deserializeMethod = converter.GetType().GetMethod("Deserialize",
                            new[] { typeof(object), typeof(ISerializationContext) });
                        if (deserializeMethod != null)
                            return (T)deserializeMethod.Invoke(converter, new[] { data, context });
                        else
                            throw new InvalidOperationException(
                                $"找到类型 {type.Name} 的转换器 {converter.GetType().Name}，但未实现正确的 Deserialize 方法。");
                    }
                    else
                    {
                        throw new NotSupportedException(
                            $"Unity 类型 {type.Name} 没有对应的反序列化器在格式管理器 {serializer.SerializerName} 中注册。请添加相应的 ITypeConverter。");
                    }
                }
                else
                {
                    throw new InvalidOperationException("SerializationContext 中 Serializer 为空。");
                }
            }
            else if (typeof(IDictionary).IsAssignableFrom(type))
            {
                // 处理字典类型
                var dictionaryType = typeof(T);
                var keyType = dictionaryType.GetGenericArguments()[0];
                var valueType = dictionaryType.GetGenericArguments()[1];
                if (data is not IDictionary dataDictionary)
                    throw new InvalidCastException($"无法将数据转换为字典类型 {type.Name}。");
                var result = (IDictionary)Activator.CreateInstance(dictionaryType);
                foreach (var key in dataDictionary.Keys)
                    result[DeserializeInternal(key, keyType, context, new HashSet<object>())] =
                        DeserializeInternal(dataDictionary[key], valueType, context, new HashSet<object>());
                return (T)result;
            }
            else if (typeof(IList).IsAssignableFrom(type))
            {
                // 处理列表类型
                var listType = typeof(T);
                var elementType = listType.GetGenericArguments()[0];
                if (data is not IList dataList)
                    throw new InvalidCastException($"无法将数据转换为列表类型 {type.Name}。");
                var result = (IList)Activator.CreateInstance(listType);
                foreach (var itemData in dataList)
                    result.Add(DeserializeInternal(itemData, elementType, context, new HashSet<object>()));
                return (T)result;
            }
            else
            {
                // 处理自定义类型
                var instance = Activator.CreateInstance(type);
                if (data is not Dictionary<string, object> dataDictionary)
                    throw new InvalidCastException($"无法将数据转换为类型 {type.Name}。");
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (var field in fields)
                    if (dataDictionary.TryGetValue(field.Name, out var value))
                        field.SetValue(instance,
                            DeserializeInternal(value, field.FieldType, context, new HashSet<object>()));

                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanWrite && p.GetIndexParameters().Length == 0);
                foreach (var property in properties)
                    if (dataDictionary.TryGetValue(property.Name, out var value))
                        property.SetValue(instance,
                            DeserializeInternal(value, property.PropertyType, context, new HashSet<object>()));

                return (T)instance;
            }
        }

        private object DeserializeInternal(object data, Type targetType, ISerializationContext context,
            HashSet<object> visited)
        {
            if (data == null) return null;

            if (targetType.IsPrimitive || targetType == typeof(string) || targetType == typeof(decimal) ||
                targetType == typeof(DateTime))
            {
                return Convert.ChangeType(data, targetType);
            }
            else if (targetType.Namespace != null && targetType.Namespace.StartsWith("UnityEngine"))
            {
                var serializer = context.Serializer;
                if (serializer != null)
                {
                    var converter = serializer.GetTypeConverterManager().GetAllConverterObjects().FirstOrDefault(c =>
                        c is ITypeConverter type_converter && type_converter.CanConvert(targetType));
                    if (converter != null)
                    {
                        var deserializeMethod = converter.GetType().GetMethod("Deserialize",
                            new[] { typeof(object), typeof(ISerializationContext) });
                        if (deserializeMethod != null)
                            return deserializeMethod.Invoke(converter, new[] { data, context });
                        else
                            throw new InvalidOperationException(
                                $"找到类型 {targetType.Name} 的转换器 {converter.GetType().Name}，但未实现正确的 Deserialize 方法。");
                    }
                    else
                    {
                        throw new NotSupportedException(
                            $"Unity 类型 {targetType.Name} 没有对应的反序列化器在格式管理器 {serializer.SerializerName} 中注册。请添加相应的 ITypeConverter。");
                    }
                }
                else
                {
                    throw new InvalidOperationException("SerializationContext 中 Serializer 为空。");
                }
            }
            else if (typeof(IDictionary).IsAssignableFrom(targetType))
            {
                var keyType = targetType.GetGenericArguments()[0];
                var valueType = targetType.GetGenericArguments()[1];
                if (data is not IDictionary dataDictionary)
                    throw new InvalidCastException($"无法将数据转换为字典类型 {targetType.Name}。");
                var result = (IDictionary)Activator.CreateInstance(targetType);
                foreach (var key in dataDictionary.Keys)
                    result[DeserializeInternal(key, keyType, context, new HashSet<object>(visited))] =
                        DeserializeInternal(dataDictionary[key], valueType, context, new HashSet<object>(visited));
                return result;
            }
            else if (typeof(IList).IsAssignableFrom(targetType))
            {
                var elementType = targetType.GetGenericArguments()[0];
                if (data is not IList dataList)
                    throw new InvalidCastException($"无法将数据转换为列表类型 {targetType.Name}。");
                var result = (IList)Activator.CreateInstance(targetType);
                foreach (var itemData in dataList)
                    result.Add(DeserializeInternal(itemData, elementType, context, new HashSet<object>(visited)));
                return result;
            }
            else
            {
                var instance = Activator.CreateInstance(targetType);
                if (data is not Dictionary<string, object> dataDictionary)
                    // 尝试将数据直接转换为目标类型，例如枚举
                    try
                    {
                        return Convert.ChangeType(data, targetType);
                    }
                    catch (InvalidCastException)
                    {
                        throw new InvalidCastException($"无法将数据转换为类型 {targetType.Name}。");
                    }

                var fields = targetType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (var field in fields)
                    if (dataDictionary.TryGetValue(field.Name, out var value))
                        field.SetValue(instance,
                            DeserializeInternal(value, field.FieldType, context, new HashSet<object>(visited)));

                var properties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanWrite && p.GetIndexParameters().Length == 0);
                foreach (var property in properties)
                    if (dataDictionary.TryGetValue(property.Name, out var value))
                        property.SetValue(instance,
                            DeserializeInternal(value, property.PropertyType, context, new HashSet<object>(visited)));

                return instance;
            }
        }
    }
}
