using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEditor.SceneManagement;

namespace VertexFormCore.Editor
{
    public class UIElementsTab : ITabGroup
    {
        private List<ToolkitItem> items = new List<ToolkitItem>();

        public string Name => "UI ELEMENTS";

        public string Description => "Tools for creating and managing UI elements.";

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
                "Create Scene Switching UI",
                "Adds a \"UI\" Canvas Game Object to Scene, with a button that enables players to switch Scenes.",
                "Enter the exact name of the destination Scene into the NavigationButton Script's SceneName Field.",
                SceneSwitchingUI));
            
            items.Add(new ToolkitItem(
                "Attach UI Effect",
                "Select the UI objects on which you want hower effect and press this button.",
                "",
                AttachUIEffect));

            items.Add(new ToolkitItem(
                "Make VR Canvas",
                "Makes the selected Canvas interactable for VR.",
                "",
                MakeCanvasUsableForVR));

            items.Add(new ToolkitItem(
                "Add Slider",
                "Adds a Slider UI element to the selected Canvas or creates a new Canvas.",
                "",
                AddSlider));

            items.Add(new ToolkitItem(
                "Add Dropdown",
                "Adds a Dropdown UI element to the selected Canvas or creates a new Canvas.",
                "",
                AddDropdown));

            items.Add(new ToolkitItem(
                "Add Toggle",
                "Adds a Toggle UI element to the selected Canvas or creates a new Canvas.",
                "",
                AddToggle));

            items.Add(new ToolkitItem(
                "Add Input Field",
                "Adds an Input Field UI element to the selected Canvas or creates a new Canvas.",
                "",
                AddInputField));
        }

        private void MakeCanvasUsableForVR()
        {
            GameObject[] selectedObjects = Selection.gameObjects;
            if (selectedObjects.Length > 0)
            {
                foreach (GameObject obj in selectedObjects)
                {
                    if (obj.GetComponent<Canvas>() != null)
                    {
                        if (obj.GetComponent<TrackedDeviceGraphicRaycaster>() != null)
                        {
                            Debug.LogWarning("TrackedDeviceGraphicRaycaster already attached to " + obj.name);
                        }
                        else
                        {
                            obj.AddComponent<TrackedDeviceGraphicRaycaster>();
                            Debug.Log("TrackedDeviceGraphicRaycaster attached to " + obj.name);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Selected object " + obj.name + " doesn't have a Canvas component.");
                    }
                    EditorSceneManager.MarkSceneDirty(obj.scene);
                }
            }
            else
            {
                Debug.LogWarning("No object selected.");
            }
        }

        private void SceneSwitchingUI()
        {
            GameObject g = Object.Instantiate(Resources.Load<GameObject>("CustomEditor/SwitchSceneUI"));
            g.name = "SwitchSceneUI";
            EditorGUIUtility.PingObject(g);
            EditorSceneManager.MarkSceneDirty(g.scene);
        }

        private void AttachUIEffect()
        {
            GameObject[] selectedObject = Selection.gameObjects;
            if (selectedObject.Length > 0)
            {
                foreach (GameObject obj in selectedObject)
                {
                    if (obj.GetComponent<UIEffect>() == null)
                    {
                        obj.AddComponent<UIEffect>();
                        Debug.Log("UIEffect attached to " + obj.name);
                    }
                    else
                    {
                        Debug.LogWarning("Already attached.");
                    }
                    EditorSceneManager.MarkSceneDirty(obj.scene);
                }
            }
            else
            {
                Debug.LogWarning("No object selected.");
            }
        }

        private GameObject GetOrCreateCanvas()
        {
            GameObject selectedObject = Selection.activeGameObject;
            Canvas canvas = null;
            GameObject selectedObj = null;
            if (selectedObject != null)
            {
                canvas = selectedObject.GetComponent<Canvas>();
                if (canvas == null)
                {
                    canvas = selectedObject.GetComponentInParent<Canvas>();
                    if (canvas != null)
                    {
                        selectedObj = selectedObject;
                    }
                }
            }

            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("Canvas");
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvasObject.transform.localScale = new Vector3(0.0014f, 0.0014f, 0.0014f);

                CanvasScaler canvasScaler = canvasObject.AddComponent<CanvasScaler>();
                canvasScaler.dynamicPixelsPerUnit = 1;
                canvasScaler.referenceResolution = canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(1920, 1080);
                canvasObject.AddComponent<TrackedDeviceGraphicRaycaster>();
                canvasObject.AddComponent<GraphicRaycaster>();

                EditorSceneManager.MarkSceneDirty(canvasObject.scene);
                Debug.Log("Created new WorldSpace Canvas with scale 0.0014f and 1920x1080 resolution");
                return canvasObject;
            }

            return (selectedObj != null) ? selectedObj : canvas.gameObject;
        }

        private void AddSlider()
        {
            GameObject canvas = GetOrCreateCanvas();
            GameObject slider = Object.Instantiate(Resources.Load<GameObject>("CustomEditor/UIElements/Slider"), canvas.transform);
            slider.name = "Slider";
            EditorGUIUtility.PingObject(slider);
            EditorSceneManager.MarkSceneDirty(canvas.scene);
            Debug.Log("Slider added to " + canvas.name);
        }

        private void AddDropdown()
        {
            GameObject canvas = GetOrCreateCanvas();
            GameObject dropdown = Object.Instantiate(Resources.Load<GameObject>("CustomEditor/UIElements/Dropdown"), canvas.transform);
            dropdown.name = "Dropdown";
            EditorGUIUtility.PingObject(dropdown);
            EditorSceneManager.MarkSceneDirty(canvas.scene);
            Debug.Log("Dropdown added to " + canvas.name);
        }

        private void AddToggle()
        {
            GameObject canvas = GetOrCreateCanvas();
            GameObject toggle = Object.Instantiate(Resources.Load<GameObject>("CustomEditor/UIElements/Toggle"), canvas.transform);
            toggle.name = "Toggle";
            EditorGUIUtility.PingObject(toggle);
            EditorSceneManager.MarkSceneDirty(canvas.scene);
            Debug.Log("Toggle added to " + canvas.name);
        }

        private void AddInputField()
        {
            GameObject canvas = GetOrCreateCanvas();
            GameObject inputField = Object.Instantiate(Resources.Load<GameObject>("CustomEditor/UIElements/InputField"), canvas.transform);
            inputField.name = "InputField";
            EditorGUIUtility.PingObject(inputField);
            EditorSceneManager.MarkSceneDirty(canvas.scene);
            Debug.Log("InputField added to " + canvas.name);
        }
    }
}