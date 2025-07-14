using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.View.BtWindows.MenuBar.Core.ManuBarIcon
{
    public class MenuBarIconBase : VisualElement,IMenuBarIcon
    {
        /// <summary>
        /// Represents the button element within the menu bar icon, responsible for handling
        /// user interactions such as clicks and facilitating the display of the menu bar
        /// icon's content and appearance.
        /// </summary>
        protected readonly Button icon_button_;

        /// <summary>
        /// Represents the tooltip text associated with the menu bar icon,
        /// providing additional contextual information when the user hovers
        /// over the icon.
        /// </summary>
        protected readonly string tooltip_;

        protected bool is_enabled_ = true;
        
        protected const string CLASS_NAME = "menubar-icon";
        protected const string CLASS_NAME_BUTTON=CLASS_NAME+"-button";

       /// <summary>
       /// Represents the base class for a menu bar icon in the user interface.
       /// Provides functionality and basic configurations for icons on a menu bar.
       /// </summary>
       /// <param name="icon">Represents the button element within the menu bar icon, responsible for handling user
       /// interactions such as clicks and facilitating the display of the menu bar icon's content and appearance.</param>
       /// <param name="tooltip">Represents the tooltip text associated with the menu bar icon, providing additional
       /// contextual information when the user hovers over the icon.</param>
        public MenuBarIconBase(Texture2D icon, string tooltip)
        {
            tooltip_ = tooltip;

            AddToClassList(CLASS_NAME);

            icon_button_ = new Button(() => OnClicked?.Invoke(this));
            icon_button_.AddToClassList(CLASS_NAME_BUTTON);

            if (icon!=null)
            {
                icon_button_.style.backgroundImage = new StyleBackground(icon);
            }
            
            // 设置工具提示
            icon_button_.tooltip = tooltip;
            
            Add(icon_button_);
            
            // 添加hover效果
            RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
        }

        protected virtual void OnMouseEnter(MouseEnterEvent evt)
        {
            icon_button_.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.2f);
        }
    
        protected virtual void OnMouseLeave(MouseLeaveEvent evt)
        {
            icon_button_.style.backgroundColor = Color.clear;
        }

        public string GetTooltip()=>tooltip_;

        public Texture2D GetIcon()=>icon_button_.style.backgroundImage.value.texture as Texture2D;
        public void SetActive(bool active)
        {
            is_enabled_ = active;
            icon_button_.SetEnabled(is_enabled_);
            style.opacity=is_enabled_?1:0.5f;
        }

        public Rect GetWorldRect() => worldBound;
        
        public event Action<IMenuBarIcon> OnClicked;
    }
}
