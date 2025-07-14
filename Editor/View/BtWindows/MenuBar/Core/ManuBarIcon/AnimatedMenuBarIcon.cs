using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace Editor.View.BtWindows.MenuBar.Core.ManuBarIcon
{
    public abstract class AnimatedMenuBarIcon : MenuBarIconBase
    {
        protected readonly AnimationConfigBase animation_config_;
        protected ValueAnimation<float> current_animation_;
        protected VisualElement animation_container_;
        protected bool is_animating_ = false;
        
        protected const string USS_CLASS_NAME = "animated-menu-bar-icon";
        protected const string USS_CLASS_NAME_CONTAINER = USS_CLASS_NAME+"-container";
        
        protected AnimatedMenuBarIcon(Texture2D icon, string tooltip,AnimationConfigBase animation_config=null) : base(icon, tooltip)
        {
            animation_config_ = animation_config ?? CreateDefaultAnimationConfig();
            SetupAnimationContainer();
            InitializeAnimation();
        }

        protected virtual void InitializeAnimation()
        {
        }

        protected abstract AnimationConfigBase CreateDefaultAnimationConfig();
        
        protected abstract void PlayEnterAnimation();
        
        protected virtual void PlayExitAnimation()
        {
            StopAnimation();
        }

        protected virtual void PlayClickAnimation()
        {
            PlayEnterAnimation();
        }

        protected virtual void StopAnimation()
        {
            current_animation_?.Stop();
            current_animation_ = null;
            is_animating_ = false;
            ResetAnimationState();
        }
        
        protected abstract void ResetAnimationState();

        private void SetupAnimationContainer()
        {
            // 移除原来的按钮
            Remove(icon_button_);
            
            animation_container_.AddToClassList(USS_CLASS_NAME_CONTAINER);
            
            // 重新添加按钮到动画容器中
            animation_container_.Add(icon_button_);
            Add(animation_container_);
        }

        protected override void OnMouseEnter(MouseEnterEvent evt)
        {
            base.OnMouseEnter(evt);

            if (animation_config_.PlayOnHover && !is_animating_)
            {
                is_animating_ = true;
                PlayEnterAnimation();
            }
        }

        protected override void OnMouseLeave(MouseLeaveEvent evt)
        {
            base.OnMouseLeave(evt);

            if (!animation_config_.Loop)
            {
                is_animating_ = false;
                PlayExitAnimation();
            }
        }
        
        protected virtual void OnIconClicked()
        {
            if (animation_config_.PlayOnClick)
            {
                is_animating_ = true;
                PlayClickAnimation();
            }
        }

        public void TriggerAnimation()
        {
            if (!is_animating_)
            {
                is_animating_ = true;
                PlayEnterAnimation();
            }
        }
        
        public void StopAnimationManually()
        {
            StopAnimation();
        }
    }
}
