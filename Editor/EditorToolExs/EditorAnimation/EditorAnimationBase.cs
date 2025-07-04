using System;

namespace Editor.EditorToolExs.EditorAnimation
{
    public abstract class EditorAnimationBase : IEditorAnimation
    {
        protected float duration_;
        protected float elapsed_time_;

        protected EditorAnimationBase(float duration)
        {
            duration_ = duration;
            elapsed_time_ = 0;
        }

        public bool Update(float delta_time)
        {
            elapsed_time_ += delta_time;
            if (elapsed_time_ >= duration_)
            {
                ApplyAnimation(1.0f);
                OnCompleted?.Invoke();
                // 动画结束
                return false;
            }
            
            // 计算当前进度（0.0 to 1.0）
            float progress = elapsed_time_ / duration_;
            ApplyAnimation(progress);
            return true;
        }

        public event Action OnCompleted;
        
        protected abstract void ApplyAnimation(float t);
    }
}
