using System;
using System.Collections.Generic;
using LogManager.Core;
using LogManager.LogManagerFactory;
using Script.LogManager;
using Script.Utillties;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using PopupWindow = UnityEditor.PopupWindow;

namespace Editor.View.BtWindows.MenuBar.Core
{
    /// <summary>
    /// Provides a base implementation for menu bar items within the editor.
    /// Handles common functionality such as user interaction, visual state updates,
    /// and menu activation mechanisms.
    /// </summary>
    public abstract class MenuBarItemBase : VisualElement, IMenuBarItem
    {
        protected readonly Label title_label_;
        protected bool is_active_;
        protected bool is_hovered_ = false;
        protected readonly string menu_name_;

        protected static LogSpaceNode log_space =>
            new LogSpaceNode("BehaviourTreeWindows").AddChild("MenuBar").AddChild("MenuBarItem");

        public event Action<IMenuBarItem> OnActivated;
        public event Action<IMenuBarItem> OnMouseEntered;

        /// <summary>
        /// Creates and returns a list of menu items for the menu bar item.
        /// This method is intended to be implemented by derived classes to define
        /// specific menu content for each menu bar item.
        /// </summary>
        /// <returns>A list of <see cref="MenuItemData"/> objects representing the menu items.</returns>
        protected abstract List<MenuItemData> CreateMenuItems();

        /// <summary>
        /// Represents the base class for menu bar items in the behavior tree editor.
        /// </summary>
        public MenuBarItemBase(string title)
        {
            menu_name_ = title;
            
            var style_sheet=AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/Resource/BtWindows/MenuBar/MenuBarItem.uss");
            if (style_sheet!=null)
            {
                styleSheets.Add(style_sheet);
            }

            // 设置基本样式
            style.height = 20;
            style.paddingLeft = 8;
            style.paddingRight = 8;
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;

            // 添加标签
            title_label_ = new Label(title);
            title_label_.AddToClassList("menu-bar-item__title");
            Add(title_label_);

            // 添加事件处理
            RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            RegisterCallback<MouseDownEvent>(OnMouseDown);

            // 设置可点击
            pickingMode = PickingMode.Position;

            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space,
                new LogEntry(LogLevel.kInfo, $"MenuBarItem: {title} 初始化完成"));
        }

        // 实现IMenuBarItem接口
        public void SetActive(bool active)
        {
            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space,
                new LogEntry(LogLevel.kInfo, $"MenuBarItem: {menu_name_} 设置激活状态为 {active}"));
            is_active_ = active;
            UpdateVisualState();
        }

        public Rect GetWorldBound()
        {
            return worldBound;
        }

        public string GetName()
        {
            return menu_name_;
        }

        public void ShowMenu()
        {
            var menu_items = CreateMenuItems();

            if (menu_items==null||menu_items.Count==0)
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace)
                    .AddLog(log_space, new LogEntry(LogLevel.kWarning, $"{menu_name_}: 没有菜单项可显示"));
                return;
            }

            var world_bound = GetWorldBound();
            var advanced_popup = new AdvancedMenuPopup(menu_items, () =>
            {
                // 菜单关闭时的回调
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace)
                    .AddLog(log_space, new LogEntry(LogLevel.kInfo, $"{menu_name_}: 菜单已关闭"));
            });
            
            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace)
                .AddLog(log_space, new LogEntry(LogLevel.kInfo, $"{menu_name_}: 显示菜单，位置: {world_bound}"));
            
            PopupWindow.Show(world_bound,advanced_popup);
        }

        /// <summary>
        /// Updates the visual state of the menu bar item based on its current properties.
        /// Adjusts the background color depending on whether the item is active, hovered, or in its default state.
        /// </summary>
        protected void UpdateVisualState()
        {
            RemoveFromClassList("menu-bar-item--active");
            RemoveFromClassList("menu-bar-item--hovered");
            RemoveFromClassList("menu-bar-item--normal");
            if (is_active_)
            {
                AddToClassList("menu-bar-item--active");
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space,
                    new LogEntry(LogLevel.kInfo, $"MenuBarItem: {menu_name_} 视觉状态 - 激活"));
            }
            else if (is_hovered_)
            {
                AddToClassList("menu-bar-item--hovered");
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space,
                    new LogEntry(LogLevel.kInfo, $"MenuBarItem: {menu_name_} 视觉状态 - 悬停"));
            }
            else
            {
                AddToClassList("menu-bar-item--normal");
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space,
                    new LogEntry(LogLevel.kInfo, $"MenuBarItem: {menu_name_} 视觉状态 - 正常"));
            }
        }

        private void OnMouseEnter(MouseEnterEvent evt)
        {
            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space,
                new LogEntry(LogLevel.kInfo, $"MenuBarItem: {menu_name_} 鼠标进入 位置={evt.mousePosition}"));
            is_hovered_ = true;
            UpdateVisualState();
            OnMouseEntered?.Invoke(this);
        }

        private void OnMouseLeave(MouseLeaveEvent evt)
        {
            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space,
                new LogEntry(LogLevel.kInfo, $"MenuBarItem: {menu_name_} 鼠标离开 位置={evt.mousePosition}"));
            is_hovered_ = false;
            UpdateVisualState();
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space,
                new LogEntry(LogLevel.kInfo, $"MenuBarItem: {menu_name_} 鼠标点击 位置={evt.mousePosition}"));
            OnActivated?.Invoke(this);
            evt.StopPropagation();
        }

        // 关闭菜单的回调方法，在菜单项操作完成后调用
        protected void CloseMenuAfterAction(Action action)
        {
            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space,
                new LogEntry(LogLevel.kInfo, $"MenuBarItem: {menu_name_} 执行操作并关闭菜单"));
            action?.Invoke();

            // 调用GUI.FocusControl(null)来取消菜单焦点
            EditorApplication.delayCall += () =>
            {
                GUI.FocusControl(null);
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space,
                    new LogEntry(LogLevel.kInfo, $"MenuBarItem: {menu_name_} 已清除焦点"));
            };
        }
    }
}