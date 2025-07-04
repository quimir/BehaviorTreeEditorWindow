using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using LiteDB;
using LogManager.Core;
using LogManager.LogConfigurationManager;
using Script.Utillties;
using Serilog;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;
using UnityEngine;
using Logger = Serilog.Core.Logger;

namespace LogManager.Storage
{
    /// <summary>
    /// The SerilogWriter class provides an implementation of the IManagedLogWriter interface,
    /// enabling integration with Serilog for logging functionality. This class manages log storage,
    /// log level configurations, log addition, and log retention policies, and provides mechanisms
    /// to ensure proper log initialization, saving, and disposal.
    /// </summary>
    public class SerilogWriter : IManagedLogWriter
    {
        private readonly LoggingLevelSwitch level_switch_; // 允许动态更改日志级别

        private Logger serilog_logger_; // 当前活动的Logger实例

        private readonly BaseConfigurationManager configuration_manager_;

        public Guid GetCurrentRunId { get; } = Guid.NewGuid();

        /// <summary>
        /// 当前日志数据库的路径
        /// </summary>
        private string current_db_path_;

        private readonly object logger_lock_ = new(); // 同步日志写入和重新配置锁

        private bool is_initialized_;

        public string LogStoragePath
        {
            get
            {
                lock (logger_lock_) // 确保读取的是有效路径
                {
                    return current_db_path_;
                }
            }

            set
            {
                AddLog(new LogSpaceNode("Root"),
                    new LogEntry(LogLevel.kInfo, $"[SerilogWriter] Attempting to set LogStoragePath to: {value}"));
                if (string.IsNullOrWhiteSpace(value))
                {
                    Debug.LogError("[SerilogWriter] New log storage path cannot be null or empty.");
                    return;
                }

                // 规划范路径格式
                var new_full_path = Path.GetFullPath(value);
                var new_directory = Path.GetDirectoryName(new_full_path);

                // 获取锁以进行检查和重新配置
                lock (logger_lock_)
                {
                    // 检查新路径是否与当前路径相同
                    if (string.Equals(current_db_path_, new_full_path, StringComparison.OrdinalIgnoreCase))
                    {
                        AddLog(new LogSpaceNode("Root"),
                            new LogEntry(LogLevel.kInfo,
                                $"[SerilogWriter] New path '{new_full_path}' is the same as the current path." +
                                $" No change needed."));
                        return;
                    }

                    // 1.检查并创建目录
                    if (string.IsNullOrEmpty(new_directory))
                    {
                        AddLog(new LogSpaceNode("Root"),
                            new LogEntry(LogLevel.kError,
                                $"[SerilogWriter] Could not determine directory from path: {new_full_path}"));
                        return; // 无法获取目录条目
                    }

                    // 更新配置并保存
                    if (configuration_manager_?.ManagerConfiguration != null)
                    {
                        // 设置value是DB文件路径，需要更新配置中的目录
                        configuration_manager_.ManagerConfiguration.LogDirectory = new_directory;
                        // 触发保存
                        AddLog(new LogSpaceNode("Root"),
                            new LogEntry(LogLevel.kInfo,
                                $"[SerilogWriter] Programmatically setting LogDirectory to: {new_directory} " +
                                $"and saving configuration."));
                        configuration_manager_?.Save(configuration_manager_.ManagerConfiguration,
                            configuration_manager_.GetDefaultPath());
                    }
                    else
                    {
                        Debug.LogError(
                            "[SerilogWriter] Cannot set LogStoragePath: Configuration Manager or Configuration is null.");
                    }
                }
            }
        }

