using System;
using UnityEngine;

namespace BehaviorTree.Core.WindowData
{
    /// <summary>
    /// Represents the editor window data for the Behavior Tree system.
    /// Provides information to define the layout and transformations
    /// of the editor window for user interaction.
    /// </summary>
    [Serializable]
    public class BtEditorWindowData
    {
        /// <summary>
        /// Represents the rectangular bounds of a window in the Unity Editor.
        /// Used to store and manage the position and size of the editor window
        /// for layout persistence across sessions in the Behavior Tree Editor.
        /// </summary>
        public Rect WindowRect;

        /// <summary>
        /// Defines the width of the split view in the Behavior Tree Editor layout.
        /// Used to manage and persist the adjustable divider between sections
        /// of the editor interface across user sessions.
        /// </summary>
        public float SplitViewWidth;

        /// <summary>
        /// Represents the transformation data for a graph view, including properties
        /// such as position and scale. This is utilized for managing the view state
        /// and layout of elements within a Behavior Tree editor context.
        /// </summary>
        public GraphViewTransform GraphViewTransform;
    }
}
