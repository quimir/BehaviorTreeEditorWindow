using System;
using System.Collections.Generic;
using LiteDB;
using LogManager.Core;
using Script.Utillties;
using UnityEngine;

namespace LogManager.QueryService
{
    public class LogQueryService : IDisposable
    {
        private readonly string db_path_;
        private readonly string collection_name_;
        private LiteDatabase db_;

        public LogQueryService(string db_path, string collection_name = "logs")
        {
            if (string.IsNullOrEmpty(db_path))
                throw new ArgumentNullException(nameof(db_path));
            if (string.IsNullOrEmpty(collection_name))
                throw new ArgumentNullException(nameof(collection_name));

            db_path_ = db_path;
            collection_name_ = collection_name;

            try
            {
                // 以共享模式打开数据库，允许SerilogWriter同时写入
                db_ = new LiteDatabase(new ConnectionString
                    { Filename = db_path_, Connection = ConnectionType.Shared });
                EnsureIndices();
            }
            catch (Exception e)
            {
                Debug.LogError($"[LogQueryService] 无法打开或初始化 LiteDB 数据库 '{db_path_}': {e.Message}");
                db_ = null;
            }
        }

        private void EnsureIndices()
        {
            if (db_ == null) return;

            try
            {
                var col = db_.GetCollection(collection_name_);
                // 为时间戳建立索引（几乎总是需要）
                col.EnsureIndex("Timestamp"); // Serilog Sink 默认使用的字段名
                // 为 RunId 建立索引 (Properties.RunId)
                col.EnsureIndex("Properties.RunId");
                // 为 LogSpace 建立索引 (Properties.LogSpace)
                col.EnsureIndex("Properties.LogSpace");
                // 为日志级别建立索引 (Level - 通常是整数)
                col.EnsureIndex("Level");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LogQueryService] 建立索引时出错: {e.Message}");
            }
        }

        public virtual IEnumerable<BsonDocument> FindRaw(BsonExpression query = null, int skip = 0,
            int limit = int.MaxValue, string order_by_field = "Timestamp", bool ascending = false)
        {
            if (db_ == null)
                // 如果数据库无效，返回空
                yield break;

            IEnumerable<BsonDocument> results;

            try
            {
                var col = db_.GetCollection(collection_name_);

                if (query == null)
                    results = col.Find(
                        ascending ? Query.All(order_by_field) : Query.All(order_by_field, Query.Descending), skip,
                        limit);
                else
                    results = col.Find(query, skip, limit);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LogQueryService] 查询日志时出错: {e.Message}");
                yield break; // 出现异常时结束协程
            }