        public SerilogWriter(BaseConfigurationManager configuration)
        {
            if (configuration == null)
            {
                Debug.LogError("[SerilogWriter] Configuration manager is null!");
                return;
            }

            configuration_manager_ = configuration;

            // 确保初始配置已加载（构造函数中可能已加载，这里是双重保险）
            configuration_manager_.ManagerConfiguration ??=
                configuration_manager_.Load(configuration_manager_.GetDefaultPath());

            // 保存当前的日志选项
            if (configuration_manager_.ManagerConfiguration != null &&
                !string.IsNullOrEmpty(configuration_manager_.GetDefaultPath()))
                configuration_manager_.Save(configuration_manager_.ManagerConfiguration,
                    configuration_manager_.GetDefaultPath());

            level_switch_ = new LoggingLevelSwitch(LogLevelConverter.ToSerilogLevel(LogLevel.kTrace));

            // 订阅配置更改事件
            configuration_manager_.ConfigurationChanged += HandleConfigurationChanged;
        }

        private void HandleConfigurationChanged(LogConfiguration new_configuration)
        {
            AddLog(new LogSpaceNode("Root"),
                new LogEntry(LogLevel.kInfo, "[SerilogWriter] Received configuration change notification."));

            // 检查路径是否改变，如果改变则重新配置Logger
            string new_potential_db_path;
            if (!string.IsNullOrEmpty(new_configuration.LogDirectory))
            {
                try
                {
                    new_potential_db_path = Path.Combine(new_configuration.LogDirectory,
                        new_configuration.BaseLogFilePath + "." + new_configuration.LogExtensionName);
                }
                catch (Exception e)
                {
                    LogInternal(null,
                        new LogEntry(LogLevel.kError,
                            $"[SerilogWriter] Error constructing new DB path from LogDirectory " +
                            $"'{new_configuration.LogDirectory}': {e.Message}"),
                        false);
                    return;
                }
            }
            else
            {
                LogInternal(null,
                    new LogEntry(LogLevel.kWarning,
                        "[SerilogWriter] LogDirectory in new configuration is empty. Cannot determine LiteDB " +
                        "path."),
                    false);
                return;
            }

            // 比较新计算的路径和当前路径，使用Path.GetFullPath来标准化路径进行比较
            string current_full_path = null;
            string new_full_path = null;

            try
            {
                if (!string.IsNullOrEmpty(current_db_path_)) current_full_path = Path.GetFullPath(current_db_path_);

                if (!string.IsNullOrEmpty(new_potential_db_path))
                    new_full_path = Path.GetFullPath(new_potential_db_path);
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[SerilogWriter] Error getting full path during configuration change check: {e.Message}");
                return; // 无法比较路径，不继续
            }

            // 只有当新路径有效且与当前路径不同才重新配置
            if (!string.IsNullOrEmpty(new_full_path) &&
                !string.Equals(current_full_path, new_full_path, StringComparison.OrdinalIgnoreCase))
            {
                LogInternal(null,
                    new LogEntry(LogLevel.kInfo,
                        $"[SerilogWriter] Log directory changed. Reconfiguring logger for new path: {new_full_path}"),
                    false);

                // 使用新的路径重新配置Logger
                // 注意：ConfigureAndAssignLogger 内部会处理关闭旧Logger、创建新Logger的逻辑
                // 它应该读取 configuration_manager_.Configuration 来获取最新的 LogDirectory
                EnsureInitialized();
            }
            else
            {
                LogInternal(null,
                    new LogEntry(LogLevel.kInfo,
                        "[SerilogWriter] Log directory unchanged or new path is invalid/empty. No logger path " +
                        "reconfiguration needed based on directory."),
                    false);
            }
        }

        public bool UpdateMinimumLevel(LogLevel new_level)
        {
            // LevelSwitch 的更改是线程安全的，不需要锁
            var serilogLevel = LogLevelConverter.ToSerilogLevel(new_level);
            level_switch_.MinimumLevel = serilogLevel;
            // UnityEngine.Debug.Log($"[SerilogWriter] Log level switch updated to: {serilogLevel}"); // Debug Log
            return level_switch_.MinimumLevel == serilogLevel;
        }

        #region 日志删除机制

