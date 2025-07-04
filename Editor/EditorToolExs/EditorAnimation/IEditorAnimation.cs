using System;

namespace Editor.EditorToolExs.EditorAnimation
{
    public interface IEditorAnimation
    {
        bool Update(float delta_time);

        event Action OnCompleted;
    }
}
