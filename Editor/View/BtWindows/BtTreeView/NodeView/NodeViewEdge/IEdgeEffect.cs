using System;
using ExTools.Utillties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.View.BTWindows.BtTreeView.NodeView.NodeViewEdge
{
    /// <summary>
    /// Represents an interface defining the behavior and visual effects of edge elements.
    /// </summary>
    public interface IEdgeEffect
    {
        /// <summary>
        /// Gets or sets a value indicating whether the edge effect is enabled.
        /// </summary>
        /// <remarks>
        /// Changing this property controls the visibility or activation of the edge effect
        /// in the implementing object. When set to true, the associated visuals or behaviors
        /// will be displayed or applied. When set to false, the associated visuals or behaviors
        /// will be hidden or disabled.
        /// </remarks>
        bool Enabled { get; set; }

        /// <summary>
        /// Updates the state of the edge by applying the specified edge state.
        /// </summary>
        /// <param name="state">The edge state to update to.</param>
        void UpdateState(EdgeState state);

        /// <summary>
        /// 更新视觉效果
        /// </summary>
        /// <param name="control_points">控制点数组</param>
        /// <param name="delta_time">时间增量</param>
        void Update(Vector2[] control_points, float delta_time);

        /// <summary>
        /// Resets the edge effect to its initial state, clearing animations, counters, or other stateful properties.
        /// </summary>
        void Reset();

        /// <summary>
        /// Gets the root visual element associated with the edge effect.
        /// </summary>
        /// <remarks>
        /// This property provides access to the primary <see cref="VisualElement"/> used for rendering or managing the
        /// visual representation of the edge effect.
        /// It serves as the base container for all visual components related to the effect.
        /// </remarks>
        VisualElement Root { get; }
        
        /// <summary>
        /// Occurs when the Enabled property changes.
        /// </summary>
        event Action<IEdgeEffect> OnEnabledChanged; 
    }
}
