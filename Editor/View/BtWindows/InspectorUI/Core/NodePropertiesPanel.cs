using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BehaviorTree.Nodes;
using Editor.EditorToolEx.Operation;
using Editor.View.BTWindows;
using Editor.View.BTWindows.BtTreeView;
using ExTools;
using ExTools.Utillties;
using LogManager.Core;
using LogManager.LogManagerFactory;
using Script.LogManager;
using Script.Utillties;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.View.BtWindows.InspectorUI.Core
{
    public class NodePropertiesPanel : BaseInspectorPanel
    {
        private PropertyTree property_tree_; // Odin property tree instance

        private IMGUIContainer odin_imgui_container_; // The container for Odin's IMGUI output

        private readonly BehaviorTreeView tree_view_;

        // 添加跟踪状态
        private BtNodeBase currentNode;

        private readonly OperationManager operation_manager_;
        private readonly Dictionary<string, object> values_before_change_ = new();
        private List<BtNodeBase> child_nodes_snapshot_ = new();

        private MemberInfo tracked_collection_member_;
        private IList collection_snapshot_;

        private readonly Dictionary<string, List<string>> node_child_guid_trackers_ = new();
        private bool needs_child_node_check_ = false;

        public NodePropertiesPanel(BehaviorTreeView tree_view, VisualElement container) : base(container)
        {
            tree_view_ = tree_view;
            operation_manager_ = new OperationManager();
        }

        private void FindTrackedCollectionMember()
        {
            tracked_collection_member_ = null;
            if (currentNode == null) return;

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            // 检查所有字段和属性
            var members = currentNode.GetType().GetMembers(flags).Where(m =>
                m.MemberType is MemberTypes.Field or MemberTypes.Property);

            foreach (var member in members)
                if (member.GetCustomAttribute<PanelDelegatedPropertyAttribute>()?.PanelType ==
                    PropertyPanelType.kChildNodes)
                {
                    // 防止第一次就查找到父类，因为父类有可能不是主要承载数据源
                    if (member.GetType().GetMember("ChildNodes", flags).FirstOrDefault() != null) continue;
                    tracked_collection_member_ = member;
                    return;
                }
        }

        private void InitializeChildNodeTracking(BtComposite node)
        {
            if (string.IsNullOrEmpty(node.Guild)) return;

            // 记录当前子节点状态
            var childGuids = node.ChildNodes?
                .Select(n => n?.Guild)
                .Where(g => !string.IsNullOrEmpty(g))
                .ToList() ?? new List<string>();

            node_child_guid_trackers_[node.Guild] = childGuids;
        }

        private void CheckChildNodeChanges(BtComposite node)
        {
            needs_child_node_check_ = false;

            if (string.IsNullOrEmpty(node.Guild)) return;

            // 获取当前子节点列表
            var current_child_guids = node.ChildNodes?
                .Select(n => n?.Guild)
                .Where(g => !string.IsNullOrEmpty(g))
                .ToList() ?? new List<string>();

            // 检查之前是否有记录
            if (node_child_guid_trackers_.TryGetValue(node.Guild, out var previousChildGuids))
            {
                // 找出被删除的节点
                var removedGuids = previousChildGuids.Where(g => !current_child_guids.Contains(g)).ToList();

                // 处理删除的节点
                foreach (var removedGuid in removedGuids)
                    tree_view_.NodeViewManager.DeleteNodeViewForGuid(removedGuid);
            }

            // 更新记录
            node_child_guid_trackers_[node.Guild] = current_child_guids;
        }

        public override void UpdatePanel(HashSet<BtNodeBase> nodes)
        {
            // 属性面板通常只显示单个节点的属性
            if (nodes is not { Count: 1 })
            {
                SetPanelVisibility(false);
                return;
            }

            SetPanelVisibility(true);
            Dispose();
            ClearPanel();

            currentNode = nodes.First();

            try
            {
                switch (currentNode)
                {
                    case null:
                        container_.Add(new Label("没有选中的节点"));
                        return;
                    // 初始化子节点跟踪
                    case BtComposite compositeNode:
                        InitializeChildNodeTracking(compositeNode);
                        break;
                }

                // --- 核心改动：动态查找被标记的集合 ---
                FindTrackedCollectionMember();

                // --- Core Change: Use IMGUIContainer ---
                try
                {
                    // 3. Create the Odin Property Tree
                    property_tree_ = PropertyTree.Create(currentNode);

                    // 4. Create the IMGUIContainer
                    odin_imgui_container_ = new IMGUIContainer();
                    odin_imgui_container_.name = "OdinIMGUIContainer";

                    // 5. Assign the drawing logic to onGUIHandler
                    //    This lambda will be called by UIToolkit when it's time to render the IMGUI content
                    odin_imgui_container_.onGUIHandler = () =>
                    {
                        if (property_tree_ != null)
                            try
                            {
                                // Call Odin's IMGUI Draw method INSIDE the handler
                                EditorGUILayout.BeginVertical();

                                // 在绘制前记录状态
                                if (currentNode is BtComposite) needs_child_node_check_ = true;

                                // 绘制属性
                                property_tree_.Draw(false);

                                // 在绘制后检查变化
                                if (needs_child_node_check_ && currentNode is BtComposite composite)
                                    CheckChildNodeChanges(composite);

                                EditorGUILayout.EndVertical();
                            }
                            catch (ExitGUIException)
                            {
                                // ExitGUIException is often thrown intentionally by IMGUI controls
                                // like dropdowns closing. Usually safe to ignore or just rethrow.
                                throw;
                            }
                            catch (Exception drawEx)
                            {
                                // Catch other potential errors during the draw call itself
                                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(
                                    new LogSpaceNode("InspectorView").AddChild("NodePropertiesPanel"),
                                    new LogEntry(LogLevel.kError, $"Error during Odin PropertyTree.Draw: {drawEx}"));
                            }
                    };

                    // 6. Add the IMGUIContainer to your main container
                    container_.Add(odin_imgui_container_);
                }
                catch (Exception ex)
                {
                    ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(
                        new LogSpaceNode("InspectorView").AddChild("NodePropertiesPanel"),
                        new LogEntry(LogLevel.kError, $"创建 Odin Inspector 或 IMGUIContainer 时出错: {ex}"));
                    container_.Add(new Label($"无法显示属性: {ex.Message}"));
                    Dispose(); // Ensure cleanup even if creation fails
                }
            }
            catch (Exception ex)
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(
                    new LogSpaceNode("InspectorView").AddChild("NodePropertiesPanel"),
                    new LogEntry(LogLevel.kError, $"发生错误，错误原因为: {ex.Message}"));
                container_.Add(new Label($"无法显示属性: {ex.Message}"));
                Dispose();
            }
        }

        public override string GetPanelHeaderText(HashSet<BtNodeBase> nodes)
        {
            return "节点属性";
        }

        public override void Dispose()
        {
            // Detach the handler to prevent potential issues if the container outlives the panel logic
            if (odin_imgui_container_ != null)
            {
                odin_imgui_container_.onGUIHandler = null; // Important!
                // Remove the container itself if it's still parented (optional, depends on how you manage elements)
                if (odin_imgui_container_.parent == container_) container_.Remove(odin_imgui_container_);

                odin_imgui_container_ = null;
            }

            // Dispose the Odin Property Tree
            if (property_tree_ != null)
            {
                property_tree_.Dispose();
                property_tree_ = null;
            }
        }
    }
}