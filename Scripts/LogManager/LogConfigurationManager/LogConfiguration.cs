using System;
using System.IO;
using ExTools.Utillties;
using Save.Serialization;
using UnityEngine;

namespace LogManager.LogConfigurationManager
{
    public enum LogWriteMode
    {
        /// <summary>
        /// 追加到文件末尾
        /// </summary>
        kAppend,

        /// <summary>
        /// 每次保存时覆盖整个文件(主要用于确保单文件严格排序)
        /// </summary>
        kOverwrite
    }

    /// <summary>
    /// Represents the configuration settings for logging operations.
    /// </summary>
    public class LogConfiguration
    {
        /// <summary>
        /// Directory path where log files will be stored.
        /// </summary>
        public string LogDirectory { get; set; } = Path.Combine(Application.persistentDataPath, "logs");

        /// <summary>
        /// Base file name used for creating log files.
        /// </summary>
        public string BaseLogFileName { get; set; } = "unity_log";

        /// <summary>
        /// File extension used for log files.
        /// </summary>
        public string LogExtensionName { get; set; } = "log";

        /// <summary>
        /// Specifies the mode in which log files are written, either by appending to existing files
        /// or overwriting them.
        /// </summary>
        public LogWriteMode LogWriteMode { get; set; } = LogWriteMode.kAppend;

        /// <summary>
        /// Determines whether log file rotation is enabled. If set to true, log files will be rotated
        /// when they reach the configured maximum size.
        /// </summary>
        public bool EnableRotation { get; set; } = true;

        /// <summary>
        /// Maximum size of a single log file in megabytes before rotation occurs.
        /// </summary>
        public float MaxFileSizeMB { get; set; } = 10.0f;
        
        [SerializeField]
        private float max_total_directory_size_mb_ = FixedValues.kDefaultMaxTotalDirectorySizeMB;

        /// <summary>
        /// Specifies the maximum total size, in megabytes, of all log files within the directory.
        /// If the <see cref="LogWriteMode"/> is set to Overwrite, this value defaults to <see cref="MaxFileSizeMB"/>.
        /// </summary>
        public float MaxTotalDirectorySizeMB
        {
            get => LogWriteMode == LogWriteMode.kOverwrite ? MaxFileSizeMB : max_total_directory_size_mb_;
            set => max_total_directory_size_mb_ = value;
        }

        private int max_log_files_ = 5;

        /// <summary>
        /// Specifies the maximum number of log files to retain when log rotation is enabled.
        /// If log rotation is disabled, this property returns -1.
        /// </summary>
        public int MaxLogFiles
        {
            get
            {
                if (EnableRotation)
                {
                    return max_log_files_;
                }

                return -1;
            }
            
            set => max_log_files_ = value;
        }

        /// <summary>
        /// Indicates whether log retention policies are enabled.
        /// </summary>
        public bool EnableRetention { get; set; } = true;
        
        private int retention_days_ = 7;

        /// <summary>
        /// The number of days for which log files will be retained before being deleted.
        /// A value of 0 indicates that no logs will be deleted.
        /// </summary>
        public int RetentionDays
        {
            get
            {
                if (EnableRetention)
                {
                    return retention_days_;
                }

                return -1;
            }
            
            set => retention_days_ = value;
        }

        /// <summary>
        /// Configurable maximum number of recent log entries to retain per node in memory.
        /// </summary>
        public int MaxRecentLogsPerNode { get; set; } = FixedValues.kDefaultMaxRecentLogs;

        /// <summary>
        /// Full file path for the base log file, constructed using the log directory, base log file name, and log extension.
        /// </summary>
        public string BaseLogFilePath => Path.Combine(LogDirectory, $"{BaseLogFileName}.{LogExtensionName}");

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(LogDirectory) && !string.IsNullOrEmpty(BaseLogFileName) &&
                   (!EnableRotation || MaxFileSizeMB > 0) &&
                   (!EnableRetention || RetentionDays >= 0); // RetentionDays=0 表示不删除
        }
    }
}