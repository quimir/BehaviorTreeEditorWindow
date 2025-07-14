using System;
using System.Collections.Generic;
using UnityEditor;

namespace Editor.View.BtWindows.MenuBar.Core
{
    /// <summary>
    /// Represents a menu item in the application's menu system.
    /// This class provides properties and methods to define menu item characteristics,
    /// manage subitems, and trigger associated actions.
    /// </summary>
    [Serializable]
    public class MenuItemData
    {
        /// <summary>
        /// Represents the name of the menu item displayed in the menu.
        /// This property determines the text label shown for a menu item
        /// in the user interface.
        /// </summary>
        public string Name;

        /// <summary>
        /// Indicates whether the menu item is enabled or disabled.
        /// This property determines if the menu item can be interacted with.
        /// When set to false, the menu item is displayed as inactive and is non-clickable.
        /// </summary>
        public bool Enabled;

        /// <summary>
        /// Indicates whether the menu item functions as a separator
        /// within a menu structure. A separator is a visual divider
        /// used to group menu items and does not have any associated action.
        /// </summary>
        public bool IsSeparator;

        /// <summary>
        /// Represents the action associated with a menu item.
        /// This delegate is executed when the corresponding menu item is selected
        /// or activated, allowing for custom behavior to be implemented.
        /// </summary>
        public Action Action;

        /// <summary>
        /// Represents the collection of submenu items associated with a menu item.
        /// This property defines the hierarchical structure of menu items, allowing
        /// a menu item to contain and display additional nested menu items.
        /// </summary>
        public List<MenuItemData> SubItems;

        /// <summary>
        /// Represents a menu item in the application's menu system.
        /// This class provides properties and methods to define menu item characteristics,
        /// manage subitems, and trigger associated actions.
        /// </summary>
        /// <param name="item_name">Represents the name of the menu item displayed in the menu.
        /// This property determines the text label shown for a menu item
        /// in the user interface.</param>
        /// <param name="is_enabled">Indicates whether the menu item is enabled or disabled.
        /// This property determines if the menu item can be interacted with.
        /// When set to false, the menu item is displayed as inactive and is non-clickable.</param>
        /// <param name="action">Represents the action associated with a menu item.
        /// This delegate is executed when the corresponding menu item is selected
        /// or activated, allowing for custom behavior to be implemented.</param>
        public MenuItemData(string item_name, bool is_enabled = true, Action action = null)
        {
            Name = item_name;
            Enabled = is_enabled;
            Action = action;
            IsSeparator = false;
            SubItems = new List<MenuItemData>();
        }

        /// <summary>
        /// Represents a menu item in the menu bar. This class provides properties
        /// to define the name, state, associated action, and subitems of a menu item.
        /// It supports hierarchical menus and defines methods to manage them.
        /// </summary>
        /// <param name="item_name">Specifies the name of the menu item. This value is displayed as the label for the
        /// menu item in the UI.</param>
        /// <param name="sub_items">Defines a list of child menu items for nested or hierarchical menu structures.
        /// This parameter represents the item's sub-menu items.</param>
        public MenuItemData(string item_name, List<MenuItemData> sub_items)
        {
            // 多级菜单来说，父类不应该添加对应的Action
            Name = item_name;
            Enabled = true;
            IsSeparator = false;
            SubItems = sub_items;
        }

        /// <summary>
        /// Adds a submenu item to the current menu item.
        /// </summary>
        /// <param name="ite_name">The name of the submenu item to be added.</param>
        /// <param name="is_enabled">Indicates whether the submenu item is enabled or disabled. The default value is <c>true</c>.</param>
        /// <param name="item_action">The action to be executed when the submenu item is selected. The default value is <c>null</c>.</param>
        /// <returns>A <c>MenuItemData</c> instance representing the added submenu item.</returns>
        public MenuItemData AddSubItem(string ite_name, bool is_enabled = true, Action item_action = null)
        {
            var sub_item = new MenuItemData(ite_name, is_enabled, item_action);
            SubItems.Add(sub_item);
            return sub_item;
        }

        /// <summary>
        /// Creates a menu item that functions as a separator.
        /// </summary>
        /// <returns>
        /// A <c>MenuItemData</c> instance configured as a separator.
        /// </returns>
        public static MenuItemData CreateSeparator()
        {
            return new MenuItemData("") { IsSeparator = true };
        }

        /// <summary>
        /// Indicates whether the current menu item has sub-items associated with it.
        /// This property returns true if the menu item contains nested child items;
        /// otherwise, it returns false. Used to determine if the menu item
        /// is expandable or navigable to further levels.
        /// </summary>
        public bool HasSubItems => SubItems is { Count: > 0 };
    }
}
