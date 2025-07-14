using System;
using System.Collections.Generic;
using ExTools.Utillties;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.View.BTWindows.BtTreeView.NodeView.NodeViewEdge
{
    public class EdgeFlowIndicator : IEdgeEffect
    {
        #region 内置变量

        private readonly Image flow_image_;

        private bool is_enabled_ = false;

        private float flow_size_ = 12.0f;

        private EdgeState current_state_ = EdgeState.kNormal;

        private bool is_reverse_ = false;

        #endregion

        #region 滚动相关变量

        private float total_edge_length_;
        
        private float passed_edge_length_;
        
        private int flow_phase_index_;
        
        private double flow_phase_start_time_;

        private double flow_phase_duration_;
        
        private float current_phase_length_;
        
        private Vector2[] current_control_points_;

        #endregion

        #region 渐变颜色配置

        private static readonly Dictionary<EdgeState, Color[]> GradientColors = new Dictionary<EdgeState, Color[]>
        {
            { EdgeState.kNormal, new[] { Color.white, Color.gray } },
            { EdgeState.kSuccess, new[] { new Color(0.5f, 1.0f, 0.5f), Color.white, new Color(0.0f, 0.6f, 0.0f) } },
            { EdgeState.kFailure, new[] { new Color(1.0f, 0.5f, 0.5f), new Color(0.8f, 0.0f, 0.0f) } },
            { EdgeState.kRunning, new[] { new Color(0.5f, 0.5f, 1.0f), new Color(0.5f, 0.0f, 1.0f) } }
        };

        #endregion

        public float FlowSize
        {
            get => flow_size_;
            set
            {
                flow_size_ = value;
                flow_image_.style.width = new Length(flow_size_, LengthUnit.Pixel);
                flow_image_.style.height = new Length(flow_size_, LengthUnit.Pixel);
                flow_image_.style.borderTopLeftRadius = new Length(flow_size_ / 2, LengthUnit.Pixel);
                flow_image_.style.borderTopRightRadius = new Length(flow_size_ / 2, LengthUnit.Pixel);
                flow_image_.style.borderBottomLeftRadius = new Length(flow_size_ / 2, LengthUnit.Pixel);
                flow_image_.style.borderBottomRightRadius = new Length(flow_size_ / 2, LengthUnit.Pixel);
            }
        }
        
        /// <summary>
        /// 获取或设置流动速度
        /// </summary>
        public float FlowSpeed { get; set; } = 150f;

        public EdgeFlowIndicator()
        {
            flow_image_ = new Image
            {
                name = "FlowImage",
                style =
                {
                    width = new Length(flow_size_, LengthUnit.Pixel),
                    height = new Length(flow_size_, LengthUnit.Pixel),
                    borderTopLeftRadius = new Length(flow_size_ / 2, LengthUnit.Pixel),
                    borderTopRightRadius = new Length(flow_size_ / 2, LengthUnit.Pixel),
                    borderBottomLeftRadius = new Length(flow_size_ / 2, LengthUnit.Pixel),
                    borderBottomRightRadius = new Length(flow_size_ / 2, LengthUnit.Pixel),
                    position = Position.Absolute
                }
            };
            
            // 初始隐藏状态
            flow_image_.style.display=DisplayStyle.None;
        }

        public bool Enabled
        {
            get => is_enabled_;
            set
            {
                if (is_enabled_ == value)
                {
                    return;
                }
                
                is_enabled_ = value;
                OnEnabledChanged?.Invoke(this);
                flow_image_.style.display=is_enabled_?DisplayStyle.Flex:DisplayStyle.None;
            }
        }
        
        public void UpdateState(EdgeState state)
        {
            if (current_state_!=state)
            {
                current_state_ = state;
                Reset();
            }
        }

        public void Update(Vector2[] control_points, float delta_time)
        {
            if (!is_enabled_ || control_points == null || control_points.Length < 2)
                return;
            
            // 保存控制点引用
            current_control_points_ = control_points;
        
            // 更新位置
            var pos_progress = (EditorApplication.timeSinceStartup - flow_phase_start_time_) / flow_phase_duration_;
            var flow_start_pointer = control_points[flow_phase_index_];
            var flow_end_point = control_points[flow_phase_index_ + 1];
            var flow_pos = Vector2.Lerp(flow_start_pointer, flow_end_point, (float)pos_progress);
            flow_image_.transform.position = flow_pos - Vector2.one * flow_size_ / 2;
        
            // 计算整体进度（用于颜色渐变）
            float progress = (passed_edge_length_ + current_phase_length_ * (float)pos_progress) / total_edge_length_;
        
            // 更新颜色
            Color flow_color = GetGradientColor(progress);
            flow_image_.style.backgroundColor = flow_color;
        
            // 检查当前阶段是否完成
            if (pos_progress >= 0.9999f)
            {
                passed_edge_length_ += current_phase_length_;
                flow_phase_index_++;
            
                // 检查是否完成所有阶段
                if (flow_phase_index_ >= control_points.Length - 1)
                {
                    flow_phase_index_ = 0;
                    passed_edge_length_ = 0;
                
                    if (current_state_ == EdgeState.kSuccess)
                    {
                        is_reverse_ = !is_reverse_;
                    }
                }
            
                // 更新新阶段信息
                flow_phase_start_time_ = EditorApplication.timeSinceStartup;
                current_phase_length_ = Vector2.Distance(control_points[flow_phase_index_], 
                    control_points[flow_phase_index_ + 1]);
                flow_phase_duration_ = current_phase_length_ / FlowSpeed;
            }
        }

        public void Reset()
        {
            flow_phase_index_ = 0;
            passed_edge_length_ = 0;
            flow_phase_start_time_ = EditorApplication.timeSinceStartup;
            is_reverse_ = false;
        
            if (current_control_points_ is { Length: > 1 })
            {
                current_phase_length_ = Vector2.Distance(current_control_points_[flow_phase_index_], 
                    current_control_points_[flow_phase_index_ + 1]);
                flow_phase_duration_ = current_phase_length_ / FlowSpeed;
            
                // 计算总路径长度
                total_edge_length_ = 0;
                for (int i = 0; i < current_control_points_.Length - 1; i++)
                {
                    total_edge_length_ += Vector2.Distance(current_control_points_[i], current_control_points_[i + 1]);
                }
            }
        }

        public VisualElement Root => flow_image_;
        
        public event Action<IEdgeEffect> OnEnabledChanged;

        #region 私有方法

        private Color GetGradientColor(float progress)
        {
            if (!GradientColors.TryGetValue(current_state_, out Color[] colors))
            {
                return Color.white;
            }
        
            // 进度调整（考虑反转情况）
            float adjusted_progress = is_reverse_ ? 1 - progress : progress;
        
            switch (current_state_)
            {
                case EdgeState.kSuccess:
                    if (colors.Length >= 3)
                    {
                        if (adjusted_progress < 0.5f)
                        {
                            return Color.Lerp(colors[0], colors[1], adjusted_progress * 2);
                        }
                        else
                        {
                            return Color.Lerp(colors[1], colors[2], (adjusted_progress - 0.5f) * 2);
                        }
                    }
                    break;
                case EdgeState.kFailure:
                    // 失败状态：浅红 -> 深红
                    if (colors.Length >= 2)
                    {
                        return Color.Lerp(colors[0], colors[1], adjusted_progress);
                    }
                    break;
                
                case EdgeState.kRunning:
                default:
                    if (colors.Length >= 2)
                    {
                        return Color.Lerp(colors[0], colors[1], adjusted_progress);
                    }
                    break;
            }
            
            if (colors.Length>=2)
            {
                return Color.Lerp(colors[0],colors[1],adjusted_progress);
            }
        
            return colors[0];
        }

        #endregion
    }
}
