using System;
using Editor.EditorToolExs.Operation.Core;

namespace Editor.EditorToolExs.Operation.Storage
{
    /// <summary>
    /// Represents a historical operation in an operation management system.
    /// This class is used to capture, record, and identify operations
    /// that are executed within the context of an <see cref="IOperationManager"/>.
    /// </summary>
    public class HistoricalOperation
    {
        /// <summary>
        /// 执行此操作的管理器。
        /// </summary>
        public IOperationManager SourceManager { get; }
        
        /// <summary>
        /// 操作被执行时的时间戳 (高精度)。
        /// </summary>
        public long TimeStamp { get; }

        public HistoricalOperation(IOperationManager sourceManager,string source_manager_id=null)
        {
            SourceManager = sourceManager;
            TimeStamp = System.DateTime.Now.Ticks;
        }
    }
}
