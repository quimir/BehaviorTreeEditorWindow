using Editor.EditorToolExs.Operation;
using Editor.EditorToolExs.Operation.Core;
using Sirenix.OdinInspector.Editor;

namespace Editor.View.BtWindows.InspectorUI.Operations
{
    public class ChangePropertyValueOperation : IOperation
    {
        private readonly PropertyTree property_tree_;
        private readonly string property_path_;
        private readonly object old_value_;
        private readonly object new_value_;
        
        public ChangePropertyValueOperation(PropertyTree property_tree, string property_path, object old_value, object new_value)
        {
            property_tree_ = property_tree;
            property_path_ = property_path;
            old_value_ = old_value;
            new_value_ = new_value;
        }
        public void Execute()
        {
        }

        public void Undo()
        {
            var property = property_tree_.GetPropertyAtPath(property_path_);
            if (property!=null)
            {
                property.ValueEntry.WeakSmartValue = old_value_;
                property_tree_.ApplyChanges();
            }
        }

        public void Redo()
        {
            var property=property_tree_.GetPropertyAtPath(property_path_);
            if (property!=null)
            {
                property.ValueEntry.WeakSmartValue = new_value_;
                property_tree_.ApplyChanges();
            }
        }

        public bool RequireSave => true;
    }
}
