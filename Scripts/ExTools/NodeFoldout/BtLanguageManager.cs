using System;
using System.Collections.Generic;
using UnityEngine;

namespace Script.Tool.NodeFoldout
{
    public class BtLanguageManager
    {
        public enum LanguageType
        {
            kSimplifiedCN,  // 简体中文
            kTraditionalCN, // 繁体中文
            kEnglish,        // 英语
            kJapanese,       // 日语
            kKorean,         // 韩语
            kCustom,
            kNone
        }

        private LanguageType current_language_type_ = LanguageType.kSimplifiedCN;
        
        private static readonly Dictionary<LanguageType, string> language_code_map_ = new Dictionary<LanguageType, string>
        {
            { LanguageType.kSimplifiedCN, "Simplified_cn" },
            { LanguageType.kTraditionalCN, "Traditional_cn" },
            { LanguageType.kEnglish, "English" },
            { LanguageType.kJapanese, "Japanese" },
            { LanguageType.kKorean, "Korean" },
            { LanguageType.kCustom ,"Custom"},
            { LanguageType.kNone, "None" }
        };

        private string custom_language_code_ = "Custom";
        
        public event Action<string> OnLanguageChanged;

        public BtLanguageManager()
        {
            SetLanguage(GetLanguageBySystemSetting());
        }

        private LanguageType GetLanguageBySystemSetting()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                    return LanguageType.kSimplifiedCN;
                case SystemLanguage.ChineseTraditional:
                    return LanguageType.kTraditionalCN;
                case SystemLanguage.Japanese:
                    return LanguageType.kJapanese;
                case SystemLanguage.Korean:
                    return LanguageType.kKorean;
                default:
                    return LanguageType.kEnglish;
            }
        }

        public void SetLanguage(LanguageType language_type)
        {
            if (current_language_type_!=language_type)
            {
                current_language_type_ = language_type;
                
                OnLanguageChanged?.Invoke(GetCurrentLanguageCode());
            }
        }

        public void SetCustomLanguage(string language_code)
        {
            if (!string.IsNullOrEmpty(language_code)&&custom_language_code_!=language_code)
            {
                custom_language_code_ = language_code;
                language_code_map_[LanguageType.kCustom] = language_code;
                
                // 如果当前是自定义语言，则触发变更事件
                if (current_language_type_==LanguageType.kCustom)
                {
                    OnLanguageChanged?.Invoke(GetCurrentLanguageCode());
                }
            }
        }

        public LanguageType GetCurrentLanguageType()
        {
            return current_language_type_;
        }

        public string GetCurrentLanguageCode()
        {
            return language_code_map_[current_language_type_];
        }

        public IEnumerable<string> GetSupportedLanguageCodes()
        {
            return language_code_map_.Values;
        }

        public string GetLanguageCode(LanguageType language_type)
        {
            if (language_code_map_.TryGetValue(language_type,out string code))
            {
                return code;
            }

            return language_code_map_[LanguageType.kEnglish];
        }
        
        public string GetLanguageName(LanguageType language_type)
        {
            switch (language_type)
            {
                case LanguageType.kSimplifiedCN: return "简体中文";
                case LanguageType.kTraditionalCN: return "繁体中文";
                case LanguageType.kEnglish: return "English";
                case LanguageType.kJapanese: return "日本語";
                case LanguageType.kKorean: return "한국어";
                case LanguageType.kCustom: return $"自定义 ({custom_language_code_})";
                default: return "未知";
            }
        }
    }
}
