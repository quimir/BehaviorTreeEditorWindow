using System;
using UnityEngine;

namespace Editor.View.BtWindows.MenuBar.Core.ManuBarIcon
{
    /// <summary>
    /// Represents an icon used within the menu bar, providing functionalities for
    /// interaction, appearance, and state control.
    /// </summary>
    public interface IMenuBarIcon
    {
        /// <summary>
        /// Retrieves the tooltip associated with the menu bar icon.
        /// </summary>
        /// <returns>
        /// A string representing the tooltip of the menu bar icon.
        /// </returns>
        string GetTooltip();

        /// <summary>
        /// Retrieves the icon associated with the menu bar element.
        /// </summary>
        /// <returns>
        /// A Texture2D object representing the icon used in the menu bar.
        /// </returns>
        Texture2D GetIcon();

        /// <summary>
        /// Sets the active state of the menu bar icon.
        /// </summary>
        /// <param name="active">
        /// A boolean value indicating if the menu bar icon should be active (true) or inactive (false).
        /// </param>
        void SetActive(bool active);

        /// <summary>
        /// An event triggered when the menu bar icon is clicked.
        /// </summary>
        /// <remarks>
        /// The event provides an instance of the <see cref="IMenuBarIcon"/>
        /// that was clicked, allowing for context-specific handling of
        /// user interaction with the icon.
        /// </remarks>
        event Action<IMenuBarIcon> OnClicked;

        /// <summary>
        /// Retrieves the rectangle that defines the world-space boundaries
        /// of the menu bar icon.
        /// </summary>
        /// <returns>
        /// A Rect structure representing the world-space bounds of the menu bar icon.
        /// </returns>
        Rect GetWorldRect();
    }
}
