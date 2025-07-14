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
    /// Manages composite operations by delegating execution and history tracking to multiple sub-operation managers.
    /// This class serves as a container for coordinating operations across multiple operation managers, allowing
    /// combined undo/redo operations and overall history tracking across its submanagers.
    /// </summary>
    public class CompositeOperationManager : IOperationManager
    {
        /// <summary>
        /// A private field that holds a collection of sub-operation managers managed by the CompositeOperationManager.
        /// This list allows the CompositeOperationManager to delegate operations such as execution, undo, redo,
        /// and history management to multiple submanagers, enabling coordinated operation management across modules.
        /// </summary>
        private List<IOperationManager> sub_managers_;

        /// <summary>
        /// A private field that stores the undo operation history for the <see cref="CompositeOperationManager"/>.
        /// This list maintains a chronological record of operations, enabling undo functionality by tracking
        /// executed operations and their associated sources.
        /// </summary>
        private List<HistoricalOperation> undo_history_ = new();

        /// <summary>
        /// A private field that stores a collection of operations available for redo within the CompositeOperationManager.
        /// This list is maintained to manage and reorder historical redo operations based on their execution timestamps,
        /// providing a structured mechanism to redo previously undone operations.
        /// </summary>
        private List<HistoricalOperation> redo_history_ = new();

        /// <summary>
        /// A private dictionary that maps sub-operation managers to their respective execution event handlers.
        /// This field is used to manage subscriptions to the <c>OnOperationExecuted</c> event for each sub-operation manager,
        /// allowing the <c>CompositeOperationManager</c> to respond to operation execution events and coordinate its
        /// behavior accordingly.
        /// </summary>
        private readonly Dictionary<IOperationManager,Action> operation_executed_handlers_ = new();

        /// <summary>
        /// A private field that maintains a mapping between sub-operation managers and their associated save state
        /// change handlers. This allows the CompositeOperationManager to manage save state change event subscriptions
        /// for its submanagers, enabling proper synchronization of save state changes across all managed operation
        /// managers.
        /// </summary>
        private readonly Dictionary<IOperationManager,Action> save_state_changed_handlers_ = new();

        private bool last_require_save_state_ = false;

        public CompositeOperationManager(params IOperationManager[] managers)
        {
            sub_managers_ = new List<IOperationManager>(managers);

            foreach (var manager in sub_managers_)
            {
                AddManagerSubscriptions(manager);
            }
        }
        
        private void OnSubManagerSaveStateChanged()
        {
            CheckSaveStateChanged();
        }

        private void CheckSaveStateChanged()
        {
            bool current_require_save = RequireSave;
            if (current_require_save!=last_require_save_state_)
            {
                last_require_save_state_=current_require_save;
                OnSaveStateChanged?.Invoke();
            }
        }

        /// <summary>
        /// Handles the event triggered when an operation is executed by a sub-operation manager.
        /// </summary>
        /// <param name="source_manager">The sub-operation manager that executed the operation.</param>
        private void OnSubManagerOperationExecuted(IOperationManager source_manager)
        {
            // 记录这个操作到全局的 undo 历史中
            undo_history_.Add(new HistoricalOperation(source_manager));

            // 排序确保总是最新的在列表末尾（通常 Add 就能保证，但以防万一）
            // 如果多个操作可能在同一 Tick 内发生，则此排序可能需要更复杂的逻辑
            // 但对于UI操作，DateTime.UtcNow.Ticks 的精度通常足够
            undo_history_ = undo_history_.OrderBy(h => h.TimeStamp).ToList();

            if (redo_history_.Count>0)
            {
                redo_history_.Clear();
            }

            OnOperationExecuted?.Invoke();
            CheckSaveStateChanged();
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
            if (!CanUndo) return false;

            // 获取时间上最新的操作记录
            var last_operation_record = undo_history_.Last();

            // 命令源管理器执行撤销
            if (last_operation_record.SourceManager.Undo())
            {
                undo_history_.RemoveAt(undo_history_.Count - 1);
                
                // 将该记录移动到redo历史中
                redo_history_.Add(last_operation_record);
                // 确保 redo 历史也是有序的
                redo_history_ = redo_history_.OrderBy(h => h.TimeStamp).ToList();
                
                CheckSaveStateChanged();
                return true;
            }

            return false;
        }

        public bool Redo()
        {
            if (!CanRedo) return false;

            // 获取时间上最新的重做操作
            var last_redo_record = redo_history_.Last();

            // 命令源管理器执行重做
            if (last_redo_record.SourceManager.Redo())
            {
                redo_history_.RemoveAt(redo_history_.Count - 1);
                // 将该操作记录移回 undo 历史中
                undo_history_.Add(last_redo_record);
                // 确保 undo 历史也是有序的
                undo_history_ = undo_history_.OrderBy(h => h.TimeStamp).ToList();
                
                CheckSaveStateChanged();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Adds a sub-operation manager to the current composite operation manager.
        /// </summary>
        /// <param name="manager">The operation manager to be added as a submanager.</param>
        /// <returns>True if the submanager was successfully added; otherwise, false.</returns>
        public bool AddSubManagerOperation(IOperationManager manager)
        {
            if (manager==null||sub_managers_.Contains(manager))
            {
                return false;
            }
            
            sub_managers_ ??= new List<IOperationManager>();
            sub_managers_.Add(manager);
            AddManagerSubscriptions(manager);
            CheckSaveStateChanged();

            return true;
        }

        /// <summary>
        /// Removes a sub-operation manager from the composite operation manager.
        /// </summary>
        /// <param name="manager">The sub-operation manager to be removed.</param>
        /// <returns>
        /// True if the sub-operation manager was successfully removed; otherwise, false.
        /// Returns false if the manager is null or not found in the composite operation manager.
        /// </returns>
        public bool RemoveSubManagerOperation(IOperationManager manager)
        {
            if (manager == null || !sub_managers_.Contains(manager)) return false;

            RemoveManagerSubscriptions(manager);
            bool removed = sub_managers_.Remove(manager);
            
            undo_history_.RemoveAll(h => h.SourceManager == manager);
            redo_history_.RemoveAll(h => h.SourceManager == manager);

            CheckSaveStateChanged();
            return removed;
        }

        public void ClearHistory()
        {
            // 清空所有子管理器的历史记录
            foreach (var manager in sub_managers_) manager.ClearHistory();

            // 清空直接的历史记录
            undo_history_.Clear();
            redo_history_.Clear();
            CheckSaveStateChanged();
        }

        /// <summary>
        /// Releases all resources used by the CompositeOperationManager instance and unsubscribes from any registered
        /// handlers.
        /// </summary>
        public void Dispose()
        {
            foreach (var manager in sub_managers_)
            {
                RemoveManagerSubscriptions(manager);
            }
            
            operation_executed_handlers_.Clear();
            save_state_changed_handlers_.Clear();
        }

        public void MarkAsSaved()
        {
            foreach(var manager in sub_managers_)
                manager.MarkAsSaved();
            
            CheckSaveStateChanged();
        }

        /// <summary>
        /// Removes subscriptions for the specified operation manager, including handlers
        /// for operation executed and save state changed events.
        /// </summary>
        /// <param name="manager">The operation manager whose subscriptions are to be removed.</param>
        private void RemoveManagerSubscriptions(IOperationManager manager)
        {
            if (operation_executed_handlers_.TryGetValue(manager,out var on_exec))
            {
                manager.OnOperationExecuted -= on_exec;
                operation_executed_handlers_.Remove(manager);
            }

            if (save_state_changed_handlers_.TryGetValue(manager,out var on_save_state_changed))
            {
                manager.OnSaveStateChanged -= on_save_state_changed;
                save_state_changed_handlers_.Remove(manager);
            }
        }

        /// <summary>
        /// Subscribes to the events of a given operation manager, enabling the composite manager to monitor and respond
        /// to the operation and save-state changes of the sub-operation manager.
        /// </summary>
        /// <param name="manager">The operation manager to subscribe to.</param>
        private void AddManagerSubscriptions(IOperationManager manager)
        {
            Action on_executed = () => OnSubManagerOperationExecuted(manager);
            operation_executed_handlers_[manager] = on_executed;
             
            Action on_save_state_changed = OnSubManagerSaveStateChanged;
            save_state_changed_handlers_[manager] = on_save_state_changed;
            
            manager.OnOperationExecuted += on_executed;
            manager.OnSaveStateChanged += on_save_state_changed;
        }

        public bool CanUndo => undo_history_.Count > 0;
        public bool CanRedo => redo_history_.Count > 0;

        public bool RequireSave=>sub_managers_.Any(m=>m.RequireSave);
        public event Action OnOperationExecuted;
        public event Action OnSaveStateChanged;
    }
}