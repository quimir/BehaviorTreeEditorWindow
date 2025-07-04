using System.Collections.Generic;
using System.Linq;
using ExTools.Utillties;
using Script.Utillties;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Editor.View.BTWindows.BtTreeView.NodeView.NodeViewEdge
{
    /// <summary>
    /// Represents a customizable edge used in the NodeView graph system to visually connect ports.
    /// </summary>
    /// <remarks>
    /// This class extends the UnityEditor.Experimental.GraphView.Edge and includes additional visual and functional
    /// components such as animations, gradients, and state management. FlowingEdge is designed to improve the
    /// user experience for visualizing connections between graph nodes in an editor environment.
    /// </remarks>
    public class FlowingEdge : Edge
    {
        private bool is_enable_flow_ = false;
        private EdgeState current_state_ = EdgeState.kNormal;

        /// <summary>
        /// Stores and manages all active edge effects applied to the current edge.
        /// </summary>
        /// <remarks>
        /// This collection maintains all instances of <see cref="IEdgeEffect"/> applied to the edge.
        /// It allows for dynamic addition and removal of effects and ensures proper lifecycle management
        /// (such as enabling, disabling, or updating effects) based on the state of the edge.
        /// </remarks>
        private readonly HashSet<IEdgeEffect> all_edge_effects_ = new HashSet<IEdgeEffect>();

        // 记录上次的时间点用于计算增量时间
        private double last_update_time_;

        public FlowingEdge()
        {
            // 注册回调
            edgeControl.RegisterCallback<GeometryChangedEvent>(OnEdgeControlGeometryChanged);

            // 初始化时间
            last_update_time_ = EditorApplication.timeSinceStartup;
        }

        /// <summary>
        /// Adds a new edge effect to the FlowingEdge, allowing for additional visual or behavioral customization.
        /// </summary>
        /// <param name="effect">The edge effect to be added to the edge. Must implement the IEdgeEffect interface.</param>
        /// <returns>
        /// Returns true if the edge effect was successfully added; otherwise, false if the effect was already present.
        /// </returns>
        public bool AddEdgeEffect(IEdgeEffect effect)
        {
            if (!all_edge_effects_.Add(effect)) return false;
            
            // 订阅事件
            effect.OnEnabledChanged+=HandleEffectEnabledChanged;
            Add(effect.Root);
            
            // 重新评估整体状态
            UpdateFlowEnableState();
            return true;
        }

        /// <summary>
        /// 根据所有效果的当前状态，更新总的 EnableFlow 状态。
        /// </summary>
        private void UpdateFlowEnableState()
        {
            bool should_be_enabled = all_edge_effects_.Any(e => e.Enabled);

            if (is_enable_flow_!=should_be_enabled)
            {
                is_enable_flow_ = should_be_enabled;
            }
        }

        private void HandleEffectEnabledChanged(IEdgeEffect obj)
        {
            UpdateFlowEnableState();
        }

        /// <summary>
        /// Removes an existing edge effect from the FlowingEdge.
        /// </summary>
        /// <param name="effect">The edge effect to be removed. Must implement the IEdgeEffect interface.</param>
        /// <returns>
        /// Returns true if the edge effect was successfully removed; otherwise, false if the effect was not found.
        /// </returns>
        public bool RemoveEdgeEffect(IEdgeEffect effect)
        {
            if (!all_edge_effects_.Remove(effect)) return false;
            
            effect.OnEnabledChanged-=HandleEffectEnabledChanged;
            Remove(effect.Root);
            
            UpdateFlowEnableState();
            return true;
        }

        /// <summary>
        /// 获取或设置流动效果是否启用
        /// </summary>
        public bool EnableFlow
        {
            get => is_enable_flow_;
            set
            {
                if (is_enable_flow_ == value)
                    return;

                // setter 作为一个“主控开关”，批量设置所有效果
                // 这会触发每个效果的 OnEnabledChanged 事件，
                // 最终通过 HandleEffectEnabledChanged -> UpdateFlowEnableState 
                // 来正确地更新 is_enable_flow_ 的值。
                is_enable_flow_ = value;
                foreach (var effect in all_edge_effects_)
                {
                    effect.Enabled = value;
                }
            }
        }

        /// <summary>
        /// 获取或设置当前边缘状态
        /// </summary>
        public EdgeState CurrentState
        {
            get => current_state_;
            set
            {
                if (current_state_ != value)
                {
                    current_state_ = value;

                    foreach (var effect in all_edge_effects_)
                    {
                        effect.UpdateState(current_state_);
                    }
                }
            }
        }

        /// <summary>
        /// Updates the visual appearance and behavior of the edge control, including the execution of associated edge effects.
        /// </summary>
        /// <returns>
        /// Returns true if the edge control was successfully updated; otherwise, false.
        /// </returns>
        public override bool UpdateEdgeControl()
        {
            if (!base.UpdateEdgeControl()) return false;
            
            // 计算时间增量
            var current_time = EditorApplication.timeSinceStartup;
            var delta_time = (float)(current_time - last_update_time_);
            last_update_time_ = current_time;

            // 更新效果组件
            if (is_enable_flow_ && edgeControl.controlPoints is { Length: > 1 })
            {
                foreach (var effect in all_edge_effects_)
                {
                    effect.Update(edgeControl.controlPoints, delta_time);
                }
            }

            return true;
        }

        /// <summary>
        /// 处理边缘几何变化事件
        /// </summary>
        private void OnEdgeControlGeometryChanged(GeometryChangedEvent evt)
        {
            if (edgeControl.controlPoints == null || edgeControl.controlPoints.Length < 2)
                return;

            foreach (var effect in all_edge_effects_)
            {
                effect.Reset();
                effect.Update(edgeControl.controlPoints, 0);
            }
        }
        
        /// <summary>
        /// Retrieves a specific edge effect of type T from the collection.
        /// </summary>
        /// <typeparam name="T">The type of the edge effect to retrieve.</typeparam>
        /// <returns>The instance of the effect if found; otherwise, null.</returns>
        public T GetEdgeEffect<T>() where T : class, IEdgeEffect
        {
            foreach (var effect in all_edge_effects_)
            {
                if (effect is T t)
                    return t;
            }

            return null;
        }
    }
}