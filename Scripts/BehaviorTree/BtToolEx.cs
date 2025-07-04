using System;
using System.Collections.Generic;
using System.Linq;
using BehaviorTree.Core;
using BehaviorTree.Nodes;
using ExTools.Utillties;
using LogManager.Core;
using LogManager.LogManagerFactory;
using Script.LogManager;
using Script.Save.Serialization;
using Script.Save.Serialization.Factory;
using Script.Utillties;
using UnityEditor;
using UnityEngine;

namespace BehaviorTree
{
    public static class BtToolEx
    {
        private static LogSpaceNode bt_space_ = new("BtToolEx");
        public static List<BtNodeBase> CloneData(this List<BtNodeBase> nodes)
        {
            var serializer = SerializerCreator.Instance.Create
            (SerializerType.kJson,new SerializationSettings
            {
                PreserveReferences = true,
                TypeNameHandling = SerializationTypeNameHandling.kAuto
            });
            
            var node_json = serializer.Serialize(nodes);
            var data= serializer.Deserialize<List<BtNodeBase>>(node_json);
            
            foreach (var child_node in data)
            {
                child_node.Guild=Guid.NewGuid().ToString();
                switch (child_node)
                {
                    case BtComposite composite:
                        if (composite.ChildNodes.Count==0)
                        {
                            break;
                        }
            
                        composite.ChildNodes = composite.ChildNodes.Intersect(data).ToList();
                        break;
                    case BtPrecondition precondition:
                        if (precondition.ChildNode==null)
                        {
                            break;
                        }
            
                        if (!data.Exists(n=>n==precondition.ChildNode))
                        {
                            precondition.ChildNode = null;
                        }
                        
                        break;
                }
            }
            
            return data;
        }

        public static bool StringEmpty(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        public static BtWindowAsset CreateBtWindowAsset(out string asset_path)
        {
            // 1.让用户选择保存路径和文件名（使用Unity的方法）
            var suggested_name = "BtWindowData"; 
            asset_path = EditorUtility.SaveFilePanelInProject(
                "Create New BtWindow Asset", suggested_name + ".asset", "asset",
                "Please select a location to save the BtWindowAsset."); // 提示信息
            
            // 如果用户取消了对话框
            if (string.IsNullOrEmpty(asset_path))
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(bt_space_,
                    new LogEntry(LogLevel.kInfo, "BtWindowAsset creation cancelled by user."));
                return null;
            }
            
            //  assetPath现在是相对于项目的路径,例如"Assets/.../??.asset"
            // 2.创建ScriptableObject实例(在内存中)
            var instance = ScriptableObject.CreateInstance<BtWindowAsset>();

            // 3.将内存中的示例保存为项目的资源文件，CreateAsset会自动处理正确的Unity序列化格式(YAML)
            AssetDatabase.CreateAsset(instance, asset_path);

            // 4.立即保存更改到磁盘，确保文件写入
            AssetDatabase.SaveAssets();

            return instance;
        }
    }
}

