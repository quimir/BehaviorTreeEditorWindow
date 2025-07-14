using System;
using System.Collections.Generic;
using System.Reflection;
using BehaviorTree.Core;
using BehaviorTree.Nodes;
using Editor.View.BTWindows.BtTreeView;
using Editor.View.BtWindows.Core;
using Editor.View.BTWindows.NodeMenuProvider;
using ExTools;
using ExTools.Utillties;
using LogManager.Core;
using LogManager.LogManagerFactory;
using Script.Tool;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.View.BtWindows.NodeMenuProvider
{
    public class SearchWindowNodeMenuProviders :ScriptableObject,ISearchWindowProvider,INodeMenuProvider
    {
        #region 保存上下文

        private BehaviorTreeWindowsBase windows_;
        private BehaviorTreeView owner_tree_view_;

        #endregion

        private Port pending_port_;
        private Vector2 menu_position_;
        
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var entries = new List<SearchTreeEntry> { new SearchTreeGroupEntry(new GUIContent("Create Node")){level = 0} };
            entries = AddNodeType<BtComposite>(entries, "组合节点");
            entries = AddNodeType<BtPrecondition>(entries, "条件节点");
            entries = AddNodeType<BtActionNode>(entries, "行为节点");
            
            return entries;
        }
        
        /// <summary>
        /// 通过反射机制，添加节点类型
        /// </summary>
        /// <param name="entries">节点列表</param>
        /// <param name="path_name">基类节点的名称</param>
        /// <typeparam name="T">基类节点类型，其会通过反射机制来获取所有基类当中所有的子类</typeparam>
        /// <returns>添加完节点类型之后的节点列表</returns>
        private static List<SearchTreeEntry> AddNodeType<T>(List<SearchTreeEntry> entries, string path_name)
        {
            if (entries == null)
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(new LogSpaceNode("BehaviourTreeWindows").AddChild("SearchWindowNodeMenuProvider"), new LogEntry(LogLevel.kError, "节点未初始化，请初始化之后再进行操作"));
                return null;
            }
 
            entries.Add(new SearchTreeGroupEntry(new GUIContent(path_name)) { level = 1 });

             // 使用一个字典来记录已经添加的分组，key为foldout_group_的名称，由于生成逻辑的问题这里不使用静态进行存储。
             var add_groups = new Dictionary<string, bool>();
            
             var root_node_types = ExTool.Instance.GetDerivedClasses(typeof(T));
            
             foreach (var root_type in root_node_types)
             {
                 var is_group = false;
                 if (root_type.GetCustomAttribute(typeof(NodeFoldoutGroup)) is NodeFoldoutGroup node_foldout_group)
                     if (!string.IsNullOrWhiteSpace(node_foldout_group.foldout_group_))
                     {
                         var foldout_name = node_foldout_group.foldout_group_;
            
                         // 检查是否已经添加过该分组
                         if (!add_groups.ContainsKey(foldout_name))
                         {
                             entries.Add(new SearchTreeGroupEntry(new GUIContent(foldout_name)) { level = 2 });
                             add_groups.Add(foldout_name, true);
                         }
            
                         is_group = true;
                     }
            
                 var menu_name_cn = root_type.Name;
                 var menu_name_en = root_type.Name;
            
                 if (root_type.GetCustomAttribute(typeof(NodeLabelAttribute)) is NodeLabelAttribute node_label)
                     if (!string.IsNullOrWhiteSpace(node_label.menu_name_))
                         menu_name_cn = node_label.menu_name_;
            
            
                 var entry = new SearchTreeEntry(new GUIContent
                 {
                     text = $"{menu_name_cn} ({menu_name_en})",
                     tooltip = $"{menu_name_en}"
                 })
                 {
                     level = is_group ? 3 : 2,
                     userData = root_type
                 };
            
                 entries.Add(entry);
             }
            
             return entries;
        }

        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            if (SearchTreeEntry.userData is not Type node_type) return false;
            
            // 使用存储的实例进行坐标转换，不再依赖静态变量
            var window_root = windows_.rootVisualElement;
            var window_mouse_position=window_root.ChangeCoordinatesTo(window_root.parent,context.screenMousePosition-windows_.position.position);
            var graph_mouse_position=owner_tree_view_.contentViewContainer.WorldToLocal(window_mouse_position);
            
            OnNodeSelected?.Invoke(node_type, graph_mouse_position, pending_port_);
            return true;
        }

        public void ShowMenu(Vector2 menu_position, Port pending_port = null)
        {
            menu_position_ = menu_position;
            pending_port_ = pending_port;
            
            // 使用存储的实例来获取上下文
            var window_root = windows_.rootVisualElement;
            var screen_position = window_root.LocalToWorld(menu_position);
            
            SearchWindow.Open(new SearchWindowContext(screen_position), this);
        }

        public void Initialize(BehaviorTreeWindowsBase windows, BehaviorTreeView owner_tree_view,
            Action<Type, Vector2, Port> on_node_selected)
        {
            windows_ = windows;
            owner_tree_view_ = owner_tree_view;
            OnNodeSelected += on_node_selected;
        }

        public event Action<Type, Vector2, Port> OnNodeSelected;
    }
}
