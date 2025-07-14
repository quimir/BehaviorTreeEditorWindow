using LogManager.Core;
using Serilog.Events;

namespace ExTools.Utillties
{
    public static  class LogLevelConverter
    {
        public static LogEventLevel ToSerilogLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.kTrace:
                    return LogEventLevel.Verbose;
                case LogLevel.kDebug:
                    return LogEventLevel.Debug;
                case LogLevel.kInfo:
                    return LogEventLevel.Information;
                case LogLevel.kWarning:
                    return LogEventLevel.Warning;
                case LogLevel.kError:
                    return LogEventLevel.Error;
                case LogLevel.kCritical:
                    return LogEventLevel.Fatal;
                default:
                    return LogEventLevel.Information;
            }
        }

        public static LogEventLevel StringToSerilogLevel(string level)
        {
            switch (level)
            {
                case "Verbose":
                    return LogEventLevel.Verbose;
                case "Debug":
                    return LogEventLevel.Debug;
                case "Information":
                    return LogEventLevel.Information;
                case "Warning":
                    return LogEventLevel.Warning;
                case "Error":
                    return LogEventLevel.Error;
                case "Fatal":
                    return LogEventLevel.Fatal;
                default:
                    return LogEventLevel.Information;
            }
        }

        public static LogLevel FromSerilogLevel(LogEventLevel level)
        {
            switch (level)
            {
                case LogEventLevel.Verbose:
                    return LogLevel.kTrace;
                case LogEventLevel.Debug:
                    return LogLevel.kDebug;
                case LogEventLevel.Information:
                    return LogLevel.kInfo;
                case LogEventLevel.Warning:
                    return LogLevel.kWarning;
                case LogEventLevel.Error:
                    return LogLevel.kError;
                case LogEventLevel.Fatal:
                    return LogLevel.kCritical;
                default:
                    // Serilog 有更多级别，这里做近似映射
                    if (level < LogEventLevel.Information) return LogLevel.kDebug;
                    return LogLevel.kError; // Error 及以上都映射到 Error 或 Critical
            }
        }

        /// <summary>
        /// 将日志级别转换为字符串，转换一般为全部大写的格式
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <returns>日志级别对应的字符串</returns>
        public static string LogLevelToString(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.kTrace:
                    return "TRACE";
                case LogLevel.kDebug:
                    return "DEBUG";
                case LogLevel.kInfo:
                    return "INFO";
                case LogLevel.kWarning:
                    return "WARNING";
                case LogLevel.kError:
                    return "ERROR";
                case LogLevel.kCritical:
                    return "CRITICAL";
                default:
                    return "UNKNOWN";
            }
        }

        public static LogLevel FromSerilogLevelInt(int log_level_int)
        {
            switch (log_level_int)
            {
                case 0:
                    return LogLevel.kTrace;
                case 1:
                    return LogLevel.kDebug;
                case 2:
                    return LogLevel.kInfo;
                case 3:
                    return LogLevel.kWarning;
                case 4:
                    return LogLevel.kError;
                case 5:
                    return LogLevel.kCritical;
                default:
                    return LogLevel.kDebug;
            }
        }

        public static int ToSerilogLevelInt(LogLevel minLevel)
        {
            return (int)minLevel;
        }
    }
}
