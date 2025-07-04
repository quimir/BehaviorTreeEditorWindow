using Editor.View.BtWindows.MenuBar.Storage;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Editor.View.BtWindows.MenuBar.Core
{
    /// <summary>
    /// Represents a UI element for managing and displaying a menu bar in an editor window.
    /// This element is responsible for encapsulating a menu bar manager to provide functionalities
    /// for adding, removing, and retrieving menu items, as well as resetting its internal menu bar manager.
    /// </summary>
    public class MenuBarElement : VisualElement
    {
        private MenuBarManagerBase menu_manager_;

        public MenuBarElement(EditorWindow editor_window)
        {
            // 使用当前活动窗口作为父窗口
            menu_manager_ = new DefaultMenuBarManager(editor_window);
            Add(menu_manager_);
        }

        public bool SetMenuBarManagerBase(MenuBarManagerBase menu_manager)
        {
            if (menu_manager!=null)
            {
                if (menu_manager_!=null&&menu_manager_==menu_manager)
                {
                    return false;
                }

                if (menu_manager_!=null)
                {
                    menu_manager_.OnDestroy();
                    Remove(menu_manager_);
                }

                menu_manager_ = menu_manager;
                Add(menu_manager_);
                return true;
            }

            return false;
        }

        public bool RemoveMenuBarManagerBase()
        {
            if (menu_manager_!=null)
            {
                menu_manager_.OnDestroy();
                Remove(menu_manager_);
                menu_manager_ = null;
                return true;
            }

            return false;
        }

        public void AddMenuItem(IMenuBarItem menu_item)
        {
            menu_manager_.AddMenuItem(menu_item);
        }

        public bool RemoveMenuItem(IMenuBarItem menu_item)
        {
            return menu_manager_.RemoveMenuItem(menu_item);
        }

        public IMenuBarItem GetMenuItem(string menu_item_name)
        {
            return menu_manager_.GetMenuItem(menu_item_name);
        }

        /// <summary>
        /// Handles cleanup logic for the MenuBarElement.
        /// This method ensures that resources and associated managers are properly disposed when the element is destroyed.
        /// Calls the OnDestroy method of the internal MenuBarManagerBase instance if it exists.
        /// </summary>
        public void OnDestroy()
        {
            menu_manager_.OnDestroy();
        }
    }
}