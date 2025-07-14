using System;
using System.Collections.Generic;
using System.Linq;
using Save.Serialization.Core.TypeConverter;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Save.Serialization.Storage.TypeConverter
{
    /// <summary>
    /// Represents a type converter for serializing and deserializing Unity components of a specific type.
    /// </summary>
    /// <typeparam name="T">The type of Unity component to be handled by this converter. Must inherit from
    /// UnityEngine.Component.</typeparam>
    public class ComponentTypeConverter<T> : ITypeConverter<T> where T : Component
    {
        public Type[] SupportedTypes => new[] { typeof(T) };
        public bool CanConvert(Type type)
        {
            return typeof(T).IsAssignableFrom(type);
        }

        public virtual object Serialize(T value, ISerializationContext context)
        {
            if (!value)
            {
                return null;
            }

            var game_object = value.gameObject;
            var components=game_object.GetComponents<T>();
            
            // 查找当前组件在同类型组件中的索引
            int component_index = -1;
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i]==value)
                {
                    component_index = i;
                    break;
                }
            }

            var result = new Dictionary<string, object>()
            {
                ["gameObjectName"] = game_object.name,
                ["gameObjectTag"] = game_object.tag,
                ["componentType"] = typeof(T).AssemblyQualifiedName,
                ["componentIndex"] = component_index,
                ["hierarchyPath"] = GetGameObjectPath(game_object),
                ["sceneName"] = game_object.scene.name,
                ["scenePath"] = game_object.scene.path,
                ["instanceId"] = value.GetInstanceID() // 用于额外验证
            };

            return result;
        }

        /// <summary>
        /// Gets the hierarchical path of the specified GameObject starting from the root.
        /// </summary>
        /// <param name="gameObject">The GameObject for which the hierarchical path should be retrieved. Must not be
        /// null.</param>
        /// <returns>The hierarchical path of the GameObject in the format "Parent/Child/Object". Returns an empty
        /// string if the GameObject is null.</returns>
        private string GetGameObjectPath(GameObject gameObject)
        {
            if (!gameObject)
            {
                return "";
            }
            
            var path=gameObject.name;
            var parent=gameObject.transform.parent;

            while (parent)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        public virtual T Deserialize(object data, ISerializationContext context)
        {
            var dict=data as Dictionary<string,object>;
            if (dict==null)
            {
                return null;
            }
            
            // 提取基本信息
            var gameObjectName = dict.TryGetValue("gameObjectName", out var nameObj) ? Convert.ToString(nameObj) : "";
            var hierarchyPath = dict.TryGetValue("hierarchyPath", out var pathObj) ? Convert.ToString(pathObj) : "";
            var sceneName = dict.TryGetValue("sceneName", out var sceneNameObj) ? Convert.ToString(sceneNameObj) : "";
            var componentIndex = dict.TryGetValue("componentIndex", out var indexObj) ? Convert.ToInt32(indexObj) : 0;
            var instanceId = dict.TryGetValue("instanceId", out var idObj) ? Convert.ToInt32(idObj) : 0;

            var game_object = FindGameObjectByPath(hierarchyPath, sceneName);
            if (!game_object)
            {
                Debug.LogWarning($"无法找到GameObject: {hierarchyPath} in scene: {sceneName}");
                return null;
            }
            
            // 查找组件
            var components = game_object.GetComponents<T>();
            if (components.Length==0)
            {
                Debug.LogWarning($"GameObject {game_object.name} 上没有找到类型为 {typeof(T).Name} 的组件");
                return null;
            }
            
            // 优先通过InstanceID查找（最精准）
            foreach (var component in components)
            {
                if (component.GetInstanceID()==instanceId)
                {
                    return component;
                }
            }
            
            // 如果InstanceId不匹配，使用索引查找
            if (componentIndex>=0 && componentIndex<components.Length)
            {
                return components[componentIndex];
            }
            
            // 如果只有一个组件，直接返回
            if (components.Length==1)
            {
                return components[0];
            }

            Debug.LogWarning("无法确定正确的组件实例，返回第一个找到的组件");
            return components[0];
        }

        /// <summary>
        /// Finds and returns a GameObject by its hierarchy path and optional specific scene.
        /// </summary>
        /// <param name="path">The hierarchical path of the GameObject (e.g., "Parent/Child/Object").</param>
        /// <param name="sceneName">The name of the scene to search in, if specified. If null or empty, only the
        /// current scene is searched.</param>
        /// <returns>The found GameObject if it exists, otherwise null.</returns>
        private GameObject FindGameObjectByPath(string path, string sceneName)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }
            
            // 先尝试在当前场景中查找
            var foundObj = GameObject.Find(path);
            if (foundObj)
            {
                return foundObj;
            }
            
            // 如果没找到，并且提供了场景名称，尝试在特定场景中查找
            if (!string.IsNullOrEmpty(sceneName))
            {
                var target_scene = SceneManager.GetSceneByName(sceneName);
                if (target_scene.IsValid())
                {
                    var pathParts = path.Split('/');
                    var rootObjects = target_scene.GetRootGameObjects();

                    foreach (var root_object in rootObjects)
                    {
                        if (root_object.name==pathParts[0])
                        {
                            var result=TraverseHierarchy(root_object, pathParts, 1);
                            if (result)
                            {
                                return result;
                            }
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Traverses the GameObject hierarchy based on a specified path and returns the corresponding GameObject.
        /// </summary>
        /// <param name="current">The current GameObject to start searching from in the hierarchy.</param>
        /// <param name="path_parts">An array of strings representing the hierarchy path segments.</param>
        /// <param name="index">The current index in the path_parts array to match with the hierarchy.</param>
        /// <returns>The matching GameObject if found, otherwise null.</returns>
        private GameObject TraverseHierarchy(GameObject current, string[] path_parts, int index)
        {
            if (index >= path_parts.Length)
            {
                return current;
            }
            
            var child=current.transform.Find(path_parts[index]);
            if(child)
                return TraverseHierarchy(child.gameObject, path_parts, index+1);

            return null;
        }
    }
}
