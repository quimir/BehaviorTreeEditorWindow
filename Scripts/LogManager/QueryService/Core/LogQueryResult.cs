using System;
using System.Collections;
using System.Collections.Generic;
using LogManager.Core;

namespace LogManager.QueryService.Core
{
    public class LogQueryResult
    {
        /// <summary>
        /// 查询到的日志条目
        /// </summary>
        public IEnumerable<LogEntry> Entries { get; set; }

        /// <summary>
        /// 总记录数（用于分页）
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 当前页数
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// 每页大小
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;

        /// <summary>
        /// 查询执行时间
        /// </summary>
        public TimeSpan QueryExecutionTime { get; set; }

        /// <summary>
        /// 是否有下一页
        /// </summary>
        public bool HasNextPage => CurrentPage < TotalPages;

        /// <summary>
        /// 是否有上一页
        /// </summary>
        public bool HasPreviousPage => CurrentPage > 1;
    }
}