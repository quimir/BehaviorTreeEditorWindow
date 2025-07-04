using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BehaviorTree.Nodes;
using Save.Serialization;
using Script.BehaviorTree;
using Script.Save.Serialization;
using UnityEngine;

namespace Editor.View.BTWindows.InspectorUI.Operations
{
    [Serializable]
    [CustomSerialize]
    public class NodePropertySnapshot
    {
        public string NodeGuid;
        public string NodeType;
        public Dictionary<string, object> PropertyValues = new();
        public string ParentNodeGuid;
        public Vector2 Position;

        public NodePropertySnapshot()
        {
        }

        public NodePropertySnapshot(BtNodeBase node)
        {
            CaptureFromNode(node);
        }

        public void CaptureFromNode(BtNodeBase node)
        {
            if (node == null) return;

            NodeGuid = node.Guild;
            NodeType = node.GetType().AssemblyQualifiedName;
            Position = node.Position;

            CaptureNodeProperties(node);
        }

        private void CaptureNodeProperties(BtNodeBase node)
        {
            var type = node.GetType();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                // 跳过不需要序列化的字段
                if (ShouldSkipField(field))
                    continue;

                try
                {
                    var value=field.GetValue(node);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
            
        }

        private bool ShouldSkipField(FieldInfo field)
        {
            // 跳过静态、只读、编辑器生成的字段
            if (field.IsStatic || field.IsInitOnly || field.IsLiteral) return true;

            // 跳过特定类型的字段
            var field_type = field.FieldType;
            if (field_type == typeof(BtNodeBase) || field_type.IsSubclassOf(typeof(BtNodeBase)) ||
                (field_type.IsGenericType && field_type.GetGenericTypeDefinition() == typeof(List<>) &&
                 field_type.GetGenericArguments()[0].IsSubclassOf(typeof(BtNodeBase))))
            {
                return true;
            }
            
            // 跳过包含特定名称的字段
            var skip_names=new[] { "parent", "child", "node", "tree", "view", "container" };
            return skip_names.Any(name => field.Name.ToLower().Contains(name));
        }

        private bool ShouldSkipProperty(PropertyInfo property)
        {
            // 跳过索引器和特定属性
            if (property.GetIndexParameters().Length>0)
            {
                return true;
            }
            
            var skip_names=new[] { "Parent", "Child", "Node", "Tree", "View", "Container" };
            return skip_names.Any(name => property.Name.ToLower().Contains(name));
        }

        private bool IsSerializableValue(object value)
        {
            if (value==null)
            {
                return true;
            }

            var type = value.GetType();
            return type.IsPrimitive || type == typeof(string) || type == typeof(Vector2);
        }
    }
}