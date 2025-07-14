using System;

namespace LogManager.Core
{
    /// <summary>
    /// Defines an interface for writing log entries, enabling storage of log data in an organized and structured manner,
    /// ensuring logs are categorized by namespace or custom log space nodes.
    /// </summary>
    public interface ILogWriter : IDisposable
    {
        /// <summary>
        /// Adds a log entry to the specified log space with an optional flag for console output.
        /// </summary>
        /// <param name="log_space_node">The log space node where the log entry will be added.</param>
        /// <param name="message">The log entry that contains the message and details to be recorded.</param>
        /// <param name="out_console">Indicates whether the log entry should also be output to the console. The default value is false.</param>
        void AddLog(LogSpaceNode log_space_node, LogEntry message, bool out_console = false);
    }

    /// <summary>
    /// An interface representing a log writer capable of managing log entries with advanced functionalities, including
    /// retention policy application, log level updating, and ensuring persistent storage for logs.
    /// </summary>
    public interface IManagedLogWriter : ILogWriter
    {
        /// <summary>
        /// 获取现在日志运行的ID，请务必给每一个管理器生成一个唯一的ID。因为在运行当中数据库是不知道那一部分的消息是本次运行时产生的日志消息。而
        /// 如果将本次运行ID写入数据库之后管理器就知道在这次ID之后的所有信息都是本次运行时产生的（这对于追加模式非常有用！）。
        /// </summary>
        Guid GetCurrentRunId { get; }
        
        /// <summary>
        /// 获取当前的日志文件路径。如果对其重新设置的话可能需要更严格的检查，此需要看日志管理器是否支持。
        /// </summary>
        string LogStoragePath { get; }

        /// <summary>
        /// Applies a retention policy that removes log entries older than the specified retention date.
        /// </summary>
        /// <param name="retention_date">The cutoff date; log entries older than this date will be deleted.</param>
        void ApplyRetentionPolicy(DateTime retention_date);

        /// <summary>
        /// Updates the minimum logging level for the log writer, ensuring that only logs at or above the specified level are recorded.
        /// </summary>
        /// <param name="new_level">The new logging level to set as the minimum threshold for log recording.</param>
        /// <returns>True if the minimum logging level was successfully updated; otherwise, false.</returns>
        bool UpdateMinimumLevel(LogLevel new_level);

        /// <summary>
        /// 触发将所有待处理日志刷写到磁盘的操作，并为安全关闭做准备。在应用程序退出前调用此方法以确保日志不丢失。
        /// </summary>
        void SaveLogs();
        
        /// <summary>
        /// 确保日志写入器已被正确初始化并准备好接收日志。如果它尚未初始化或已被清理，则进行重新配置。
        /// </summary>
        void EnsureInitialized();
    }
}