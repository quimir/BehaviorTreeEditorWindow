using System;
using System.Collections.Generic;
using System.Linq;
using LogManager.Core;
using LogManager.LogManagerFactory;
using Script.LogManager;
using Script.Utillties;

namespace Editor.EditorToolEx.Operation
{
    public class CompositeOperationManager : IOperationManager
    {
        private List<IOperationManager> sub_managers_;
        private List<HistoricalOperation> undo_history_ = new();
        private List<HistoricalOperation> redo_history_ = new();

        public CompositeOperationManager(params IOperationManager[] managers)
        {
            sub_managers_ = new List<IOperationManager>(managers);

            // 订阅所有子管理器的执行事件
            foreach (var manager in sub_managers_)
                if (manager is OperationManager concrete_manager)
                    concrete_manager.OnOperationExecuted += () => OnSubManagerOperationExecuted(concrete_manager);
        }

        private void OnSubManagerOperationExecuted(IOperationManager source_manager)
        {
            // 记录这个操作到全局的 undo 历史中
            undo_history_.Add(new HistoricalOperation(source_manager));

            // 排序确保总是最新的在列表末尾（通常 Add 就能保证，但以防万一）
            // 如果多个操作可能在同一 Tick 内发生，则此排序可能需要更复杂的逻辑
            // 但对于UI操作，DateTime.UtcNow.Ticks 的精度通常足够
            undo_history_ = undo_history_.OrderBy(h => h.TimeStamp).ToList();

            OnOperationExecuted?.Invoke();
        }

        public void ExecuteOperation(IOperation operation)
        {
            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(
                new LogSpaceNode("OperationManager"),
                new LogEntry(LogLevel.kWarning, "警告: 不推荐直接在 CompositeOperationManager 上执行操作。请在具体的模块管理器上执行。"), true);

            // 如果确实需要，可以委托给第一个管理器
            if (sub_managers_.Count > 0) sub_managers_[0].ExecuteOperation(operation);
        }

        public bool Undo()
        {
            if (!CanRedo) return false;

            // 获取时间上最新的操作记录
            var last_operation_record = undo_history_.Last();
            undo_history_.RemoveAt(undo_history_.Count - 1);

            // 命令源管理器执行撤销
            if (last_operation_record.SourceManager.Undo())
            {
                // 将该记录移动到redo历史中
                redo_history_.Add(last_operation_record);
                // 确保 redo 历史也是有序的
                redo_history_ = redo_history_.OrderBy(h => h.TimeStamp).ToList();

                OnOperationUndone?.Invoke();
                return true;
            }

            // 如果子管理器撤销失败，把记录放回去
            undo_history_.Add(last_operation_record);

            return false;
        }

        public bool Redo()
        {
            if (!CanRedo) return false;

            // 获取时间上最新的重做操作
            var last_redo_record = redo_history_.Last();
            redo_history_.RemoveAt(redo_history_.Count - 1);

            // 命令源管理器执行重做
            if (last_redo_record.SourceManager.Redo())
            {
                // 将该操作记录移回 undo 历史中
                undo_history_.Add(last_redo_record);
                // 确保 undo 历史也是有序的
                undo_history_ = undo_history_.OrderBy(h => h.TimeStamp).ToList();

                OnOperationRedon?.Invoke();
                return true;
            }

            // 如果子管理器重做失败，把记录放回去
            redo_history_.Add(last_redo_record);

            return false;
        }

        public bool AddSubManagerOperation(IOperationManager manager)
        {
            if (manager == null) return false;
            sub_managers_ ??= new List<IOperationManager>();
            manager.OnOperationExecuted += () => OnSubManagerOperationExecuted(manager);

            return true;
        }

        public void ClearHistory()
        {
            // 清空所有子管理器的历史记录
            foreach (var manager in sub_managers_) manager.ClearHistory();

            // 清空直接的历史记录
            undo_history_.Clear();
            redo_history_.Clear();
        }

        public void Dispose()
        {
            foreach (var manager in sub_managers_)
                manager.OnOperationExecuted -= () => OnSubManagerOperationExecuted(manager);
        }

        public bool CanUndo => undo_history_.Count > 0;
        public bool CanRedo => redo_history_.Count > 0;

        public bool RequireSave => sub_managers_.Any(m => m.RequireSave);
        public event Action OnOperationExecuted;
        public event Action OnOperationUndone;
        public event Action OnOperationRedon;
    }
}