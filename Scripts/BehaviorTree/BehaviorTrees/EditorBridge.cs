using System;
using ExTools.Singleton;

namespace BehaviorTree.BehaviorTrees
{
    /// <summary>
    /// EditorBridge provides communication mechanisms between the behavior tree editor tools
    /// and the runtime system, enabling interactions such as opening behavior tree windows
    /// based on specific requests.
    /// </summary>
    /// <remarks>
    /// This class inherits from SingletonWithLazy to ensure a single, thread-safe instance
    /// throughout the application's lifecycle.
    /// </remarks>
    public class EditorBridge : SingletonWithLazy<EditorBridge>
    {
        /// <summary>
        /// Event triggered when a request to open a behavior tree editor window is made.
        /// </summary>
        /// <remarks>
        /// This delegate-based event is utilized for invoking the functionality required to
        /// display and interact with a specific behavior tree in the editor environment.
        /// The event passes the unique identifier (tree_id) of the behavior tree that should
        /// be opened for inspection or modification.
        /// </remarks>
        /// <example>
        /// The event is typically subscribed to in editor-specific initializers or components,
        /// where it binds to a method capable of opening a behavior tree editor window.
        /// </example>
        public Action<string> OnOpenBehaviorTreeWindowRequested;
    }
}
