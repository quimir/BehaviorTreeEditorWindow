using System.Collections.Generic;
using UnityEditor;

namespace Editor.EditorToolExs.EditorAnimation
{
    public class EditorAnimationManager
    {
        private readonly List<IEditorAnimation> active_animations_ = new();
        private double last_update_time_;

        public EditorAnimationManager()
        {
            EditorApplication.update += OnEditorAnimationUpdate;
            last_update_time_ = EditorApplication.timeSinceStartup;
        }

        private void OnEditorAnimationUpdate()
        {
            if (active_animations_.Count==0)
            {
                return;
            }
            
            // 计算增量时间deltaTime
            double current_time = EditorApplication.timeSinceStartup;
            float delta_time = (float)(current_time - last_update_time_);
            last_update_time_ = current_time;
            
            // 从后向前遍历，方便在循环中移除已完成的动画
            for (int i = active_animations_.Count-1; i>=0; i--)
            {
                if (!active_animations_[i].Update(delta_time))
                {
                    active_animations_.RemoveAt(i);
                }
            }
        }

        public void StartAnimation(IEditorAnimation animation)
        {
            if (animation!=null)
            {
                if (active_animations_.Contains(animation)) return;
                active_animations_.Add(animation);
            }
        }
    }
}
