using System.Collections.Generic;
using Editor.View.BTWindows.BtTreeView;
using Editor.View.BTWindows.BtTreeView.NodeView;

namespace Editor.View.BtWindows.SearchBar.Core
{
    public class NodeSearchableItem : ISearchableItem
    {
        private readonly BehaviorTreeNodeView node_view_;
        private readonly BehaviorTreeView tree_view_;

        public NodeSearchableItem(BehaviorTreeNodeView node_view,BehaviorTreeView tree_view)
        {
            node_view_ = node_view;
            tree_view_ = tree_view;
        }
        public string SearchableContent=>$"{node_view_.NodeData.NodeName} {node_view_.GetType().Name}";
        public void OnFound()
        {
            tree_view_.AddSelectedNode(new List<BehaviorTreeNodeView> { node_view_ });
        }
    }
}
