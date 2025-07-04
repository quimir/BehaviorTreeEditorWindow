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
    public static class LogEntryConverter
    {
        public static QueriedLogEntry Convert(BsonDocument doc)
        {
            if (doc == null)
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(
                    new LogSpaceNode("LogEntryConverter"),
                    new LogEntry(LogLevel.kError, "Attempted to convert null BsonDocument to LogEntry."));
                return new QueriedLogEntry(new LogEntry(LogLevel.kError, "Error: Null document converted."),
                    "LogEntryConverter", Guid.Empty);
            }

            var entry = new LogEntry(LogLevel.kInfo, "Placeholder Message");

            try
            {
                // 根据 Serilog.Sinks.LiteDB 的默认映射规则进行转换
                entry.Timestamp = doc["_t"].AsDateTime;
                entry.Message = doc["_mt"].AsString ?? doc["_m"].AsString; // 使用格式化消息，回退到原始消息模板
                entry.Level =
                    LogLevelConverter.FromSerilogLevel(
                        LogLevelConverter
                            .StringToSerilogLevel(doc["_l"]
                                .AsString)); // 需要 LogLevelConverter.SerilogLevelToLogLevel 方法

                // 尝试从 Enrich 属性中读取其他信息
                entry.ThreadId = doc.TryGetValue("ThreadId", out var threadIdBson) ? threadIdBson.AsInt32 : 0;
                // Serilog 通常将 SourceFilePath 存储完整路径，我们在这里获取文件名
                entry.SourceFilePath = doc.TryGetValue("SourceFilePath", out var fileBson)
                    ? Path.GetFileName(fileBson.AsString)
                    : "N/A";
                entry.SourceLineNumber = doc.TryGetValue("SourceLineNumber", out var lineBson) ? lineBson.AsInt32 : 0;
                entry.SourceMemberName = doc.TryGetValue("SourceMemberName", out var memberBson)
                    ? memberBson.AsString
                    : "N/A";
                entry.SceneName = doc.TryGetValue("SceneName", out var sceneBson) ? sceneBson.AsString : "N/A";
                entry.GameObjectInfo = doc.TryGetValue("GameObjectInfo", out var goBson) ? goBson.AsString : "N/A";
                // StackTrace 可能是 Enrich 添加的 StackTraceString
                entry.StackTrace = doc.TryGetValue("StackTraceString", out var stackBson) ? stackBson.AsString : null;

                // 处理 ContextData (排除标准 Serilog 字段和 Enrich 字段)
                entry.ContextData = new Dictionary<string, string>();
                var mappedKeys = new HashSet<string>
                {
                    "_id", "_t", "_m", "_mt", "_l", "_i", // Standard Serilog keys
                    "RunId", "ThreadId", "SourceMemberName", "SourceFilePath", "SourceLineNumber",
                    "SceneName", "GameObjectInfo", "StackTraceString", "LogSpace" // SerilogWriter Enrich keys
                    // ... 任何其他已知的 Enrich 属性或标准 Serilog 属性
                };

                foreach (var element in doc.GetElements())
                    if (!mappedKeys.Contains(element.Key) && element.Value.IsNumber) // 仅添加简单标量值
                        entry.ContextData[element.Key] = element.Value.AsString; // 或根据类型转换为合适字符串
                // TODO: 如果 ContextData 可能包含嵌套结构，需要更复杂的逻辑
            }
            catch (Exception e)
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace)
                    .AddLog(new LogSpaceNode("LogEntryConverter"), new LogEntry(LogLevel.kError, e.ToString()));
                // 如果转换失败，返回一个表示错误的 LogEntry，并填充已知字段
                entry.Level = LogLevel.kError;
                entry.Message = $"Error converting document: {e.Message}. Raw: {doc.ToString()}";
                entry.Timestamp = DateTime.Now; // 填充当前时间或使用 doc["_t"] 如果它能被安全读取
            }

            var log_space_path =
                doc.TryGetValue("LogSpace", out var log_space_bson) ? log_space_bson.AsString : "N/A";
            var run_id = doc.TryGetValue("RunId", out var run_id_bson)
                ? run_id_bson.IsGuid ? run_id_bson.AsGuid : Guid.Empty
                : Guid.Empty;

            return new QueriedLogEntry(entry, log_space_path, run_id);
        }
    }
}