        public void ApplyRetentionPolicy(DateTime retention_time)
        {
            if (!is_initialized_ || string.IsNullOrEmpty(current_db_path_))
            {
                Debug.LogWarning(
                    "[SerilogWriter] SerilogWriter is not initialized or LiteDB path is null. Cannot apply retention " +
                    "policy.");
                return;
            }

            var current_db_path = current_db_path_;

            if (!configuration_manager_.ManagerConfiguration.EnableRetention ||
                configuration_manager_.ManagerConfiguration.RetentionDays <= 0 ||
                configuration_manager_.ManagerConfiguration == null)
            {
                // 即使被外部调用，也要检查内部配置是否允许执行
                if (configuration_manager_.ManagerConfiguration is { EnableRetention: false })
                    AddLog(new LogSpaceNode("Root"),
                        new LogEntry(LogLevel.kInfo,
                            "[SerilogWriter] Retention policy disabled in configuration. Skipping cleanup."));
                else if (configuration_manager_.ManagerConfiguration is { RetentionDays: <= 0 })
                    AddLog(new LogSpaceNode("Root"),
                        new LogEntry(LogLevel.kInfo,
                            "[SerilogWriter] Retention policy enabled but RetentionDays <= 0. Skipping cleanup."));
                else
                    AddLog(new LogSpaceNode("Root"),
                        new LogEntry(LogLevel.kWarning,
                            "[SerilogWriter] Configuration is null or invalid for retention policy. Skipping cleanup."));

                return;
            }

            AddLog(new LogSpaceNode("Root"),
                new LogEntry(LogLevel.kInfo,
                    $"[SerilogWriter] Applying retention policy: Deleting logs older than " +
                    $"{retention_time:yyyy-MM-dd HH:mm:ss} UTC from {current_db_path}"));

            var was_initialized = is_initialized_;

            if (is_initialized_)
                lock (logger_lock_)
                {
                    // 关闭连接防止锁定
                    CloseAndFlushCurrentLogger();
                    Thread.Sleep(100);
                }

            try
            {
                // 执行清理操作
                ApplyRetentionPolicy();
            }
            finally
            {
                if (was_initialized)
                    lock (logger_lock_)
                    {
                        try
                        {
                            EnsureInitialized();

                            if (serilog_logger_ != null && Log.Logger == Logger.None)
                                AddLog(new LogSpaceNode("Root"), new LogEntry(LogLevel.kInfo,
                                    "[SerilogWriter] Logger state restored after retention policy application."));
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(
                                $"[SerilogWriter] Problems occurred when initializing with the log rotation mechanism. " +
                                $"The problems come from:{e.Message}");
                        }
                    }
            }
        }

        private void ApplyRetentionPolicy()
        {
            var config = configuration_manager_?.ManagerConfiguration;
            if (config == null) return;

            if (!config.EnableRetention)
            {
                var log_directory = config.LogDirectory;
                if (!Directory.Exists(log_directory)) return;

                CloseAndFlushCurrentLogger();
                Directory.Delete(log_directory, true);
            }
            else
            {
                if (!ApplyDirectoryTotalSizeLimitPolicy()) ApplyFileSystemRetentionPolicy();
            }
        }

