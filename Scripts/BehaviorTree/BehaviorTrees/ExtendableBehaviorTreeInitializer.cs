using System.Collections.Generic;
using System.Linq;
using BehaviorTree.Core;
using Script.BehaviorTree;
using Script.Utillties;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BehaviorTree.BehaviorTrees
{
    /// <summary>
    /// Handles the initialization logic for an extendable behavior tree system.
    /// This class is triggered during the Unity editor application lifecycle
    /// to set up and manage behavior tree components or extensions.
    /// </summary>
    [InitializeOnLoad]
    public class ExtendableBehaviorTreeInitializer
    {
        static ExtendableBehaviorTreeInitializer()
        {
            EditorApplication.delayCall+=DelayedInitialization;

            EditorSceneManager.sceneOpened += ((scene, mode) =>
            {
                EditorApplication.delayCall += DelayedInitialization;
            });
        }

        /// <summary>
        /// Performs delayed initialization for all instances of the ExtendableBehaviorTree
        /// found in the current context (including inactive objects).
        /// This is triggered through Unity Editor's delayed call mechanisms.
        /// The method ensures that each behavior tree is properly set up and initialized
        /// if not already configured.
        /// </summary>
        private static void DelayedInitialization()
        {
            var behavior_trees=UnityEngine.Object.FindObjectsOfType<ExtendableBehaviorTree>(true);

            var trees_to_register = new Dictionary<string, IBehaviorTrees>();

            foreach (var bt in behavior_trees)
            {
                bt.InitializeIfNeeded();

                if (!string.IsNullOrEmpty(bt.GetTreeId()))
                {
                    trees_to_register[bt.GetTreeId()] = bt;
                }
            }

            if (trees_to_register.Any())
            {
                BehaviorTreeManagers.instance.RegisterTrees(trees_to_register);
            }
        }
    }
}
