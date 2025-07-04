using System;

namespace Editor.View.BtWindows.MenuBar.Core
{
    /// <summary>
    /// Defines the interface for managing a menu bar and its menu items,
    /// providing functionalities to add, remove, and handle interactions with menu items.
    /// </summary>
    public interface IMenuBarManager
    {
        /// <summary>
        /// Adds a menu item to the menu bar and registers its associated events.
        /// </summary>
        /// <param name="item">The menu item to add to the menu bar.</param>
        void AddMenuItem(IMenuBarItem item);

        /// <summary>
        /// Removes a menu item from the menu bar and unregisters its associated events.
        /// </summary>
        /// <param name="item">The menu item to remove from the menu bar.</param>
        /// <returns>
        /// A boolean value indicating whether the removal was successful.
        /// Returns true if the menu item was successfully removed; otherwise, false.
        /// </returns>
        bool RemoveMenuItem(IMenuBarItem item);

        /// <summary>
        /// Closes the currently active menu and executes an optional callback upon completion.
        /// </summary>
        /// <param name="on_closed_callback">An optional callback to invoke after the active menu has been closed.</param>
        void CloseActiveMenu(Action on_closed_callback = null);

        /// <summary>
        /// Retrieves a menu item from the menu bar by its name.
        /// </summary>
        /// <param name="menu_item_name">The name of the menu item to retrieve.</param>
        /// <returns>The menu item that matches the specified name, or null if no matching item is found.</returns>
        IMenuBarItem GetMenuItem(string menu_item_name);

        /// <summary>
        /// Retrieves the currently active menu item in the menu bar.
        /// </summary>
        /// <returns>
        /// The active menu item, or null if no menu item is currently active.
        /// </returns>
        IMenuBarItem GetActiveMenuItem();

        /// <summary>
        /// Performs cleanup and resource deallocation when the object is being destroyed.
        /// This includes unsubscribing from events and closing active menus to ensure
        /// proper resource management and to avoid memory leaks or unintended behaviors.
        /// </summary>
        void OnDestroy();

        /// <summary>
        /// Event triggered when a menu item in the menu bar is activated.
        /// </summary>
        /// <remarks>
        /// This event is fired whenever a specific menu item is activated, either through
        /// user interaction or programmatic means. Subscribers to this event can use it
        /// to handle menu activation logic, such as displaying a submenu, executing
        /// commands, or updating the UI based on the selection.
        /// </remarks>
        /// <event>
        /// The event passes the activated menu item of type <see cref="IMenuBarItem"/>
        /// as a parameter, enabling listeners to process or interact with the activated menu item.
        /// </event>
        event Action<IMenuBarItem> OnMenuItemActivated;

        /// <summary>
        /// Event triggered when the state of the menu bar changes.
        /// </summary>
        /// <remarks>
        /// This event is fired whenever the open or close state of the menu bar is altered.
        /// Subscribers can utilize this event to execute logic such as updating the visual
        /// representation of the menu or performing actions based on menu state transitions.
        /// </remarks>
        /// <event>
        /// The event passes a boolean parameter indicating the new state of the menu bar:
        /// true if the menu bar is opened, and false if it is closed.
        /// </event>
        event Action<bool> OnMenuStateChanged;
    }
}