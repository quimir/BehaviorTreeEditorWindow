using BehaviorTree.BehaviorTrees;
using BehaviorTree.Core;
using Editor.View.BTWindows;
using Editor.View.BtWindows.Core;
using UnityEditor;

namespace Editor.EditorToolExs
{
    /// <summary>
    /// Represents an initializer that is executed when the Unity Editor loads.
    /// This static class is decorated with the <see cref="InitializeOnLoadAttribute" />,
    /// which ensures any static constructor or initialization logic within this class is
    /// run automatically when the Unity Editor starts or recompiles scripts.
    /// Designed for extending editor functionality such as automation,
    /// initialization of custom editor tools, or execution of tasks at startup.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorInitializer
    {
        static EditorInitializer()
        {
            EditorBridge.Instance.OnOpenBehaviorTreeWindowRequested += OpenBehaviorTreeWindow;
        }

        private static void OpenBehaviorTreeWindow(string tree_id)
        {
            if (string.IsNullOrEmpty(tree_id)) return;

            BehaviorTreeWindowsBase.CreateWindowForTrees<BehaviorTreeWindows>(tree_id);
        }
    }
}