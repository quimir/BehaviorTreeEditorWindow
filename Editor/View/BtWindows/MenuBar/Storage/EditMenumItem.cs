using System.Collections.Generic;
using System.Linq;
using Editor.View.BTWindows.BtTreeView.NodeView;
using Editor.View.BtWindows.Core;
using Editor.View.BtWindows.MenuBar.Core;
using LogManager.Core;
using LogManager.LogManagerFactory;
using Script.Utillties;

namespace Editor.View.BtWindows.MenuBar.Storage
{
    public class EditMenuItemBase : MenuBarItemBase
    {
        public EditMenuItemBase() : base("编辑")
        {
        }

        protected override List<MenuItemData> CreateMenuItems()
        {
            var behavior_tree_window = BehaviorTreeWindows.FocusedWindow;

            if (behavior_tree_window == null)
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace)
                    .AddLog(log_space, new LogEntry(LogLevel.kWarning, $"{menu_name_}: WindowRoot为null"));
                return new List<MenuItemData>();
            }

            var hasSelection = behavior_tree_window.BehaviorTreeView.selection.Count != 0;
            var hasCopiedNodes = behavior_tree_window.BehaviorTreeView.CopyNode.Count != 0;

            var menuItems = new List<MenuItemData>();

            // 添加基本编辑操作
            menuItems.Add(new MenuItemData("复制", hasSelection, () =>
            {
                behavior_tree_window.BehaviorTreeView.CopyNodeData();
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace)
                    .AddLog(log_space, new LogEntry(LogLevel.kInfo, "执行复制操作"));
            }));

            menuItems.Add(new MenuItemData("剪切", hasSelection, () =>
            {
                behavior_tree_window.BehaviorTreeView.CutNodeData();
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace)
                    .AddLog(log_space, new LogEntry(LogLevel.kInfo, "执行剪切操作"));
            }));

            menuItems.Add(new MenuItemData("删除", hasSelection, () =>
            {
                behavior_tree_window.BehaviorTreeView.DeleteNodeData(
                    behavior_tree_window.BehaviorTreeView.selection.OfType<BaseNodeView>().ToList());
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace)
                    .AddLog(log_space, new LogEntry(LogLevel.kInfo, "执行删除操作"));
            }));

            // 添加分隔符
            menuItems.Add(MenuItemData.CreateSeparator());

            menuItems.Add(new MenuItemData("粘贴", hasCopiedNodes, () =>
            {
                behavior_tree_window.BehaviorTreeView.PasteNodeData();
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace)
                    .AddLog(log_space, new LogEntry(LogLevel.kInfo, "执行粘贴操作"));
            }));

            return menuItems;
        }
    }
}