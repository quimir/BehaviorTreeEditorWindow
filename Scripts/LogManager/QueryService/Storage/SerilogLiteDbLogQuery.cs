using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using LiteDB;
using LogManager.Core;
using LogManager.QueryService.Core;
using Debug = UnityEngine.Debug;

namespace LogManager.QueryService.Storage
{
    public class SerilogLiteDbLogQuery : ILogQuery,IDisposable
    {
        private readonly LiteDatabase data_base_;
        private readonly ILiteCollection<LogEntry> log_collection_;
        private bool disposed_ = false;

        public SerilogLiteDbLogQuery(string data_base_path)
        {
            data_base_ = new LiteDatabase(data_base_path);
            log_collection_ = data_base_.GetCollection<LogEntry>("logs");

            CreateIndexes();
        }

        private void CreateIndexes()
        {
            log_collection_.EnsureIndex(x => x.Timestamp);
            log_collection_.EnsureIndex(x => x.Level);
            log_collection_.EnsureIndex(x => x.SceneName);
            log_collection_.EnsureIndex(x => x.ThreadId);
            log_collection_.EnsureIndex(x => x.SourceFilePath);
        }

        public IEnumerable<LogEntry> Query(LogQueryParameters parameters)
        {
            var result = QueryWithResult(parameters);
            return result.Entries;
        }

        private LogQueryResult QueryWithResult(LogQueryParameters parameters)
        {
            var stop_watch = Stopwatch.StartNew();

            try
            {
                var query=BuildQuery(parameters);
                
                // 获取总数
                var total_count = query.Count();
                
                // 应用排序
                var sorted_query=ApplySorting(query, parameters);
                
                // 应用分页
                var paged_query=ApplyPaging(sorted_query, parameters);

                var entries = paged_query.ToList();
                
                stop_watch.Stop();

                return new LogQueryResult
                {
                    Entries = entries,
                    TotalCount = total_count,
                    CurrentPage = parameters.PageNumber,
                    PageSize = parameters.PageSize,
                    QueryExecutionTime = stop_watch.Elapsed
                };
            }
            catch (Exception e)
            {
                stop_watch.Stop();
                Debug.LogError($"日志查询失败: {e.Message}");
                throw;
            }
        }

        private IEnumerable<LogEntry> ApplyPaging(ILiteQueryable<LogEntry> sortedQuery, LogQueryParameters parameters)
        {
            if (!parameters.IsPaginationEnabled)
                return sortedQuery.ToEnumerable();

            return sortedQuery.Skip(parameters.SkipCount).Limit(parameters.TakeCount).ToEnumerable();
        }

        private ILiteQueryable<LogEntry> ApplySorting(ILiteQueryable<LogEntry> query, LogQueryParameters parameters)
        {
            switch (parameters.SortBy)
            {
                case LogQuerySortBy.kTimestamp:
                    return parameters.SortDirection == LogQuerySortDirection.kAscending 
                        ? query.OrderBy(x => x.Timestamp)
                        : query.OrderByDescending(x => x.Timestamp);
                    
                case LogQuerySortBy.kLevel:
                    return parameters.SortDirection == LogQuerySortDirection.kAscending 
                        ? query.OrderBy(x => x.Level)
                        : query.OrderByDescending(x => x.Level);
                    
                default:
                    return query.OrderByDescending(x => x.Timestamp);
            }
        }

        private ILiteQueryable<LogEntry> BuildQuery(LogQueryParameters parameters)
        {
            var query = log_collection_.Query();
            
            // 时间范围过滤
            if (parameters.StartTime.HasValue)
            {
                query=query.Where(x=>x.Timestamp>=parameters.StartTime.Value);
            }

            if (parameters.EndTime.HasValue)
            {
                query = query.Where(x=>x.Timestamp<=parameters.EndTime.Value);
            }
            
            // 日志级别过滤
            if (parameters.ExactLevel.HasValue)
            {
                query = query.Where(x => x.Level == parameters.ExactLevel.Value);
            }
            else if (parameters.MinimumLevel.HasValue)
            {
                query=query.Where(x=>x.Level>=parameters.MinimumLevel.Value);
            }
            
            // 消息内容过滤
            if (!string.IsNullOrEmpty(parameters.MessageContains))
            {
                query=query.Where(x=>x.Message.Contains(parameters.MessageContains));
            }
            
            // 正则表达式匹配
            if (!string.IsNullOrEmpty(parameters.MessageRegexPattern))
            {
                
            }
            
            // LogSpace过滤
            if (parameters.IncludedLogSpaces?.Any()==true)
            {
                query =
                    query.Where(x => parameters.IncludedLogSpaces.Any(space => x.OriginalCategory.StartsWith(space)));
            }

            if (parameters.ExcludedLogSpaces?.Any()==true)
            {
                query=query.Where(x=>!parameters.ExcludedLogSpaces.Any(space=>x.OriginalCategory.StartsWith(space)));
            }
            
            // 场景名过滤
            if (parameters.SceneNames?.Any()==true)
            {
                query=query.Where(x=>parameters.SceneNames.Contains(x.SceneName));
            }
            
            // 线程ID过滤
            if (parameters.ThreadIds?.Any()==true)
            {
                query=query.Where(x=>parameters.ThreadIds.Contains(x.ThreadId));
            }
            
            // 源文件名过滤
            if (!string.IsNullOrEmpty(parameters.SourceFileName))
            {
                query=query.Where(x=>x.SourceFilePath.Contains(parameters.SourceFileName));
            }
            
            // 方法名过滤
            if (!string.IsNullOrEmpty(parameters.SourceMemberName))
            {
                query = query.Where(x => x.SourceMemberName == parameters.SourceMemberName);
            }
            
            // 游戏对象过滤
            if (!string.IsNullOrEmpty(parameters.GameObjectContains))
            {
                query = query.Where(x => x.GameObjectInfo.Contains(parameters.GameObjectContains));
            }
            
            // 堆栈跟踪过滤
            if (parameters.HasStackTrace.HasValue)
            {
                query = parameters.HasStackTrace.Value ? query.Where(x=>!string.IsNullOrEmpty(x.StackTrace)) : 
                    query.Where(x=>string.IsNullOrEmpty(x.StackTrace));
            }

            return query;
        }
        
        private IEnumerable<LogEntry> ApplyPostProcessingFilters(IEnumerable<LogEntry> entries, LogQueryParameters parameters)
        {
            // 应用正则表达式过滤
            if (!string.IsNullOrEmpty(parameters.MessageRegexPattern))
            {
                var regex = new Regex(parameters.MessageRegexPattern, RegexOptions.IgnoreCase);
                entries = entries.Where(x => regex.IsMatch(x.Message));
            }

            // 应用上下文数据过滤
            if (parameters.ContextDataFilters?.Any() == true)
            {
                entries = entries.Where(entry =>
                {
                    if (entry.ContextData == null) return false;
                
                    return parameters.ContextDataFilters.All(filter =>
                        entry.ContextData.ContainsKey(filter.Key) &&
                        entry.ContextData[filter.Key].Contains(filter.Value));
                });
            }

            // 应用自定义过滤器
            if (parameters.CustomFilter != null)
            {
                var compiledFilter = parameters.CustomFilter.Compile();
                entries = entries.Where(compiledFilter);
            }

            return entries;
        }

        public int GetCount(LogQueryParameters parameters)
        {
            var query = BuildQuery(parameters);
            return query.Count();
        }

        public void Dispose()
        {
            if (!disposed_)
            {
                data_base_?.Dispose();
                disposed_ = true;
            }
        }
    }
}
