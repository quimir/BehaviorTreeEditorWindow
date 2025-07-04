using System;
using System.Collections.Generic;
using System.IO;
using LiteDB;
using LogManager.Core;
using LogManager.LogManagerFactory;
using Script.LogManager.QueryService;
using Script.Utillties;

namespace LogManager.QueryService.Storage
{
    public class SerilogLogQuery:ILogQuery
    {
        private readonly string db_path_;

        private const string LogCollectionName = FixedValues.kSeriLogLogSpace;

        public SerilogLogQuery(string lite_db_file_path)
        {
            if (string.IsNullOrEmpty(lite_db_file_path))
            {
                throw new ArgumentException("LiteDB database file path cannot be null or empty.", nameof(lite_db_file_path));
            }

            try
            {
                db_path_ = Path.GetFullPath(lite_db_file_path);
            }
            catch (Exception e)
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(new LogSpaceNode("LogQuery").AddChild("SerilogLogQuery"),new LogEntry(LogLevel.kError,$"Invalid LiteDB file path for querying: {lite_db_file_path}. Error: {e.Message}"));
                throw;
            }

            if (!File.Exists(db_path_))
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(new LogSpaceNode("LogQuery").AddChild("SerilogLogQuery"), new LogEntry(LogLevel.kWarning,$"LiteDB database file not found at: {db_path_}. Queries will return empty results."));
            }
        }

        private BsonExpression BuildLiteDbQuery(LogQueryParameters parameters)
        {
            var filters = new List<BsonExpression>();

            // 时间范围 (_t 字段)
        if (parameters.StartTime.HasValue)
        {
             // $._t >= @0 表示文档的 _t 字段大于等于第一个参数
            filters.Add(BsonExpression.Create("$._t >= @0", parameters.StartTime.Value));
        }
        if (parameters.EndTime.HasValue)
        {
            filters.Add(BsonExpression.Create("$._t <= @0", parameters.EndTime.Value));
        }

        // 日志级别 (_l 字段)
        if (parameters.ExactLevel.HasValue)
        {
            string levelName = LogLevelConverter.LogLevelToString(parameters.ExactLevel.Value);
            filters.Add(BsonExpression.Create("$._l = @0", levelName));
        }

        // 消息内容 (_mt 字段)
        if (!string.IsNullOrEmpty(parameters.MessageContains))
        {
             string likePattern = $"%{parameters.MessageContains}%";
             // $._mt LIKE @0 表示 _mt 字段匹配 like 模式
            filters.Add(BsonExpression.Create("$._mt LIKE @0", likePattern));
        }

        // LogSpace (LogSpace 字段)
        if (parameters.IncludedLogSpaces != null && parameters.IncludedLogSpaces.Count > 0)
        {
            // 构建一个单独的 BsonExpression，用 Or 组合所有包含的 LogSpace 条件
            BsonExpression logSpaceFilter = null;
            foreach (var space in parameters.IncludedLogSpaces)
            {
                var currentSpaceFilter = BsonExpression.Create("$.LogSpace LIKE @0", $"{space}%");
                if (logSpaceFilter == null)
                {
                    logSpaceFilter = currentSpaceFilter;
                }
                else
                {
                    logSpaceFilter=LiteDB.Query.Or(logSpaceFilter,currentSpaceFilter);
                }
            }
            if (logSpaceFilter != null)
            {
                filters.Add(logSpaceFilter); // 将 LogSpace 过滤条件加入主列表
            }
        }

        return null;
        }
        
        public IEnumerable<LogEntry> Query(LogQueryParameters parameters)
        {
            throw new System.NotImplementedException();
        }

        public int GetCount(LogQueryParameters parameters)
        {
            throw new System.NotImplementedException();
        }
    }
}
