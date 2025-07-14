using System;
using System.Linq;
using Editor.View.BtWindows.SearchBar.Core;
using Editor.View.BtWindows.SearchBar.Storage;
using ExTools.Utillties;
using LogManager.Core;
using LogManager.LogManagerFactory;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.View.BtWindows.SearchBar
{
    /// <summary>
    /// Represents a search view component for a user interface, extending from VisualElement.
    /// </summary>
    public class SearchView : VisualElement
    {
        private SearchController controller_;
        private VisualElement parent_container_;

        // UI元素引用
        private VisualElement search_bar_root_;
        private CustomSearchField search_field_;
        private VisualElement filters_container_;
        private Label result_label_;
        private Button clear_button_;
        private Button prev_button_;
        private Button next_button_;
        private Button close_button_;
        private VisualElement separator_;

        public bool OpenWindow { get; private set; } = false;

        private const string USS_NO_RESULTS_CLASS = "no-results";
        private const string USS_HIDDEN_CLASS = "hidden";

        public SearchView(SearchController controller)
        {
            InitUI();
            SetController(controller);
        }

        public SearchView()
        {
            InitUI();
        }

        private void InitUI()
        {
            // 创建浮动搜索栏容器
            search_bar_root_ = new VisualElement();
            search_bar_root_.AddToClassList("search-bar-floating");

            // 搜索图标
            var search_icon = new VisualElement();
            search_icon.AddToClassList("search-icon");
            search_bar_root_.Add(search_icon);

            // 搜索输入框
            // search_field_ = new TextField();
            search_field_ = new CustomSearchField();
            search_field_.AddToClassList("search-field");
            search_field_.RegisterCallback<FocusInEvent>(OnSearchFieldFocused);
            search_bar_root_.Add(search_field_);

            // 清除按钮（初始隐藏）
            clear_button_ = new Button(ClearSearch)
            {
                text = "×"
            };
            clear_button_.AddToClassList("clear-button");
            clear_button_.AddToClassList(USS_HIDDEN_CLASS);
            search_bar_root_.Add(clear_button_);

            // 过滤器容器
            filters_container_ = new VisualElement();
            filters_container_.AddToClassList("filters-container");
            search_bar_root_.Add(filters_container_);

            // 分隔符
            separator_ = new VisualElement();
            separator_.AddToClassList("separator");
            search_bar_root_.Add(separator_);

            // 结果标签
            result_label_ = new Label();
            result_label_.AddToClassList("result-label");
            search_bar_root_.Add(result_label_);

            // 上一个结果按钮
            prev_button_ = new Button(() => controller_?.ActivatePreviousResult());
            prev_button_.AddToClassList("nav-button");
            prev_button_.AddToClassList("prev-button");
            prev_button_.tooltip = "上一个结果";
            search_bar_root_.Add(prev_button_);

            // 下一个结果按钮
            next_button_ = new Button(() => controller_?.ActivateNextResult());
            next_button_.AddToClassList("nav-button");
            next_button_.AddToClassList("next-button");
            next_button_.tooltip = "下一个结果";
            search_bar_root_.Add(next_button_);

            // 关闭按钮
            close_button_ = new Button(Hide)
            {
                text = "×"
            };
            close_button_.AddToClassList("close-button");
            close_button_.tooltip = "关闭搜索";
            search_bar_root_.Add(close_button_);

            // 添加到当前容器
            Add(search_bar_root_);

            // 加载样式
            LoadStyles();

            // 初始隐藏
            Hide();
        }

        private void ClearSearch()
        {
            search_field_.SetValueWithoutNotify("");
            controller_.PerformSearch("");
            UpdateClearButtonVisibility("");
            search_field_.Focus();
        }

        private void OnSearchFieldFocused(FocusInEvent evt)
        {
            // 当搜索框获得聚点时，确保执行搜索
            if (!string.IsNullOrEmpty(search_field_.value)) controller_?.PerformSearch(search_field_.value);
        }

        private void LoadStyles()
        {
            var style_sheet =
                AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/Resource/Shared/SearchBarStyles.uss");
            if (style_sheet != null)
            {
                styleSheets.Add(style_sheet);
            }
        }

        /// <summary>
        /// Sets the search controller and updates related UI and event handling.
        /// </summary>
        /// <param name="controller">The <see cref="SearchController"/> instance to associate with this view. If null,
        /// the view will be hidden.</param>
        public void SetController(SearchController controller)
        {
            // 接触旧控制器事件
            if (controller_ != null) controller_.OnResultsChanged -= HandleResultsUpdated;

            controller_ = controller;

            if (controller_ == null)
            {
                style.display = DisplayStyle.None;
                return;
            }

            controller_.OnResultsChanged += HandleResultsUpdated;

            search_field_.RegisterValueChangedCallback((_,new_value) =>
            {
                controller_.PerformSearch(new_value);
                UpdateClearButtonVisibility(new_value);
            });
            
            search_field_.RegisterKeyDownCallback(OnKeyDown);

            clear_button_.clicked += () => search_field_.SetValueWithoutNotify("");

            BuildFilterUI();
        }

        /// <summary>
        /// Updates the visibility of the clear button based on the current search field value.
        /// </summary>
        /// <param name="evtNewValue">The current value of the search field. If empty, the clear button will be hidden;
        /// otherwise, it will be shown.</param>
        private void UpdateClearButtonVisibility(string evtNewValue)
        {
            if (string.IsNullOrEmpty(evtNewValue))
                clear_button_.AddToClassList(USS_HIDDEN_CLASS);
            else
                clear_button_.RemoveFromClassList(USS_HIDDEN_CLASS);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    // 立即停止此元素桑的其他回调（包括内置回调）
                    // 仍然保留，作为第一道防线
                    evt.StopImmediatePropagation();
                    evt.PreventDefault();

                    // 捕获当前的 shift 键状态，因为 evt 对象在调度执行时可能已失效
                    bool isShiftPressed = evt.shiftKey;

                    // 核心解决方案：将导航逻辑调度到当前事件处理流程之后执行
                    // 这就避免了与 TextField 的内置“提交”行为发生直接冲突
                    schedule.Execute(() =>
                    {
                        bool has_results = controller_?.GetLastResults()?.HasResults ?? false;
                        if (!has_results)
                        {
                            return; // 没有结果，不执行任何操作
                        }

                        if (isShiftPressed)
                        {
                            controller_?.ActivatePreviousResult();
                        }
                        else
                        {
                            controller_?.ActivateNextResult();
                        }
                    });
                    break;
                case KeyCode.Escape:
                    Hide();
                    evt.StopPropagation();
                    evt.PreventDefault();
                    break;
            }
        }

        /// <summary>
        /// Displays the search view within the specified parent container, initializes its state,
        /// and ensures the search field is ready for interaction.
        /// </summary>
        /// <param name="parent_container">The parent <see cref="VisualElement"/> where the search view will be added.
        /// Throws an error log if null is provided.</param>
        public void Show(VisualElement parent_container)
        {
            parent_container_ = parent_container;

            if (parent_container_==null)
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(new 
                    LogSpaceNode(null),new LogEntry(LogLevel.kError,"Parent container cannot be null"),true);
                return;
            }
            
            // 将搜索栏添加到父容器
            parent?.Remove(this);

            parent_container_.Add(this);
            
            // 显示搜索栏
            style.display = DisplayStyle.Flex;
            style.top = 15f;
            style.right = 15f;
            
            // 聚焦搜索栏
            search_field_.Focus();
            
            // 执行空搜索以初始化
            controller_?.PerformSearch("");

            OpenWindow = true;
        }

        /// <summary>
        /// Hides the search view by setting its display style to none, clearing the search input field,
        /// stopping the search operation, updating UI elements, and removing the view from its parent container if applicable.
        /// </summary>
        public void Hide()
        {
            style.display = DisplayStyle.None;
            
            // 清除搜索内容
            search_field_?.SetValueWithoutNotify("");
            controller_?.PerformSearch("");
            UpdateClearButtonVisibility("");
            
            // 从父容器中移除
            parent?.Remove(this);
            OpenWindow = false;
        }

        /// <summary>
        /// Dynamically rebuilds the filter UI based on the available filters provided by the associated controller.
        /// Clears existing filter elements, and for each available filter, adds a button to the container.
        /// Each button allows toggling the corresponding filter's state and triggers a search update via the controller.
        /// </summary>
        private void BuildFilterUI()
        {
            filters_container_.Clear();

            if (controller_?.AvailableFilters == null) return;
            
            // 只显示在UI中可见的过滤器
            var visibleFilters = controller_.AvailableFilters.Values.Where(f => f.IsVisibleInUI);

            foreach (var filter in visibleFilters)
            {
                var toggle = new Button();
                toggle.AddToClassList("filter-toggle");
                toggle.tooltip = filter.Tooltip;

                // 设置图标
                if (filter.Icon != null) toggle.style.backgroundImage = filter.Icon;

                // 设置初始化状态
                if (filter.IsDefaultActive) toggle.AddToClassList("active");

                toggle.clicked += () =>
                {
                    var is_active = toggle.ClassListContains("active");
                    if (is_active)
                        toggle.RemoveFromClassList("active");
                    else
                        toggle.AddToClassList("active");

                    // 将UI操作转发给控制器
                    controller_.ToggleFilter(filter.FilterId, !is_active);
                    controller_.PerformSearch(search_field_.value);
                };

                filters_container_.Add(toggle);
            }
        }

        private void HandleResultsUpdated(SearchResults results)
        {
            // 更新结果标签
            if (results.HasResults)
            {
                var current_index = controller_.GetCurrentIndex();
                result_label_.text = $"{current_index + 1}/{results.ScoredResults.Count}";
                search_field_.RemoveFromClassList(USS_NO_RESULTS_CLASS);
                result_label_.RemoveFromClassList(USS_NO_RESULTS_CLASS);
            }
            else if (!string.IsNullOrEmpty(results.OriginalQuery))
            {
                result_label_.text = "0/0";
                search_field_.AddToClassList(USS_NO_RESULTS_CLASS);
                result_label_.AddToClassList(USS_NO_RESULTS_CLASS);
            }
            else
            {
                result_label_.text = "";
                search_field_.RemoveFromClassList(USS_NO_RESULTS_CLASS);
                result_label_.RemoveFromClassList(USS_NO_RESULTS_CLASS);
            }

            // 更新导航按钮状态
            var has_results = results.HasResults;
            prev_button_.SetEnabled(has_results);
            next_button_.SetEnabled(has_results);
        }

        /// <summary>
        /// Toggles the visibility of the search view within the specified container. If the search view is currently hidden,
        /// it will be shown. Otherwise, it will be hidden.
        /// </summary>
        /// <param name="parent_container">The parent container <see cref="VisualElement"/> in which the search view resides.
        /// This container is used to display the search view when toggled to visible.</param>
        public void Toggle(VisualElement parent_container)
        {
            if (style.display==DisplayStyle.None)
            {
                Show(parent_container);
            }
            else
            {
                Hide();
            }
        }
    }
}