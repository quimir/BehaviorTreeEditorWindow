using System;
using System.Collections.Generic;
using System.Reflection;
using ExTools;
using LogManager.Core;
using LogManager.LogManagerFactory;
using Script.LogManager;
using Script.Save.Serialization.Storage;
using Script.Utillties;

namespace Script.Tool.NodeFoldout
{
    public class BtNodeLocalizationStorage
    {
        private static readonly LogSpaceNode name_space_=new LogSpaceNode("NodeFoldout").AddChild("BtNodeLocalizationStorage");

        private Dictionary<Type, Dictionary<string, string>> internal_data_ = new();

        private Dictionary<string, Dictionary<string, string>> external_data_ = new();

        private Dictionary<Type, string> type_to_key_map_ = new();

        private Dictionary<Type, bool> use_external_config_map_ = new();

        private Dictionary<string, Dictionary<Type, string>> display_name_cache_ = new();

        public void UpdateExternalData(Dictionary<string, Dictionary<string, string>> data)
        {
            external_data_ = data ?? new Dictionary<string, Dictionary<string, string>>();
            ClearCache();
        }

        public void ClearCache()
        {
            display_name_cache_.Clear();
        }

        public void ProcessNodeType(Type node_type)
        {
            var use_external_config = false;
            string config_key = null;
            var localizations = new Dictionary<string, string>();

            // 尝试获取BtNodeDisplayAttribute特性
            if (node_type.GetCustomAttribute(typeof(BtNodeDisplayAttribute)) is BtNodeDisplayAttribute
                display_attribute)
            {
                // 存储默认名称
                string defaultName = display_attribute.DisplayName ?? node_type.Name;
                localizations["Default"] = defaultName;

                // 判断使用哪种配置模式
                if (!string.IsNullOrEmpty(display_attribute.LocalizationPath))
                {
                    // 使用内部配置（JSON）
                    try
                    {
                        var serializer = new JsonSerializerWithStorage();
                        var json_data =
                            serializer.LoadFromFile<Dictionary<string, string>>(display_attribute.LocalizationPath);
                        if (json_data != null)
                            foreach (var pair in json_data)
                                localizations[pair.Key] = pair.Value;
                    }
                    catch (Exception ex)
                    {
                        ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(name_space_,
                            new LogEntry(LogLevel.kWarning, $"无法解析节点 {node_type.Name} 的本地化JSON: {ex.Message}"));
                    }
                }
                else
                {
                    use_external_config = true;
                    config_key=display_attribute.ConfigKey??node_type.Name;
                }
                
                internal_data_[node_type] = localizations;
                use_external_config_map_[node_type] = use_external_config;
                if (use_external_config)
                {
                    type_to_key_map_[node_type] = config_key;
                }
            }
            else
            {
                // 向后兼容旧特性
                if (node_type.GetCustomAttribute(typeof(NodeLabelAttribute)) is NodeLabelAttribute
                    old_display_attribute)
                {
                    localizations = new Dictionary<string, string>
                    {
                        { "Default", old_display_attribute.menu_name_ ?? node_type.Name },
                        { "Simplified_cn", old_display_attribute.menu_name_ ?? node_type.Name },
                        { "English", old_display_attribute.label_ ?? node_type.Name }
                    };

                    internal_data_[node_type] = localizations;
                }
                else
                {
                    // 没有任何特性的情况，使用类名
                    internal_data_[node_type] = new Dictionary<string, string>
                    {
                        { "Default", node_type.Name }
                    };
                }
            }
        }

        public string GetNodeDisplayName(Type node_type, string language_code)
        {
            // 检查缓存
            if (display_name_cache_.TryGetValue(language_code, out var cache_for_language))
            {
                if (cache_for_language.TryGetValue(node_type, out var cached_name)) return cached_name;
            }
            else
            {
                display_name_cache_[language_code] = new Dictionary<Type, string>();
            }

            var display_name = node_type.Name;

            // 确定使用那种配置
            if (use_external_config_map_.TryGetValue(node_type, out var use_external) && use_external)
            {
                // 使用外部配置
                if (type_to_key_map_.TryGetValue(node_type, out var config_key) &&
                    external_data_.TryGetValue(config_key, out var external_localizations))
                {
                    // 尝试获取当前语言
                    if (external_localizations.TryGetValue(language_code, out var localized_name))
                        display_name = localized_name;
                    // 尝试获取英文或其他备选语言
                    else if (external_localizations.TryGetValue("English", out var english_name))
                        display_name = english_name;
                    else if (external_localizations.Count > 0)
                        // 获取第一个可用的语言
                        using (var enumerator = external_localizations.Values.GetEnumerator())
                        {
                            if (enumerator.MoveNext()) display_name = enumerator.Current;
                        }
                }
            }
            else if (internal_data_.TryGetValue(node_type, out var internal_localizations))
            {
                // 使用内部配置，尝试获取当前语言
                if (internal_localizations.TryGetValue(language_code, out var localized_name))
                    display_name = localized_name;
                // 尝试获取英文或默认名称
                else if (internal_localizations.TryGetValue("English", out var english_name))
                    display_name = english_name;
                else if (internal_localizations.TryGetValue("Default", out var default_name))
                    display_name = default_name;
            }

            display_name_cache_[language_code][node_type] = display_name;
            return display_name;
        }

        public IEnumerable<string> GetNodeSearchTerms(Type node_type)
        {
            var terms = new List<string> { node_type.Name };

            // 确定使用那种配置
            if (use_external_config_map_.TryGetValue(node_type, out var use_external) && use_external)
                // 使用外部配置
                if (type_to_key_map_.TryGetValue(node_type, out var config_key) &&
                    external_data_.TryGetValue(config_key, out var external_localization))
                    // 添加所有语言的名称作为搜索项
                    terms.AddRange(external_localization.Values);

            // 添加配置的语言名称
            if (internal_data_.TryGetValue(node_type, out var internal_localization))
                terms.AddRange(internal_localization.Values);

            return new HashSet<string>(terms);
        }

        public bool IsUsingExternalConfig(Type node_type)
        {
            return use_external_config_map_.TryGetValue(node_type, out var use_external) && use_external;
        }

        public string GetConfigKey(Type node_type)
        {
            if (type_to_key_map_.TryGetValue(node_type, out var key)) return key;

            return node_type.Name;
        }

        public IEnumerable<Type> GetExternalConfigTypes()
        {
            foreach (var pair in use_external_config_map_)
                if (pair.Value)
                    yield return pair.Key;
        }
    }
}