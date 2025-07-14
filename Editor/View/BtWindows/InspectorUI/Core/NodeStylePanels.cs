using System;
using System.Collections.Generic;
using System.Linq;
using BehaviorTree.Nodes;
using Editor.EditorToolExs;
using Editor.EditorToolExs.BtNodeWindows;
using Editor.EditorToolExs.Operation;
using Editor.EditorToolExs.Operation.Core;
using Editor.EditorToolExs.Operation.Storage;
using Editor.View.BTWindows;
using Editor.View.BTWindows.BtTreeView;
using Editor.View.BTWindows.InspectorUI.Operations;
using ExTools.Utillties;
using LogManager.Core;
using LogManager.LogManagerFactory;
using Script.BehaviorTree.Save;

using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.View.BtWindows.InspectorUI.Core
{
    public class NodeStylePanels : BaseInspectorPanel
    {
        private PropertyTree property_tree_;
        private IMGUIContainer odin_imgui_container_;
        private IOperationManager operation_manager_;
        
        private BehaviorTreeView tree_view_;

        #region 跟踪样式更改

        private BtNodeBase current_node_;
        private HashSet<BtNodeBase> current_batch_nodes_;
        private BtNodeStyle last_recorded_style_;
        private BtNodeStyle temp_batch_style_;
        private bool is_applying_operation_; // 防止操作应用时触发新的操作记录

        #endregion

        public NodeStylePanels(BehaviorTreeView tree_view,VisualElement container):base(container)
        {
            operation_manager_ = new OperationManager();
            tree_view_ = tree_view;
        }

        public void CreateSingleNodeStyleEditor(BtNodeBase node)
        {
            current_node_ = node;
            current_batch_nodes_ = null;

            BtNodeStyle node_style = null;
            node_style=tree_view_.NodeStyleManager.TryGetNodeStyle(node);

            if (node_style == null)
            {
                container_.Add(new Label("该节点无法找到风格"));
                return;
            }

            try
            {
                // 记录初始样式状态
                last_recorded_style_ = EditorExTools.Instance.CloneBtNodeStyle(node_style);

                property_tree_ = PropertyTree.Create(node_style);

                odin_imgui_container_ = new IMGUIContainer();
                odin_imgui_container_.name = "OdinIMGUIContainer";

                odin_imgui_container_.onGUIHandler = () =>
                {
                    if (property_tree_ != null)
                        try
                        {
                            EditorGUI.BeginChangeCheck();
                            property_tree_.Draw(false);

                            if (EditorGUI.EndChangeCheck())
                                // 创建撤销/重做操作
                                if (property_tree_.WeakTargets[0] is BtNodeStyle current_style)
                                {
                                    var operation = new NodeStyleOperation(current_node_, last_recorded_style_,
                                        current_style, "修改节点样式");

                                    // 不直接应用更改，而是通过操作管理器
                                    property_tree_.ApplyChanges();
                                    operation_manager_.ExecuteOperation(operation);

                                    // 更新记录样式
                                    last_recorded_style_ = EditorExTools.Instance.CloneBtNodeStyle(current_style);

                                    RefreshNodeView(node);
                                }
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
                                new LogSpaceNode("InspectorView").AddChild("NodeStylePanel"),
                                new LogEntry(LogLevel.kError, $"Error during Odin PropertyTree.Draw: {drawEx}"));
                        }
                };
                
                container_.Add(odin_imgui_container_);

                var reset_button = new Button(() =>
                {
                    BtNodeStyle current_style = tree_view_.NodeStyleManager.TryGetNodeStyle(current_node_);
                    BtNodeStyle default_style = tree_view_.NodeStyleManager.CreateDefaultStyle(current_node_);

                    var operation = new NodeStyleOperation(current_node_, current_style, default_style, "重置节点样式");

                    operation_manager_.ExecuteOperation(operation);
                    RefreshNodeView(current_node_);
                    CreateSingleNodeStyleEditor(current_node_); // 重新创建面板以反映变更
                })
                {
                    text = "重置为默认样式",
                    style =
                    {
                        flexGrow = 1, marginTop = 10, minHeight = 20
                    }
                };
                
                container_.Add(reset_button);
            }
            catch (Exception ex)
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(
                    new LogSpaceNode("InspectorView").AddChild("NodeStylePanel"),
                    new LogEntry(LogLevel.kError, $"创建 Odin Inspector 或 IMGUIContainer 时出错: {ex}"));
                container_.Add(new Label($"无法显示属性: {ex.Message}"));
                Dispose(); // Ensure cleanup even if creation fails
            }
        }

        public void CreateBatchNodeStyleEditor(HashSet<BtNodeBase> nodes)
        {
            if (nodes == null || nodes.Count == 0)
            {
                container_.Add(new Label("未选中任何节点"));
                return;
            }

            // 创建一个临时样式对象，用于批量处理
            var temp_style = new BtNodeStyle();

            try
            {
                // 创建一个临时对象
                property_tree_ = PropertyTree.Create(temp_style);

                odin_imgui_container_ = new IMGUIContainer();
                odin_imgui_container_.name = "OdinIMGUIContainer";

                odin_imgui_container_.onGUIHandler = () =>
                {
                    if (property_tree_ != null)
                        try
                        {
                            EditorGUILayout.LabelField("批量样式编辑", EditorStyles.boldLabel);

                            // 绘制属性数
                            EditorGUI.BeginChangeCheck();
                            property_tree_.Draw(false);

                            // 检查是否有变更
                            if (EditorGUI.EndChangeCheck())
                            {
                                // 应用更改到临时对象
                                property_tree_.ApplyChanges();

                                // 批量创建操作
                                var operation = new BatchNodeStyleOperation(current_batch_nodes_, temp_batch_style_,
                                    "批量修改节点样式");

                                operation_manager_.ExecuteOperation(operation);

                                // 遍历所有选中的节点，应用样式变更
                                foreach (var node in nodes)
                                {
                                    tree_view_.NodeStyleManager.ApplyStyle(node,temp_style);
                                    RefreshNodeView(node);
                                }
                            }
                        }
                        catch (ExitGUIException)
                        {
                            throw;
                        }
                        catch (Exception drawEx)
                        {
                            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(
                                new LogSpaceNode("InspectorView").AddChild("BatchNodeStylePanel"),
                                new LogEntry(LogLevel.kError, $"批量样式编辑错误: {drawEx}"));
                        }
                };
                
                container_.Add(odin_imgui_container_);

                // 批量重置按钮
                var reset_button = new Button(() =>
                {
                    foreach (var node in nodes)
                    {
                        tree_view_.NodeStyleManager.ResetNodeStyle(node);
                        RefreshNodeView(node);
                    }
                })
                {
                    text = "批量重置样式",
                    style = { marginTop = 10 }
                };
                
                container_.Add(reset_button);
            }
            catch (Exception ex)
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(
                    new LogSpaceNode("InspectorView").AddChild("BatchNodeStylePanel"),
                    new LogEntry(LogLevel.kError, $"创建批量样式编辑器时出错: {ex}"));

               container_.Add(new Label($"无法显示批量样式编辑器: {ex.Message}"));
                Dispose();
            }
        }

        public override void UpdatePanel(HashSet<BtNodeBase> nodes)
        {
            SetPanelVisibility(true);// 样式面板在单选和多选时都可能显示
            ClearPanel();
            Dispose();

            if (nodes==null||nodes.Count==0)
            {
                SetPanelVisibility(false);
                return;
            }

            if (nodes.Count==1)
            {
                CreateSingleNodeStyleEditor(nodes.First());
            }
            else
            {
                CreateBatchNodeStyleEditor(nodes);
            }
        }

        public override string GetPanelHeaderText(HashSet<BtNodeBase> nodes)
        {
            if (nodes.Count == 1)
            {
                return "节点样式";
            }
            else
            {
                return $"节点样式 (已选中 {nodes.Count}) 个节点";
            }
        }

        public override void Dispose()
        {
            if (odin_imgui_container_ != null)
            {
                odin_imgui_container_.onGUIHandler = null;

                if (odin_imgui_container_.parent == container_)
                    container_.Remove(odin_imgui_container_);

                odin_imgui_container_ = null;
            }

            if (property_tree_ != null)
            {
                property_tree_.Dispose();

                property_tree_ = null;
            }
        }

        /// <summary>
        /// Refreshes the view of the given behavior tree node in the editor.
        /// </summary>
        /// <param name="node">The node whose view needs to be refreshed.</param>
        private void RefreshNodeView(BtNodeBase node)
        {
            tree_view_.GetNodeViewByGuid(node.Guild)?.ApplyStyle();
        }
    }
}