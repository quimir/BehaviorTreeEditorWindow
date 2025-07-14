using System;
using System.Collections.Generic;
using System.Linq;
using Editor.EditorToolExs.Operation.Core;
using ExTools.Utillties;
using LogManager.Core;
using LogManager.LogManagerFactory;

namespace Editor.EditorToolExs.Operation.Storage
{
    /// <summary>
    /// Manages the execution, undo, and redo of operations, as well as maintaining
    /// the operation history in an application. Provides functionalities to execute
    /// operations, manage history size, and supports undo/redo capabilities.
    /// </summary>
    public class OperationManager : IOperationManager
    {
        #region 事件变量

        public event Action OnOperationExecuted;
        public event Action OnSaveStateChanged;

        #endregion
        
        private static readonly LogSpaceNode log_space_ = new("OperationManager");
        private readonly Stack<IOperation> undo_stack_ = new();
        private readonly Stack<IOperation> redo_stack_ = new();
        private readonly int max_history_size_ = 100; // 最大历史记录数量

        private bool is_grouping_;
        private CompositeOperation current_group_ = null;
        
        private bool is_operation_in_progress_ = false;
        
        /// <summary>
        /// Gets a value indicating whether an operation (Execute, Undo, or Redo) is currently being processed.
        /// </summary>
        public bool IsOperationInProgress => is_operation_in_progress_;

        private bool last_require_save_state_ = false;

        private int saved_operation_count_ = 0;

        /// <summary>
        /// Begins grouping subsequent operations into a single composite operation.
        /// This allows multiple operations to be treated as one, enabling them to be undone or redone together.
        /// If an operation group is already in progress, calling this method again will have no effect.
        /// </summary>
        public void BeginOperationGroup()
        {
            if (is_grouping_)
            {
                return;
            }

            is_grouping_ = true;
            current_group_ = new CompositeOperation(new IOperation[] { });
        }

        /// <summary>
        /// Ends the grouping of operations that were previously started by calling BeginOperationGroup.
        /// If operations were grouped, the group is finalized and treated as a single composite operation,
        /// which can be undone or redone as a single unit. If no operations were added to the group,
        /// it is discarded. Calling this method when no operation group is in progress has no effect.
        /// </summary>
        public void EndOperationGroup()
        {
            if (!is_grouping_||current_group_==null)
            {
                return;
            }

            is_grouping_ = false;
            // 如果组里有操作，则将整个组作为一个操作来执行
            if (current_group_.Operations.Any())
            {
                PushToUndoStack(current_group_);
            }

            current_group_ = null;
        }

        public void ExecuteOperation(IOperation operation)
        {
            if (operation == null||is_operation_in_progress_) return;
            
            // 上锁
            is_operation_in_progress_ = true;
            
            // 如果正在分组，则将操作添加到当前组，而不是立即执行
            if (is_grouping_)
            {
                if (current_group_==null)
                {
                    BeginOperationGroup();
                }

                if (current_group_ != null) current_group_.AddOperation(operation);
                
                // 执行子操作
                try
                {
                    is_operation_in_progress_ = true;
                    operation.Execute();
                }
                finally
                {
                    is_operation_in_progress_ = false;
                }
                
                // 注意：不将子操作压入堆栈，等待 EndOperationGroup 统一处理
                return;
            }

            // 对于单个操作，执行并压入堆栈
            try
            {
                is_operation_in_progress_ = true;
                operation.Execute();
                PushToUndoStack(operation);
            }
            catch (Exception e)
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                    new LogEntry(LogLevel.kError, $"执行操作失败: {e.Message}"));
            }
            finally
            {
                is_operation_in_progress_ = false;// 解锁
            }
        }

        /// <summary>
        /// Adds the specified operation to the undo stack and clears the redo stack.
        /// Enforces the undo history size limit by removing the oldest operation when the maximum size is exceeded.
        /// Triggers an event to notify that a new operation has been executed and logs the operation for debugging or
        /// tracking purposes.
        /// </summary>
        /// <param name="operation">The operation to be added to the undo stack.</param>
        private void PushToUndoStack(IOperation operation)
        {
            undo_stack_.Push(operation);
            redo_stack_.Clear();
            
            // 限制历史记录大小
            while (undo_stack_.Count > max_history_size_)
            {
                var oldest = undo_stack_.ToArray()[undo_stack_.Count - 1];
                var temp_stack = new Stack<IOperation>();

                // 保留除最旧记录外的所有记录
                for (var i = 0; i < undo_stack_.Count - 1; i++) temp_stack.Push(undo_stack_.Pop());

                undo_stack_.Clear();
                while (temp_stack.Count > 0) undo_stack_.Push(temp_stack.Pop());
            }

            // 触发事件
            OnOperationExecuted?.Invoke();
            
            CheckSaveStateChanged();

            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                new LogEntry(LogLevel.kInfo, $"执行操作: {operation.GetType().Name}"));
        }

        public bool Undo()
        {
            if (!CanUndo||is_operation_in_progress_) return false;

            is_operation_in_progress_ = true;// 上锁
            try
            {
                var operation = undo_stack_.Pop();
                operation.Undo();
                redo_stack_.Push(operation);
                
                CheckSaveStateChanged();

                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                    new LogEntry(LogLevel.kInfo, $"撤销操作: {operation.GetType().Name}"));
                return true;
            }
            catch (Exception e)
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                    new LogEntry(LogLevel.kError, $"撤销操作失败: {e.Message}"));
                return false;
            }
            finally
            {
                is_operation_in_progress_ = false;// 解锁
            }
        }

        public bool Redo()
        {
            if (!CanRedo) return false;

            is_operation_in_progress_ = true;// 上锁
            try
            {
                var operation = redo_stack_.Pop();
                operation.Redo();
                undo_stack_.Push(operation);
                
                CheckSaveStateChanged();

                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                    new LogEntry(LogLevel.kInfo, $"重做操作: {operation.GetType().Name}"));
                return true;
            }
            catch (Exception e)
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                    new LogEntry(LogLevel.kError, $"重做操作失败: {e.Message}"));
                return false;
            }
            finally
            {
                is_operation_in_progress_ = false;// 解锁
            }
        }

        public void ClearHistory()
        {
            undo_stack_.Clear();
            redo_stack_.Clear();

            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace)
                .AddLog(log_space_, new LogEntry(LogLevel.kInfo, "清除操作历史"));
        }

        public void MarkAsSaved()
        {
            saved_operation_count_=undo_stack_.Count;
            CheckSaveStateChanged();
        }

        /// <summary>
        /// Checks if the save state of the operation history has changed and triggers the
        /// OnSaveStateChanged event if necessary. The save state reflects whether there are
        /// unsaved changes in the undo stack compared to the last saved state.
        /// </summary>
        private void CheckSaveStateChanged()
        {
            bool current_require_save = RequireSave;
            if (current_require_save!=last_require_save_state_)
            {
                last_require_save_state_=current_require_save;
                OnSaveStateChanged?.Invoke();
            }
        }

        public bool CanUndo => undo_stack_.Count > 0;
        public bool CanRedo => redo_stack_.Count > 0;

        public bool RequireSave => undo_stack_.Count != saved_operation_count_;
    }
}