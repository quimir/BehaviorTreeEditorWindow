using System;
using LogManager.Core;

namespace Script.LogManager.QueryService
{
    public struct QueriedLogEntry
    {
        public LogEntry Entry { get; }
        
        public string LogSpacePath { get; }
        
        public Guid RunId { get; }

        public QueriedLogEntry(LogEntry entry, string log_space_path, Guid run_id)
        {
            Entry = entry;
            LogSpacePath = log_space_path;
            RunId = run_id;
        }
    }
}