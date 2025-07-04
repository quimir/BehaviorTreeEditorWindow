using System;
using UnityEngine;

namespace Script.Tool.NodeFoldout
{
    [AttributeUsage(AttributeTargets.Class)]
    public class BtNodeFoldoutAttribute : Attribute
    {
        public readonly string[] PathSegments;

        public BtNodeFoldoutAttribute(string path)
        {
            PathSegments=string.IsNullOrEmpty(path)? Array.Empty<string>():path.Split('/');
        }
    }
    
    [AttributeUsage(AttributeTargets.Class)]
    public class BtNodeDisplayAttribute:Attribute
    {
        /// <summary>
        /// 默认显示名称
        /// </summary>
        public readonly string DisplayName;

        /// <summary>
        /// 外部配置文件的路径
        /// </summary>
        public readonly string LocalizationPath;

        /// <summary>
        /// 外部配置的键名，默认为类名.如果没有指定LocalizationPath默认会在Application.persistentDataPath下的"BtNodeLocalization.json"进行查找
        /// </summary>
        public readonly string ConfigKey;

        public BtNodeDisplayAttribute(string display_name)
        {
            DisplayName = display_name;
            LocalizationPath = null;
            ConfigKey = null;
        }

        /// <summary>
        /// An attribute used to define display-related metadata for a behavior tree node class.
        /// </summary>
        public BtNodeDisplayAttribute(string Localization_path = null, string config_key = null)
        {
            DisplayName = null;
            LocalizationPath = Localization_path;
            ConfigKey = config_key;
        }
    }
}
