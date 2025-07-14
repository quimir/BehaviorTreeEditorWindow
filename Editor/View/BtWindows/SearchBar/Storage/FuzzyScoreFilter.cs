using Editor.View.BtWindows.SearchBar.Core;
using Script.Tool;
using UnityEditor;
using UnityEngine;

namespace Editor.View.BtWindows.SearchBar.Storage
{
    public class FuzzyScoreFilter : ISearchFilter
    {
        public string FilterId => "fuzzy_score";
        public string Tooltip => "模糊名称匹配";
        public Texture2D Icon => EditorGUIUtility.FindTexture("d_TextAsset Icon");
        public bool IsDefaultActive => true;
        public bool IsVisibleInUI => false;

        public float GetScore(ISearchableItem item, string query)
        {
            var content = item.SearchableContent.ToLower();

            var lower_query = query.ToLower();

            if (string.IsNullOrEmpty(lower_query)) return 0f;
            
            // 完全匹配，给于最高分
            if (content==lower_query)
            {
                return 1000f;
            }
            
            // 前缀匹配
            if (content.StartsWith(lower_query))
            {
                float score = 100f - (content.Length - lower_query.Length);
                return score;
            }
            
            // 包含匹配，分值较低
            if (content.Contains(lower_query))
            {
                float score = 10f * ((float)lower_query.Length / content.Length) - (content.Length * 0.1f);
                return score>0?score:0f;
            }

            return 0f;
        }
    }
}