using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExTools.Utillties;
using Script.Save.Serialization;
using Script.Utillties;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Save.Serialization
{
    /// <summary>
    /// 自定义序列化特性，用于标记需要特殊处理的字段或属性.一般来说其不会处理NonSerialize标记的属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, Inherited = false)]
    public class CustomSerializeAttribute : Attribute
    {
        public Type ConverterType { get; } = null;
    }

    /// <summary>
    /// An attribute used to designate the serialization type for a specific class, structure, or enumeration.
    /// This enables associating a serializer implementation with a given <see cref="SerializerType"/>.
    /// </summary>
    public class SerializerTypeAttribute : Attribute
    {
        public SerializerType Type { get; }

        public SerializerTypeAttribute(SerializerType type)
        {
            Type = type;
        }
    }

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

    public class GameObjectTypeConverter : ITypeConverter<GameObject>
    {
        public Type[] SupportedTypes => new[] { typeof(GameObject) };

        public bool CanConvert(Type type)
        {
            return type == typeof(GameObject);
        }

        public object Serialize(GameObject value, ISerializationContext context)
        {
            if (!value) return null;

            // 创建一个通用的字典表示GameObject
            var result = new Dictionary<string, object>
            {
                ["Path"] = GetFullPath(value),
                ["InstanceID"] = value.GetInstanceID()
            };

            return result;
        }

        private object GetFullPath(GameObject value)
        {
            var path = value.name;
            var parent = value.transform.parent;

            while (parent)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        public GameObject Deserialize(object data, ISerializationContext context)
        {
            if (data == null) return null;

            // 假设data是Dictionary<string,object>
            var dict = data as Dictionary<string, object>;
            if (dict == null) return null;

            // 首先尝试通过InstanceID查找
            var instance_id = 0;
            if (dict.TryGetValue("InstanceID", out var id_obj)) instance_id = Convert.ToInt32(id_obj);

            if (instance_id != 0)
            {
#if UNITY_EDITOR
                var obj = UnityEditor.EditorUtility.InstanceIDToObject(instance_id);
                if (obj) return obj as GameObject;
#endif
            }

            // 尝试通过路径查找
            string path = null;
            if (dict.TryGetValue("Path", out var path_obj)) path = path_obj as string;

            if (!string.IsNullOrEmpty(path)) return GameObject.Find(path);

            return null;
        }
    }

    public class GameObjectTypeConverters : ITypeConverter<GameObject>
    {
        public Type[] SupportedTypes => new[] { typeof(GameObject) };

        public bool CanConvert(Type type)
        {
            return type == typeof(GameObject);
        }

        public object Serialize(GameObject value, ISerializationContext context)
        {
            if (!value)
                return null;

            // 创建一个通用的字典表示GameObject
            var result = new Dictionary<string, object>
            {
                ["name"] = value.name,
                ["tag"] = value.tag,
                ["layer"] = value.layer,
                ["activeInHierarchy"] = value.activeInHierarchy
            };

            // 存储场景信息
            var scene = value.scene;
            result["sceneName"] = scene.name;
            result["scenePath"] = scene.path;

            // 存储路径信息
            var hierarchyPath = GetGameObjectPath(value);
            result["hierarchyPath"] = hierarchyPath;

            return result;
        }

        public GameObject Deserialize(object data, ISerializationContext context)
        {
            // 假设data是Dictionary<string,object>
            var dict = data as Dictionary<string, object>;
            if (dict == null) return null;

            // 提取值
            var name = dict.TryGetValue("name", out var nameObj) ? Convert.ToString(nameObj) : "";
            var tag = dict.TryGetValue("tag", out var tagObj) ? Convert.ToString(tagObj) : "Untagged";
            var layer = dict.TryGetValue("layer", out var layerObj) ? Convert.ToInt32(layerObj) : 0;
            var activeInHierarchy = !dict.TryGetValue("activeInHierarchy", out var activeObj) || Convert.ToBoolean(activeObj);
            var hierarchyPath = dict.TryGetValue("hierarchyPath", out var pathObj) ? Convert.ToString(pathObj) : "";
            var sceneName = dict.TryGetValue("sceneName", out var sceneNameObj) ? Convert.ToString(sceneNameObj) : "";

            // 根据层级路径查找GameObject
            var result = FindGameObjectByPath(hierarchyPath, sceneName);

            // 如果找到了对象，我们可以验证一些属性
            if (result)
            {
                // 可选：更新找到的对象以匹配序列化时的状态
                if (!result.CompareTag(tag) && !string.IsNullOrEmpty(tag))
                    result.tag = tag;

                if (result.layer != layer)
                    result.layer = layer;

                if (result.activeInHierarchy != activeInHierarchy)
                    result.SetActive(activeInHierarchy);
            }

            return result;
        }

        // 获取GameObject的完整路径（包括所有父对象）
        private string GetGameObjectPath(GameObject obj)
        {
            if (!obj)
                return "";

            var path = obj.name;
            var parent = obj.transform.parent;

            while (parent)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        // 根据路径查找GameObject
        private GameObject FindGameObjectByPath(string path, string sceneName)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            // 先尝试在当前场景中查找
            var foundObj = GameObject.Find(path);
            if (foundObj)
                return foundObj;

            // 如果没找到，并且提供了场景名称，尝试在特定场景中查找
            if (!string.IsNullOrEmpty(sceneName))
            {
                // 遍历场景中的根对象
                var targetScene = SceneManager.GetSceneByName(sceneName);
                if (targetScene.IsValid())
                {
                    var pathParts = path.Split('/');
                    var rootObjects = targetScene.GetRootGameObjects();

                    foreach (var rootObj in rootObjects)
                        if (rootObj.name == pathParts[0])
                        {
                            // 找到了匹配的根对象，现在遍历子路径
                            var result = TraverseHierarchy(rootObj, pathParts, 1);
                            if (result)
                                return result;
                        }
                }
            }

            return null;
        }

        // 递归遍历层级结构来查找指定路径的GameObject
        private GameObject TraverseHierarchy(GameObject current, string[] pathParts, int index)
        {
            if (index >= pathParts.Length)
                return current;

            var child = current.transform.Find(pathParts[index]);
            if (child)
                return TraverseHierarchy(child.gameObject, pathParts, index + 1);

            return null;
        }
    }

    public class Vector2TypeConverter : ITypeConverter<Vector2>
    {
        public Type[] SupportedTypes => new[] { typeof(Vector2) };

        public bool CanConvert(Type type)
        {
            return type == typeof(Vector2);
        }

        public object Serialize(Vector2 value, ISerializationContext context)
        {
            // 创建一个通用的字典表示Vector2
            var result = new Dictionary<string, object>
            {
                ["x"] = value.x,
                ["y"] = value.y
            };

            return result;
        }

        public Vector2 Deserialize(object data, ISerializationContext context)
        {
            // 假设data是Dictionary<string,object>
            var dict = data as Dictionary<string, object>;
            if (dict == null) return Vector2.zero;

            var x = 0f;
            var y = 0f;

            if (dict.TryGetValue("x", out var x_obj)) x = Convert.ToSingle(x_obj);

            if (dict.TryGetValue("y", out var y_obj)) y = Convert.ToSingle(y_obj);

            return new Vector2(x, y);
        }
    }

    public class Vector3TypeConverter : ITypeConverter<Vector3>
    {
        public Type[] SupportedTypes => new[] { typeof(Vector3) };

        public bool CanConvert(Type type)
        {
            return type == typeof(Vector3);
        }

        public object Serialize(Vector3 value, ISerializationContext context)
        {
            // 创建一个通用的字典表示Vector3
            var result = new Dictionary<string, object>
            {
                ["x"] = value.x,
                ["y"] = value.y,
                ["z"] = value.z
            };

            return result;
        }

        public Vector3 Deserialize(object data, ISerializationContext context)
        {
            // 假设data是Dictionary<string,object>
            var dict = data as Dictionary<string, object>;
            if (dict == null) return Vector3.zero;

            var x = 0f;
            var y = 0f;
            var z = 0f;

            if (dict.TryGetValue("x", out var x_obj)) x = Convert.ToSingle(x_obj);

            if (dict.TryGetValue("y", out var y_obj)) y = Convert.ToSingle(y_obj);

            if (dict.TryGetValue("z", out var z_obj)) z = Convert.ToSingle(z_obj);

            return new Vector3(x, y, z);
        }
    }

    public class QuaternionTypeConverter : ITypeConverter<Quaternion>
    {
        public Type[] SupportedTypes => new[] { typeof(Quaternion) };

        public bool CanConvert(Type type)
        {
            return type == typeof(Quaternion);
        }


        public object Serialize(Quaternion value, ISerializationContext context)
        {
            var result = new Dictionary<string, object>
            {
                ["x"] = value.x,
                ["y"] = value.y,
                ["z"] = value.z,
                ["w"] = value.w
            };

            return result;
        }

        public Quaternion Deserialize(object data, ISerializationContext context)
        {
            if (data is not Dictionary<string, object> dict) return default;

            float x = 0;
            float y = 0;
            float z = 0;
            float w = 0;

            if (dict.TryGetValue("x", out var x_obj)) x = Convert.ToSingle(x_obj);

            if (dict.TryGetValue("y", out var y_obj)) y = Convert.ToSingle(y_obj);

            if (dict.TryGetValue("z", out var z_obj)) z = Convert.ToSingle(z_obj);

            if (dict.TryGetValue("w", out var w_obj)) w = Convert.ToSingle(w_obj);

            return new Quaternion(x, y, z, w);
        }
    }

    public class ColorTypeConverter : ITypeConverter<Color>
    {
        public Type[] SupportedTypes => new[] { typeof(Color) };

        public bool CanConvert(Type type)
        {
            return type == typeof(Color);
        }

        public object Serialize(Color value, ISerializationContext context)
        {
            return new Dictionary<string, object>
                { ["r"] = value.r, ["g"] = value.g, ["b"] = value.b, ["a"] = value.a };
        }

        public Color Deserialize(object data, ISerializationContext context)
        {
            var dict = data as Dictionary<string, object>;

            return dict == null
                ? Color.black
                : new Color(Convert.ToSingle(dict["r"]), Convert.ToSingle(dict["g"]), Convert.ToSingle(dict["b"]),
                    Convert.ToSingle(dict["a"]));
        }
    }

    public class UnityEventConverter : ITypeConverter<UnityEvent>
    {
        public Type[] SupportedTypes => new[] { typeof(UnityEvent) };

        public bool CanConvert(Type type)
        {
            return type == typeof(UnityEvent);
        }

        public object Serialize(UnityEvent value, ISerializationContext context)
        {
            if (value == null) return null;

            var result = new Dictionary<string, object>();
            var listeners = new List<Dictionary<string, object>>();

            // 获取持久化调用列表
            var persistent_calls_field = typeof(UnityEventBase)?.GetField("m_PersistentCalls",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var persistent_calls = persistent_calls_field?.GetValue(value);
            // 获取调用列表
            var calls_field = persistent_calls?.GetType()
                .GetField("m_Calls", BindingFlags.Instance | BindingFlags.NonPublic);
            try
            {
                // 序列化每个监听器
                if (calls_field?.GetValue(persistent_calls) is IList calls)
                    foreach (var call in calls)
                    {
                        var listener = new Dictionary<string, object>();
                        var call_type = call.GetType();

                        // 获取目标对象
                        var target_fields = call.GetType()
                            .GetField("m_Targets", BindingFlags.Instance | BindingFlags.NonPublic);
                        var target = target_fields?.GetValue(call) as UnityEngine.Object;
                        listener["targetId"] = target ? target.GetInstanceID() : 0;

                        // 获取方法名
                        var method_name_field = call_type.GetField("m_MethodName",
                            BindingFlags.Instance | BindingFlags.NonPublic);
                        listener["methodName"] = (string)method_name_field?.GetValue(call);

                        // 获取调用模式
                        var mode_field = call_type.GetField("m_Mode", BindingFlags.Instance | BindingFlags.NonPublic);
                        listener["mode"] = (int)mode_field?.GetValue(call);
                    }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return null;
        }

        public UnityEvent Deserialize(object data, ISerializationContext context)
        {
            throw new NotImplementedException();
        }
    }

    public class Matrix4x4TypeConverter : ITypeConverter<Matrix4x4>
    {
        public Type[] SupportedTypes => new[] { typeof(Matrix4x4) };

        public bool CanConvert(Type type)
        {
            return type == typeof(Matrix4x4);
        }

        public object Serialize(Matrix4x4 value, ISerializationContext context)
        {
            return new Dictionary<string, object>
            {
                ["m00"] = value.m00, ["m01"] = value.m01, ["m02"] = value.m02, ["m03"] = value.m03,
                ["m10"] = value.m10, ["m11"] = value.m11, ["m12"] = value.m12, ["m13"] = value.m13,
                ["m20"] = value.m20, ["m21"] = value.m21, ["m22"] = value.m22, ["m23"] = value.m23,
                ["m30"] = value.m30, ["m31"] = value.m31, ["m32"] = value.m32, ["m33"] = value.m33
            };
        }

        public Matrix4x4 Deserialize(object data, ISerializationContext context)
        {
            return data is not Dictionary<string, object> dict
                ? Matrix4x4.zero
                : new Matrix4x4(
                    new Vector4(Convert.ToSingle(dict["m00"]), Convert.ToSingle(dict["m01"]),
                        Convert.ToSingle(dict["m02"]), Convert.ToSingle(dict["m03"])),
                    new Vector4(Convert.ToSingle(dict["m10"]), Convert.ToSingle(dict["m11"]),
                        Convert.ToSingle(dict["m12"]), Convert.ToSingle(dict["m13"])),
                    new Vector4(Convert.ToSingle(dict["m20"]), Convert.ToSingle(dict["m21"]),
                        Convert.ToSingle(dict["m22"]), Convert.ToSingle(dict["m23"])),
                    new Vector4(Convert.ToSingle(dict["m30"]), Convert.ToSingle(dict["m31"]),
                        Convert.ToSingle(dict["m32"])
                        , Convert.ToSingle(dict["m33"])));
        }
    }

    public class LayerMaskTypeConverter : ITypeConverter<LayerMask>
    {
        public Type[] SupportedTypes => new[] { typeof(LayerMask) };

        public bool CanConvert(Type type)
        {
            return type == typeof(LayerMask);
        }

        public object Serialize(LayerMask value, ISerializationContext context)
        {
            var result = new Dictionary<string, object>
            {
                ["value"] = value.value
            };

            var layers = new List<string>();
            for (var i = 0; i < 32; i++)
                if (((1 << i) & value.value) != 0)
                    layers.Add(LayerMask.LayerToName(i));

            result["layers"] = layers;
            return result;
        }

        public LayerMask Deserialize(object data, ISerializationContext context)
        {
            var dict = data as Dictionary<string, object>;

            return dict == null
                ? new LayerMask()
                : Convert.ToInt32(dict["value"]);
        }
    }
}