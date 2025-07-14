using Editor.EditorToolExs.Operation.Core;
using Editor.View.BtWindows.SearchBar.Storage;

namespace Editor.View.BtWindows.SearchBar.Operation
{
    public class SearchOperation :IOperation
    {
        private readonly CustomSearchField target_field_;
        private readonly string old_value_;
        private readonly string new_value_;
        private readonly int old_caret_index_;
        private readonly int new_caret_index_;
        
        public SearchOperation(CustomSearchField targetField, string oldValue, string newValue, int oldCaretIndex, int newCaretIndex)
        {
            target_field_ = targetField;
            old_value_ = oldValue;
            new_value_= newValue;
            old_caret_index_ = oldCaretIndex;
            new_caret_index_ = newCaretIndex;
        }
        public void Execute() => target_field_.SetValueAndCaret(new_value_,new_caret_index_);
    
        // 撤销
        public void Undo() => target_field_.SetValueAndCaret(old_value_,old_caret_index_);

        // 重做
        public void Redo() => Execute();

        public bool RequireSave => true;
    }
}
