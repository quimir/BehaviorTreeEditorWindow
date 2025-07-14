using System.Collections.Generic;
using System.Linq;
using Editor.EditorToolExs.Operation.Core;

namespace Editor.EditorToolExs.Operation.Storage
{
    /// <summary>
    /// Represents a composite operation that aggregates multiple operations
    /// into a single executable unit. This class serves as a container for
    /// managing and executing a collection of operations as one cohesive group.
    /// </summary>
    public class CompositeOperation :IOperation
    {
        private readonly List<IOperation> operations_ = new();

        /// <summary>
        /// Gets the collection of operations contained within this composite operation.
        /// This property provides access to the underlying list of individual operations
        /// that are managed as a part of the composite group. These operations can be
        /// executed, undone, or redone collectively through the composite operation.
        /// </summary>
        public List<IOperation> Operations => operations_;

        public CompositeOperation(IEnumerable<IOperation> operations)
        {
            operations_.AddRange(operations);
        }
        
        public void Execute()
        {
            foreach (var operation in operations_)
            {
                operation.Execute();
            }
        }

        public void Undo()
        {
            foreach (var operation in operations_.AsEnumerable().Reverse())
            {
                operation.Undo();
            }
        }

        public void Redo()
        {
            Execute();
        }

        /// <summary>
        /// Adds a new operation to the composite operation. The provided operation
        /// will be included as part of the group of operations aggregated by this
        /// composite.
        /// </summary>
        /// <param name="operation">The operation to be added to the composite operation. Must implement the
        /// <see cref="IOperation"/> interface.</param>
        public void AddOperation(IOperation operation)
        {
            operations_.Add(operation);
        }

        public bool RequireSave => operations_.Any(op => op.RequireSave);
    }
}
