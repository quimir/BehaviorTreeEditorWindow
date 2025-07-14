using System.Collections.Generic;
using Save.Serialization.Core.TypeConverter.SerializerAttribute;

namespace Editor.View.BtWindows.SearchBar.Core
{
    /// <summary>
    /// Represents the results of a search operation. This includes the original query and the scored results.
    /// </summary>
    public class SearchResults
    {
        /// <summary>
        /// Gets the original query string submitted for the search operation.
        /// This property holds the exact input provided by the user or source
        /// which initiated the search. It serves as a reference to understand
        /// the context or intent of the search.
        /// </summary>
        public string OriginalQuery { get; }

        /// <summary>
        /// Gets the list of search results along with their associated scores.
        /// Each entry in the list represents an item found during the search and its relevance
        /// as determined by the scoring algorithm. The score reflects how well the item matches the query.
        /// </summary>
        public IReadOnlyList<(ISearchableItem Item,float Score)> ScoredResults { get; }

        /// <summary>
        /// Indicates whether the search operation yielded any results.
        /// This property returns true if the list of scored results
        /// contains one or more entries, otherwise false.
        /// </summary>
        [NonSerialize]
        public bool HasResults=>ScoredResults.Count>0;
    
        public SearchResults(string originalQuery, IReadOnlyList<(ISearchableItem Item,float Score)> scoredResults)
        {
            OriginalQuery = originalQuery;
            ScoredResults = scoredResults;
        }
    }
}
