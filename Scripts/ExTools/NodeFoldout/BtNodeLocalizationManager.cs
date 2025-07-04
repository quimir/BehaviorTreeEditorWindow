using System;
using System.Collections.Generic;
using System.IO;
using BehaviorTree.Nodes;
using ExTools.Singleton;
using LogManager.Core;
using LogManager.LogManagerFactory;
using Script.BehaviorTree;
using Script.LogManager;
using Script.Utillties;
using UnityEngine;

namespace Script.Tool.NodeFoldout
{
    public class BtNodeLocalizationManager :SingletonWithLazy<BtNodeLocalizationManager>
    {
        private static LogSpaceNode name_space_=new LogSpaceNode("NodeFoldout").AddChild("BtNodeLocalizationManager");
        
        #region 组件

        private BtLocalizationConfigLoader config_loader_;

        private BtLanguageManager language_manager_;
        
        private BtNodeLocalizationStorage storage_;

        #endregion

        #region 事件

        public event Action OnLocalizationUpdated;

        #endregion
        
        private bool is_fully_initialized_ = false;

        protected override void InitializationInternal()
        {
            config_loader_=new BtLocalizationConfigLoader();
            language_manager_=new BtLanguageManager();
            storage_=new BtNodeLocalizationStorage();

            config_loader_.OnConfigLoaded += (config) =>
            {
                storage_.UpdateExternalData(config);
                OnLocalizationUpdated?.Invoke();
            };

            language_manager_.OnLanguageChanged += (language) =>
            {
                storage_.ClearCache();
                OnLocalizationUpdated?.Invoke();
            };
            
            CompleteInitialization();
        }

        private void CompleteInitialization()
        {
            if (is_fully_initialized_)
            {
                return;
            }

            is_fully_initialized_ = true;
            
            // 设置默认配置路径
            string default_path = Path.Combine(Application.persistentDataPath, "BtNodeLocalization.json");
            
            config_loader_.SetConfigFilePath(default_path);
        }

        public void CollectAllNodeLocalization()
        {
            // 获取所有可能的节点类型
            var node_types = new List<Type>();
            node_types.AddRange(ExTool.Instance.GetDerivedClasses(typeof(BtComposite)));
            node_types.AddRange(ExTool.Instance.GetDerivedClasses(typeof(BtPrecondition)));
            node_types.AddRange(ExTool.Instance.GetDerivedClasses(typeof(BtActionNode)));

            foreach (var type in node_types)
            {
                try
                {
                    storage_.ProcessNodeType(type);
                }
                catch (Exception e)
                {
                    ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(name_space_,new LogEntry(LogLevel.kError,$"处理节点类型 {type.Name} 的本地化信息时出错: {e.Message}"));
                }
            }
            
            // 尝试加载外部配置
            config_loader_.LoadConfig();
            
            OnLocalizationUpdated?.Invoke();
        }

        public string GetNodeDisplayName(Type node_type)
        {
            return storage_.GetNodeDisplayName(node_type, language_manager_.GetCurrentLanguageCode());
        }

        public string GetNodeDisplayName(Type node_type, BtLanguageManager.LanguageType language_type)
        {
            return storage_.GetNodeDisplayName(node_type, language_manager_.GetLanguageCode(language_type));
        }

        public IEnumerable<string> GetNodeSearchTerms(Type node_type)
        {
            return storage_.GetNodeSearchTerms(node_type);
        }

        public void SetLanguage(BtLanguageManager.LanguageType language_type)
        {
            language_manager_.SetLanguage(language_type);
        }

        public BtLanguageManager.LanguageType GetCurrentLanguage()
        {
            return language_manager_.GetCurrentLanguageType();
        }

        public void SetConfigFilePath(string path)
        {
            config_loader_.SetConfigFilePath(path);
            config_loader_.LoadConfig();
        }

        public string GetConfigFilePath()
        {
            return config_loader_.GetConfigFilePath();
        }
        
        public bool CreateExampleConfigFile(string file_path)
        {
            return config_loader_.CreateExampleConfigFile(file_path);
        }

        /// <summary>
        /// Updates the external localization configuration file with the latest node localization data.
        /// It collects the node localization information from the storage, retrieves or initializes
        /// the localization data for each relevant node type, and saves the updated configuration file.
        /// </summary>
        /// <returns>
        /// A boolean value indicating whether the external configuration file update was successful.
        /// </returns>
        public bool UpdateExternalConfigFile()
        {
            // 收集使用外部配置的节点信息
            var config_data = new Dictionary<string, Dictionary<string, string>>();

            foreach (var node_type in storage_.GetExternalConfigTypes())
            {
                string key = storage_.GetConfigKey(node_type);

                if (config_data.ContainsKey(key))
                {
                    continue;
                }
                
                // 从外部配置加载现有数据
                var existing_data=config_loader_.GetLocalizationData(key);
                if (existing_data!=null)
                {
                    config_data[key] = existing_data;
                }
                else
                {
                    config_data[key] = new Dictionary<string, string>()
                    {
                        { "Default", node_type.Name },
                        { "Simplified_cn", node_type.Name },
                        { "English", node_type.Name }
                    };
                }
            }

            return config_loader_.SaveConfig(config_data);
        }

        public BtLanguageManager GetLanguageManager()
        {
            return language_manager_;
        }
        
        public BtLocalizationConfigLoader GetConfigLoader()
        {
            return config_loader_;
        }
        
        public BtNodeLocalizationStorage GetStorage()
        {
            return storage_;
        }
    }
}
