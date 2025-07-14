using System;
using BehaviorTree.BehaviorTrees;
using ExTools.Utillties;
using LogManager.Core;
using LogManager.LogManagerFactory;
using UnityEditor;
using UnityEngine;

namespace Editor.View.BtWindows.Core
{
    /// <summary>
    /// Represents a transfer window for attaching behavior trees to game objects in the Unity editor.
    /// This class provides a utility for selecting a game object and confirming or canceling the attachment operation.
    /// </summary>
    public class BehaviorTreeTransferWindow : EditorWindow
    {
        private GameObject target_game_object_;
        private Action<GameObject> on_confirm_;
        private Action on_cancel_;
        private string window_title_;
        private bool is_waiting_for_selection_ = true;

        /// <summary>
        /// Opens a modal utility window for attaching behavior trees to a game object.
        /// </summary>
        /// <param name="on_confirm">Callback action to invoke when the user confirms the operation, passing the selected game object.</param>
        /// <param name="on_cancel">Callback action to invoke when the user cancels the operation.</param>
        /// <param name="window_title">The title of the window. Defaults to "选择游戏对象".</param>
        public static void ShowWindow(Action<GameObject> on_confirm, Action on_cancel, string window_title = "选择游戏对象")
        {
            var window = GetWindow<BehaviorTreeTransferWindow>(window_title);
            window.on_confirm_ = on_confirm;
            window.on_cancel_ = on_cancel;
            window.window_title_ = window_title;
            window.minSize = new Vector2(320, 200);
            
            window.ShowModalUtility();
        }

        private void OnGUI()
        {
            GUILayout.Label(window_title_, EditorStyles.boldLabel);

            target_game_object_ =
                (GameObject)EditorGUILayout.ObjectField("目标GameObject", target_game_object_, typeof(GameObject), true);

            bool has_existing_component = false;

            if (target_game_object_)
            {
                has_existing_component = target_game_object_.GetComponent<ExtendableBehaviorTree>() != null;
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("对象名称:", target_game_object_.name);
                EditorGUILayout.LabelField("场景:", target_game_object_.scene.name);
            
                if (has_existing_component)
                {
                    EditorGUILayout.LabelField("状态:", "已存在行为树组件", EditorStyles.boldLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("状态:", "可以添加行为树组件");
                }
                EditorGUILayout.EndVertical();
            }

            GUILayout.Space(10);

            if (has_existing_component)
            {
                EditorGUILayout.HelpBox(
                    "该GameObject已经存在ExtendableBehaviorTree组件！\n" +
                    "由于[DisallowMultipleComponent]限制，无法添加多个相同组件。", 
                    MessageType.Warning
                );
            }
            else if (target_game_object_==null)
            {
                EditorGUILayout.HelpBox("请选择一个GameObject", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("可以将行为树组件添加到此对象", MessageType.Info);
            }

            GUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled=target_game_object_!=null&&!has_existing_component;
            if (GUILayout.Button("确认"))
                try
                {
                    is_waiting_for_selection_ = false;
                    on_confirm_?.Invoke(target_game_object_);
                    Close();
                }
                catch (Exception e)
                {
                    ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace)
                        .AddLog(new LogSpaceNode(null), new LogEntry(LogLevel.kError, $"执行回调时出错: {e.Message}"));
                    EditorUtility.DisplayDialog("错误", $"操作失败： {e.Message}", "确定");
                    return;
                }

            GUI.enabled = true;

            GUI.enabled = is_waiting_for_selection_;
            if (GUILayout.Button("取消"))
            {
                is_waiting_for_selection_ = false;
                on_cancel_?.Invoke();
                Close();
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
        }

        private void OnDestroy()
        {
            if (is_waiting_for_selection_)
            {
                on_cancel_?.Invoke();
            }
        }
    }
}