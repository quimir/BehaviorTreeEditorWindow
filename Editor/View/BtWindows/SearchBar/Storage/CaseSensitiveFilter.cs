using System.Collections.Generic;
using Editor.View.BtWindows.SearchBar.Core;
using UnityEditor;
using UnityEngine;

namespace Editor.View.BtWindows.SearchBar.Storage
{
    public class CaseSensitiveFilter : IMutuallyExclusiveFilter
    {
        public string FilterId=>"case_sensitive";
        public string Tooltip => "大小写敏感匹配";
        public Texture2D Icon => AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/Resource/Shared/Image/switch_case.png");
        public bool IsDefaultActive => false;
        public bool IsVisibleInUI => true;

        public float GetScore(ISearchableItem item, string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return 0f;
            }

            var content = item.SearchableContent;
            
            // 大小写铭感的完全匹配
            if (content==query)
            {
                return 50f;
            }
            
            // 大小写敏感的前缀匹配
            if (content.StartsWith(query))
            {
                return 50f * 0.8f;
            }

            if (content.Contains(query))
            {
                return 50f * 0.5f;
            }

            return 0f;
        }

        public IEnumerable<string> GetMutuallyExclusiveFilterIds()
        {
            return new[] { "fuzzy_score" };
        }
    }
}
