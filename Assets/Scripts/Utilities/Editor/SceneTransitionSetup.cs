using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Unbound.Utilities.Editor
{
    /// <summary>
    /// Editor utility for setting up SceneTransitionManager in scenes.
    /// Provides menu items and setup helpers for easy configuration.
    /// </summary>
    public static class SceneTransitionSetup
    {
        [MenuItem("GameObject/Unbound/Scene Transition Manager", false, 10)]
        private static void CreateSceneTransitionManager(MenuCommand menuCommand)
        {
            // Create the SceneTransitionManager GameObject
            GameObject transitionManager = new GameObject("SceneTransitionManager");
            transitionManager.AddComponent<SceneTransitionManager>();

            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(transitionManager, "Create SceneTransitionManager");

            // Place the object in the scene hierarchy
            if (Selection.activeTransform != null)
            {
                transitionManager.transform.SetParent(Selection.activeTransform);
            }

            // Select the newly created object
            Selection.activeObject = transitionManager;

            Debug.Log("SceneTransitionManager created successfully! You can now configure it in the Inspector.");
        }

        [MenuItem("Tools/Unbound/Setup Scene Transitions")]
        private static void SetupSceneTransitions()
        {
            // Check if SceneTransitionManager already exists in the scene
            SceneTransitionManager existingManager = Object.FindFirstObjectByType<SceneTransitionManager>();

            if (existingManager != null)
            {
                Debug.LogWarning("SceneTransitionManager already exists in the scene.");
                Selection.activeObject = existingManager.gameObject;
                return;
            }

            // Create new SceneTransitionManager
            GameObject transitionManager = new GameObject("SceneTransitionManager");
            transitionManager.AddComponent<SceneTransitionManager>();

            // Mark scene as dirty
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("SceneTransitionManager setup completed! Scene has been marked as dirty.");
        }

        [MenuItem("Tools/Unbound/Scene Transition Manager Documentation")]
        public static void OpenDocumentation()
        {
            string readmePath = "Assets/Scripts/Utilities/SCENE_TRANSITIONS_README.md";
            Object readme = AssetDatabase.LoadAssetAtPath<Object>(readmePath);

            if (readme != null)
            {
                AssetDatabase.OpenAsset(readme);
            }
            else
            {
                Debug.LogError("Documentation file not found at: " + readmePath);
            }
        }

        [MenuItem("Tools/Unbound/Validate Scene Setup")]
        public static void ValidateSceneSetup()
        {
            bool hasErrors = false;

            // Check for SceneTransitionManager
            SceneTransitionManager transitionManager = Object.FindFirstObjectByType<SceneTransitionManager>();
            if (transitionManager == null)
            {
                Debug.LogError("❌ No SceneTransitionManager found in scene. Use 'Tools > Unbound > Setup Scene Transitions' to add one.");
                hasErrors = true;
            }
            else
            {
                Debug.Log("✅ SceneTransitionManager found in scene.");
            }

            // Check for SaveManager integration
            SaveManager saveManager = Object.FindFirstObjectByType<SaveManager>();
            if (saveManager == null)
            {
                Debug.LogWarning("⚠️ No SaveManager found in scene. Auto-save on transition will be disabled.");
            }
            else
            {
                Debug.Log("✅ SaveManager found in scene. Auto-save integration enabled.");
            }

            // Check scenes in build settings
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            if (scenes.Length == 0)
            {
                Debug.LogError("❌ No scenes found in Build Settings. Add your scenes to File > Build Settings.");
                hasErrors = true;
            }
            else
            {
                Debug.Log($"✅ Found {scenes.Length} scene(s) in Build Settings:");
                foreach (EditorBuildSettingsScene scene in scenes)
                {
                    Debug.Log($"   - {scene.path}");
                }
            }

            if (!hasErrors)
            {
                Debug.Log("🎉 Scene setup validation completed successfully!");
            }
            else
            {
                Debug.Log("⚠️ Please fix the errors above before using scene transitions.");
            }
        }

        [MenuItem("Tools/Unbound/Create Transition Example")]
        public static void CreateTransitionExample()
        {
            // Check if example already exists
            SceneTransitionExample existingExample = Object.FindFirstObjectByType<SceneTransitionExample>();
            if (existingExample != null)
            {
                Debug.LogWarning("SceneTransitionExample already exists in the scene.");
                Selection.activeObject = existingExample.gameObject;
                return;
            }

            // Create example GameObject
            GameObject exampleObject = new GameObject("SceneTransitionExample");
            exampleObject.AddComponent<SceneTransitionExample>();

            // Mark scene as dirty
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("SceneTransitionExample created! Use this to test different transition methods.");
        }
    }

    /// <summary>
    /// Custom editor for SceneTransitionManager to provide setup helpers
    /// </summary>
    [CustomEditor(typeof(SceneTransitionManager))]
    public class SceneTransitionManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Quick Setup", EditorStyles.boldLabel);

            if (GUILayout.Button("Open Documentation"))
            {
                SceneTransitionSetup.OpenDocumentation();
            }

            if (GUILayout.Button("Validate Scene Setup"))
            {
                SceneTransitionSetup.ValidateSceneSetup();
            }

            if (GUILayout.Button("Create Example Usage"))
            {
                SceneTransitionSetup.CreateTransitionExample();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "For button integration:\n" +
                "1. Select a UI Button\n" +
                "2. Add OnClick event\n" +
                "3. Drag this GameObject to the event\n" +
                "4. Select a method from the dropdown",
                MessageType.Info
            );
        }
    }
}