        /// <summary>
        /// Applies a policy to enforce a maximum total size limit for log files within the log directory.
        /// </summary>
        /// <returns>
        /// A boolean value indicating whether the directory size limit policy was successfully applied. Returns false
        /// if the configuration is missing, the size limit is invalid, the specified directory does not exist, or an
        /// error occurs during the process.
        /// </returns>
        private bool ApplyDirectoryTotalSizeLimitPolicy()
        {
            var config = configuration_manager_?.ManagerConfiguration;
            if (config == null || config.MaxTotalDirectorySizeMB <= 0) return false;

            var log_directory = config.LogDirectory;
            if (!Directory.Exists(log_directory)) return false;

            try
            {
                // 计算文件夹总大小
                var dir_info = new DirectoryInfo(log_directory);
                var total_size_bytes = dir_info.GetFiles("*.*", SearchOption.AllDirectories).Sum(file => file.Length);

                var limit_bytes = (long)(config.MaxTotalDirectorySizeMB * 1024 * 1024);

                if (total_size_bytes > limit_bytes)
                {
                    Debug.LogWarning(
                        $"[SerilogWriter] Log directory size ({total_size_bytes / 1024.0 / 1024.0:F2} MB) " +
                        $"exceeds limit ({config.MaxFileSizeMB} MB). Deleting entire directory: {log_directory}");

                    CloseAndFlushCurrentLogger();

                    // 递归删除整个文件夹
                    Directory.Delete(log_directory, true);
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SerilogWriter] Error applying directory size limit policy: {e.Message}");
            }

            return false;
        }

        /// <summary>
        /// Applies file system retention policies to the log directory to manage log file quantity and persistence.
        /// This method evaluates the current log files in the directory and enforces retention policies based on
        /// both time (deleting logs older than a specified number of days) and count (limiting the number of log files).
        /// </summary>
        private void ApplyFileSystemRetentionPolicy()
        {
            var config = configuration_manager_?.ManagerConfiguration;
            if (config == null) return;

            var log_directory = config.LogDirectory;
            if (!Directory.Exists(log_directory)) return;

            // 构建文件匹配模式
            var search_pattern = $"{config.BaseLogFileName}-*.{config.LogExtensionName}";
            ApplyTimeBasedRetention(log_directory, search_pattern, config.RetentionDays);

            ApplyCountBasedRetention(log_directory, search_pattern, config.MaxLogFiles);
        }

        private void ApplyCountBasedRetention(string logDirectory, string searchPattern, int configMaxLogFiles)
        {
            if (configMaxLogFiles <= 0) return; // 不限制文件数量

            try
            {
                var log_files = Directory.GetFiles(logDirectory, searchPattern)
                    .Select(file => new FileInfo(file)).OrderBy(file_info => file_info.CreationTime).ToList();

                if (log_files.Count <= configMaxLogFiles) return; // 文件数量未超过限制

                var files_to_delete = log_files.Take(log_files.Count - configMaxLogFiles);

                foreach (var file in files_to_delete)
                    try
                    {
                        file.Delete();
                    }
                    catch (IOException ioEx)
                    {
                        Debug.LogWarning(
                            $"[SerilogWriter] Could not delete excess log file '{file.Name}' as it may be locked: " +
                            $"{ioEx.Message}");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[SerilogWriter] Error deleting excess log file '{file.Name}': {e.Message}");
                    }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SerilogWriter] Error in count-based retention: {e.Message}");
            }
        }

