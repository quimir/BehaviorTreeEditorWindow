using System;
using System.Collections.Generic;
using System.Linq;
using Editor.View.BTWindows.BtTreeView.NodeView;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Editor.View.BtWindows.InspectorUI.Core
{
    public class BehaviorTreeInspectorView : VisualElement, IDisposable
    {
        public class uxml_factory : UxmlFactory<BehaviorTreeInspectorView, UxmlTraits>
        {
        }

        private ScrollView scroll_view_;

        private readonly HashSet<BaseInspectorPanel> inspector_panels_ = new();

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

            scroll_view_.Add(inspectorPanel.Container);
        }

        public void RemoveInspectorPanel(BaseInspectorPanel inspectorPanel)
        {
            if (inspectorPanel == null) return;
            
            inspector_panels_.Remove(inspectorPanel);
            inspectorPanel.Container.RemoveFromHierarchy();
            inspectorPanel.Dispose();
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
        }
    }
}