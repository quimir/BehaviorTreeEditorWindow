using System.Collections.Generic;
using LogManager.Core;

namespace LogManager.QueryService.Core
{
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