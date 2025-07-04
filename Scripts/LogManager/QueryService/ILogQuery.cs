using System;
using System.Collections.Generic;
using LogManager.Core;

namespace LogManager.QueryService
{
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
        public DateTime? StartTime { get; set; }
        
        public DateTime? EndTime { get; set; }
        
        public LogLevel? MinimumLevel { get; set; }
        
        /// <summary>
        /// 查询的精确日志级别。如果设置了 MinimumLevel，此字段通常无效。
        /// </summary>
        public LogLevel? ExactLevel { get; set; }
        
        public string MessageContains { get; set; }
        
        /// <summary>
        /// 包含的日志空间路径列表 (精确匹配或前缀匹配，取决于实现)。如果为 null 或空，表示不按 LogSpace 过滤。
        /// </summary>
        public List<string> IncludedLogSpaces { get; set; }
        
        public List<string> ExcludedLogSpaces { get; set; }

        public List<Guid> IncludedRunIds { get; set; }
        
        public List<Guid> ExcludedRunIds { get; set; }
        
        // -- 排序条件 --
        public LogQuerySortBy SortBy { get; set; } = LogQuerySortBy.kTimestamp;
        
        public LogQuerySortDirection SortDirection { get; set; } = LogQuerySortDirection.kDescending;
        
        // -- 分页条件 --
        public int PageSize { get; set; } = 0;

        public int PageNumber { get; set; } = 1;

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
    
    public interface ILogQuery
    {
        /// <summary>
        /// 根据指定的查询参数执行日志查询。
        /// </summary>
        /// <param name="parameters">查询参数，包含过滤、排序、分页等信息。</param>
        /// <returns>符合条件的日志条目集合。</returns>
        IEnumerable<LogEntry> Query(LogQueryParameters parameters);
        
        /// <summary>
        /// 根据指定的查询参数获取符合条件的日志总条数。
        /// </summary>
        /// <param name="parameters">查询参数，仅过滤条件会生效，分页/排序无效。</param>
        /// <returns>符合条件的日志总条数。</returns>
        int GetCount(LogQueryParameters parameters);
    }
}