using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LogManager.Core;
using LogManager.LogManagerFactory;
using Script.LogManager;
using Script.Utillties;

namespace Editor.EditorToolEx.Operation
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
        public event Action OnOperationUndone;
        public event Action OnOperationRedon; 

        #endregion
        
        private static readonly LogSpaceNode log_space_ = new("OperationManager");
        private readonly Stack<IOperation> undo_stack_ = new();
        private readonly Stack<IOperation> redo_stack_ = new();
        private readonly int max_history_size_ = 100; // 最大历史记录数量

        public void ExecuteOperation(IOperation operation)
        {
            if (operation == null) return;

            try
            {
                operation.Execute();
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

                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                    new LogEntry(LogLevel.kInfo, $"执行操作: {operation.GetType().Name}"));
            }
            catch (Exception e)
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                    new LogEntry(LogLevel.kError, $"执行操作失败: {e.Message}"));
            }
        }

        public bool Undo()
        {
            if (!CanUndo) return false;

            try
            {
                var operation = undo_stack_.Pop();
                operation.Undo();
                redo_stack_.Push(operation);
                
                // 触发事件
                OnOperationUndone?.Invoke();

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
        }

        public bool Redo()
        {
            if (!CanRedo) return false;

            try
            {
                var operation = redo_stack_.Pop();
                operation.Redo();
                undo_stack_.Push(operation);
                
                // 触发事件
                OnOperationRedon?.Invoke();

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
        }

        public void ClearHistory()
        {
            undo_stack_.Clear();
            redo_stack_.Clear();

            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace)
                .AddLog(log_space_, new LogEntry(LogLevel.kInfo, "清除操作历史"));
        }

        public bool CanUndo => undo_stack_.Count > 0;
        public bool CanRedo => redo_stack_.Count > 0;

        public bool RequireSave => undo_stack_.Any(op => op.RequireSave) || redo_stack_.Any(op => op.RequireSave);

        public int RedoCount => redo_stack_.Count;
    }
}