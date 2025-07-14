using UnityEngine;

namespace Editor.View.BtWindows.SearchBar.Core
{
    /// <summary>
    /// Defines the contract for a search filter, which is responsible for providing
    /// filtering mechanisms and scoring logic for searchable items.
    /// </summary>
    public interface ISearchFilter
    {
        /// <summary>
        /// 过滤器的唯一ID，用于内部管理。
        /// </summary>
        string FilterId { get; }
        
        /// <summary>
        /// 当鼠标悬停在UI图标上时显示的工具提示。
        /// </summary>
        string Tooltip { get; }
        
        /// <summary>
        /// 在SearchView中显示的图标。
        /// </summary>
        Texture2D Icon { get; }
        
        /// <summary>
        /// 此过滤器是否默认启用。
        /// </summary>
        bool IsDefaultActive { get; }
        
        /// <summary>
        /// 此过滤器是否在UI中可见（可由用户切换）。
        /// </summary>
        bool IsVisibleInUI { get; }

        /// <summary>
        /// 根据查询字符串为可搜索项计算一个匹配分数。
        /// </summary>
        /// <param name="item">要评分的可搜索项。</param>
        /// <param name="query">用户输入的查询字符串。</param>
        /// <returns>匹配分数。0表示不匹配，分数越高表示越相关。</returns>
        float GetScore(ISearchableItem item, string query);
    }
}
