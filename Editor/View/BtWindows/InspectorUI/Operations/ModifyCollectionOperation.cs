using System;
using System.Collections;
using System.Reflection;
using Editor.EditorToolEx.Operation;

namespace Editor.View.BTWindows.InspectorUI.Operations
{
    public class ModifyCollectionOperation : IOperation
    {
        private readonly object target_;// 目标节点对象
        private readonly MemberInfo collection_member_info_;
        private readonly object item_to_modify_;
        private readonly int index_;
        private readonly bool was_added_;

        private readonly Action<object> on_execute_or_redo_callback_;
        private readonly Action<object> on_undo_callback_;


        public ModifyCollectionOperation(object target, MemberInfo collectionMemberInfo, object itemToModify, int index, bool wasAdded, Action<object> onExecuteOrRedoCallback, Action<object> onUndoCallback)
        {
            target_ = target;
            collection_member_info_ = collectionMemberInfo;
            item_to_modify_ = itemToModify;
            index_ = index;
            was_added_ = wasAdded;
            on_execute_or_redo_callback_ = onExecuteOrRedoCallback;
            on_undo_callback_ = onUndoCallback;
        }

        private IList GetList()
        {
            return collection_member_info_.MemberType == MemberTypes.Field
                ? (IList)((FieldInfo)collection_member_info_).GetValue(target_)
                : (IList)((PropertyInfo)collection_member_info_).GetValue(target_);
        }

        public void Execute()
        {
            var list = GetList();
            if (was_added_)
            {
                if (!list.Contains(item_to_modify_))
                {
                    list.Insert(index_,item_to_modify_);
                }
            }
            else
            {
                if (list.Contains(item_to_modify_))
                {
                    list.Remove(item_to_modify_);
                }
            }
            
            // 执行副作用回调
            on_execute_or_redo_callback_?.Invoke(target_);
        }

        public void Undo()
        {
            var list = GetList();
            if (was_added_)
            {
                if (list.Contains(item_to_modify_))
                {
                    list.Remove(item_to_modify_);
                }
            }
            else
            {
                if (!list.Contains(item_to_modify_))
                {
                    list.Insert(index_,item_to_modify_);
                }
            }
            
            // 执行撤销的副作用回调
            on_undo_callback_?.Invoke(target_);
        }

        public void Redo()
        {
            Execute();
        }

        public bool RequireSave => true;
    }
}
