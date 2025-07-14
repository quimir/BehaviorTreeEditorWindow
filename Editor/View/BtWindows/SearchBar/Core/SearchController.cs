using System;
using System.Collections.Generic;
using System.Linq;

namespace Editor.View.BtWindows.SearchBar.Core
{
    /// <summary>
    /// A controller class responsible for managing the search functionality within the application.
    /// </summary>
    /// <remarks>
    /// The SearchController class provides functionality for managing searchable data, applying filters,
    /// conducting search operations, and interacting with search results.
    /// </remarks>
    public class SearchController
    {
        /// <summary>
        /// Event triggered when the search results have changed or been updated.
        /// </summary>
        /// <remarks>
        /// The <c>OnResultsChanged</c> event is invoked whenever new search results are available,
        /// such as after performing a search or navigating between results. Subscribers to this
        /// event are notified with the updated <see cref="SearchResults"/> object, allowing them
        /// to react to changes in the search results or update the UI accordingly.
        /// </remarks>
        public event Action<SearchResults> OnResultsChanged;

        /// <summary>
        /// A collection of available search filters that can be applied to refine search results.
        /// </summary>
        /// <remarks>
        /// The <c>AvailableFilters</c> property provides access to the complete set of filters represented
        /// as a read-only dictionary mapping filter identifiers to their respective <see cref="ISearchFilter"/> objects.
        /// These filters define criteria for modifying or narrowing down search results and may include
        /// default filters that are initialized as active.
        /// </remarks>
        public IReadOnlyDictionary<string, ISearchFilter> AvailableFilters { get; }
        
        private readonly Func<IEnumerable<ISearchableItem>> data_provider_;
        private readonly List<ISearchFilter> active_filters_ = new();
        private SearchResults last_results_;

        /// <summary>
        /// Tracks the current index of the selected search result within the search results.
        /// </summary>
        /// <remarks>
        /// This variable represents the position of the currently active or focused item in the
        /// list of search results, enabling navigation and selection within the results.
        /// It is updated when navigating to the next or previous result or when search results
        /// change. A value of <c>-1</c> indicates no active selection or no results available.
        /// </remarks>
        private int current_index_ = -1;

        /// <summary>
        /// Manages the search functionality within the editor's search bar interface. Handles
        /// search operations, tracks active filters, maintains available search filters, and
        /// provides search results. Triggers the <c>OnResultsChanged</c> event when results are updated.
        /// </summary>
        /// <param name="data_provider">Function that provides the collection of searchable items to the search controller.</param>
        /// <param name="available_filters">A collection of currently active search filters used to refine search results.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public SearchController(Func<IEnumerable<ISearchableItem>> data_provider,
            IEnumerable<ISearchFilter> available_filters)
        {
            data_provider_ = data_provider ?? throw new ArgumentNullException(nameof(data_provider));
            AvailableFilters = available_filters.ToDictionary(f => f.FilterId);
            
            // 初始化默认激活的过滤器
            active_filters_.AddRange(AvailableFilters.Values.Where(f => f.IsDefaultActive));;
        }

        /// <summary>
        /// Executes a search operation using the provided query. Filters the items
        /// based on active search filters, calculates scores for matching items, and
        /// updates the search results. Triggers the <c>OnResultsChanged</c> event
        /// upon completion.
        /// </summary>
        /// <param name="query">The search query string used to filter and score items.</param>
        public void PerformSearch(string query)
        {
            var scored_items = new List<(ISearchableItem, float)>();
            if (!string.IsNullOrEmpty(query) && active_filters_.Any())
            {
                var all_items = data_provider_?.Invoke();
                if (all_items!=null)
                {
                    foreach (var item in all_items)
                    {
                        // 使用Sum获取所有活动过滤器的总分
                        float total_score = active_filters_.Sum(filter => filter.GetScore(item, query));
                        if (total_score>0)
                        {
                            scored_items.Add((item,total_score));
                        }
                    }
                   
                }
            }

            var sorted_results = scored_items.OrderByDescending(r => r.Item2).ToList();
            last_results_=new SearchResults(query,sorted_results);
            current_index_ = last_results_.HasResults ? 0 : -1;
            
            // 在新搜索后立即激活第一个结果
            if (current_index_==0)
            {
                last_results_.ScoredResults[0].Item.OnFound();
            }
            
            OnResultsChanged?.Invoke(last_results_);
        }

