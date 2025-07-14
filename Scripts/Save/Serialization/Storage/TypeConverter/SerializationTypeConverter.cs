using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExTools.Utillties;
using Save.Serialization.Core;
using Save.Serialization.Core.TypeConverter;
using Script.Save.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Save.Serialization
{
    public class GameObjectTypeConverter : ITypeConverter<GameObject>
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