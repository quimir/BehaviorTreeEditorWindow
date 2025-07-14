using System.Collections.Generic;

namespace Editor.View.BtWindows.SearchBar.Core
{
    /// <summary>
    /// Represents a filter that is mutually exclusive with other filters, meaning
    /// it cannot be active simultaneously with certain other defined filters.
    /// This interface extends the <see cref="ISearchFilter"/> to include functionality
    /// for identifying and managing mutually exclusive filter relationships.
    /// </summary>
    public interface IMutuallyExclusiveFilter : ISearchFilter
    {
        /// <summary>
        /// Retrieves a collection of filter identifiers that are mutually exclusive
        /// with the current filter. These identifiers represent filters that cannot
        /// be active at the same time as the current filter.
        /// </summary>
        /// <returns>
        /// A collection of strings representing the IDs of mutually exclusive filters.
        /// </returns>
        IEnumerable<string> GetMutuallyExclusiveFilterIds();
    }
}