        /// <summary>
        /// Performs a cleanup operation on the log database to enforce retention policies by deleting log entries older
        /// than the specified cutoff date.
        /// </summary>
        /// <param name="db_path">The file path to the database where log entries are stored.</param>
        /// <param name="cutoff_date">The date and time threshold. Log entries created before this date will be
        /// deleted.</param>
        private void PerformRetentionCleanup(string db_path, DateTime cutoff_date)
        {
            const int max_retries = 3;

            const int max_total_time_ms = 300;

            var start_time = DateTime.Now;

            for (var i = 0; i < max_retries; i++)
                try
                {
                    if ((DateTime.Now - start_time).TotalMilliseconds > max_total_time_ms)
                    {
                        Debug.LogWarning(
                            "[SerilogWriter] Retention cleanup timeout. Skipping to avoid blocking initialization.");

                        return;
                    }

                    try
                    {
                        if (!CanAccessDatabase(db_path))
                        {
                            if (i < max_retries - 1)
                            {
                                Debug.LogWarning($"[SerilogWriter] Database locked, quick retry {i + 1}/{max_retries}");

                                Log.CloseAndFlush();

                                Thread.Sleep(16); // 约一帧的时间

                                continue;
                            }
                            else
                            {
                                Debug.LogWarning(
                                    "[SerilogWriter] Database remains locked. Skipping retention cleanup to avoid blocking.");

                                return;
                            }
                        }

                        using (var db = new LiteDatabase(db_path))
                        {
                            var collection = db.GetCollection("logs"); // 使用与Sink相同的集合名

                            var deleted_count = collection.DeleteMany(Query.LT("_t", cutoff_date));

                            // Only log if initialized and logging system is active
                            if (is_initialized_ && serilog_logger_ != null && Log.Logger != Logger.None)

                                AddLog(new LogSpaceNode("Root"), new LogEntry(LogLevel.kInfo, deleted_count > 0
                                    ? $"[SerilogWriter] Retention policy applied. Deleted {deleted_count} old log entries."
                                    : "[SerilogWriter] Retention policy applied. No old log entries found to delete."));
                            else // During initial cleanup, log to Unity's Debug directly
                                Debug.Log(deleted_count > 0
                                    ? $"[SerilogWriter] Retention policy applied. Deleted {deleted_count} old log " +
                                      $"entries during initialization."
                                    : "[SerilogWriter] Retention policy applied. No old log entries found to delete " +
                                      "during initialization.");
                            return; // Success, exit loop
                        }
                    }

                    catch (IOException ioEx) when (ioEx.Message.Contains("Sharing violation") ||
                                                   ioEx.Message.Contains("locked"))
                    {
                        if (i < max_retries - 1)
                        {
                            Debug.LogWarning($"[SerilogWriter] File locked, quick retry {i + 1}/{max_retries}");

                            Thread.Sleep(16); // 最小延迟
                        }
                        else
                        {
                            Debug.LogWarning(
                                "[SerilogWriter] Database file remains locked. Skipping retention cleanup.");

                            return; // 快速失败，不阻塞
                        }
                    }
                }

                catch (Exception e)
                {
                    Debug.LogError($"[SerilogWriter] Error applying retention policy: {e.Message}");

                    return;
                }
        }

        private bool CanAccessDatabase(string filePath)
        {
            if (!File.Exists(filePath))
                return true; // 文件不存在，可以创建
            try
            {
                // 快速检查：尝试以独占模式打开文件
                using (new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    // 立即关闭，只是测试访问性
                }

                return true;
            }

            catch (IOException)
            {
                return false; // 文件被锁定
            }
            catch (Exception)
            {
                return true; // 其他异常认为可以尝试访问
            }
        }

        private void ApplyTimeBasedRetention(string log_directory, string searchPattern, int configRetentionDays)
        {
            if (configRetentionDays <= 0) return; // RetentionDays=0 表示不删除

            var log_files = Directory.GetFiles(log_directory, searchPattern);
            var cutoff_data_utc = DateTime.UtcNow.AddDays(-configRetentionDays);

            foreach (var file_path in log_files)
                try
                {
                    var file_creation_time_utc = File.GetCreationTimeUtc(file_path);
                    if (file_creation_time_utc < cutoff_data_utc) File.Delete(file_path);
                }
                catch (IOException ioEx)
                {
                    Debug.LogWarning(
                        $"[SerilogWriter] Could not delete old log file '{Path.GetFileName(file_path)}' as it may " +
                        $"be locked: {ioEx.Message}");
                }
                catch (Exception e)
                {
                    Debug.LogError(
                        $"[SerilogWriter] Error deleting old log file '{Path.GetFileName(file_path)}': {e.Message}");
                }
        }

        #endregion

        #region 日志机制

