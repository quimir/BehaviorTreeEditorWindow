using System;
using System.Collections.Generic;
using BehaviorTree.Nodes;
using Editor.EditorToolExs.Operation;
using Editor.EditorToolExs.Operation.Core;
using UnityEngine.UIElements;

namespace Editor.View.BtWindows.InspectorUI.Core
{
    /// <summary>
    /// Represents the base class for creating inspector panels in the Behavior Tree editor.
    /// Defines the core functionalities such as updating, setting visibility, and clearing
    /// the panel content. This class is designed to be inherited by specific inspector panel
    /// implementations.
    /// </summary>
    public abstract class BaseInspectorPanel:IDisposable
    {
        protected VisualElement container_;

        /// <summary>
        /// Provides access to manage operations specific to the inspector panel.
        /// This property encapsulates an instance of <see cref="IOperationManager"/>, allowing
        /// execution, undoing, redoing of operations, and managing operation history within the inspector context.
        /// </summary>
        public IOperationManager InspectorOperationManager { get; protected set; }

        /// <summary>
        /// Represents the container element of the inspector panel.
        /// This property provides access to the underlying VisualElement that
        /// hosts the content of the panel.
        /// </summary>
        public VisualElement Container => container_;

        public BaseInspectorPanel(VisualElement container)
        {
            container_ = container;
            container_.style.paddingBottom = 10;
            container_.style.display=DisplayStyle.None;
        }

        public void ResetContainer(VisualElement container)
        {
            container_ = container;
        }

        /// <summary>
        /// Updates the panel with the specified behavior tree nodes.
        /// </summary>
        /// <param name="nodes">A collection of behavior tree nodes to use for updating the panel.</param>
        public abstract void UpdatePanel(HashSet<BtNodeBase> nodes);

        /// <summary>
        /// Retrieves the header text for the panel based on the provided behavior tree nodes.
        /// </summary>
        /// <param name="nodes">A collection of behavior tree nodes used to determine the header text.</param>
        /// <returns>A string representing the header text of the panel.</returns>
        public abstract string GetPanelHeaderText(HashSet<BtNodeBase> nodes);

        /// <summary>
        /// Releases resources used by the panel and performs cleanup operations.
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// Sets the visibility of the panel.
        /// </summary>
        /// <param name="isVisible">A boolean indicating whether the panel should be visible or hidden.</param>
        public void SetPanelVisibility(bool isVisible)
        {
            container_.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // 清除面板内容
        public void ClearPanel()
        {
            container_.Clear();
        }
    }
}
