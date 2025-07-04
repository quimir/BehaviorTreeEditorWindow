using System;
using System.Reflection;
using Editor.EditorToolEx.Operation;

namespace Editor.View.BTWindows.InspectorUI.Operations
{
    public class ChangePropertyOperation : IOperation
    {
        private readonly object target_;
        private readonly PropertyInfo property_info_;
        private readonly object old_value_;
        private readonly object new_value_;

        public ChangePropertyOperation(object target, string property_name, object old_value, object new_value)
        {
            target_ = target;
            // 通过反射获取属性信息，确保属性存在
            property_info_ = target.GetType().GetProperty(property_name,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

            if (property_info_==null)
            {
                throw new ArgumentException($"Property '{property_name}' not found on type '{target.GetType().Name}'.");
            }

            old_value_ = old_value;
            new_value_ = new_value;
        }
        public void Execute()
        {
            // 执行/重做：设置为新值
            property_info_.SetValue(target_,new_value_);
        }

        public void Undo()
        {
            // 撤销：恢复为旧值
            property_info_.SetValue(target_,old_value_);
        }

        public void Redo()
        {
            Execute();
        }

        public bool RequireSave => true;
    }
}