        public void AddLog(LogSpaceNode log_space_node, LogEntry message, bool out_console = false)
        {
            if (log_space_node == null)
            {
                LogInternal("Undefined", message, out_console);
                return;
            }

            // 1.将日志添加到节点的内存缓冲区
            if (configuration_manager_.ManagerConfiguration.MaxRecentLogsPerNode > 0) // 检查配置是否允许内存缓存
                log_space_node.AddRecentLog(message, configuration_manager_.ManagerConfiguration.MaxRecentLogsPerNode);

            // 2. 将日志发送给 Serilog
            var log_space_path = log_space_node.GetFullPath();
            LogInternal(log_space_path, message, out_console);
        }

        private void LogInternal(string logSpacePath, LogEntry entry, bool out_console)
        {
            if (serilog_logger_ == null)
            {
                Debug.LogError("[SerilogWriter] Logger not initialized. Cannot log message.");
                return;
            }

            Logger logger_instance;

            lock (logger_lock_)
            {
                logger_instance = serilog_logger_;
            }

            // 更新场景名
            entry.UpdateSceneNameIfPossible();

            // 将LogEntry转换为Serilog能理解的格式
            var serilog_level = LogLevelConverter.ToSerilogLevel(entry.Level);

            IDisposable log_space_context = null;
            IDisposable member_name_context = null;

            var context_disposables = new List<IDisposable>();

            try
            {
                // 使用 Serilog 的上下文功能添加 LogSpace 和其他信息
                // LogContext 在 Serilog 中通常用于一个范围，这里我们为单条日志添加属性
                log_space_context = LogContext.PushProperty("LogSpace",
                    string.IsNullOrEmpty(logSpacePath) ? "Root" : logSpacePath);
                member_name_context = LogContext.PushProperty("SourceMemberName", entry.SourceMemberName);

                context_disposables.Add(LogContext.PushProperty("SourceFilePath", entry.SourceFilePath));
                context_disposables.Add(LogContext.PushProperty("SourceLineNumber", entry.SourceLineNumber));
                context_disposables.Add(LogContext.PushProperty("SceneName", entry.SceneName));
                context_disposables.Add(LogContext.PushProperty("GameObjectInfo", entry.GameObjectInfo));

                // 处理自定义上下文
                if (entry.ContextData != null)
                    foreach (var kvp in entry.ContextData)
                        // 使用PushProperty,它韩慧IDisposable，确保能被清理
                        context_disposables.Add(LogContext.PushProperty(kvp.Key, kvp.Value));

                Exception exception = null; //Serilog期望实际Exception类型
                if (!string.IsNullOrEmpty(entry.StackTrace) && entry.Level >= LogLevel.kError)
                    // 如果只有堆栈字符串，最好作为属性传递
                    context_disposables.Add(LogContext.PushProperty("StackTraceString", entry.StackTrace));

                // 使用收取到的LoggerInstance写入日志.Serilog 的 Write 方法是线程安全的，但我们获取 loggerInstance 需要在锁内完成
                logger_instance.Write(serilog_level, exception, entry.Message);
            }

            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                // 清理所有LogContext推入的属性
                if (log_space_context != null) log_space_context.Dispose();

                if (member_name_context != null) member_name_context.Dispose();

                foreach (var disposable in context_disposables) disposable.Dispose();
            }

