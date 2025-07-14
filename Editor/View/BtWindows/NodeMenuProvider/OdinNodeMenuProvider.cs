using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BehaviorTree.Nodes;
using ExTools;
using ExTools.NodeFoldout;
using Script.Tool;
using Script.Tool.NodeFoldout;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace Editor.View.BTWindows.NodeMenuProvider
{
    public class NodeMenuItem
    {
        public Type NodeType { get; }
        public string DisplayName { get; }

        public NodeMenuItem(Type nodeType, string displayName)
        {
            NodeType = nodeType;
            DisplayName = displayName;
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    public class NodeHierarchyComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (x == y) return 0;

            if (string.IsNullOrEmpty(x)) return -1;

            if (string.IsNullOrEmpty(y)) return 1;

            return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
        }
    }

    public class NodeDisplayNameComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (x == y) return 0;

            if (string.IsNullOrEmpty(x)) return -1;

            if (string.IsNullOrEmpty(y)) return 1;

            // 特殊字符优先，然后数字，最后字母
            var x_category = GetSortCategory(x[0]);
            var y_category = GetSortCategory(y[0]);

            if (x_category != y_category) return x_category.CompareTo(y_category);

            return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
        }

        private int GetSortCategory(char c)
        {
            if (char.IsLetter(c)) return 2;

            if (char.IsDigit(c)) return 1;

            return 0;
        }
    }

    public class OdinNodeMenuProvider
    {
        #region 私有字段

        // 节点类型集合
        private readonly List<Type> node_types_ = new();

        // 搜索相关
        private readonly Dictionary<Type, List<string>> node_search_terms_ = new();

        #endregion

        #region 构造函数

        public OdinNodeMenuProvider()
        {
            Initialize();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 构建并返回菜单树
        /// </summary>
        public OdinMenuTree BuildMenuTree()
        {
            // 收集最新的节点类型
            CollectNodeTypes();
            // 构建菜单树
            return CreateMenuTree();
        }

        /// <summary>
        /// 获取节点的搜索词列表
        /// </summary>
        public List<string> GetNodeSearchTerms(Type node_type)
        {
            return node_search_terms_.GetValueOrDefault(node_type, new List<string>());
        }

        /// <summary>
        /// 获取所有节点类型
        /// </summary>
        public IReadOnlyList<Type> GetAllNodeTypes()
        {
            return node_types_.AsReadOnly();
        }

        /// <summary>
        /// 刷新节点数据
        /// </summary>
        public void RefreshNodeData()
        {
            CollectNodeTypes();
        }

        #endregion

        private void Initialize()
        {
            // 确保本地化管理器已初始化
            if (BtNodeLocalizationManager.Instance != null)
                BtNodeLocalizationManager.Instance.CollectAllNodeLocalization();

            // 收集节点类型
            CollectNodeTypes();
        }

        private void CollectNodeTypes()
        {
            node_types_.Clear();
            node_search_terms_.Clear();

            try
            {
                // 收集BtComposite、BtPrecondition、BtActionNode类型
                var composite_types = ExTool.Instance.GetDerivedClasses(typeof(BtComposite));
                var precondition_types = ExTool.Instance.GetDerivedClasses(typeof(BtPrecondition));
                var action_types = ExTool.Instance.GetDerivedClasses(typeof(BtActionNode));

                node_types_.AddRange(composite_types);
                node_types_.AddRange(precondition_types);
                node_types_.AddRange(action_types);

                // 为每个节点类型构建搜索词
                foreach (var type in node_types_)
                {
                    var search_terms = new List<string> { type.Name };

                    // 如果本地化管理器可用，添加本地化搜索词
                    if (BtNodeLocalizationManager.Instance != null)
                    {
                        var localized_terms = BtNodeLocalizationManager.Instance.GetNodeSearchTerms(type);
                        if (localized_terms != null) search_terms.AddRange(localized_terms);
                    }

                    node_search_terms_[type] = search_terms.Distinct().ToList();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"收集节点类型时出错: {ex.Message}");
            }
        }

        private OdinMenuTree CreateMenuTree()
        {
            try
            {
                var menu_tree = new OdinMenuTree(false)
                {
                    Config =
                    {
                        DrawSearchToolbar = true,
                        AutoHandleKeyboardNavigation = true,
                        // AutoFocusSearchBar = false, // 避免自动聚焦搜索栏导致的选择问题
                        // UseCachedExpandedStates = false // 不使用缓存的展开状态
                    }
                };

                // 设置自定义搜索函数
                menu_tree.Config.SearchFunction = (menu_item) => CustomSearchFunction(menu_item, menu_tree);

                // 创建层级结构的菜单（类似蓝图的层级展示）
                BuildHierarchicalMenu(menu_tree);

                return menu_tree;
            }
            catch (Exception ex)
            {
                Debug.LogError($"构建菜单树时出错: {ex.Message}");
                // 返回一个基本的菜单树作为后备
                return new OdinMenuTree(false);
            }
        }

        private void BuildHierarchicalMenu(OdinMenuTree menu_tree)
        {
            // 按照层级结构分组节点
            var grouped_nodes = GroupNodesByHierarchy();

            // 按层级顺序添加节点
            foreach (var group in grouped_nodes.OrderBy(g => g.Key, new NodeHierarchyComparer()))
            {
                // 为每个分组创建子菜单
                var group_path = group.Key;
                var nodes_in_group = group.Value.OrderBy(GetNodeDisplayName, new NodeDisplayNameComparer()).ToList();

                foreach (var node_type in nodes_in_group)
                {
                    var display_name = GetNodeDisplayName(node_type);
                    var menu_path = string.IsNullOrEmpty(group_path) ? display_name : $"{group_path}/{display_name}";

                    // 创建菜单项
                    var menu_item = menu_tree.Add(menu_path, new NodeMenuItem(node_type, display_name));

                    // 为菜单项添加图标
                    SetMenuItemIcon(menu_item, node_type);
                }
            }

            // 默认展开第一级菜单项
            foreach (var root_item in menu_tree.MenuItems)
                if (root_item.ChildMenuItems.Count > 0)
                {
                    root_item.Toggled = true;

                    // 也展开第二级，以获得更好的导航体验
                    foreach (var child_item in root_item.ChildMenuItems.Take(3)) // 只展开前3个子项避免过度展开
                        if (child_item.ChildMenuItems.Count > 0)
                            child_item.Toggled = true;
                }
        }

        private Dictionary<string, List<Type>> GroupNodesByHierarchy()
        {
            var grouped = new Dictionary<string, List<Type>>();

            foreach (var type in node_types_)
            {
                var hierarchy_path = GetNodeHierarchyPath(type);
                if (!grouped.ContainsKey(hierarchy_path))
                    grouped[hierarchy_path] = new List<Type>();

                grouped[hierarchy_path].Add(type);
            }

            return grouped;
        }

        private string GetNodeHierarchyPath(Type node_type)
        {
            try
            {
                // 获取BtNodeFoldoutAttribute
                var foldout_attr = node_type.GetCustomAttribute<BtNodeFoldoutAttribute>();
                var path_segments = new List<string> { "行为树" }; // 根层级

                if (foldout_attr?.PathSegments != null && foldout_attr.PathSegments.Length > 0)
                {
                    // 限制最多5层，如果超过则取前4层加上根层级
                    var segments_to_add = foldout_attr.PathSegments.Take(Math.Min(4, foldout_attr.PathSegments.Length));
                    path_segments.AddRange(segments_to_add);
                }
                else
                {
                    // 如果没有指定路径，根据类型自动分类
                    if (typeof(BtComposite).IsAssignableFrom(node_type))
                        path_segments.Add("组合节点");
                    else if (typeof(BtPrecondition).IsAssignableFrom(node_type))
                        path_segments.Add("条件节点");
                    else if (typeof(BtActionNode).IsAssignableFrom(node_type))
                        path_segments.Add("行为节点");
                    else
                        path_segments.Add("其他节点");
                }

                return string.Join("/", path_segments);
            }
            catch
            {
                return "行为树/其他节点"; // 默认路径
            }
        }

        private string GetNodeDisplayName(Type node_type)
        {
            try
            {
                if (BtNodeLocalizationManager.Instance != null)
                    return BtNodeLocalizationManager.Instance.GetNodeDisplayName(node_type);

                // 回退到旧的NodeLabelAttribute方式
                if (node_type.GetCustomAttribute(typeof(NodeLabelAttribute)) is NodeLabelAttribute node_label)
                    if (!string.IsNullOrWhiteSpace(node_label.menu_name_))
                        return node_label.menu_name_;

                return node_type.Name;
            }
            catch
            {
                return node_type.Name;
            }
        }

        private bool CustomSearchFunction(OdinMenuItem menu_item, OdinMenuTree menu_tree)
        {
            try
            {
                // 如果没有搜索词，显示所有项目
                if (string.IsNullOrEmpty(menu_tree.Config.SearchTerm)) return true;

                var search_term = menu_tree.Config.SearchTerm.Trim();
                if (string.IsNullOrEmpty(search_term)) return true;

                var node_item = menu_item.Value as NodeMenuItem;
                if (node_item == null)
                    // 对于文件夹节点，检查名称
                    return menu_item.Name.IndexOf(search_term, StringComparison.OrdinalIgnoreCase) >= 0;

                var search_terms = node_search_terms_.GetValueOrDefault(node_item.NodeType, new List<string>());

                // 检查是否有任何搜索词匹配
                return search_terms.Any(term =>
                           term.IndexOf(search_term, StringComparison.OrdinalIgnoreCase) >= 0) ||
                       node_item.DisplayName.IndexOf(search_term, StringComparison.OrdinalIgnoreCase) >= 0 ||
                       menu_item.Name.IndexOf(search_term, StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return true; // 出错时默认显示
            }
        }

        private void SetMenuItemIcon(IEnumerable<OdinMenuItem> menu_item, Type node_type)
        {
            try
            {
                var odinMenuItems = menu_item as OdinMenuItem[] ?? menu_item.ToArray();
                foreach (var item in odinMenuItems)
                    if (typeof(BtComposite).IsAssignableFrom(node_type))
                        item.Icon = EditorIcons.Tree.Raw;
                    else if (typeof(BtPrecondition).IsAssignableFrom(node_type))
                        item.Icon = EditorIcons.Bell.Raw;
                    else if (typeof(BtActionNode).IsAssignableFrom(node_type)) item.Icon = EditorIcons.Play.Raw;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"设置菜单图标时出错: {ex.Message}");
            }
        }
    }
}