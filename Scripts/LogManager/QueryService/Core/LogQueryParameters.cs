using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using LogManager.Core;

namespace LogManager.QueryService.Core
{
    /// <summary>
    /// 定义日志查询的排序字段。
    /// </summary>
    public enum LogQuerySortBy
    {
        /// <summary>
        /// 按时间戳排序 (默认)。
        /// </summary>
        kTimestamp,
        
        /// <summary>
        /// 按日志级别排序。
        /// </summary>
        kLevel
    }

    /// <summary>
    /// 定义日志查询结果的排序方向。
    /// </summary>
    public enum LogQuerySortDirection
    {
        /// <summary>
        /// 升序。
        /// </summary>
        kAscending,
        
        /// <summary>
        /// 降序 (默认，新日志在前)。
        /// </summary>
        kDescending
    }

    public class LogQueryParameters
    {
        /// <summary>
        /// Specifies the start time for querying logs.
        /// Only log entries with a timestamp on or after this time will be included in the query results.
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// Specifies the end time for querying logs.
        /// Only log entries with a timestamp on or before this time will be included in the query results.
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Specifies the minimum log level to include in the query results.
        /// Only log entries with a level equal to or higher than this value will be included.
        /// </summary>
        public LogLevel? MinimumLevel { get; set; }
        
        /// <summary>
        /// 查询的精确日志级别。如果设置了 MinimumLevel，此字段通常无效。
        /// </summary>
        public LogLevel? ExactLevel { get; set; }

        /// <summary>
        /// Specifies a substring that should be present in the log message to include the log entry in the query results.
        /// Only log entries containing this substring in their message will be matched.
        /// </summary>
        public string MessageContains { get; set; }
        
        /// <summary>
        /// 包含的日志空间路径列表 (精确匹配或前缀匹配，取决于实现)。如果为 null 或空，表示不按 LogSpace 过滤。
        /// </summary>
        public List<string> IncludedLogSpaces { get; set; }

        /// <summary>
        /// Specifies the log spaces to exclude from the query results.
        /// Entries associated with any of the specified log spaces will be omitted from the output.
        /// </summary>
        public List<string> ExcludedLogSpaces { get; set; }

        /// <summary>
        /// Specifies a collection of unique identifiers (GUIDs) representing the runs or sessions to be included in
        /// the log query. Only log entries associated with these specified run IDs will be considered in the query
        /// results.
        /// </summary>
        public List<Guid> IncludedRunIds { get; set; }

        /// <summary>
        /// Represents a collection of run identifiers that should be excluded from the query results.
        /// Only log entries associated with run identifiers not in this list will be included in the query.
        /// </summary>
        public List<Guid> ExcludedRunIds { get; set; }

        /// <summary>
        /// Determines the criteria by which the log entries should be sorted.
        /// Can be set to sort by timestamp or log level.
        /// </summary>
        public LogQuerySortBy SortBy { get; set; } = LogQuerySortBy.kTimestamp;

        /// <summary>
        /// Determines the sorting direction to be applied when querying log entries.
        /// Specifies whether the results should be ordered in ascending or descending order based on the chosen sorting criteria.
        /// </summary>
        public LogQuerySortDirection SortDirection { get; set; } = LogQuerySortDirection.kDescending;

        /// <summary>
        /// Defines the number of log entries to include in a single page of results when pagination is enabled.
        /// A value greater than zero activates pagination and limits the number of log entries returned.
        /// </summary>
        public int PageSize { get; set; } = 0;

        /// <summary>
        /// Determines the current page number for paginated log query results.
        /// Used in conjunction with the PageSize property to calculate the starting index
        /// of the results within the total dataset.
        /// </summary>
        public int PageNumber { get; set; } = 1;
        
        /// <summary>
        /// 按源文件名过滤
        /// </summary>
        public string SourceFileName { get; set; }

        /// <summary>
        /// 按方法名过滤
        /// </summary>
        public string SourceMemberName { get; set; }

        /// <summary>
        /// 按场景名过滤
        /// </summary>
        public List<string> SceneNames { get; set; }

        /// <summary>
        /// 按线程ID过滤
        /// </summary>
        public List<int> ThreadIds { get; set; }

        /// <summary>
        /// 按游戏对象信息过滤
        /// </summary>
        public string GameObjectContains { get; set; }

        /// <summary>
        /// 自定义上下文数据过滤器
        /// </summary>
        public Dictionary<string, string> ContextDataFilters { get; set; }

        /// <summary>
        /// 是否包含堆栈跟踪的日志
        /// </summary>
        public bool? HasStackTrace { get; set; }

        /// <summary>
        /// 正则表达式匹配消息内容
        /// </summary>
        public string MessageRegexPattern { get; set; }

        /// <summary>
        /// 自定义过滤表达式（高级用法）
        /// </summary>
        public Expression<Func<LogEntry, bool>> CustomFilter { get; set; }

        public LogQueryParameters()
        {
        }
        
        public LogQueryParameters(LogLevel? minimumLevel = null, string messageContains = null)
        {
            MinimumLevel = minimumLevel;
            MessageContains = messageContains;
        }

        public bool IsPaginationEnabled => PageSize > 0 && PageNumber >= 1;

        public int SkipCount => IsPaginationEnabled ? (PageNumber - 1) * PageSize : 0;
        public int TakeCount => IsPaginationEnabled ? PageSize : int.MaxValue;// int.MaxValue effectively means "take all remaining" if not paginating
    }
}