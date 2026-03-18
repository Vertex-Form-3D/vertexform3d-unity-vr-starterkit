using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard;
using VertexFormCore;
using UnityEngine.SceneManagement;

namespace VertexFormCore.Editor
{
    public static class VertexFormSDKMenu
    {
        [MenuItem("VertexForm3D SDK/Project Setup", false, 1)]
        public static void OpenProjectSetup()
        {
            ProjectSetUpEditor window = EditorWindow.GetWindow<ProjectSetUpEditor>("Project SetUp");
            window.minSize = new Vector2(450, 400); // Adjusted to fit UI elements
            window.Show();
        }


        [MenuItem("VertexForm3D SDK/Main UI Database", false, 2)]
        public static void OpenMainMapUIDatabase()
        {
            string[] guids = AssetDatabase.FindAssets("t:UILayoutConfig");
            if (guids.Length == 0)
            {
                string path = "Assets/VertexForm3D/ScriptableObjects/Main UI Database.asset";
                var config = ScriptableObject.CreateInstance<UILayoutConfig>();
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.SaveAssets();
                EditorUtility.OpenPropertyEditor(config);
                Debug.Log("Created and opened Main UI Database: " + path);
                return;
            }
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            UILayoutConfig configAsset = AssetDatabase.LoadAssetAtPath<UILayoutConfig>(assetPath);
            EditorUtility.OpenPropertyEditor(configAsset);
            Debug.Log("Opened Main Map UI Database: " + assetPath);
        }
        [MenuItem("VertexForm3D SDK/Platforms", false, 2)]
        public static void OpenPlatformAndSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:Platforms");
            if (guids.Length == 0)
            {
                string path = "Assets/VertexForm3D/ScriptableObjects/Platforms.asset";
                var config = ScriptableObject.CreateInstance<Platforms>();
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.SaveAssets();
                EditorUtility.OpenPropertyEditor(config);
                Debug.Log("Created and opened Platform and Settings: " + path);
                return;
            }
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            Platforms configAsset = AssetDatabase.LoadAssetAtPath<Platforms>(assetPath);
            EditorUtility.OpenPropertyEditor(configAsset);
            Debug.Log("Opened Platform and Settings: " + assetPath);
        }
        [MenuItem("VertexForm3D SDK/SettingsUI", false, 2)]
        public static void OpenSettingsUI()
        {
            string[] guids = AssetDatabase.FindAssets("t:SettingsUI");
            if (guids.Length == 0)
            {
                string path = "Assets/VertexForm3D/ScriptableObjects/SettingsUI.asset";
                var config = ScriptableObject.CreateInstance<SettingsUISO>();
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.SaveAssets();
                EditorUtility.OpenPropertyEditor(config);
                Debug.Log("Created and opened SettingsUI: " + path);
                return;
            }
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            SettingsUISO configAsset = AssetDatabase.LoadAssetAtPath<SettingsUISO>(assetPath);
            EditorUtility.OpenPropertyEditor(configAsset);
            Debug.Log("Opened SettingsUI: " + assetPath);
        }

        [MenuItem("VertexForm3D SDK/Creator Toolkit/Favorites", false, 4)]
        public static void OpenFavorites()
        {
            VertexFormToolkit window = EditorWindow.GetWindow<VertexFormToolkit>("VertexForm 3D");
            window.SelectTab("FAVORITES");
        }


        [MenuItem("VertexForm3D SDK/Creator Toolkit/XR Game Objects", false, 5)]
        public static void OpenXRGameObjectsTab()
        {
            VertexFormToolkit window = EditorWindow.GetWindow<VertexFormToolkit>("VertexForm 3D");
            window.SelectTab("XR GAME OBJECTS");
        }

        [MenuItem("VertexForm3D SDK/Creator Toolkit/Scene Switching", false, 6)]
        public static void OpenSceneSwitchingTab()
        {
            VertexFormToolkit window = EditorWindow.GetWindow<VertexFormToolkit>("VertexForm 3D");
            window.SelectTab("SCENE SWITCHING");
        }


        [MenuItem("VertexForm3D SDK/Creator Toolkit/Player Position", false, 7)]
        public static void OpenPlayerPositionTab()
        {
            VertexFormToolkit window = EditorWindow.GetWindow<VertexFormToolkit>("VertexForm 3D");
            window.SelectTab("PLAYER POSITION");
        }

        [MenuItem("VertexForm3D SDK/Creator Toolkit/UI Elements", false, 8)]
        public static void OpenUIElementsTab()
        {
            VertexFormToolkit window = EditorWindow.GetWindow<VertexFormToolkit>("VertexForm 3D");
            window.SelectTab("UI ELEMENTS");
        }

        [MenuItem("VertexForm3D SDK/Creator Toolkit/Presentation Tools", false, 9)]
        public static void OpenPresentationToolsTab()
        {
            VertexFormToolkit window = EditorWindow.GetWindow<VertexFormToolkit>("VertexForm 3D");
            window.SelectTab("PRESENTATION TOOLS");
        }

        [MenuItem("VertexForm3D SDK/Creator Toolkit/Dev Tools", false, 10)]
        public static void OpenDevToolsTab()
        {
            VertexFormToolkit window = EditorWindow.GetWindow<VertexFormToolkit>("VertexForm 3D");
            window.SelectTab("DEV TOOLS");
        }

        [MenuItem("VertexForm3D SDK/Build Addressables", false, 11)]
        public static void OpenBuildAddressablesWindow()
        {
            AddressablesBuildEditor window = EditorWindow.GetWindow<AddressablesBuildEditor>("Build Addressables");
            window.minSize = new Vector2(450, 400); // Adjusted to fit UI elements
            window.Show();
        }

        [MenuItem("VertexForm3D SDK/XR Device Simulator", false, 1000)]
        private static void AddXRDeviceSimulator()
        {
            GameObject g = Object.Instantiate(Resources.Load<GameObject>("CustomEditor/XR Device Simulator"));
            g.name = "XR Device Simulator";
            EditorGUIUtility.PingObject(g);
        }

        [MenuItem("VertexForm3D SDK/Help", false, 15)]
        public static void OpenHelp()
        {
            VertexForm3DHelp.ShowWindow();
        }
        // [MenuItem("VertexForm3D SDK/Check for Updates", false, 13)]
        // public static void CheckForUpdates()
        // {
        //     VertexForm3DUpdateChecker.CheckForUpdatesManually();
        // }
    }
}

#if UNITY_EDITOR
[InitializeOnLoad]
public class SceneSavingEditor
{
    static SceneSavingEditor()
    {
        // Subscribe to the sceneSaving event
        EditorSceneManager.sceneSaving += OnSceneSaving;
    }

    private static void OnSceneSaving(Scene scene, string path)
    {
        // Find all XRGrabNetworkInteractable components in the scene
        var interactables = Resources.FindObjectsOfTypeAll<XRGrabNetworkInteractable>();
        foreach (var interactable in interactables)
        {
            // Call SetInitialPosition and SetInitialRotation
            interactable.SetInitialPosition();
            interactable.SetInitialRotation();
        }

        // Handle TMP_InputField components
        var tmpInputFields = Resources.FindObjectsOfTypeAll<TMP_InputField>();
        foreach (var inputfield in tmpInputFields)
        {
            if (inputfield.GetComponent<XRKeyboardDisplay>() == null)
            {
                inputfield.gameObject.AddComponent<XRKeyboardDisplay>();
            }
            inputfield.GetComponent<XRKeyboardDisplay>().inputField = inputfield;
        }
    }
}
#endif