using Editor.View.BtWindows.Core;
using Editor.View.BtWindows.MenuBar.Core;
using Editor.View.BtWindows.MenuBar.Core.ManuBarIcon;
using Editor.View.BtWindows.MenuBar.Core.MenuBarManager;
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

            var save_window_icon =
                new MenuBarIconBase(
                    AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/Resource/Shared/Image/save.png"),
                    "将窗口进行保存");
            save_window_icon.OnClicked += (sender) =>
            {
                var owner_window = BehaviorTreeWindows.FocusedWindow;
                if (owner_window==null)
                {
                    Debug.LogWarning("无法保存，因为没有聚焦的行为树窗口。");
                }
                
                owner_window.SaveWindow();
            };
            AddIconItem(save_window_icon);
        }
    }
}