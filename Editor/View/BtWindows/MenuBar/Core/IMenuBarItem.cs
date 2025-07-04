using System;
using UnityEngine;

namespace Editor.View.BtWindows.MenuBar.Core
{
    /// <summary>
    /// Represents a menu bar item interface with functionality to manage its state, display menu options,
    /// and trigger events related to activation and user interaction.
    /// </summary>
    public interface IMenuBarItem
    {
        /// <summary>
        /// Sets the active state of the menu bar item.
        /// </summary>
        /// <param name="active">A boolean value indicating whether the menu bar item should be active or inactive.</param>
        void SetActive(bool active);

        /// <summary>
        /// Displays the associated menu for the menu bar item.
        /// </summary>
        void ShowMenu();

        /// <summary>
        /// Retrieves the world-space boundary of the menu bar item represented as a rectangle.
        /// </summary>
        /// <returns>A Rect structure containing the world-space boundary of the menu bar item.</returns>
        Rect GetWorldBound();

        /// <summary>
        /// Retrieves the name of the menu bar item.
        /// </summary>
        /// <returns>A string representing the name of the menu bar item.</returns>
        string GetName();

        /// <summary>
        /// Event triggered when the menu bar item is activated.
        /// Activation typically occurs through user interaction, such as a click,
        /// and can be used to execute associated actions or display menus.
        /// </summary>
        event Action<IMenuBarItem> OnActivated;

        /// <summary>
        /// Event triggered when the mouse pointer enters the boundaries of the menu bar item.
        /// This event can be used to handle hover-specific behavior, such as highlighting
        /// the item or displaying additional information.
        /// </summary>
        event Action<IMenuBarItem> OnMouseEntered;
    }
}
