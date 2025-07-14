using System;
using System.Collections.Generic;
using System.Linq;
using BehaviorTree.BehaviorTrees;
using BehaviorTree.Core.WindowData;
using ExTools.Utillties;
using LogManager.Core;
using LogManager.LogManagerFactory;
using Save.Serialization.Core;
using Save.Serialization.Core.TypeConverter;
using Save.Serialization.Factory;
using Save.Serialization.Storage.Serializer;
using Save.Serialization.Storage.Serializer.JsonNet;
using Script.Save.Serialization;
using Script.Save.Serialization.Factory;
using UnityEditor;
using UnityEngine;

namespace Editor.View.BtWindows.Core.EditorRestore
{
    /// <summary>
    /// Manages the lifecycle events for the Behavior Tree Editor, specifically controlling
    /// application states such as determining whether the editor is in the process of quitting.
    /// Utilized primarily to avoid unnecessary operations when exiting the editor environment.
    /// </summary>
    [InitializeOnLoad]
    public static class BehaviorTreeEditorLifecycleManager
    {
        private const string OpenWindowsPrefsKey = "BehaviorTreeEditor_OpenWindows";
        public static bool IsQuitting { get; private set; } = false;
        private static bool save_scheduled_;

        static BehaviorTreeEditorLifecycleManager()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.wantsToQuit += WasEditorQuitting;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            EditorApplication.delayCall += RestoreWindowsOnStartup;
        }

        private static void OnBeforeAssemblyReload()
        {
            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(
                new LogSpaceNode("Root"), new LogEntry(LogLevel.kInfo, "[LifecycleManager] 检测到脚本" +
                                                                       "即将重载，请求保存窗口状态..."));
            RequestSaveState();
        }

        /// <summary>
        /// Schedules a request to save the state of open windows in the Behavior Tree Editor.
        /// If a save operation is already scheduled, this request will be ignored.
        /// The actual save operation is delayed until the current event loop completes,
        /// ensuring that only one save operation is performed even if multiple events trigger in the same frame.
        /// </summary>
        private static void RequestSaveState()
        {
            // 如果已经有一个保存在“预约”中，则忽略本次请求
            if (save_scheduled_) return;
            
            if (IsQuitting)
            {
                return;
            }

            save_scheduled_ = true;
            
            // 使用 delayCall 将实际的保存操作推迟到当前事件循环结束时执行。
            // 这确保了即使有多个事件在同一帧内触发，也只执行一次保存。
            SaveOpenWindowState();
            
            EditorApplication.delayCall += () =>
            {
                // 等待下一帧进行恢复
                save_scheduled_ = false;
            };
        }

