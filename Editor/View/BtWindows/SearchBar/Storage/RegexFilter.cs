using System;
using System.Text.RegularExpressions;
using Editor.View.BtWindows.SearchBar.Core;
using UnityEditor;
using UnityEngine;

namespace Editor.View.BtWindows.SearchBar.Storage
{
    public class RegexFilter : ISearchFilter
    {
        public string FilterId => "regex";
        public string Tooltip => "使用正则表达式";

        public Texture2D Icon =>
            AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/Resource/Shared/Image/regular_expression.png");
        public bool IsDefaultActive => false;
        public bool IsVisibleInUI => true;

        public float GetScore(ISearchableItem item, string query)
        {
            try
            {
                // 正则匹配成功，给于一个固定的高分
                if (Regex.IsMatch(item.SearchableContent,query,RegexOptions.IgnoreCase))
                {
                    return 80f;
                }
            }
            catch (Exception e)
            {
                return 0f;
            }

            return 0f;
        }
    }
}
