namespace Editor.EditorToolEx.Operation
{
    /// <summary>
    /// Represents a generic operation interface that provides methods for executing,
    /// undoing, and redoing an operation. It can be used as a base for implementing
    /// specific operation behavior and supports the determination of whether the operation
    /// requires saving changes.
    /// </summary>
    public interface IOperation
    {
        /// <summary>
        /// Executes the operation defined in the implementing or derived class.
        /// The method is intended to perform the main action or logic associated
        /// with the specific operation being implemented.
        /// </summary>
        void Execute();

        /// <summary>
        /// Reverts the changes made by the previously executed operation.
        /// This method is intended to undo the specific actions taken by the Execute method
        /// in the context of the implementing or derived class.
        /// </summary>
        void Undo();

        /// <summary>
        /// Re-executes the previously undone operation in the context of the implementing or derived class.
        /// This method is intended to reverse the effect of the Undo method by restoring the state
        /// affected by the previously executed operation.
        /// </summary>
        void Redo();

        /// <summary>
        /// Gets a value indicating whether the operation requires the document or editor's state
        /// to be marked as modified or dirty, typically triggering a save operation or
        /// prompting the user to save changes.
        /// </summary>
        /// <remarks>
        /// When set to true, this property implies the operation has caused a state change
        /// that necessitates saving. When false, the state remains unchanged in terms of requiring a save action.
        /// </remarks>
        bool RequireSave { get; }
    }
}
