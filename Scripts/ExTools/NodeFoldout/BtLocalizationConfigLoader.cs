using System;
using System.Collections.Generic;
using System.IO;
using ExTools.Utillties;
using LogManager.Core;
using LogManager.LogManagerFactory;
using Save.Serialization.Core.TypeConverter;
using Save.Serialization.Storage.Serializer.JsonNet;

namespace ExTools.NodeFoldout
{
    /// <summary>
    /// Handles the loading, saving, and management of localization configuration files for nodes.
    /// Provides functionality to load configuration data from files or create example configurations.
    /// Configurations are stored as a dictionary of dictionaries, containing localized strings for specific keys.
    /// </summary>
    public class BtLocalizationConfigLoader
    {
        private static LogSpaceNode name_space_=new LogSpaceNode("NodeFoldout").AddChild("BtNodeLocalizationConfigLoader");
        
        private string config_file_path_;

        private Dictionary<string, Dictionary<string, string>> config_cache_;

        private bool is_loaded_ = false;

        public event Action<Dictionary<string, Dictionary<string, string>>> OnConfigLoaded;

        public void SetConfigFilePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            
            if (config_file_path_!=path)
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(name_space_,new LogEntry(LogLevel.kInfo,$"成功配置节点文件路径: {path}"));
                config_file_path_ = path;
                config_cache_ = null;
                is_loaded_ = false;
            }
        }

        public string GetConfigFilePath()
        {
            return config_file_path_;
        }

        public bool LoadConfig(bool force_reload = false)
        {
            // 如果已加载且不需要强制重置加载，则直接返回
            if (is_loaded_&&!force_reload)
            {
                return true;
            }
            
            config_cache_=new Dictionary<string, Dictionary<string, string>>();
            is_loaded_ = false;
            
            // 检查文件是否存在
            if (string.IsNullOrEmpty(config_file_path_)||!File.Exists(config_file_path_))
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(name_space_,new LogEntry(LogLevel.kWarning,$"节点本地化配置文件不存在: {config_file_path_}"));
                return false;
            }

            try
            {
                // 读取配置文件
                var json_writer = new JsonSerializerWithStorage();
                config_cache_ =
                    json_writer.LoadFromFile<Dictionary<string, Dictionary<string, string>>>(config_file_path_);

                if (config_cache_==null)
                {
                    config_cache_=new Dictionary<string, Dictionary<string, string>>();
                    ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(name_space_,new LogEntry(LogLevel.kError,"节点本地化配置文件格式错误"));
                    return false;
                }

                is_loaded_ = true;
                
                // 触发加载时间
                OnConfigLoaded?.Invoke(config_cache_);
                
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(name_space_,new LogEntry(LogLevel.kInfo,"成功加载本地化配置文件"));

                return true;
            }
            catch (Exception e)
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(name_space_,new LogEntry(LogLevel.kError,$"加载节点本地化配置文件失败: {e.Message}"));
                config_cache_=new Dictionary<string, Dictionary<string, string>>();
                return false;
            }
        }

        public Dictionary<string, string> GetLocalizationData(string key)
        {
            // 确保配置已加载
            if (!is_loaded_&&!LoadConfig())
            {
                return null;
            }

            return config_cache_.GetValueOrDefault(key);
        }

        public Dictionary<string, Dictionary<string, string>> GetAllConfig()
        {
            if (!is_loaded_)
            {
                LoadConfig();
            }

            return config_cache_;
        }

        public bool SaveConfig(Dictionary<string, Dictionary<string, string>> config_data)
        {
            if (string.IsNullOrWhiteSpace(config_file_path_)||config_file_path_==null)
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(name_space_,new LogEntry(LogLevel.kError,"无法保存本地化配置文件：路径为空"));
                return false;
            }

            try
            {
                // 确保目录存在
                Directory.CreateDirectory(Path.GetDirectoryName(config_file_path_));
                
                var json_serializer=new JsonSerializerWithStorage(new SerializationSettings
                {
                    PrettyPrint = true,
                    PreserveReferences = true
                });
                
                json_serializer.SaveToFile(config_data,config_file_path_);
                
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(name_space_,new LogEntry(LogLevel.kInfo,$"成功保存本地化配置文件：{config_file_path_}"));

                config_cache_ = config_data;
                is_loaded_ = true;
                
                OnConfigLoaded?.Invoke(config_cache_);

                return true;
            }
            catch (Exception e)
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(name_space_,new LogEntry(LogLevel.kError,$"保存本地化配置文件失败：{e.Message}"));
                return false;
            }
        }

        public bool CreateExampleConfigFile(string file_path)
        {
            if (string.IsNullOrEmpty(file_path))
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(name_space_,new LogEntry(LogLevel.kError,"无法创建本地化配置文件：路径为空"));
                return false;
            }
        
            // 创建示例配置
            var exampleConfig = new Dictionary<string, Dictionary<string, string>>
            {
                {
                    "ExampleNode", new Dictionary<string, string>
                    {
                        { "Simplified_cn", "示例节点" },
                        { "Traditional_cn", "示例節點" },
                        { "English", "Example Node" },
                        { "Japanese", "サンプルノード" },
                        { "Korean", "예제 노드" }
                    }
                }
            };
        
            // 设置路径并保存
            SetConfigFilePath(file_path);
            return SaveConfig(exampleConfig);
        }
    }
}
