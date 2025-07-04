using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Editor.View.BtWindows.MenuBar.Core
{
    /// <summary>
    /// Represents a popup window for an advanced menu within the editor's UI system.
    /// </summary>
    /// <remarks>
    /// This class provides functionality for displaying dynamic and hierarchical menu items using
    /// a popup window. It extends the <see cref="PopupWindowContent"/> class to offer advanced
    /// customization and event-driven behaviors tailored for menu operations in Unity's editor environment.
    /// </remarks>
    public class AdvancedMenuPopup : PopupWindowContent
    {
        private class MenuLevel
        {
            public readonly List<MenuItemData> Items;
            public Rect PanelRect; // 菜单面板在窗口中的相对矩形
            public int ParentItemIndex; // 在上一级菜单中，是那个索引的项展开了此菜单
            public int HoveredItemIndex = -1;

            public MenuLevel(List<MenuItemData> items, Rect panel_rect, int parent_item_index)
            {
                Items = items;
                PanelRect = panel_rect;
                ParentItemIndex = parent_item_index;
            }
        }

        private readonly List<MenuLevel> menu_levels_ = new();
        private readonly Action on_close_callback_;
        private readonly GUIStyle item_style_;
        private readonly GUIStyle separator_style_;

        private const float ITEM_HEIGHT = 22f;
        private const float SEPARATOR_HEIGHT = 8f;
        private const float ARROW_WIDTH = 20f;
        private const float PANEL_MIN_WIDTH = 150f;
        private const float SUBMENU_OPEN_DELAY = 0.2f; // 悬停延迟打开，防止划过时闪烁

        private int last_hovered_level_ = -1;
        private int last_hovered_index_ = -1;
        private float hover_start_time_;

        #region 空间记录器

        private Vector2 cached_window_size_;
        private bool window_size_calculated_ = false;
        private int last_menu_levels_count_ = 0;

        #endregion
        
        public AdvancedMenuPopup(List<MenuItemData> initial_items, Action onCloseCallback)
        {
            on_close_callback_ = onCloseCallback;

            // 样式初始化
            item_style_ = new GUIStyle(EditorStyles.label)
            {
                padding = new RectOffset(10, 10, 4, 4),
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black },
                hover = EditorStyles.label.hover
            };
            separator_style_ = new GUIStyle
            {
                fixedHeight = SEPARATOR_HEIGHT,
                padding = new RectOffset(5, 5, 3, 3)
            };

            // 初始化一级菜单
            var initial_rect = new Rect(0, 0, 0, 0);
            var first_level = new MenuLevel(initial_items, initial_rect, -1);
            CalculatePanelSize(first_level);
            menu_levels_.Add(first_level);
            last_menu_levels_count_ = menu_levels_.Count;
            
            // 预计算窗口大小
            UpdateWindowSize();
        }

        /// <summary>
        /// Updates the current size of the popup window based on the dimensions and layout
        /// of all active menu levels. This method recalculates the total bounds occupied
        /// by the menu levels and adjusts the cached window size accordingly. If no levels
        /// are present, the size is set to a minimal value.
        /// This method ensures the UI window adapts dynamically to changes in menu structure,
        /// maintaining appropriate dimensions for content display.
        /// </summary>
        private void UpdateWindowSize()
        {
            if (menu_levels_.Count==0)
            {
                cached_window_size_=Vector2.one;
                return;
            }

            if (menu_levels_.Count==1)
            {
                var first_level = menu_levels_[0];
                cached_window_size_=new Vector2(first_level.PanelRect.width, first_level.PanelRect.height);
                window_size_calculated_ = true;
                return;
            }
            
            // 计算所有菜单面板的总边界框
            var total_bounds = menu_levels_[0].PanelRect;
            foreach (var level in menu_levels_)
            {
                total_bounds = Rect.MinMaxRect(
                    Mathf.Min(total_bounds.xMin, level.PanelRect.xMin),
                    Mathf.Min(total_bounds.yMin, level.PanelRect.yMin),
                    Mathf.Max(total_bounds.xMax, level.PanelRect.xMax),
                    Mathf.Max(total_bounds.yMax, level.PanelRect.yMax));
            }

            cached_window_size_ = total_bounds.size;
            window_size_calculated_ = true;
        }

        /// <summary>
        /// Ensures the popup window size is up-to-date by checking for any changes in the
        /// number of active menu levels. If a difference is detected, the window size is recalculated
        /// to match the new layout. This method helps maintain consistent and accurate dimensions
        /// for the popup UI when menu levels are added or removed dynamically.
        /// </summary>
        private void CheckAndUpdateWindowSize()
        {
            if (last_menu_levels_count_!=menu_levels_.Count)
            {
                last_menu_levels_count_=menu_levels_.Count;
                UpdateWindowSize();
            }
        }

        public override Vector2 GetWindowSize()
        {
            CheckAndUpdateWindowSize();
            // 返回缓存的窗口大小，避免重复计算导致位置漂移
            if (window_size_calculated_)
            {
                return cached_window_size_;
            }
            
            if (menu_levels_.Count == 0) return Vector2.one;

            UpdateWindowSize();
            return cached_window_size_;
        }

        public override void OnGUI(Rect rect)
        {
            GUI.FocusControl(null);

            var active_menu_region = new List<Rect>();
            foreach (var level in menu_levels_) active_menu_region.Add(level.PanelRect);

            // 判断点击事件
            if (Event.current.type == EventType.MouseDown)
            {
                var clicked_on_any_menu =
                    active_menu_region.Any(panel_rect => panel_rect.Contains(Event.current.mousePosition));
                // 如果点击位置在窗口内，但不再任何一个菜单面板上，则关闭窗口
                if (rect.Contains(Event.current.mousePosition) && !clicked_on_any_menu)
                {
                    editorWindow?.Close();
                    return;
                }
            }

            var current_hover_level = -1;
            var current_hover_index = -1;

            // 绘制所有菜单层级
            for (var i = 0; i < menu_levels_.Count; i++)
                DrawMenuPanel(menu_levels_[i], i, ref current_hover_level, ref current_hover_index);

            ProcessHover(current_hover_level, current_hover_index);

            // 只在需要时请求重绘
            if (current_hover_level!=-1||last_hovered_level_!=-1)
            {
                editorWindow?.Repaint();
            }
        }

        /// <summary>
        /// Renders a specific level of the menu within the popup window. This method handles
        /// the layout, positioning, and boundary adjustments for the menu level being drawn,
        /// as well as ensures proper alignment and visibility within the parent window.
        /// It also takes care of rendering individual menu items, managing hover states,
        /// and responding to user interactions such as mouse clicks on menu items or submenus.
        /// </summary>
        /// <param name="level">The menu level to be drawn, including its bounds and items.</param>
        /// <param name="level_index">The zero-based index of the menu level being processed.</param>
        /// <param name="currentHoverLevel">A reference to the index of the currently hovered menu level.</param>
        /// <param name="currentHoverIndex">A reference to the index of the currently hovered menu item within the active level.</param>
        private void DrawMenuPanel(MenuLevel level, int level_index, ref int currentHoverLevel,
            ref int currentHoverIndex)
        {
            // 对于子菜单，需要调整绘制位置以适应窗口大小限制
            var draw_rect = level.PanelRect;

            // 如果是子菜单且超出了窗口边界，需要进行位置调整
            if (level_index>0)
            {
                var window_rect=new Rect(0,0,cached_window_size_.x,cached_window_size_.y);
                
                // 如果子菜单超出右边界，则显示在左侧
                if (draw_rect.xMax>window_rect.width)
                {
                    var parent_level=menu_levels_[level_index-1];
                    draw_rect.x = parent_level.PanelRect.x - draw_rect.width - 2.0f;
                }
                
                // 如果子菜单超出下边界，则向上调整
                if (draw_rect.yMax>window_rect.height)
                {
                    draw_rect.y = window_rect.height - draw_rect.height;
                }
                
                // 更新菜单层级的实际位置
                level.PanelRect = draw_rect;
            }
            
            // 绘制背景，由于PopupWindow本身会绘制一个"grey_border"风格的背景因此无法取消多级菜单的颜色部分
            GUI.Box(draw_rect,GUIContent.none);

            var y = level.PanelRect.y;
            for (var i = 0; i < level.Items.Count; i++)
            {
                var item = level.Items[i];

                if (item.IsSeparator)
                {
                    var sep_rect = new Rect(
                        level.PanelRect.x + separator_style_.padding.left,
                        y + separator_style_.padding.top,
                        level.PanelRect.width - separator_style_.padding.horizontal,
                        1);
                    EditorGUI.DrawRect(sep_rect, new Color(0.3f, 0.3f, 0.3f));
                    y += SEPARATOR_HEIGHT;
                    continue;
                }

                var item_rect = new Rect(level.PanelRect.x, y, level.PanelRect.width, ITEM_HEIGHT);
                var is_hovered = item_rect.Contains(Event.current.mousePosition);

                if (is_hovered)
                {
                    currentHoverLevel = level_index;
                    currentHoverIndex = i;
                    if (item.Enabled) EditorGUI.DrawRect(item_rect, new Color(0.35f, 0.35f, 0.35f));
                }

                EditorGUI.BeginDisabledGroup(!item.Enabled);
                GUI.Label(item_rect, item.Name, item_style_);
                if (item.HasSubItems)
                {
                    var arrow_rect = new Rect(item_rect.xMax - ARROW_WIDTH, item_rect.y, ARROW_WIDTH, item_rect.height);
                    GUI.Label(arrow_rect, ">");
                }

                EditorGUI.EndDisabledGroup();

                if (is_hovered && item.Enabled && Event.current.type == EventType.MouseDown &&
                    Event.current.button == 0)
                {
                    if (!item.HasSubItems)
                    {
                        item.Action?.Invoke();
                        editorWindow.Close();
                    }

                    Event.current.Use();
                }

                y += ITEM_HEIGHT;
            }
        }

        /// <summary>
        /// Processes hover events within the menu, identifying the currently hovered menu level
        /// and item index. This method tracks changes in hover state, updates internal state,
        /// and triggers submenu opening based on hover timing and conditions.
        /// </summary>
        /// <param name="currentHoverLevel">The index of the menu level currently being hovered. Pass -1 if no level is
        /// hovered.</param>
        /// <param name="currentHoverIndex">The index of the item currently being hovered within the selected menu
        /// level. Pass -1 if no item is hovered.</param>
        private void ProcessHover(int currentHoverLevel, int currentHoverIndex)
        {
            if (currentHoverLevel != last_hovered_level_ || currentHoverIndex != last_hovered_index_)
            {
                last_hovered_level_ = currentHoverLevel;
                last_hovered_index_ = currentHoverIndex;
                hover_start_time_ = Time.realtimeSinceStartup;

                // 如果鼠标移动到更浅的层级，立即关闭更深的子菜单
                if (currentHoverLevel != -1 && menu_levels_.Count > currentHoverLevel + 1)
                {
                    menu_levels_.RemoveRange(currentHoverLevel + 1, menu_levels_.Count - (currentHoverLevel + 1));
                    UpdateWindowSize();
                }
            }

            if (last_hovered_level_ == -1) return;

            var hovered_item = menu_levels_[last_hovered_level_].Items[last_hovered_index_];
            if (hovered_item.Enabled && hovered_item.HasSubItems && menu_levels_.Count == last_hovered_level_ + 1)
                if (Time.realtimeSinceStartup - hover_start_time_ > SUBMENU_OPEN_DELAY)
                {
                    OpenSubMenu(last_hovered_level_, last_hovered_index_);
                    // 重置计时器防止重复打开
                    hover_start_time_ = float.MaxValue;
                }
        }

        /// <summary>
        /// Opens a new sub-menu at the given menu level and item index. This method determines
        /// the position and dimensions of the sub-menu based on the parent item's location
        /// and the layout of the current menu structure. The sub-menu is dynamically added
        /// to the list of active menu levels, and the popup window size is updated accordingly.
        /// </summary>
        /// <param name="level_index">The index of the current menu level that contains the parent item.</param>
        /// <param name="item_index">The index of the menu item within the specified level that triggers the sub-menu.</param>
        private void OpenSubMenu(int level_index, int item_index)
        {
            var parent_level = menu_levels_[level_index];
            var parent_item = parent_level.Items[item_index];

            float current_y_in_parent_panel = parent_level.PanelRect.y;
            for (int i = 0; i < item_index; i++)
            {
                if (parent_level.Items[i].IsSeparator)
                {
                    current_y_in_parent_panel += SEPARATOR_HEIGHT;
                }
                else
                {
                    current_y_in_parent_panel += ITEM_HEIGHT;
                }
            }

            // 计算新子菜单的位置
            var sub_menu_x = parent_level.PanelRect.xMax + 2.0f;
            var sub_menu_y = current_y_in_parent_panel;

            var sub_level = new MenuLevel(parent_item.SubItems, new Rect(sub_menu_x, sub_menu_y, 0, 0), item_index);
            CalculatePanelSize(sub_level);

            menu_levels_.Add(sub_level);
            
            // 新增子菜单，需要更新窗口大小
            UpdateWindowSize();
        }

        /// <summary>
        /// Calculates and updates the dimensions of a given menu level's panel. This includes
        /// determining the maximum width required for the menu items and the total height
        /// based on the content, separators, and any sub-items. The resulting dimensions are
        /// applied to the panel's rect, ensuring proper layout and visual representation
        /// of the menu.
        /// </summary>
        /// <param name="level">The menu level for which the panel size is to be calculated.
        /// It contains the menu items and the panel's current rect, which will be adjusted.</param>
        private void CalculatePanelSize(MenuLevel level)
        {
            var max_width = PANEL_MIN_WIDTH;
            float total_height = 0;
            foreach (var item in level.Items)
            {
                if (item.IsSeparator)
                {
                    total_height += SEPARATOR_HEIGHT;
                    continue;
                }

                var width = item_style_.CalcSize(new GUIContent(item.Name)).x;
                if (item.HasSubItems) width += ARROW_WIDTH;

                if (width > max_width) max_width = width;

                total_height += ITEM_HEIGHT;
            }

            level.PanelRect=new Rect(level.PanelRect.x, level.PanelRect.y, max_width, total_height);
        }

        public override void OnClose()
        {
            on_close_callback_?.Invoke();
        }
    }
}