using System;
using System.Collections.Generic;
using System.Linq;
using Editor.View.BtWindows.MenuBar.Core.ManuBarIcon;
using ExTools.Utillties;
using LogManager.Core;
using LogManager.LogManagerFactory;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using PopupWindow = UnityEditor.PopupWindow;

namespace Editor.View.BtWindows.MenuBar.Core.MenuBarManager
{
    /// <summary>
    /// Represents the base class for managing a menu bar in an editor window.
    /// Provides functionality to handle menu item activation and menu state changes, as well as registering global
    /// events related to menu bar interactions.
    /// </summary>
    public class MenuBarManagerBase : VisualElement, IMenuBarManager
    {
        private readonly List<IMenuBarItem> menu_items_ = new();
        private readonly List<IMenuBarIcon> icon_items_ = new();
        
        private readonly VisualElement text_menu_container_;
        private readonly VisualElement icon_menu_container_;

        /// <summary>
        /// Represents the currently active menu item in the menu bar.
        /// This variable holds a reference to the <c>IMenuBarItem</c> object that is marked as active.
        /// </summary>
        protected IMenuBarItem active_menu_item_;

        /// <summary>
        /// Indicates whether the menu is currently open or not.
        /// This variable is used to track the open state of the menu bar and is set to true when a menu is opened
        /// and false when it is closed or deactivated.
        /// </summary>
        protected bool is_menu_open_;

        /// <summary>
        /// Holds a reference to the <c>EditorWindow</c> associated with the menu bar manager.
        /// This variable is used to interact with or manipulate the editor window, such as repainting or responding
        /// to user actions within the window.
        /// </summary>
        protected readonly EditorWindow parent_window_;

        public event Action<IMenuBarItem> OnMenuItemActivated;
        public event Action<bool> OnMenuStateChanged;

        /// <summary>
        /// Represents a predefined logging space specifically structured for the menu bar management in behavior tree windows.
        /// This static property provides a hierarchical log space for organizing and categorizing log entries
        /// related to the <c>MenuBarManagerBase</c>.
        /// </summary>
        protected static LogSpaceNode log_space_ => new LogSpaceNode("BehaviourTreeWindows").AddChild("MenuBar")
            .AddChild("MenuBarManagerBase");

        protected MenuBarManagerBase(EditorWindow window)
        {
            parent_window_ = window;

            var style_sheet =
                AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/Resource/Shared/MenuBarStyles.uss");
            if (style_sheet!=null)
            {
                styleSheets.Add(style_sheet);
            }
            
            style.flexDirection=FlexDirection.Row;
            style.alignItems=Align.Center;
            style.height=20;

            text_menu_container_ = new VisualElement();
            text_menu_container_.AddToClassList("menu-text-container");
            Add(text_menu_container_);

            var separator = new VisualElement
            {
                style =
                {
                    width = 1,
                    backgroundColor = Color.gray,
                    marginLeft = 4,
                    marginRight = 4,
                    alignSelf = Align.Stretch
                }
            };
            Add(separator);
            
            icon_menu_container_ = new VisualElement();
            icon_menu_container_.AddToClassList("menu-icon-container");
            Add(icon_menu_container_);

            // 注册全局事件
            RegisterCallback<MouseDownEvent>(OnGlobalMouseDown, TrickleDown.TrickleDown);
            RegisterCallback<MouseMoveEvent>(OnGlobalMouseMove, TrickleDown.TrickleDown);

            // 注册EditorApplication.update事件来检测弹出窗口状态
            EditorApplication.update += CheckPopupState;

            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                new LogEntry(LogLevel.kInfo, $"{GetType().Name}: 初始化完成"));
        }

        // 内部方法，用于安全地触发OnMenuItemActivated事件
        protected virtual void RaiseOnMenuItemActivated(IMenuBarItem item)
        {
            OnMenuItemActivated?.Invoke(item);
        }

        // 内部方法，用于安全地触发OnMenuStateChanged事件
        protected virtual void RaiseOnMenuStateChanged(bool isOpen)
        {
            OnMenuStateChanged?.Invoke(isOpen);
        }

