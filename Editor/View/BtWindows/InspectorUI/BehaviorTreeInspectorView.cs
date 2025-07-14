using System;
using System.Collections.Generic;
using System.Linq;
using Editor.EditorToolExs.BtNodeWindows;
using Editor.EditorToolExs.Operation;
using Editor.EditorToolExs.Operation.Storage;
using Editor.View.BTWindows.BtTreeView.NodeView;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Editor.View.BtWindows.InspectorUI.Core
{
    /// <summary>
    /// Represents the inspector view for a behavior tree editor. Provides functionality
    /// for managing and displaying inspector panels associated with behavior tree nodes.
    /// </summary>
    public class BehaviorTreeInspectorView : VisualElement, IDisposable
    {
        
        public class uxml_factory : UxmlFactory<BehaviorTreeInspectorView, UxmlTraits>
        {
        }

        /// <summary>
        /// Represents the primary scrollable container in the behavior tree inspector view.
        /// Used for managing, organizing, and displaying dynamically added inspector panels
        /// associated with behavior tree nodes.
        /// </summary>
        private ScrollView scroll_view_;

        private readonly HashSet<BaseInspectorPanel> inspector_panels_ = new();

        /// <summary>
        /// Manages composite operations within the inspector view of the behavior tree editor.
        /// Responsible for coordinating and delegating operations between multiple sub-managers
        /// associated with dynamically added inspector panels.
        /// </summary>
        public CompositeOperationManager OperationManager { get; } = new();

        public BehaviorTreeInspectorView()
        {
            Init();

            InitStyle();
        }

        /// <summary>
        /// Adds an inspector panel to the Behavior Tree Inspector View.
        /// </summary>
        /// <param name="inspectorPanel">The inspector panel to add. Must inherit from <see cref="BaseInspectorPanel"/>.</param>
        public void AddInspectorPanel(BaseInspectorPanel inspectorPanel)
        {
            if (inspectorPanel == null) return;

            inspector_panels_.Add(inspectorPanel);

            OperationManager.AddSubManagerOperation(inspectorPanel.InspectorOperationManager);

            scroll_view_.Add(inspectorPanel.Container);
        }

        /// <summary>
        /// Removes an inspector panel from the Behavior Tree Inspector View.
        /// </summary>
        /// <param name="inspectorPanel">The inspector panel to remove. Must be an instance of
        /// <see cref="BaseInspectorPanel"/>.</param>
        public void RemoveInspectorPanel(BaseInspectorPanel inspectorPanel)
        {
            if (inspectorPanel == null) return;
            
            inspector_panels_.Remove(inspectorPanel);
            inspectorPanel.Container.RemoveFromHierarchy();
            inspectorPanel.Dispose();
            OperationManager.RemoveSubManagerOperation(inspectorPanel.InspectorOperationManager);
            scroll_view_.Remove(inspectorPanel.Container);;
        }

        private void InitStyle()
        {
            var style_sheet =
                AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Editor/Resource/BtWindows/NodePropertiesPanelStyles.uss");

            if (style_sheet != null) styleSheets.Add(style_sheet);

            AddToClassList("properties-panel-container");
        }

        private void Init()
        {
            // 创建一个ScrollView作为容器
            scroll_view_ = new ScrollView
            {
                name = "InspectorScrollView",
                style =
                {
                    flexGrow = 1 // 让ScrollView占满空用空间
                }
            };
            
            Add(scroll_view_);
        }

        /// <summary>
        /// Updates the view data in the Behavior Tree Inspector View based on the currently selected nodes.
        /// </summary>
        /// <param name="data">A set of selected items in the editor, expected to include instances of <see cref="BehaviorTreeNodeView"/>.</param>
        public void UpdateViewData(HashSet<ISelectable> data)
        {
            var node_views = data.OfType<BehaviorTreeNodeView>().ToList();

            var selected_nodes = node_views.Select(n => n.NodeData).ToHashSet();

            if (selected_nodes.Count == 0)
            {
                scroll_view_.style.display = DisplayStyle.None;
                // 隐藏所有面板
                foreach (var inspectorPanel in inspector_panels_) inspectorPanel.SetPanelVisibility(false);

                return;
            }

            scroll_view_.style.display = DisplayStyle.Flex;

            foreach (var inspector_panel in inspector_panels_) inspector_panel.UpdatePanel(selected_nodes);
        }

        public void Dispose()
        {
            foreach (var inspector in inspector_panels_) inspector.Dispose();

            inspector_panels_.Clear();
            OperationManager.Dispose();
        }
    }
}