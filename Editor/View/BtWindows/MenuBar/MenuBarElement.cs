using Editor.View.BtWindows.MenuBar.Core;
using Editor.View.BtWindows.MenuBar.Core.MenuBarManager;
using Editor.View.BtWindows.MenuBar.Storage;
using UnityEditor;
using UnityEngine.UIElements;

namespace Editor.View.BtWindows.MenuBar
{
    /// <summary>
    /// Represents a UI element for managing and displaying a menu bar in an editor window.
    /// This element is responsible for encapsulating a menu bar manager to provide functionalities
    /// for adding, removing, and retrieving menu items, as well as resetting its internal menu bar manager.
    /// </summary>
    public class MenuBarElement : VisualElement
    {
        /// <summary>
        /// Internal variable that holds the reference to an instance of MenuBarManagerBase.
        /// It is responsible for managing the state and functionality of the menu bar
        /// in the associated editor window.
        /// </summary>
        private MenuBarManagerBase menu_manager_;

        public MenuBarElement(EditorWindow editor_window)
        {
            // 使用当前活动窗口作为父窗口
            menu_manager_ = new DefaultMenuBarManager(editor_window);
            Add(menu_manager_);
        }

        /// <summary>
        /// Sets the internal MenuBarManagerBase for the MenuBarElement.
        /// This method ensures that the provided manager is correctly assigned, replaces any existing manager,
        /// and disposes the previous manager if applicable.
        /// </summary>
        /// <param name="menu_manager">The new MenuBarManagerBase instance to be set.</param>
        /// <returns>True if the MenuBarManagerBase is successfully set; otherwise, false if the manager is null or
        /// unchanged.</returns>
        public bool SetMenuBarManagerBase(MenuBarManagerBase menu_manager)
        {
            if (menu_manager != null)
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

        /// <summary>
        /// Removes the current MenuBarManagerBase from the MenuBarElement.
        /// This method ensures that the internal manager is properly disposed of and detached from the element.
        /// </summary>
        /// <returns>True if the MenuBarManagerBase is successfully removed; otherwise, false if no manager is
        /// currently set.</returns>
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

        /// <summary>
        /// Adds a menu item to the MenuBarManager associated with this MenuBarElement.
        /// This method delegates the addition of the menu item to the internal MenuBarManager instance.
        /// </summary>
        /// <param name="menu_item">The <c>IMenuBarItem</c> instance to be added to the menu bar. It must not be null
        /// and is expected to have a unique identifier.</param>
        public void AddMenuItem(IMenuBarItem menu_item)
        {
            menu_manager_.AddMenuItem(menu_item);
        }

        /// <summary>
        /// Removes a specified menu item from the menu bar.
        /// Delegates the removal operation to the associated MenuBarManagerBase instance.
        /// </summary>
        /// <param name="menu_item">The menu item to be removed, represented by an IMenuBarItem instance.</param>
        /// <returns>True if the menu item is removed successfully; otherwise, false if the menu item is not found
        /// or the operation fails.</returns>
        public bool RemoveMenuItem(IMenuBarItem menu_item)
        {
            return menu_manager_.RemoveMenuItem(menu_item);
        }

        /// <summary>
        /// Retrieves a menu bar item by its name from the associated MenuBarManager.
        /// This method searches for the specified menu item and returns it if found.
        /// </summary>
        /// <param name="menu_item_name">The name of the menu item to retrieve.</param>
        /// <returns>The menu bar item that matches the provided name, or null if no matching item is found.</returns>
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