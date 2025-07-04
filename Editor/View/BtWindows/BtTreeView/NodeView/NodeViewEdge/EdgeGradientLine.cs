using System;
using System.Collections.Generic;
using ExTools.Utillties;
using Script.Utillties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.View.BTWindows.BtTreeView.NodeView.NodeViewEdge
{
    /// <summary>
    /// 简化版边缘渐变线条效果
    /// </summary>
    public class EdgeGradientLine : IEdgeEffect
    {
        private readonly VisualElement line_container_;
        private bool is_enabled_ = false;
        private EdgeState current_state_ = EdgeState.kNormal;
        private float line_width_ = 3.0f;
        private float animation_speed_ = 0.5f;
        private float animation_time_ = 0f;

        // 渐变颜色配置
        private static readonly Dictionary<EdgeState, Color[]> GradientColors = new()
        {
            { EdgeState.kNormal, new[] { Color.white, Color.gray } },
            { EdgeState.kSuccess, new[] { new Color(0.5f, 1.0f, 0.5f), Color.white, new Color(0.0f, 0.6f, 0.0f) } },
            { EdgeState.kFailure, new[] { new Color(1.0f, 0.5f, 0.5f), new Color(0.8f, 0.0f, 0.0f) } },
            { EdgeState.kRunning, new[] { new Color(0.5f, 0.5f, 1.0f), new Color(0.5f, 0.0f, 1.0f) } }
        };

        // 当前状态的颜色数组
        private Color[] current_colors_;

        public EdgeGradientLine()
        {
            line_container_ = new VisualElement
            {
                name = "LineContainer",
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    left = 0,
                    top = 0,
                    right = 0,
                    bottom = 0,
                }
            };

            // 初始隐藏
            line_container_.style.display = DisplayStyle.None;

            // 设置初始颜色
            if (GradientColors.TryGetValue(current_state_, out var colors)) current_colors_ = colors;
        }

        /// <summary>
        /// 获取或设置线条宽度
        /// </summary>
        public float LineWidth
        {
            get => line_width_;
            set => line_width_ = value;
        }

        /// <summary>
        /// 获取或设置渐变动画速度
        /// </summary>
        public float GradientAnimationSpeed
        {
            get => animation_speed_;
            set => animation_speed_ = value;
        }

        #region IEdgeEffect 接口实现

        public bool Enabled
        {
            get => is_enabled_;
            set
            {
                if (is_enabled_ == value) return;
                is_enabled_ = value;
                OnEnabledChanged?.Invoke(this);
                line_container_.style.display = is_enabled_ ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void UpdateState(EdgeState state)
        {
            if (current_state_ != state)
            {
                current_state_ = state;

                // 更新颜色数组
                if (GradientColors.TryGetValue(current_state_, out var colors)) current_colors_ = colors;
            }
        }

        public void Update(Vector2[] controlPoints, float deltaTime)
        {
            if (!is_enabled_ || controlPoints == null || controlPoints.Length < 2 || current_colors_ == null)
                return;

            // 更新线段几何形状
            UpdateLineSegments(controlPoints);

            // 更新动画时间
            animation_time_ += deltaTime * animation_speed_;
            if (animation_time_ > 1f) animation_time_ -= 1f;

            // 获取子线段的总长度
            var totalLength = 0f;
            for (var i = 0; i < controlPoints.Length - 1; i++)
                totalLength += Vector2.Distance(controlPoints[i], controlPoints[i + 1]);

            // 更新每个线段的颜色
            var currentLength = 0f;
            for (var i = 0; i < line_container_.childCount; i++)
            {
                var segment = line_container_.ElementAt(i);
                var segmentLength = Vector2.Distance(controlPoints[i], controlPoints[i + 1]);

                // 计算这个线段在总长度中的位置
                var startPos = currentLength / totalLength;
                var endPos = (currentLength + segmentLength) / totalLength;

                // 动画偏移
                startPos = (startPos + animation_time_) % 1f;
                endPos = (endPos + animation_time_) % 1f;

                // 计算颜色
                var segmentColor = GetGradientColor(startPos);
                segment.style.backgroundColor = segmentColor;

                currentLength += segmentLength;
            }
        }

        public void Reset()
        {
            animation_time_ = 0;
            line_container_.Clear();
        }

        public VisualElement Root => line_container_;
        public event Action<IEdgeEffect> OnEnabledChanged;

        #endregion

        #region 私有辅助方法

        private Color GetGradientColor(float position)
        {
            if (current_colors_ == null || current_colors_.Length == 0)
                return Color.white;

            if (current_colors_.Length == 1)
                return current_colors_[0];

            // 计算在哪两个颜色之间插值
            var scaledPos = position * (current_colors_.Length - 1);
            var index = Mathf.FloorToInt(scaledPos);
            var t = scaledPos - index;

            // 确保索引在有效范围内
            index = index % current_colors_.Length;
            var nextIndex = (index + 1) % current_colors_.Length;

            // 颜色插值
            return Color.Lerp(current_colors_[index], current_colors_[nextIndex], t);
        }

        private void UpdateLineSegments(Vector2[] controlPoints)
        {
            // 确保线段数量与控制点一致
            EnsureLineSegments(controlPoints.Length - 1);

            // 更新每个线段
            for (var i = 0; i < controlPoints.Length - 1; i++)
            {
                var startPoint = controlPoints[i];
                var endPoint = controlPoints[i + 1];
                var segment = line_container_.ElementAt(i);

                // 计算线段长度和角度
                var length = Vector2.Distance(startPoint, endPoint);
                var angle = Mathf.Atan2(endPoint.y - startPoint.y, endPoint.x - startPoint.x) * Mathf.Rad2Deg;

                // 线段中心点
                var center = (startPoint + endPoint) / 2;

                // 设置线段样式
                segment.style.width = length;
                segment.style.height = line_width_;
                segment.style.position = Position.Absolute;
                segment.style.left = center.x - length / 2;
                segment.style.top = center.y - line_width_ / 2;
                segment.style.rotate = new Rotate(new Angle(angle));

                // 圆角使线段更美观
                segment.style.borderTopLeftRadius = new Length(line_width_ / 2, LengthUnit.Pixel);
                segment.style.borderTopRightRadius = new Length(line_width_ / 2, LengthUnit.Pixel);
                segment.style.borderBottomLeftRadius = new Length(line_width_ / 2, LengthUnit.Pixel);
                segment.style.borderBottomRightRadius = new Length(line_width_ / 2, LengthUnit.Pixel);
            }
        }

        private void EnsureLineSegments(int count)
        {
            // 添加缺少的线段
            while (line_container_.childCount < count)
            {
                var segment = new VisualElement
                {
                    name = $"segment_{line_container_.childCount}",
                    pickingMode = PickingMode.Ignore,
                    style =
                    {
                        position = Position.Absolute,
                        borderTopLeftRadius = new Length(line_width_ / 2, LengthUnit.Pixel),
                        borderTopRightRadius = new Length(line_width_ / 2, LengthUnit.Pixel),
                        borderBottomLeftRadius = new Length(line_width_ / 2, LengthUnit.Pixel),
                        borderBottomRightRadius = new Length(line_width_ / 2, LengthUnit.Pixel)
                    }
                };

                line_container_.Add(segment);
            }

            // 移除多余的线段
            while (line_container_.childCount > count) line_container_.RemoveAt(line_container_.childCount - 1);
        }

        #endregion
    }
}