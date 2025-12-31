using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VertextFormCore;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEditor.SceneManagement;

namespace VertexFormCore.Editor
{
    public class DevToolsTab : ITabGroup
    {
        private List<ToolkitItem> items = new List<ToolkitItem>();

        public string Name => "DEV TOOLS";

        public string Description => "Development Tools helps to improve performance and save time.";

        public bool HasSubTabs => false;

        public List<SubTabCategory> GetSubTabCategories()
        {
            return new List<SubTabCategory>();
        }

        public List<ToolkitItem> GetItems()
        {
            return items;
        }

        public void InitializeItems()
        {
            items.Clear();

            items.Add(new ToolkitItem(
                "Make Login Scene",
                "Make This scene Login scene",
                "Add this scene into buildSettings at first position.",
                MakeThisLoginScene));

            items.Add(new ToolkitItem(
                "Make home Scene",
                "Make This scene home scene",
                "Add this scene into buildSettings at second position.",
                MakeThisHomeScene));

            items.Add(new ToolkitItem(
                "Make Mixed Reality Scene",
                "Make This scene Mixed Reality scene",
                "This block is used to enable mixed reality feature in the addressables scenes. Do not use it in home scene or login scene.",
                MakeThisMixedRealityScene));

            // Updated Project Data item with text that matches the existing outline
            items.Add(new ToolkitItem(
                "Project Data",
                "Opens the Project Data asset",
                "Enable \"Only Local Bundles\" if you are only using local Addressables (built into the app) and no remote Addressables (cloud storage and delivery).",
                SelectProjectData));

            items.Add(new ToolkitItem(
                "Scene Changer",
                "Opens the Scene Changer Window",
                "To change or select scene which are in Build Settings,",
                SceneChangerWindow));

            items.Add(new ToolkitItem(
                "XR Device Simulator",
                "Add XR Device Simulator",
                "To do testing in unity editor",
                AddXRDeviceSimulator));
            
            items.Add(new ToolkitItem(
                "Local XR Rig",
                "Add Local XR Rig",
                "To do testing in unity editor",
                AddLocalXRRig));

            items.Add(new ToolkitItem(
                "Create Network Sync Event",
                "Adds a \"Network Sync Event\" Game Object to the active Scene, to sync events over the multiplayer network.",
                "",
                CreateNetworkSyncEvent));
            
            items.Add(new ToolkitItem(
                "Attach AnimationHandler Script",
                "Animate the object by moving, Rotate and Scale.",
                "",
                AttachAnimationHandlerScript));
            
            items.Add(new ToolkitItem(
                "Attach UIAnimationHandler Script",
                "Animate the UI via Fading, Changing Color and Moving.",
                "",
                AttachUIAnimationHandlerScript));
            
            items.Add(new ToolkitItem(
                "Add Addressable Object Loader",
                "Load SkyBox, Texture, Sprite, Audio, Video via addressables.",
                "",
                AddObjectLoaderViaAddressables));
        }

        #region DEV TOOLS METHODS

        public void CreateNetworkSyncEvent()
        {
            GameObject g = Object.Instantiate(Resources.Load<GameObject>("CustomEditor/SyncEvent"));
            g.name = "SyncEvent";
            EditorGUIUtility.PingObject(g);
        }

        private void SelectProjectData()
        {
            string[] guids = AssetDatabase.FindAssets("t:ProjectDataScriptableObject");
            if (guids.Length == 0)
            {
                Debug.LogError("ProjectDataScriptableObject asset not found in the project!");
                return;
            }

            // Convert the GUID to an asset path and load the asset
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            ProjectDataScriptableObject projectData = AssetDatabase.LoadAssetAtPath<ProjectDataScriptableObject>(path);

            // Select the asset in the Project window
            Selection.activeObject = projectData;
            EditorUtility.FocusProjectWindow(); // Optional: Focus the Project window
            Debug.Log("Selected ProjectDataScriptableObject: " + path);
        }

        private void SceneChangerWindow()
        {
            SceneChanger.Init();
        }

        private void AddXRDeviceSimulator()
        {
            GameObject g = Object.Instantiate(Resources.Load<GameObject>("CustomEditor/XR Device Simulator"));
            g.name = "XR Device Simulator";
            EditorGUIUtility.PingObject(g);
        }

        public void AddLocalXRRig()
        {
            GameObject g = Object.Instantiate(Resources.Load<GameObject>("CustomEditor/Complete XR Origin"));
            g.name = "LocalXRRig";
            EditorGUIUtility.PingObject(g);
        }
        public void MakeThisHomeScene()
        {
            GameObject homescene = GameObject.Find("HomeSceneComponent");
            if (homescene == null)
            {
                GameObject hsc = PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("CustomEditor/HomeSceneComponent")) as GameObject;
                hsc.name = "HomeSceneComponent";
                EditorGUIUtility.PingObject(hsc);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(hsc.scene);
            }
        }

        public void MakeThisLoginScene()
        {
            GameObject loginscene = GameObject.Find("LoginSceneComponent");
            if (loginscene == null)
            {
                GameObject lsc = PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("CustomEditor/LoginSceneComponent")) as GameObject;
                lsc.name = "LoginSceneComponent";
                EditorGUIUtility.PingObject(lsc);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(lsc.scene);
            }
        }

        public void MakeThisMixedRealityScene()
        {
            GameObject MixedRealityScene = GameObject.Find("MixedRealityScene");
            if (MixedRealityScene == null)
            {
                GameObject mrs = PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("CustomEditor/MixedRealityScene")) as GameObject;
                mrs.name = "MixedRealityScene";
                EditorGUIUtility.PingObject(mrs);
                EditorSceneManager.MarkSceneDirty(mrs.scene);
            }
        }

        public void AttachAnimationHandlerScript()
        {
            GameObject[] selectedObject = Selection.gameObjects;
            if (selectedObject.Length > 0)
            {
                foreach (GameObject obj in selectedObject)
                {
                    obj.AddComponent<AnimationHandler>();
                    EditorSceneManager.MarkSceneDirty(obj.scene);
                }
            }
            else
            {
                Debug.LogWarning("No object selected.");
            }
        }

        public void AttachUIAnimationHandlerScript()
        {
            GameObject[] selectedObject = Selection.gameObjects;
            if (selectedObject.Length > 0)
            {
                foreach (GameObject obj in selectedObject)
                {
                    if (obj.GetComponent<RectTransform>()!=null)
                    {
                        obj.AddComponent<UIAnimationHandler>();
                        EditorSceneManager.MarkSceneDirty(obj.scene);
                    }
                }
            }
            else
            {
                Debug.LogWarning("No object selected.");
            }
        }

        public void AddObjectLoaderViaAddressables()
        {
            GameObject a = new GameObject("AddressablesObjectLoader");
            a.AddComponent<AddressableObjectLoader>();
            EditorGUIUtility.PingObject(a);
        }
        #endregion
    }
}