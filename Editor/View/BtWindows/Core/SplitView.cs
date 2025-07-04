using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.View.BtWindows.Core
{
    public class SplitView : TwoPaneSplitView
    {
        // 默认的分割位置
        private const float DEFAULT_SPLIT_POSITION = 400f;

        private float last_dimension_ = 0;
        
        public float FixedPaneInitialDimension
        {
            get=>fixedPaneInitialDimension;
            set=>fixedPaneInitialDimension=value;
        }
        public new class UxmlTraits : TwoPaneSplitView.UxmlTraits
        {
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                if (ve is SplitView splitView)
                {
                    // 确保最外层的一定是按照从左到右水平排布以避免在GUI当中不能调整
                    splitView.orientation = TwoPaneSplitViewOrientation.Horizontal;
                    // 确保设置初始化尺寸
                    splitView.fixedPaneInitialDimension = DEFAULT_SPLIT_POSITION;
                }
            }
        }

        public class uxml_factory : UxmlFactory<SplitView, UxmlTraits>
        {
        }

        public SplitView()
        {
            // 同Init逻辑一致，保证水平并保留一定空隙防止在GUI当中不能调整
            orientation = TwoPaneSplitViewOrientation.Horizontal;

            // 确保正确的布局属性
            style.flexDirection = FlexDirection.Row;
            style.width = new StyleLength(Length.Percent(100));
            style.height = new StyleLength(Length.Percent(100));

            pickingMode = PickingMode.Position;
            focusable = false;
            
            // 注册拖拽结束事件，用于保存分别位置
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<MouseUpEvent>(OnDragEnd);
        }

        private void OnDragEnd(MouseUpEvent evt)
        {
            if (Mathf.Abs(last_dimension_-fixedPaneIndex==0?fixedPane.resolvedStyle.width:fixedPane.resolvedStyle.height)>0.1f)
            {
                float currentDimension = fixedPaneIndex == 0 ? fixedPane.resolvedStyle.width : fixedPane.resolvedStyle.height;
                last_dimension_ = currentDimension;
            
                fixedPaneInitialDimension=currentDimension;
            }
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (last_dimension_==0)
            {
                last_dimension_ = fixedPaneInitialDimension;
            }
        }
    }
}