        /// <summary>
        /// Handles the quitting process of the Unity editor by performing necessary state preservation steps,
        /// including saving window states and logging the quit event.
        /// </summary>
        /// <returns>True if the editor is allowed to quit; otherwise, false.</returns>
        private static bool WasEditorQuitting()
        {
            IsQuitting = true;
            EditorPrefs.SetBool("UnityEditor_DidQuitCleanly", true);
            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace)
                .AddLog(new LogSpaceNode("Root"), new LogEntry(LogLevel.kInfo, 
                    "编辑器正在退出，保存所有窗口状态..."), true);
            SaveOpenWindowState();
            return true;
        }

        /// <summary>
        /// Restores the layout and state of open Behavior Tree Editor windows upon startup.
        /// Reads the saved window configurations from EditorPrefs, validates their data, and attempts to reconstruct
        /// the windows based on their serialized states.Removes any unrelated or invalid window data to ensure a clean
        /// state during restoration.
        /// </summary>
        private static void RestoreWindowsOnStartup()
        {
            var json = EditorPrefs.GetString(OpenWindowsPrefsKey, null);
            if (string.IsNullOrEmpty(json)) return;

            var window_states = JsonUtility.FromJson<WindowStateList>(json);
            if (window_states == null || window_states.States.Count == 0)
                return;

            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(
                new LogSpaceNode("Root"),
                new LogEntry(LogLevel.kInfo, $"[LifecycleManager] 准备恢复 {window_states.States.Count} 个窗口..."));

            var all_windows = Resources.FindObjectsOfTypeAll<BehaviorTreeWindows>();
            var zombie_windows =
                new List<BehaviorTreeWindows>(all_windows.Where(w => string.IsNullOrEmpty(w.WindowInstanceId)));
            

            // 获取序列化器，用于重构临时数
            var serializer = SerializerCreator.Instance.Create<JsonSerializerWithStorage>(SerializerType.kJson,
                new SerializationSettings
                {
                    PrettyPrint = true,
                    PreserveReferences = true,
                    TypeNameHandling = SerializationTypeNameHandling.kAuto
                });

            foreach (var state in window_states.States)
            {
                var can_restore = false;

                // 根据持久化烈性选择不同的恢复路径
                switch (state.PersistenceType)
                {
                    case TreePersistenceType.kTemporary:
                        if (!string.IsNullOrEmpty(state.SerializedTreeData))
                            try
                            {
                                // 从Json数据反序列化，重构核心数据
                                var restored_data =
                                    serializer.DeserializeFromText<BehaviorTreeWindowData>(state.SerializedTreeData);
                                if (restored_data != null)
                                {
                                    var temp_tree = new BehaviorTreeTemp(restored_data);
                                    temp_tree.SetTreeId(state.AssociatedTreeId);
                                    BehaviorTreeManagers.instance.RegisterTree(state.AssociatedTreeId, temp_tree);
                                    can_restore = true;
                                }
                            }
                            catch (Exception e)
                            {
                                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace)
                                    .AddLog(new LogSpaceNode("Root"),
                                        new LogEntry(LogLevel.kError, $"[LifecycleManager] 从JSON重建临时树失败: " +
                                                                      $"{e}"), true);
                            }

                        break;
                    case TreePersistenceType.kPersistent:
                        // 对于持久化的树，我们只需检查它是否已由其他机制
                        if (BehaviorTreeManagers.instance.GetTree(state.AssociatedTreeId) != null) can_restore = true;
                        break;
                }

                // 如果树已经成功恢复或找到，现在就恢复窗口本身
                if (can_restore)
                {
                    var already_initialized_window =
                        all_windows.FirstOrDefault(n => n.WindowInstanceId == state.WindowId);
                    if (already_initialized_window!=null)
                    {
                        already_initialized_window.RefreshWindow();
                        continue;
                    }

                    BehaviorTreeWindows window_to_adopt = null;
                    
                    // 位置匹配
                    if (zombie_windows.Count>0)
                    {
                        window_to_adopt = zombie_windows.FirstOrDefault(w => w.position == state.Position);
                    }
                    
                    // 如果匹配失败，则随机认领一个
                    if (window_to_adopt==null&&zombie_windows.Count>0)
                    {
                        ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(
                            new LogSpaceNode("Root"),
                            new LogEntry(LogLevel.kWarning, $"[LifecycleManager] 未找到位置匹配的窗口，为 Tree ID: {state.AssociatedTreeId} 随机认领一个。"));

                        window_to_adopt = zombie_windows[0];
                    }

                    if (window_to_adopt)
                    {
                        // 成功认领一个窗口 (无论是通过位置还是备用策略)
                        zombie_windows.Remove(window_to_adopt);
                        
                        ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(
                            new LogSpaceNode("Root"),
                            new LogEntry(LogLevel.kInfo, $"[LifecycleManager] 成功复用一个现有窗口来恢复 Tree ID: {state.AssociatedTreeId}"));
                        
                        window_to_adopt.InitializeForRestoration(state.WindowId,state.AssociatedTreeId);
                        window_to_adopt.Focus();
                    }
                    else
                    {
                        // 策略 4: 创建新窗口 (如果上面所有策略都失败)
                        ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(
                            new LogSpaceNode("Root"),
                            new LogEntry(LogLevel.kWarning, $"[LifecycleManager] 所有复用策略均失败，为 Tree ID: {state.AssociatedTreeId} 创建一个新窗口。"));

                        var window = EditorWindow.CreateWindow<BehaviorTreeWindows>();
                        window.InitializeForRestoration(state.WindowId,state.AssociatedTreeId);
                        // 新创建的窗口，可以尝试设置其应有的位置
                        if (state.Position is { width: > 0, height: > 0 })
                        {
                            window.position=state.Position;
                        }
                        
                        window.Show();
                        window.Focus();
                    }
                }
                else
                {
                    ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(
                        new LogSpaceNode("Root"),
                        new LogEntry(LogLevel.kWarning,
                            $"[LifecycleManager] 无法恢复窗口 {state.WindowId}，因为其关联的行为树 " +
                            $"{state.AssociatedTreeId} 数据丢失或无效。"),
                        true);
                }
            }

            // 关闭任何未被认领的僵尸窗口 (如果Unity恢复的窗口比我们保存的状态要多)
            foreach (var remaining_zombie in zombie_windows)
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(
                    new LogSpaceNode("Root"),
                    new LogEntry(LogLevel.kWarning, $"[LifecycleManager] 关闭一个多余的、未被认领的窗口。"));
                remaining_zombie.Close();
            }

            EditorPrefs.DeleteKey(OpenWindowsPrefsKey);
        }

        private static void SaveOpenWindowState()
        {
            var open_windows = Resources.FindObjectsOfTypeAll<BehaviorTreeWindows>();

            // 创建一个可序列化的列表来存储窗口ID和其关联的树ID
            var window_states = new List<WindowState>();

            // 获取序列化器
            var serializer = SerializerCreator.Instance.Create<JsonSerializerWithStorage>(SerializerType.kJson,
                new SerializationSettings
                {
                    PrettyPrint = true,
                    PreserveReferences = true,
                    TypeNameHandling = SerializationTypeNameHandling.kAuto
                });

            foreach (var window in open_windows)
            {
                if (string.IsNullOrEmpty(window.WindowInstanceId)) continue;

                window.SaveWindow();
                var tree = BehaviorTreeManagers.instance.GetTreeByWindowId(window.WindowInstanceId);
                if (tree == null) continue;

                var state = new WindowState
                {
                    WindowId = window.WindowInstanceId,
                    AssociatedTreeId = tree.GetTreeId(),
                    Position = window.position,
                };

                if (tree is BehaviorTreeTemp)
                {
                    state.PersistenceType = TreePersistenceType.kTemporary;

                    // 获取临时数的核心数据
                    var data_to_serialize = tree.GetNodeWindow();
                    if (data_to_serialize != null)
                        // 将核心数据序列化为Json字符串
                        state.SerializedTreeData = serializer.SerializeToText(data_to_serialize);
                }
                else
                {
                    state.PersistenceType = TreePersistenceType.kPersistent;
                    state.SerializedTreeData = string.Empty;
                }

                window_states.Add(state);
            }

            var json = JsonUtility.ToJson(new WindowStateList { States = window_states }, true);
            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace)
                .AddLog(new LogSpaceNode("Root"), new LogEntry(LogLevel.kInfo, "进行窗口保存注册"));
            EditorPrefs.SetString(OpenWindowsPrefsKey, json);
        }

        /// <summary>
        /// Handles actions when the play mode state changes. Specifically, it saves the open window states
        /// when the editor is transitioning from edit mode to play mode.
        /// </summary>
        /// <param name="state">The new play mode state of the Unity editor.</param>
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                RequestSaveState();
        }
    }
}