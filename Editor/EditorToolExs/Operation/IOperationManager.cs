using System;

namespace Editor.EditorToolEx.Operation
{
    public interface IOperationManager
    {
        /// <summary>
        /// Executes the specified operation.
        /// The operation is applied and can be undone or redone
        /// depending on the implementation of the operation.
        /// </summary>
        /// <param name="operation">The IOperation instance to be executed.
        /// It encapsulates the specific logic to be performed.</param>
        void ExecuteOperation(IOperation operation);

        /// <summary>
        /// Undoes the most recently executed operation, if available.
        /// The undone operation is moved to the redo stack and can potentially be redone.
        /// </summary>
        /// <returns>
        /// Returns true if an operation was successfully undone, otherwise false.
        /// False is returned if there are no operations to undo or if an error occurs during the undo process.
        /// </returns>
        bool Undo();

        /// <summary>
        /// Redoes the most recently undone operation, if available.
        /// The operation is moved back to the undo stack after being reapplied.
        /// </summary>
        /// <returns>
        /// Returns true if an operation was successfully redone, otherwise false.
        /// False is returned if there are no operations to redo or if an error occurs during the redo process.
        /// </returns>
        bool Redo();

        /// <summary>
        /// Clears the history of executed and undone operations.
        /// This method empties both the undo and redo stacks, effectively
        /// resetting the operation history and making it impossible to undo
        /// or redo any previous actions.
        /// </summary>
        void ClearHistory();

        /// <summary>
        /// Gets a value indicating whether there are operations available to undo.
        /// </summary>
        /// <remarks>
        /// The property returns true if there are one or more operations in the undo stack,
        /// otherwise, it returns false. It provides a quick way to check if the most recent
        /// operation can be reversed.
        /// </remarks>
        bool CanUndo { get; }

        /// <summary>
        /// Gets a value indicating whether there are operations available to redo.
        /// </summary>
        /// <remarks>
        /// The property returns true if there are one or more operations in the redo stack,
        /// indicating that actions previously undone can be reapplied.
        /// Returns false if the redo stack is empty, meaning no actions are available to redo.
        /// </remarks>
        bool CanRedo { get; }

        /// <summary>
        /// Gets a value indicating whether there are operations that require saving.
        /// </summary>
        /// <remarks>
        /// This property evaluates all the operations currently present in both the undo and redo stacks
        /// to determine if any of them have been marked as requiring a save. If at least one operation
        /// necessitates saving, the property returns true; otherwise, it returns false. This provides
        /// a mechanism to track changes that might need to be persisted.
        /// </remarks>
        bool RequireSave { get; }

        public event Action OnOperationExecuted;
        public event Action OnOperationUndone;
        public event Action OnOperationRedon;
    }
}