        /// <summary>
        /// Monitors the state of popup windows and updates the menu bar state accordingly.
        /// Ensures the menu bar reflects changes when a popup window is closed or its focus is shifted.
        /// </summary>
        protected virtual void CheckPopupState()
        {
            var has_open_popup = EditorWindow.focusedWindow is PopupWindow;

            if (!is_menu_open_ || has_open_popup) return;
            is_menu_open_ = false;

            if (active_menu_item_ == null) return;
            active_menu_item_.SetActive(false);
            active_menu_item_ = null;
        }

        /// <summary>
        /// Handles global mouse move events within the menu bar.
        /// </summary>
        /// <param name="evt">The mouse moves event data.</param>
        protected virtual void OnGlobalMouseMove(MouseMoveEvent evt)
        {
        }

        /// <summary>
        /// Handles global mouse down events to determine interactions with the menu bar.
        /// Closes the active menu if the mouse click occurs outside the bounds of the menu items.
        /// </summary>
        /// <param name="evt">The mouse down event containing details about the click position and event context.</param>
        protected virtual void OnGlobalMouseDown(MouseDownEvent evt)
        {
            if (!is_menu_open_) return;

            // 检查点击是否在菜单上
            var clicked_on_menu_item = menu_items_.Any(item => item.GetWorldBound().Contains(evt.mousePosition));

            // 如果点击不在菜单栏上，则关闭菜单
            if (!clicked_on_menu_item)
            {
                CloseActiveMenu();
                evt.StopPropagation();
            }
        }

