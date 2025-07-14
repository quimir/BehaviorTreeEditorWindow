namespace Editor.View.BtWindows.SearchBar.Core
{
    /// <summary>
    /// Represents an item that can be searched within a searchable system.
    /// </summary>
    public interface ISearchableItem
    {
        /// <summary>
        /// 用于搜索和评分的主要文本内容。
        /// 可以是名称、类型、描述等的组合。
        /// </summary>
        string SearchableContent { get; }

        /// <summary>
        /// 当用户在搜索结果中选中此项时执行的操作。
        /// </summary>
        void OnFound();
    }
}
