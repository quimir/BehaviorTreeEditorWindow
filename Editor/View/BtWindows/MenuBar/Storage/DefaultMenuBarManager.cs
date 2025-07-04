using Editor.View.BtWindows.MenuBar.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.View.BtWindows.MenuBar.Storage
{
    public sealed class DefaultMenuBarManager : MenuBarManagerBase
    {
        public DefaultMenuBarManager(EditorWindow window) : base(window)
        {
            // 设置菜单栏样式
            style.flexDirection = FlexDirection.Row;
            style.height = 20;
            style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            style.borderBottomWidth = 1;
            style.borderBottomColor = new Color(0.1f, 0.1f, 0.1f);
            
            AddMenuItem(new FileMenuItemBase());
            AddMenuItem(new EditMenuItemBase());
        }
    }
}