            if (results != null)
                foreach (var doc in results)
                    yield return doc;
        }

        public IEnumerable<LogEntry> Find(BsonExpression query = null, int skip = 0, int limit = int.MaxValue,
            string order_by_field = "Timestamp", bool ascending = false)
        {
            foreach (var doc in FindRaw(query, skip, limit, order_by_field, ascending))
            {
                if (TryConvertBsonToLogEntry(doc,out var logEntry))
                {
                    yield return logEntry;
                }
                else
                {
                    Debug.LogWarning($"[LogQueryService] 无法将 BSON 转换为 LogEntry: {doc.ToString()}");
                }
            }
        }
        
        // --- 便捷查询方法 (基于 Find) ---

        public IEnumerable<LogEntry> GetLogsByRunId(Guid runId, int skip = 0, int limit = int.MaxValue)
        {
            // 假设 RunId 存储在 Properties.RunId
            var query = Query.EQ("Properties.RunId", new BsonValue(runId));
            return Find(query, skip, limit);
        }

        public IEnumerable<LogEntry> GetLogsByExactSpace(string logSpace, int skip = 0, int limit = int.MaxValue)
        {
            // 假设 LogSpace 存储在 Properties.LogSpace
            var query = Query.EQ("Properties.LogSpace", logSpace);
            return Find(query, skip, limit);
        }

        public IEnumerable<LogEntry> GetLogsBySpacePrefix(string logSpacePrefix, int skip = 0, int limit = int.MaxValue)
        {
            var query = Query.StartsWith("Properties.LogSpace", logSpacePrefix);
            return Find(query, skip, limit);
        }

        public IEnumerable<LogEntry> GetLogsByLevel(LogLevel minLevel, int skip = 0, int limit = int.MaxValue)
        {
            // Serilog 内部 Level 是 LogEventLevel 枚举，通常存为整数
            // 需要知道 LogLevel 和 LogEventLevel 的映射关系
            int serilogLevelValue = LogLevelConverter.ToSerilogLevelInt(minLevel); // 你需要实现这个转换
            var query = Query.GTE("Level", serilogLevelValue); // 大于等于指定级别
            return Find(query, skip, limit);
        }

        public IEnumerable<LogEntry> GetLogsByDateRange(DateTime startDateUtc, DateTime endDateUtc, int skip = 0, int limit = int.MaxValue)
        {
            var query = Query.And(
                Query.GTE("Timestamp", startDateUtc),
                Query.LTE("Timestamp", endDateUtc)
            );
            return Find(query, skip, limit, "Timestamp", true); // 按时间升序
        }

        public virtual int Delete(BsonExpression query)
        {
            if (db_==null)
            {
                return 0;
            }

            if (query==null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            try
            {
                var col = db_.GetCollection(collection_name_);
                return col.DeleteMany(query);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LogQueryService] 删除日志时出错: {e.Message}");
                return 0;
            }
        }

        public int DeleteOlderThan(DateTime cutoff_date_utc)
        {
            var query=Query.LT("Timestamp", cutoff_date_utc);
            return Delete(query);
        }

        public bool TryConvertBsonToLogEntry(BsonDocument doc, out LogEntry log_entry)
        {
            log_entry = null;

            if (doc == null) return false;

            try
            {
                // 提取核心字段
                var timestamp = doc["Timestamp"].AsDateTime;
                var levelInt = doc["Level"].AsInt32; // Serilog LogEventLevel as int
                var message = doc.TryGetValue("RenderedMessage", out var value)
                    ?
                    value.AsString
                    : // Serilog 可能有 RenderedMessage
                    doc.TryGetValue("MessageTemplate", out var value1)
                        ? value1.AsString
                        : // 或者只有 MessageTemplate
                        ""; // 或 MessageTemplate (根据Sink配置)
                // --- 需要实现从 Serilog int 到你的 LogLevel 枚举的转换 ---
                var level = LogLevelConverter.FromSerilogLevelInt(levelInt);

                // Serilog 可能将自定义属性存储在 "Properties" 子文档中
                var properties = doc.TryGetValue("Properties", out var value2) ? value2.AsDocument : null;
                var threadId = properties != null && properties.TryGetValue("ThreadId", out var property1)
                    ? property1.AsInt32
                    : 0; // Serilog Enricher 加的 ThreadId
                var logSpace = properties != null && properties.TryGetValue("LogSpace", out var property2)
                    ? property2.AsString
                    : "Unknown";

                // 提取其他你在 LogEntry 中定义的字段 (如果它们也被存储了)
                var memberName = properties != null && properties.TryGetValue("SourceMemberName", out var property3)
                    ? property3.AsString
                    : "";
                var filePath = properties != null && properties.TryGetValue("SourceFilePath", out var property4)
                    ? property4.AsString
                    : "";
                var lineNumber = properties != null && properties.TryGetValue("SourceLineNumber", out var property5)
                    ? property5.AsInt32
                    : 0;
                var sceneName = properties != null && properties.TryGetValue("SceneName", out var property6)
                    ? property6.AsString
                    : "";
                var gameObjectInfo = properties != null && properties.TryGetValue("GameObjectInfo", out var property7)
                    ? property7.AsString
                    : "";

                // 提取异常信息 (Serilog 可能存为 "Exception" 字段)
                var stackTrace = doc.TryGetValue("Exception", out var value3) ? value3.AsString : null;
                // 如果保存了 StackTraceString 属性:
                if (stackTrace == null && properties != null && properties.TryGetValue("StackTraceString", out var property8))
                    stackTrace = property8.AsString;

                // 提取自定义上下文 ContextData (这个比较复杂，取决于你怎么存的)
                // 假设 LogInternal 将 ContextData 的 KVP 作为顶级属性或 Properties 子属性添加了
                var contextData = new Dictionary<string, string>();
                if (properties != null)
                {
                    // 查找 Properties 中不属于标准 Enricher 的字段，认为是 ContextData
                    var standardProps = new HashSet<string>
                    {
                        "RunId", "ThreadId", "LogSpace", "SourceMemberName", "SourceFilePath", "SourceLineNumber",
                        "SceneName", "GameObjectInfo", "ApplicationVersion", "UnityVersion", "RunIdShort",
                        "StackTraceString" /*和其他已知的标准属性*/
                    };
                    foreach (var kvp in properties)
                        if (!standardProps.Contains(kvp.Key))
                            contextData[kvp.Key] = kvp.Value.AsString; // 假设值都是字符串
                }
                
                // 使用 LogEntry 的构造函数创建实例
                // 注意：LogEntry 构造函数可能需要调整，因为它原本期望很多参数来自 CallerInfo，
                // 而这里我们是从数据库读取的。最好有一个专门用于反序列化的构造函数或 Set 方法。
                // 这里用一个 *假设的* 构造函数或方法来填充数据：

                log_entry = new LogEntry(level, message)
                {
                    Timestamp = timestamp,
                    ThreadId = threadId,
                    OriginalCategory = logSpace,
                    SourceMemberName = memberName,
                    SourceFilePath = filePath,
                    SourceLineNumber = lineNumber,
                    SceneName = sceneName,
                    StackTrace = stackTrace,
                    ContextData = contextData,
                    GameObjectInfo = gameObjectInfo
                };

                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LogQueryService] BSON 到 LogEntry 转换失败: {e.Message}. BSON: {doc.ToString()}");
                log_entry = null;
                return false;
            }
        }

        public void Dispose()
        {
            db_?.Dispose();
            db_ = null;
        }
    }
}