            if (out_console) LogToUnityConsole(serilog_level, entry.ToString());
        }

        private void LogToUnityConsole(LogEventLevel level, string message)
        {
            switch (level)
            {
                case LogEventLevel.Error:
                case LogEventLevel.Fatal:
                    Debug.LogError(message);
                    break;
                case LogEventLevel.Warning:
                    Debug.LogWarning(message);
                    break;
                default:
                    Debug.Log(message);
                    break;
            }
        }

        #endregion

        public void SaveLogs()
        {
            Debug.Log("不需要显示调用SaveLogs因为在Serilog当中已经被Serilog接管");
        }

        #region 初始化部分

        public void EnsureInitialized()
        {
            lock (logger_lock_)
            {
                // 如果已经初始化，并且 Serilog.Log.Logger 也指向我们的实例，则无需再次初始化
                // 注意：检查 Serilog.Log.Logger 是否指向我们的实例可能有点复杂，
                // 简单起见，我们依赖 is_initialized_ 标记，并在 Dispose/Shutdown 时重置它。
                // 或者更严格地检查：Log.Logger 是否是 Logger.None 并且我们的内部 logger_ 为 null
                // 这里我们采取基于 is_initialized_ 标记的简单方法，结合 Dispose 时的重置。
                if (is_initialized_ && serilog_logger_ != null && !ReferenceEquals(serilog_logger_, Logger.None) &&
                    Log.Logger != Logger.None) return;

                // 先确定将要使用的数据库路径
                var db_path = DetermineDbPath();

                // 在初始化SeriLog之前先应用保留政策
                if (!string.IsNullOrEmpty(db_path) && configuration_manager_.ManagerConfiguration is
                        { EnableRetention: true, RetentionDays: > 0 })
                    try
                    {
                        // 确保目录存在
                        var directory = Path.GetDirectoryName(db_path);
                        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                            Directory.CreateDirectory(directory);

                        // 应用清理机制
                        ApplyRetentionPolicy();
                        if (File.Exists(db_path))
                        {
                            var cutoff_date_utc =
                                DateTime.UtcNow.AddDays(-configuration_manager_.ManagerConfiguration.RetentionDays);
                            // 直接在这里执行清理，此时尚未初始化Serilog，文件没有被锁定
                            PerformRetentionCleanup(db_path, cutoff_date_utc);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning(
                            $"[SerilogWriter] Failed to apply retention policy during initialization: {e.Message}");
                    }

                try
                {
                    InitializeInternalLogger(db_path);
                }
                catch (Exception e)
                {
                    Debug.LogError(
                        $"[SerilogWriter] Error initializing internal logger after retention policy: {e.Message}");
                    ResetToFailedState();
                }
            }
        }

        private void InitializeInternalLogger(string db_path = null)
        {
            CloseAndFlushCurrentLogger();
            // 如果没有提供db_path,则尝试从配置获取
            if (string.IsNullOrEmpty(db_path)) db_path = DetermineDbPath();

            if (string.IsNullOrEmpty(db_path))
            {
                Debug.LogError("[SerilogWriter] Base log file path is not configured. Cannot initialize logger.");
                return;
            }

            var new_logger_instance = CreateLoggerInstance(db_path);

            if (new_logger_instance == null || ReferenceEquals(new_logger_instance, Logger.None))
            {
                Debug.LogError("[SerilogWriter] Failed to create a valid Serilog logger instance.");
                ResetToFailedState();
                return;
            }

            lock (logger_lock_)
            {
                serilog_logger_ = new_logger_instance;
                Log.Logger = serilog_logger_;
                current_db_path_ = db_path;
                is_initialized_ = true;
            }
        }

        private Logger CreateLoggerInstance(string db_path)
        {
            // 确保日志目录存在
            if (!string.IsNullOrEmpty(db_path))
                try
                {
                    var directory = Path.GetDirectoryName(db_path);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                        Directory.CreateDirectory(directory);
                }
                catch (Exception e)
                {
                    Debug.LogError(
                        $"[SerilogWriter] Failed to create log directory for '{db_path}': {e.Message}. LiteDB logging " +
                        $"for this path may fail.");
                    // 如果创建目录失败，可能需要考虑是否继续配置 LiteDB sink
                    // 目前的设计是 CreateLoggerInstance 继续，配置 LiteDB Sink 会捕获异常
                }

            var logger_config = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(level_switch_)
                .Enrich.FromLogContext()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("RunId", GetCurrentRunId)
                .Enrich.WithProperty("ApplicationVersion", Application.version)
                .Enrich.WithProperty("UnityVersion", Application.unityVersion);

            if (!string.IsNullOrEmpty(db_path))
            {
                var collection_name = FixedValues.kSeriLogLogSpace;

                try
                {
                    logger_config.WriteTo.LiteDB(db_path, collection_name);
                }
                catch (Exception e)
                {
                    Debug.LogError(
                        $"[SerilogWriter] Failed to configure LiteDB sink for path '{db_path}': {e.Message}. LiteDB " +
                        $"logging for this path will be disabled.");
                }
            }
            else
            {
                Debug.LogWarning("[SerilogWriter] LiteDB path is null or empty. Skipping LiteDB sink configuration.");
            }

            logger_config.WriteTo.Sink(new UnityConsoleSink());

            try
            {
                var logger = logger_config.CreateLogger();
                return logger;
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[SerilogWriter] Failed to create Serilog logger instance: {e.Message}. Logging to Unity " +
                    $"Console will be disabled.");
                return (Logger)Logger.None;
            }
        }

        #endregion


        /// <summary>
        /// Determines the file path for the database used by the logging system, based on the current configuration
        /// settings.
        /// </summary>
        /// <returns>
        /// A string representing the full path to the database file if valid configuration is found, or an empty
        /// string if the configuration is insufficient or invalid.
        /// </returns>
        private string DetermineDbPath()
        {
            var current_config = configuration_manager_?.ManagerConfiguration;
            if (current_config == null)
            {
                Debug.LogError("[SerilogWriter] Cannot determine DB path: Configuration is null in manager.");
                return null;
            }

            return string.IsNullOrEmpty(configuration_manager_?.ManagerConfiguration.BaseLogFilePath)
                ? string.Empty
                : configuration_manager_.ManagerConfiguration.BaseLogFilePath;
        }

        #region 关闭机制

        private void CleanupCurrentLogger()
        {
            if (serilog_logger_ != null)
            {
                var logger_to_dispose = serilog_logger_;
                serilog_logger_ = null;

                if (ReferenceEquals(Log.Logger, logger_to_dispose)) Log.Logger = Logger.None;

                try
                {
                    (logger_to_dispose as IDisposable)?.Dispose();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SerilogWriter] Error disposing logger: {e.Message}");
                }
            }

            is_initialized_ = false;
            current_db_path_ = null;
        }

        private void CloseAndFlushCurrentLogger()
        {
            lock (logger_lock_)
            {
                if (serilog_logger_ == null && is_initialized_) return;

                CleanupCurrentLogger();
                ResetToFailedState();

                try
                {
                    Log.CloseAndFlush();
                }
                catch (Exception e)
                {
                    Debug.LogError("[SerilogWriter] Error while disposing the logger instance: " + e.Message);
                }
            }
        }

        private void ResetToFailedState()
        {
            serilog_logger_ = null;
            Log.Logger = Logger.None;
            is_initialized_ = false;
            current_db_path_ = null;
        }

        public void Dispose()
        {
            // 取消订阅配置更改事件
            if (configuration_manager_ != null)
            {
                configuration_manager_.ConfigurationChanged -= HandleConfigurationChanged;
                AddLog(new LogSpaceNode("Root"),
                    new LogEntry(LogLevel.kInfo, "[SerilogWriter] Unsubscribed from configuration changes."));
            }

            CloseAndFlushCurrentLogger();

            GC.SuppressFinalize(this);
        }

        #endregion
    }

    public class UnityConsoleSink : ILogEventSink
    {
        private readonly IFormatProvider format_provider_;

        public UnityConsoleSink(IFormatProvider format_provider = null)
        {
            format_provider_ = format_provider;
        }

        public void Emit(LogEvent logEvent)
        {
            // 使用 LogEvent 的 RenderMessage 获取格式化后的消息
            var message = logEvent.RenderMessage(format_provider_);

            if (logEvent.Properties.TryGetValue("LogSpace", out var logSpaceValue) && logSpaceValue is ScalarValue sv)
                message = $"[{sv.Value}] {message}";
        }
    }
}