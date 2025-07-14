using System;
using Editor.EditorToolExs.Operation.Core;
using Editor.EditorToolExs.Operation.Storage;
using Editor.View.BtWindows.SearchBar.Operation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.View.BtWindows.SearchBar.Storage
{
    public class CustomSearchField : VisualElement,IDisposable
    {
        public const string ussClassName = "custom-search-field";
        public const string textInputUssClassName = ussClassName + "__text-input";
        public const string textDisplayUssClassName = ussClassName + "__text-display";
        public const string placeholderUssClassName = ussClassName + "__placeholder";
        public const string caretUssClassName = ussClassName + "__caret";
        public const string focusedUssClassName = ussClassName + "--focused";

        /// <summary>
        /// Represents a label used as a placeholder in the custom search field.
        /// This label displays placeholder text when the search field is empty
        /// and is styled with the specified placeholder USS class name.
        /// </summary>
        private Label placeholder_;

        /// <summary>
        /// Represents the container used to capture events and act as
        /// the holder for text and the caret in the custom search field.
        /// This element enables user interaction and text-related operations
        /// within the search field.
        /// </summary>
        private VisualElement text_input_;

        /// <summary>
        /// Represents a label used for displaying the currently entered text in the custom search field.
        /// This label is styled with a specific USS class name and is managed within the text input VisualElement.
        /// Its primary purpose is to render the text value in the field for user visibility while ensuring proper
        /// alignment and interaction.
        /// </summary>
        private Label text_display_;

        /// <summary>
        /// Represents the visual element used as the caret indicator for the text input field
        /// within the custom search field. This element displays the caret position during
        /// text editing and is styled with the corresponding USS class name.
        /// </summary>
        private VisualElement caret_;

        /// <summary>
        /// Represents a scheduled item responsible for managing the blinking behavior of the caret
        /// in the custom search field. This control ensures the caret's visibility toggles periodically
        /// when the search field gains focus, providing a visual indication of the active input state.
        /// </summary>
        private IVisualElementScheduledItem caret_blink_;

        private string value_ = "";
        private int caret_index_ = 0;
        private bool is_focused_ = false;

        public bool ClearOnHide { get; set; } = true;

        /// <summary>
        /// Gets or sets the current value of the search field.
        /// When the value is set, the control updates its display and triggers the `onValueChanged` callback,
        /// if specified, passing the old value and the new value as parameters.
        /// Assigning a value will also reflect any necessary UI updates.
        /// </summary>
        public string value
        {
            get => value_;
            set
            {
                if (value_ != value)
                {
                    var oldValue = value_;
                    value_ = value;
                    UpdateDisplay();
                    OnValueChanged?.Invoke(oldValue, value_);
                }
            }
        }

        /// <summary>
        /// Gets or sets the placeholder text displayed in the search field when it is empty.
        /// This text provides a hint to the user about the expected input or purpose of the search field.
        /// </summary>
        public string placeholderText
        {
            get => placeholder_.text;
            set => placeholder_.text = value;
        }

        public Action<string, string> OnValueChanged;
        public Action<KeyDownEvent> onKeyDown;
        public OperationManager SearchOperationManager { get; } = new();

        public CustomSearchField()
        {
            AddToClassList(ussClassName);
            InitializeUI();
            SetupEventHandlers();
            focusable = true;
        }

        private void InitializeUI()
        {
            text_input_ = new VisualElement();
            text_input_.AddToClassList(textInputUssClassName);
            Add(text_input_);

            placeholder_ = new Label("搜索...");
            placeholder_.AddToClassList(placeholderUssClassName);
            placeholder_.pickingMode = PickingMode.Ignore;
            Add(placeholder_);

            text_display_ = new Label();
            text_display_.AddToClassList(textDisplayUssClassName);
            text_display_.pickingMode = PickingMode.Ignore;
            text_input_.Add(text_display_);

            caret_ = new VisualElement();
            caret_.AddToClassList(caretUssClassName);
            caret_.pickingMode = PickingMode.Ignore;
            caret_.style.visibility = Visibility.Hidden; // 默认隐藏
            text_input_.Add(caret_);

            UpdateDisplay();
        }

        private void SetupEventHandlers()
        {
            RegisterCallback<FocusInEvent>(OnFocusIn);
            RegisterCallback<FocusOutEvent>(OnFocusOut);
            RegisterCallback<KeyDownEvent>(OnKeyDown);
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (ClearOnHide)
            {
                Dispose();
            }
        }

        /// <summary>
        /// Handles the mouse down event for the CustomSearchField, setting focus and updating the caret position based
        /// on the click location.
        /// </summary>
        /// <param name="evt">The mouse down event triggering the action, containing information such as button pressed
        /// and mouse position.</param>
        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button == 0) // 左键
            {
                Focus();
                // 根据鼠标点击位置计算光标索引
                var mouseX = evt.localMousePosition.x;
                var newIndex = 0;
                var minDistance = float.MaxValue;

                // 遍历所有可能的插入点（包括字符串末尾）
                for (var i = 0; i <= value_.Length; i++)
                {
                    var textWidth = text_display_.MeasureTextSize(value_.Substring(0, i), 
                        0, MeasureMode.Undefined, 0,
                        MeasureMode.Undefined).x;
                    var distance = Mathf.Abs(mouseX - textWidth);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        newIndex = i;
                    }
                }

                caret_index_ = newIndex;
                UpdateCaretVisuals();
                evt.StopPropagation();
            }
        }

        /// <summary>
        /// Updates the value and caret position of the CustomSearchField.
        /// </summary>
        /// <param name="newValue">The new value to set for the field.</param>
        /// <param name="newCaretIndex">The new index for placing the caret within the value.</param>
        private void ChangeValue(string newValue, int newCaretIndex)
        {
            if (value_ != newValue)
            {
                var oldValue = value_;
                var oldCaret = caret_index_;

                // **注意：此处添加 Undo/Redo 操作**
                var operation = new SearchOperation(this, oldValue, newValue, oldCaret, newCaretIndex);
                SearchOperationManager.ExecuteOperation(operation);

                OnValueChanged?.Invoke(oldValue, newValue);
            }
            else
            {
                // 如果文本没变，只更新光标位置
                caret_index_ = Mathf.Clamp(newCaretIndex, 0, value_.Length);
                UpdateCaretVisuals();
            }
        }

        private void OnFocusIn(FocusInEvent evt)
        {
            is_focused_ = true;
            AddToClassList(focusedUssClassName);
            caret_.style.visibility = Visibility.Visible;
            // 开始光标闪烁
            caret_blink_?.Pause();
            caret_blink_ = caret_.schedule.Execute(() =>
                caret_.style.visibility = caret_.style.visibility == Visibility.Visible
                    ? Visibility.Hidden
                    : Visibility.Visible
            ).Every(500);
            UpdateDisplay();
        }

        private void OnFocusOut(FocusOutEvent evt)
        {
            is_focused_ = false;
            RemoveFromClassList(focusedUssClassName);
            // 停止光标闪烁并隐藏
            caret_blink_?.Pause();
            caret_.style.visibility = Visibility.Hidden;
            UpdateDisplay();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            var handled = false;

            // 优先处理用户自定义的回车/ESC等事件
            if (evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter or KeyCode.Escape)
            {
                onKeyDown?.Invoke(evt);
                handled = true;
            }
            else
            {
                switch (evt.keyCode)
                {
                    case KeyCode.Backspace:
                        if (caret_index_ > 0 && value_.Length > 0)
                        {
                            var newText = value_.Remove(caret_index_ - 1, 1);
                            ChangeValue(newText, caret_index_ - 1);
                            handled = true;
                        }

                        break;

                    case KeyCode.Delete:
                        if (caret_index_ < value_.Length)
                        {
                            var newText = value_.Remove(caret_index_, 1);
                            ChangeValue(newText, caret_index_);
                            handled = true;
                        }

                        break;

                    case KeyCode.LeftArrow:
                        caret_index_ = Mathf.Max(0, caret_index_ - 1);
                        UpdateCaretVisuals();
                        handled = true;
                        break;

                    case KeyCode.RightArrow:
                        caret_index_ = Mathf.Min(value_.Length, caret_index_ + 1);
                        UpdateCaretVisuals();
                        handled = true;
                        break;

                    case KeyCode.Home:
                        caret_index_ = 0;
                        UpdateCaretVisuals();
                        handled = true;
                        break;

                    case KeyCode.End:
                        caret_index_ = value_.Length;
                        UpdateCaretVisuals();
                        handled = true;
                        break;
                }
            }

            // 处理字符输入
            if (!handled && evt.character != '\0' && !char.IsControl(evt.character))
            {
                var newText = value_.Insert(caret_index_, evt.character.ToString());
                ChangeValue(newText, caret_index_ + 1);
                handled = true;
            }

            if (handled)
            {
                evt.StopPropagation();
                evt.PreventDefault();
            }
        }

        private void UpdateDisplay()
        {
            text_display_.text = value_;
            placeholder_.style.display =
                string.IsNullOrEmpty(value_) && !is_focused_ ? DisplayStyle.Flex : DisplayStyle.None;
            UpdateCaretVisuals();
        }

        /// <summary>
        /// Updates the visual appearance of the caret within the `CustomSearchField`, ensuring its position aligns correctly
        /// with the current caret index and maintains proper visibility during focus.
        /// </summary>
        private void UpdateCaretVisuals()
        {
            if (is_focused_)
            {
                float caretX;
                if (string.IsNullOrEmpty(value_))
                {
                    // If the value is empty, the caret should be at the very beginning (x=0).
                    caretX = 0; 
                }
                else
                {
                    // Calculate the width of the text before the caret to position the caret.
                    caretX = text_display_.MeasureTextSize(value_.Substring(0, caret_index_), 
                        0, MeasureMode.Undefined,
                        0, MeasureMode.Undefined).x;
                }

                caret_.style.left = caretX;

                // Reset the blink, so the caret shows immediately after moving.
                caret_.style.visibility = Visibility.Visible;
                caret_blink_?.Resume();
            }
        }

        public new void Focus()
        {
            text_input_.Focus();
        }

        /// <summary>
        /// Sets the value of the CustomSearchField without triggering value change notifications.
        /// </summary>
        /// <param name="newValue">The new value to set for the field.</param>
        public void SetValueWithoutNotify(string newValue)
        {
            SetValueAndCaret(newValue,0);
        }

        /// <summary>
        /// Sets the value of the CustomSearchField and updates the caret position.
        /// </summary>
        /// <param name="newValue">The new value to set for the field.</param>
        /// <param name="newCaretIndex">The new index position for the caret within the field.</param>
        public void SetValueAndCaret(string newValue, int newCaretIndex)
        {
            value_ = newValue;
            caret_index_ = Mathf.Clamp(newCaretIndex, 0, value_.Length);
            UpdateDisplay();
        }

        /// <summary>
        /// Registers a callback to be invoked when the value changes in the CustomSearchField.
        /// </summary>
        /// <param name="callback">The callback action to invoke, which provides the old value and the new value of the
        /// field as parameters.</param>
        public void RegisterValueChangedCallback(Action<string, string> callback)
        {
            OnValueChanged += callback;
        }

        /// <summary>
        /// Adds a callback to handle key down events in the CustomSearchField.
        /// </summary>
        /// <param name="callback">The callback function to execute when a key down event occurs,
        /// providing details of the KeyDownEvent.</param>
        public void RegisterKeyDownCallback(Action<KeyDownEvent> callback)
        {
            onKeyDown += callback;
        }


        public void Dispose()
        {
            SetValueAndCaret("",0);
        }
    }
}