        public virtual void AddMenuItem(IMenuBarItem item)
        {
            if (menu_items_.Contains(item) || item == null)
            {
                return;
            }
            
            menu_items_.Add(item);
            text_menu_container_.Add((VisualElement)item);

            // 订阅菜单项事件
            item.OnActivated += OnMenuItemActivatedInternal;
            item.OnMouseEntered += OnMenuItemMouseEnter;

            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                new LogEntry(LogLevel.kInfo, $"{GetType().Name}: 添加菜单项 {item.GetName()}"));
        }

        /// <summary>
        /// Handles the mouse enter event for a menu bar item, enabling the display and activation
        /// of the new menu item if the menu is currently open.
        /// Ensures the currently active menu item is properly closed before switching.
        /// </summary>
        /// <param name="memMenuBarItem">The menu bar item that the mouse has entered.</param>
        protected virtual void OnMenuItemMouseEnter(IMenuBarItem memMenuBarItem)
        {
            if (!is_menu_open_ || memMenuBarItem == active_menu_item_) return;

            // 将要打开的新菜单项保存起来
            var item_to_open = memMenuBarItem;

            CloseActiveMenu(() =>
            {
                if (!parent_window_) // 确保父窗口还存在
                    return;

                is_menu_open_ = true;
                active_menu_item_ = item_to_open;
                active_menu_item_.SetActive(true);
                active_menu_item_.ShowMenu();
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                    new LogEntry(LogLevel.kInfo, $"菜单栏进行移动，切换到的名称为: {active_menu_item_.GetName()}"));
            });
        }

        /// <summary>
        /// Handles the activation of a menu item and triggers the corresponding event.
        /// Invokes necessary logic upon menu item activation and raises the `OnMenuItemActivated` event to notify subscribers.
        /// </summary>
        /// <param name="obj">The menu item that has been activated.</param>
        protected void OnMenuItemActivatedInternal(IMenuBarItem obj)
        {
            HandleMenuItemActivation(obj);
            OnMenuItemActivated?.Invoke(obj);
        }

        /// <summary>
        /// Manages the activation of menu items and updates the menu state accordingly.
        /// Handles the logic for toggling, switching, and opening menu items while maintaining proper logging of operations.
        /// </summary>
        /// <param name="menuBarItem">The menu item to activate or toggle. Represents the menu item that invokes the action.</param>
        protected virtual void HandleMenuItemActivation(IMenuBarItem menuBarItem)
        {
            // 如果菜单已经打开且点击了当前活动菜单项，则关闭菜单
            if (is_menu_open_ && menuBarItem == active_menu_item_)
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                    new LogEntry(LogLevel.kInfo, $"{GetType().Name}: 关闭菜单 {menuBarItem.GetName()} (再次点击)"));
                CloseActiveMenu();
                return;
            }

            // 如果菜单已经打开，但点击了不同的菜单项，则切换到新菜单
            if (is_menu_open_ && menuBarItem != active_menu_item_)
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                    new LogEntry(LogLevel.kInfo,
                        $"{GetType().Name}: 切换从 {(active_menu_item_ != null ? active_menu_item_.GetName() : "null")} 到 " +
                        $"{menuBarItem.GetName()}"));

                var item_to_open = menuBarItem;
                // 关闭当前活动菜单
                CloseActiveMenu(() =>
                {
                    if (!parent_window_) return;

                    is_menu_open_ = true;
                    active_menu_item_ = item_to_open;
                    active_menu_item_.SetActive(true);
                    active_menu_item_.ShowMenu();
                });

                return;
            }

            // 如果菜单未打开，则打开菜单
            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                new LogEntry(LogLevel.kInfo, $"{GetType().Name}: 首次打开菜单 {menuBarItem.GetName()}"));
            is_menu_open_ = true;
            active_menu_item_ = menuBarItem;
            active_menu_item_.SetActive(true);
            menuBarItem.ShowMenu();
        }

        public virtual bool RemoveMenuItem(IMenuBarItem item)
        {
            if (!menu_items_.Contains(item))
            {
                return false;
            }

            menu_items_.Remove(item);
            text_menu_container_.Remove((VisualElement)item);
            
            // 取消订阅菜单项事件
            item.OnActivated -= OnMenuItemActivatedInternal;
            item.OnMouseEntered -= OnMenuItemMouseEnter;

            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                new LogEntry(LogLevel.kInfo, $"{GetType().Name}: 成功移除菜单项 {item.GetName()}"));
            return true;
        }

        public virtual void CloseActiveMenu(Action on_closed_callback = null)
        {
            if (!is_menu_open_)
            {
                // 如果菜单本来就是关的，但有后续操作，直接执行
                on_closed_callback?.Invoke();
                return;
            }

            is_menu_open_ = false;

            // 立即更新激活项的状态
            if (active_menu_item_ != null)
            {
                active_menu_item_.SetActive(false);
                active_menu_item_ = null;
            }

            EditorApplication.delayCall += () =>
            {
                var windows = Resources.FindObjectsOfTypeAll<PopupWindow>();
                if (windows.Length > 0) windows[0].Close();

                parent_window_?.Repaint();

                // 在窗口关闭后，执行传入的回调
                on_closed_callback?.Invoke();
            };
        }

        public IMenuBarItem GetMenuItem(string menu_item_name)
        {
            return menu_items_.Find(item => item.GetName() == menu_item_name);
        }

        public void AddIconItem(IMenuBarIcon icon)
        {
            if (icon_items_.Contains(icon)||icon==null)
            {
                return;
            }
            
            icon_items_.Add(icon);
            icon_menu_container_.Add((VisualElement)icon);
            
            // 订阅点击事件
            icon.OnClicked += OnIconClicked;
            
            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace)
                .AddLog(log_space_, new LogEntry(LogLevel.kInfo, $"添加图标项: {icon.GetTooltip()}"));
        }

        public bool RemoveIconItem(IMenuBarIcon icon)
        {
            if (!icon_items_.Contains(icon))
            {
                return false;
            }

            icon_items_.Remove(icon);
            icon_menu_container_.Remove((VisualElement)icon);
            
            icon.OnClicked -= OnIconClicked;

            return true;
        }

        public event Action<IMenuBarIcon> OnIconClicked;

        public IMenuBarItem GetActiveMenuItem()
        {
            return active_menu_item_;
        }

        public void OnDestroy()
        {
            // 取消事件订阅
            EditorApplication.update -= CheckPopupState;

            // 接触菜单项事件订阅
            foreach (var item in menu_items_)
            {
                item.OnActivated -= OnMenuItemActivatedInternal;
                item.OnMouseEntered -= OnMenuItemMouseEnter;
            }

            CloseActiveMenu();
        }
    }
}