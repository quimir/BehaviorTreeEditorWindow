using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Script.Tool;
using Script.Utillties;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LogManager.Core
{
    public enum LogLevel
    {
        /// <summary>
        /// Trace level log, intended for detailed informational events typically useful during development and debugging.
        /// </summary>
        kTrace,

        /// <summary>
        /// 调试级别
        /// </summary>
        kDebug,

        /// <summary>
        /// 一般信息
        /// </summary>
        kInfo,

        /// <summary>
        /// 警告信息
        /// </summary>
        kWarning,

        /// <summary>
        /// 错误信息
        /// </summary>
        kError,

        /// <summary>
        /// 严重信息
        /// </summary>
        kCritical
    }

    /// <summary>
    /// Represents a log entry containing details about a logging event, such as timestamp, log level, message,
    /// thread ID, caller information, stack trace, and additional contextual data.
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// 时间戳，用来记录当前的时间
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 日志级别
        /// </summary>
        public LogLevel Level { get; set; }

        /// <summary>
        /// 具体消息内容
        /// </summary>
        public string Message { get; set; }

        // --- Added Context Fields ---
        /// <summary>
        /// 托管线程ID
        /// </summary>
        public int ThreadId { get; set; }

        /// <summary>
        /// Category 不再是主要过滤字段，但可以保留原始值供参考
        /// </summary>
        public string OriginalCategory { get; set; }

        /// <summary>
        /// 记录当前调用日志的方法属性名称,此属性会由系统自动收集不需要填写
        /// </summary>
        public string SourceMemberName { get; set; }

        /// <summary>
        /// 源文件的路径，该路径会调度系统方法自动填写
        /// </summary>
        public string SourceFilePath { get; set; }

        /// <summary>
        /// 调用日志方法的源文件行号，此方法也是会调度系统方法自动填写
        /// </summary>
        public int SourceLineNumber { get; set; }

        /// <summary>
        /// 堆栈跟踪，其会在Level>=kError的时候自动启用跟踪。其会自动进行调度系统功能进行跟踪。
        /// </summary>
        public string StackTrace { get; set; }

        /// <summary>
        /// 当前活跃的场景名字，其会自动获取当前活跃的场景
        /// </summary>
        public string SceneName { get; internal set; }

        /// <summary>
        /// 相关游戏对象信息，其在填写之后收集必要的游戏对象信息以此来进行跟踪。 (e.g., "PlayerObject (PlayerController)")
        /// </summary>
        public string
            GameObjectInfo { get; set; }

        /// <summary>
        /// 动态添加自定义上下文信息。
        /// </summary>
        public Dictionary<string, string> ContextData { get; set; }

        /// <summary>
        /// 配置日志信息
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="message">具体消息内容</param>
        /// <param name="originalCategory">用于过滤分组(例如: Audio,NetWork,AI等),该分组已经被LogSpaceNode所取代</param>
        /// <param name="member_name">记录当前调用日志的方法属性名称,此属性会由系统自动收集不需要填写</param>
        /// <param name="file_path">源文件的路径，该路径会调度系统方法自动填写</param>
        /// <param name="line_number">调用日志方法的源文件行号，此方法也是会调度系统方法自动填写</param>
        /// <param name="stack_trace">堆栈跟踪，其会在Level>=kError的时候自动启用跟踪。其会自动进行调度系统功能进行跟踪。</param>
        /// <param name="context_object">相关游戏对象信息，其在填写之后收集必要的游戏对象信息以此来进行跟踪。 (e.g.,
        /// "PlayerObject (PlayerController)")</param>
        /// <param name="context_data">自定义上下文信息。</param>
        public LogEntry(LogLevel level, string message, string originalCategory = "Default",
            [CallerMemberName] string member_name = "",
            [CallerFilePath] string file_path = "",
            [CallerLineNumber] int line_number = 0,
            string stack_trace = null, UnityEngine.Object context_object = null,
            Dictionary<string, string> context_data = null)
        {
            Timestamp = DateTime.Now; // Or DateTime.UtcNow for consistency across timezones
            Level = level;
            Message = message;

            // --- Populate Added Fields ---
            ThreadId = Thread.CurrentThread.ManagedThreadId;
            OriginalCategory = originalCategory;
            SourceMemberName = member_name;
            SourceFilePath = Path.GetFileName(file_path);
            SourceLineNumber = line_number;
            StackTrace =
                stack_trace ?? (level >= LogLevel.kError ? Environment.StackTrace : null); // 仅在 Error 或更高级别默认记录堆栈
            SceneName = SceneManager.GetActiveScene().name;

            GameObjectInfo = context_object != null
                ? $"{context_object.name} ({context_object.GetType().Name})"
                : "N/A";

            ContextData = context_data ?? new Dictionary<string, string>(); // Initialize if null
        }

        /// <summary>
        /// Updates the scene name property of the log entry if the current thread is the Unity main thread.
        /// This method attempts to assign the name of the currently active Unity scene to the SceneName property.
        /// If the method fails to retrieve the scene name or is called from a non-main thread, fallback values are assigned.
        /// </summary>
        internal void UpdateSceneNameIfPossible()
        {
            if (IsUnityMainThread())
                try
                {
                    SceneName = SceneManager.GetActiveScene().name;
                }
                catch (Exception e)
                {
                    // 防御性编程，如果获取场景名失败
                    SceneName = "ErrorGettingScene";
                    // 考虑记录这个内部错误
                    Debug.LogWarning($"Failed to get active scene name for log entry: {e.Message}");
                }
            else
                SceneName = "N/A (Non-Main Thread)";
        }

        /// <summary>
        /// Determines if the current thread is the main thread of the Unity application.
        /// </summary>
        /// <returns>True if the current thread is the Unity main thread; otherwise, false.</returns>
        private static bool IsUnityMainThread()
        {
            return UnityThread.IsMainThread;
        }

        public override string ToString()
        {
            var string_builder = new StringBuilder();
            string_builder.Append($"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}]");
            string_builder.Append($"[{LogLevelConverter.LogLevelToString(Level)}]");
            string_builder.Append($"[TID: {ThreadId}]"); // 明确是线程ID

            if (!string.IsNullOrEmpty(OriginalCategory) && OriginalCategory != "Default")
                string_builder.Append($"[{OriginalCategory}]");

            string_builder.Append($"[Scene:{SceneName}]");
            if (GameObjectInfo != "N/A") string_builder.Append($"[GameObject:{GameObjectInfo}]");

            if (!string.IsNullOrEmpty(SourceMemberName) && SourceLineNumber > 0) // 确保有有效来源信息
                // 使用 Path.GetFileName 确保只显示文件名，而不是完整路径
                string_builder.Append($"[{Path.GetFileName(SourceFilePath)}:{SourceLineNumber} ({SourceMemberName})]");

            string_builder.Append($" [{Message}]");

            if (ContextData is { Count: > 0 })
            {
                string_builder.Append(" {");
                var first = true;
                foreach (var kvp in ContextData)
                {
                    if (!first) string_builder.Append(", ");

                    // 对 Key 和 Value 进行简单的转义，防止它们包含引号或特殊字符破坏结构
                    var escapedKey = kvp.Key.Replace("\"", "\\\"");
                    var escapedValue = kvp.Value.Replace("\"", "\\\"");
                    string_builder.Append($"\"{escapedKey}\":\"{escapedValue}\"");
                    first = false;
                }

                string_builder.Append("}");
            }

            // 将堆栈跟踪放在新行，以便阅读
            if (!string.IsNullOrEmpty(StackTrace))
                string_builder.Append($"{Environment.NewLine}StackTrace:{Environment.NewLine}{StackTrace}");

            return string_builder.ToString();
        }
    }
}