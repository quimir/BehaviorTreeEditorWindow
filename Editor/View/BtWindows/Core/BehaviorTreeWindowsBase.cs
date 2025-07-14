using System;
using System.Linq;
using BehaviorTree.BehaviorTreeBlackboard;
using BehaviorTree.BehaviorTrees;
using BehaviorTree.Core;
using BehaviorTree.Core.WindowData;
using BehaviorTree.Nodes;
using Editor.View.BtWindows.MenuBar;
using Editor.View.BtWindows.MenuBar.Core;
using Script.BehaviorTree;
using UnityEditor;
using UnityEngine;

namespace Editor.View.BtWindows.Core
{
    /// <summary>
    /// A base class for creating and managing behavior tree editor windows in Unity.
    /// This class extends <see cref="EditorWindow"/> to provide a foundational structure
    /// for implementing functionality such as refreshing, saving, or handling domain reloads
    /// for behavior tree visualization and manipulation.
    /// </summary>
    public abstract class BehaviorTreeWindowsBase : EditorWindow
    {
        /// <summary>
        /// Gets or sets the unique identifier for an instance of the behavior tree editor window.
        /// This property is used to associate a specific window instance with a behavior tree,
        /// enabling behaviors such as window tracking, linking trees to windows, and
        /// managing multiple editor windows in Unity. The identifier is typically assigned
        /// automatically when a window instance is initialized.
        /// </summary>
        [SerializeField]
        public string WindowInstanceId
        {
            get;
            protected set;
        }

        private MenuBarElement menu_bar_;

        /// <summary>
        /// Retrieves an instance of the <see cref="MenuBarElement"/> associated with the behavior tree editor window.
        /// This property provides access to the menu bar, which is used for managing and displaying menu items
        /// such as file operations, editing tools, or custom actions within the editor window. The menu bar is
        /// initialized as part of the window's UI setup and can be customized or extended with additional menu items
        /// as needed.
        /// </summary>
        public MenuBarElement MenuBar => menu_bar_;
        
        protected bool is_refreshing_ = false;

        /// <summary>
        /// Refreshes the contents of the behavior tree editor window,
        /// typically reinitializing UI elements or reloading editor data.
        /// </summary>
        /// <remarks>
        /// This method should be invoked when there is a need to clear and
        /// recreate the window's contents, such as after structural changes
        /// in the data or user-triggered interactions.
        /// </remarks>
        public virtual void RefreshWindow()
        {
            is_refreshing_ = true;
            
            // 在下一帧恢复状态
            EditorApplication.delayCall += () => { is_refreshing_ = false; };
        }

        /// <summary>
        /// Saves the current state of the behavior tree editor window,
        /// ensuring that all modifications or changes are persistently stored.
        /// </summary>
        /// <remarks>
        /// This method is designed to handle situations where user modifications
        /// to the behavior tree need to be committed, which may include actions
        /// like updating serialized data or writing changes to a file system.
        /// Implementations should ensure that the save operation addresses any
        /// necessary validation or cleanup.
        /// </remarks>
        public abstract void SaveWindow();

        /// <summary>
        /// Creates a temporary behavior tree with default configurations, including
        /// an initialized root node, transformation data, and blackboard.
        /// </summary>
        /// <remarks>
        /// This method is typically used to generate a temporary behavior tree
        /// instance when no active tree exists or when initializing new editor windows.
        /// </remarks>
        /// <returns>
        /// A new instance of a temporary behavior tree implementing the <see cref="IBehaviorTrees"/> interface.
        /// </returns>
        public static IBehaviorTrees CreateTempTree()
        {
            var data = new BehaviorTreeWindowData
            {
                EditorWindowData = new BtEditorWindowData
                {
                    GraphViewTransform = new GraphViewTransform
                    {
                        position = Vector3.zero,
                        scale = new Vector3(1.0f, 1.0f, 1.0f)
                    }
                },
                RootNode = new BtMainNode
                {
                    Guild = Guid.NewGuid().ToString(),
                    NodeName = "程序根节点",
                    Position = new Vector2(100,100)
                },
                Blackboard = new LayeredBlackboard(null)
            };
            
            var temp_tree=new BehaviorTreeTemp(data);

            return temp_tree;
        }

        /// <summary>
        /// Opens a new window or brings an existing window to focus to display
        /// the behavior tree associated with the given tree identifier.
        /// </summary>
        /// <typeparam name="T">
        /// The window type derived from <see cref="BehaviorTreeWindowsBase"/> that will handle
        /// the behavior tree visualization. This type must have a parameterless constructor.
        /// </typeparam>
        /// <param name="tree_id">
        /// The unique identifier of the behavior tree to be displayed in the editor window.
        /// If the identifier is invalid or empty, the method does nothing.
        /// </param>
        public static void CreateWindowForTrees<T>(string tree_id) where T : BehaviorTreeWindowsBase, new()
        {
            if (string.IsNullOrEmpty(tree_id))
            {
                return;
            }

            var all_window = Resources.FindObjectsOfTypeAll<T>();

            var window_id = BehaviorTreeManagers.instance.GetWindowIdByTreeId(tree_id);
            var existing_window = all_window.FirstOrDefault(w => w.WindowInstanceId == window_id);
            if (existing_window)
            {
                existing_window.Focus();
            }
            else
            {
                var new_window = CreateWindow<T>();
                new_window.titleContent = new GUIContent("BehaviorTreeWindow");
                new_window.InitWindow(tree_id);
                new_window.Show();
                new_window.Focus();
            }
        }

        protected virtual void CreateGUI()
        {
            menu_bar_=new MenuBarElement(this);
            rootVisualElement.Add(menu_bar_);
        }

        protected virtual void OnEnable()
        {
        }

        protected virtual void OnDisable()
        {
        }

        protected abstract void InitWindow(string trees_id);

        protected virtual void OnInspectorUpdate()
        {
            // 刷新状态不允许直接使用
            if (is_refreshing_)
            {
                return;
            }
            
        }
        
        protected virtual void OnFocus()
        {
            OnFocusGained();
        }

        protected virtual void OnLostFocus()
        {
            OnFocusLost();
        }

        protected virtual void OnFocusGained()
        {}
        
        protected virtual void OnFocusLost(){}
    }
}
