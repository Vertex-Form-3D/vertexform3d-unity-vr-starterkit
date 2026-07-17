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
        [MenuItem("Vertex Form/Project Setup", false, 1)]
        public static void OpenProjectSetup()
        {
            ProjectSetUpEditor window = EditorWindow.GetWindow<ProjectSetUpEditor>();
            VertexFormEditorHeader.ApplyWindowTitle(window, "Project Setup");
            window.minSize = new Vector2(450, 400);
            window.Show();
        }


        [MenuItem("Vertex Form/Main UI Database", false, 3)]
        public static void OpenMainMapUIDatabase()
        {
            string[] guids = AssetDatabase.FindAssets("t:UILayoutConfig");
            if (guids.Length == 0)
            {
                string path = "Assets/VertexForm3D/ScriptableObjects/Main UI Database.asset";
                var config = ScriptableObject.CreateInstance<UILayoutConfig>();
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.SaveAssets();
                VertexFormEditorHeader.OpenBrandedPropertyEditor(config, "Main UI Database");
                Debug.Log("Created and opened Main UI Database: " + path);
                return;
            }
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            UILayoutConfig configAsset = AssetDatabase.LoadAssetAtPath<UILayoutConfig>(assetPath);
            VertexFormEditorHeader.OpenBrandedPropertyEditor(configAsset, "Main UI Database");
            Debug.Log("Opened Main Map UI Database: " + assetPath);
        }
        [MenuItem("Vertex Form/Platform Selection", false, 2)]
        public static void OpenPlatformAndSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:Platforms");
            if (guids.Length == 0)
            {
                string path = "Assets/VertexForm3D/ScriptableObjects/Platforms.asset";
                var config = ScriptableObject.CreateInstance<Platforms>();
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.SaveAssets();
                VertexFormEditorHeader.OpenBrandedPropertyEditor(config, "Platforms");
                Debug.Log("Created and opened Platform and Settings: " + path);
                return;
            }
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            Platforms configAsset = AssetDatabase.LoadAssetAtPath<Platforms>(assetPath);
            VertexFormEditorHeader.OpenBrandedPropertyEditor(configAsset, "Platforms");
            Debug.Log("Opened Platform and Settings: " + assetPath);
        }
        [MenuItem("Vertex Form/SettingsUI", false, 3)]
        public static void OpenSettingsUI()
        {
            string[] guids = AssetDatabase.FindAssets("t:SettingsUI");
            if (guids.Length == 0)
            {
                string path = "Assets/VertexForm3D/ScriptableObjects/SettingsUI.asset";
                var config = ScriptableObject.CreateInstance<SettingsUISO>();
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.SaveAssets();
                VertexFormEditorHeader.OpenBrandedPropertyEditor(config, "Settings");
                Debug.Log("Created and opened SettingsUI: " + path);
                return;
            }
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            SettingsUISO configAsset = AssetDatabase.LoadAssetAtPath<SettingsUISO>(assetPath);
            VertexFormEditorHeader.OpenBrandedPropertyEditor(configAsset, "Settings");
            Debug.Log("Opened SettingsUI: " + assetPath);
        }

        [MenuItem("Vertex Form/Creator Toolkit/Favorites", false, 4)]
        public static void OpenFavorites()
        {
            OpenCreatorToolkit("FAVORITES");
        }


        [MenuItem("Vertex Form/Creator Toolkit/XR Game Objects", false, 5)]
        public static void OpenXRGameObjectsTab()
        {
            OpenCreatorToolkit("XR GAME OBJECTS");
        }

        [MenuItem("Vertex Form/Creator Toolkit/Scene Switching", false, 6)]
        public static void OpenSceneSwitchingTab()
        {
            OpenCreatorToolkit("SCENE SWITCHING");
        }


        [MenuItem("Vertex Form/Creator Toolkit/Player Position", false, 7)]
        public static void OpenPlayerPositionTab()
        {
            OpenCreatorToolkit("PLAYER POSITION");
        }

        [MenuItem("Vertex Form/Creator Toolkit/UI Elements", false, 8)]
        public static void OpenUIElementsTab()
        {
            OpenCreatorToolkit("UI ELEMENTS");
        }

        [MenuItem("Vertex Form/Creator Toolkit/Presentation Tools", false, 9)]
        public static void OpenPresentationToolsTab()
        {
            OpenCreatorToolkit("PRESENTATION TOOLS");
        }

        [MenuItem("Vertex Form/Creator Toolkit/Avatars", false, 10)]
        public static void OpenAvatarsTab()
        {
            OpenCreatorToolkit("AVATARS");
        }

        [MenuItem("Vertex Form/Creator Toolkit/Dev Tools", false, 11)]
        public static void OpenDevToolsTab()
        {
            OpenCreatorToolkit("DEV TOOLS");
        }

        private static void OpenCreatorToolkit(string tabName)
        {
            VertexFormToolkit window = EditorWindow.GetWindow<VertexFormToolkit>();
            VertexFormEditorHeader.ApplyWindowTitle(window, "Creator Toolkit");
            window.SelectTab(tabName);
        }

        [MenuItem("Vertex Form/Build Addressables", false, 12)]
        public static void OpenBuildAddressablesWindow()
        {
            AddressablesBuildEditor window = EditorWindow.GetWindow<AddressablesBuildEditor>();
            VertexFormEditorHeader.ApplyWindowTitle(window, "Addressables Management");
            window.minSize = new Vector2(450, 400);
            window.Show();
        }

        [MenuItem("Vertex Form/XR Device Simulator", false, 1000)]
        private static void AddXRDeviceSimulator()
        {
            GameObject g = Object.Instantiate(Resources.Load<GameObject>("CustomEditor/XR Device Simulator"));
            g.name = "XR Device Simulator";
            EditorGUIUtility.PingObject(g);
        }

        [MenuItem("Vertex Form/Help", false, 15)]
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
            if (inputfield.GetComponent<VertexFormTMPInputKeyboardPolicy>() == null)
                inputfield.gameObject.AddComponent<VertexFormTMPInputKeyboardPolicy>();
        }
    }
}
#endif
