#define DEBUG_MODE

namespace ExTools.Utillties
{
    public static class FixedValues
    {
        public const int kMinNodeWidth = 250;
        public const int kMinNodeHeight = 80;
        public const int kStartNodeWidth = 180;
        public const int kStartNodeHeight = 100;
        public const string kBehaviorSaveDirectory = "BehaviorTrees";
        /// <summary>
        /// Represents the fixed distance value used to determine the spacing or proximity
        /// between nodes and lines in a graphical user interface, such as a behavior tree view.
        /// This constant is typically leveraged for visual alignment and collision checking
        /// between graph elements within a specified distance threshold.
        /// </summary>
        public const int kNodeBetweenLineDistance = 50;
        public const int kMaxFilePaths = 10;
        public const int kSerializableDataVersion = 1;
        public const int kPointNumber = 4;
        public const string kBtDateFileExtension = ".btwindowtemp";
        public const int kParallelThreshold = 500;
        public const string kDefaultLogFileName = "log.txt";
        public const int kDefaultMaxRecentLogs = 100;// Configurable maximum number of recent logs
        public const string kLogConfigurationFileName = "log_config.json";
        public const string kDefaultLogSpace = "SerilogWriterLog";
        public const int kMenuBarItemMinWidth = 100;
        public const int kMenuBarItemHeight = 20;
        public const int kMenuBarPadding = 10;
        public const int kSubMenuArrowWidth = 20;
        public const int kMeumBarPadding = 10;
        public const string kSeriLogLogSpace = "logs";
        public const char kPathSeparator = '.';
        public const float kDefaultMaxTotalDirectorySizeMB = 100.0f;
        public const string kDefaultFileStoragePath = "FileStorage.json";
    }
}
