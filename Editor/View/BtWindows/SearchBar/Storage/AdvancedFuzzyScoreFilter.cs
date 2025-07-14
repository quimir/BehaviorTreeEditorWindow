using System;
using Editor.View.BtWindows.SearchBar.Core;
using UnityEditor;
using UnityEngine;

namespace Editor.View.BtWindows.SearchBar.Storage
{
    public class AdvancedFuzzyScoreFilter : ISearchFilter
    {
        public string FilterId => "advanced_fuzzy_score";
        public string Tooltip => "智能模糊匹配";

        public Texture2D Icon =>
            AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/Resource/Shared/Image/fuzzy.png");

        public bool IsDefaultActive => true;
        public bool IsVisibleInUI => true;

        private const float ERROR_TOLERANCE_RATE = 0.2f;
        private const int MIN_PREFIX_LENGTH = 2;
        private const float PREFIX_MATCH_BONUS = 500f;
        private const float EXACT_MATCH_BONUS = 1000f;

        public float GetScore(ISearchableItem item, string query)
        {
            if (string.IsNullOrEmpty(query))
                return 0f;

            var content = item.SearchableContent;
            var contentLower = content.ToLower();
            var queryLower = query.ToLower();

            // 1. 完全匹配 - 最高分
            if (contentLower == queryLower)
                return EXACT_MATCH_BONUS;

            // 2. 前缀匹配检查
            var prefixScore = GetPrefixMatchScore(contentLower, queryLower);
            if (prefixScore <= 0)
                return 0f; // 前缀不匹配则直接返回0

            // 3. 计算编辑距离得分
            var editDistanceScore = GetEditDistanceScore(contentLower, queryLower);

            // 4. 计算最终得分
            var finalScore = prefixScore + editDistanceScore;

            // 5. 长度惩罚 - 更短的匹配获得更高分数
            var lengthPenalty = Math.Max(0, (content.Length - query.Length) * 0.1f);

            return Math.Max(0f, finalScore - lengthPenalty);
        }

        /// <summary>
        /// 计算前缀匹配得分
        /// </summary>
        private float GetPrefixMatchScore(string content, string query)
        {
            if (query.Length < MIN_PREFIX_LENGTH)
                return content.StartsWith(query) ? PREFIX_MATCH_BONUS : 0f;

            // 检查前缀是否足够匹配
            var prefixLength = Math.Min(MIN_PREFIX_LENGTH, query.Length);
            var queryPrefix = query.Substring(0, prefixLength);

            // 完全前缀匹配
            if (content.StartsWith(queryPrefix))
                return PREFIX_MATCH_BONUS;

            // 允许前缀有少量容错
            if (content.Length >= prefixLength)
            {
                var contentPrefix = content.Substring(0, prefixLength);
                var prefixDistance = CalculateLevenshteinDistance(contentPrefix, queryPrefix);

                // 前缀容错：最多允许1个字符错误
                if (prefixDistance <= 1)
                    return PREFIX_MATCH_BONUS * 0.8f;
            }

            return 0f;
        }

        /// <summary>
        /// 计算基于编辑距离的得分
        /// </summary>
        private float GetEditDistanceScore(string content, string query)
        {
            var distance = CalculateLevenshteinDistance(content, query);
            var maxAllowedErrors = (int)Math.Ceiling(query.Length * ERROR_TOLERANCE_RATE);

            if (distance > maxAllowedErrors)
                return 0f;

            // 错误越少，得分越高
            var errorRate = (float)distance / query.Length;
            var baseScore = 100f * (1f - errorRate);

            // 根据匹配类型给予不同的奖励
            if (content.Contains(query))
                baseScore *= 1.5f; // 包含完整查询字符串
            else if (IsSubsequenceMatch(content, query))
                baseScore *= 1.2f; // 子序列匹配

            return baseScore;
        }

        /// <summary>
        /// 计算两个字符串的编辑距离（Levenshtein Distance）
        /// </summary>
        private int CalculateLevenshteinDistance(string source, string target)
        {
            if (string.IsNullOrEmpty(source))
                return string.IsNullOrEmpty(target) ? 0 : target.Length;

            if (string.IsNullOrEmpty(target))
                return source.Length;

            var sourceLength = source.Length;
            var targetLength = target.Length;
            var matrix = new int[sourceLength + 1, targetLength + 1];

            // 初始化第一行和第一列
            for (var i = 0; i <= sourceLength; i++)
                matrix[i, 0] = i;

            for (var j = 0; j <= targetLength; j++)
                matrix[0, j] = j;

            // 填充矩阵
            for (var i = 1; i <= sourceLength; i++)
            for (var j = 1; j <= targetLength; j++)
            {
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;

                matrix[i, j] = Math.Min(
                    Math.Min(
                        matrix[i - 1, j] + 1, // 删除
                        matrix[i, j - 1] + 1), // 插入
                    matrix[i - 1, j - 1] + cost // 替换
                );
            }

            return matrix[sourceLength, targetLength];
        }

        /// <summary>
        /// 检查target是否是source的子序列
        /// </summary>
        private bool IsSubsequenceMatch(string source, string target)
        {
            if (string.IsNullOrEmpty(target))
                return true;

            if (string.IsNullOrEmpty(source))
                return false;

            var sourceIndex = 0;
            var targetIndex = 0;

            while (sourceIndex < source.Length && targetIndex < target.Length)
            {
                if (source[sourceIndex] == target[targetIndex]) targetIndex++;
                sourceIndex++;
            }

            return targetIndex == target.Length;
        }
    }
}