        /// <summary>
        /// Toggles the activation state of a specified search filter. Updates the active
        /// filters collection based on the desired state of the filter.
        /// </summary>
        /// <param name="filter_id">The unique identifier of the filter to toggle.</param>
        /// <param name="is_active">A boolean indicating whether the filter should be activated or deactivated.</param>
        public void ToggleFilter(string filter_id, bool is_active)
        {
            if (AvailableFilters.TryGetValue(filter_id, out var filter))
            {
                if (is_active)
                {
                    // 处理互斥过滤器
                    if (filter is IMutuallyExclusiveFilter mutuallyExclusiveFilter)
                    {
                        var exclusive_ids = mutuallyExclusiveFilter.GetMutuallyExclusiveFilterIds();
                        foreach (var exclusiveID in exclusive_ids)
                        {
                            if (AvailableFilters.TryGetValue(exclusiveID,out var exclusiveFilter))
                            {
                                active_filters_.Remove(exclusiveFilter);
                            }
                        }
                    }
                    if (!active_filters_.Contains(filter))
                    {
                        active_filters_.Add(filter);
                    }
                }
                else
                {
                    active_filters_.Remove(filter);
                    
                    // 如果关闭的是互斥过滤器，重新启用被排斥的默认过滤器
                    if (filter is IMutuallyExclusiveFilter mutuallyExclusiveFilter)
                    {
                        var exclusiveIds = mutuallyExclusiveFilter.GetMutuallyExclusiveFilterIds();
                        foreach (var exclusiveId in exclusiveIds)
                        {
                            if (AvailableFilters.TryGetValue(exclusiveId, out var exclusiveFilter) &&
                                exclusiveFilter.IsDefaultActive)
                            {
                                active_filters_.Add(exclusiveFilter);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Activates the next search result in the list of scored results. Updates the
        /// current result index to the next result, wraps around if reaching the end
        /// of the list. Invokes the <c>OnResultsChanged</c> event to notify UI
        /// listeners of the change.
        /// </summary>
        public void ActivateNextResult()
        {
            if (last_results_ is not { HasResults: true })
            {
                return;
            }

            current_index_ = (current_index_ + 1) % last_results_.ScoredResults.Count;
            var selected_item = last_results_.ScoredResults[current_index_].Item;
            selected_item.OnFound();
            
            // 通知UI更新当前索引
            OnResultsChanged?.Invoke(last_results_);
        }

        /// <summary>
        /// Selects and activates the previous search result in the current set of results.
        /// Updates the current index to point to the previous result, wraps to the end
        /// of the result list if the beginning is reached, and invokes the
        /// <c>OnResultsChanged</c> event to notify the UI of the update.
        /// Triggers the <c>OnFound</c> action of the selected search item.
        /// </summary>
        public void ActivatePreviousResult()
        {
            if (last_results_ is not { HasResults: true })
            {
                return;
            }

            current_index_ = (current_index_ - 1 + last_results_.ScoredResults.Count) %
                             last_results_.ScoredResults.Count;
            var selected_item = last_results_.ScoredResults[current_index_].Item;
            selected_item.OnFound();
            
            // 通知UI更新当前索引
            OnResultsChanged?.Invoke(last_results_);
        }

        /// <summary>
        /// Retrieves the index of the currently active result within the search results.
        /// Returns -1 if no result is currently active.
        /// </summary>
        /// <returns>The zero-based index of the currently active search result, or -1 if no result is active.</returns>
        public int GetCurrentIndex()
        {
            return current_index_;
        }

        /// <summary>
        /// Retrieves the most recent search results generated by the search operation.
        /// </summary>
        /// <returns>The last computed <c>SearchResults</c> object or null if no search has been performed.</returns>
        public SearchResults GetLastResults()
        {
            return last_results_;
        }
    }
}
