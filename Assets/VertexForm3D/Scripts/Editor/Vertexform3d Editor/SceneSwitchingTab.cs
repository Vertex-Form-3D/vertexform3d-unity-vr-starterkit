using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace VertexFormCore.Editor
{
    public class SceneSwitchingTab : ITabGroup
    {

        private List<ToolkitItem> items = new List<ToolkitItem>();
        public string Name => "SCENE SWITCHING";

        public string Description => "Tools for scene transitions and teleportation within scenes.";

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

            // Scene Transitions Category Items
            items.Add(new ToolkitItem(
                "Create Scene Switching UI",
                "Adds a \"UI\" Canvas Game Object to Scene, with a button that enables players to switch Scenes.",
                "Enter the exact name of the destination Scene into the NavigationButton Script's SceneName Field.",
                SceneSwitchingUI));

            items.Add(new ToolkitItem(
                "Create Scene Switching Teleport",
                "Adds a \"Scene Switch Teleport\" Game Object to the Scene, that enables players to switch Scenes.",
                "Enter the exact name of the destination Scene into the SceneSwitcherTeleport Script's SceneName Field.",
                SceneSwitchingTeleport));

            items.Add(new ToolkitItem(
                "Make Scene Switching Teleport",
                "Makes the selected Game Object a Teleportable that enables players to switch Scenes.",
                "Enter the exact name of the destination Scene into the SceneSwitcherTeleport Script's SceneName Field.",
                MakeSceneSwitchingTeleport));                       

        }


        #region PORTALS AND TELEPORTS METHODS

        private void SceneSwitchingUI()
        {
            GameObject g = Object.Instantiate(Resources.Load<GameObject>("CustomEditor/SwitchSceneUI"));
            g.name = "SwitchSceneUI";
        }

        private void SceneSwitchingTeleport()
        {
            GameObject g = Object.Instantiate(Resources.Load<GameObject>("CustomEditor/Scene Switcher Teleport"));
            g.name = "SceneSwitcherTeleport";
        }

        private void MakeSceneSwitchingTeleport()
        {
            GameObject[] selectedObject = Selection.gameObjects;
            if (selectedObject.Length > 0)
            {
                foreach (GameObject obj in selectedObject)
                {
                    if (obj.GetComponent<SceneSwitcherTeleport>() == null)
                    {
                        obj.AddComponent<SceneSwitcherTeleport>();
                        Debug.Log("SceneSwitcherTeleport attached to " + obj.name);
                    }
                    else
                    {
                        Debug.LogWarning("Already attached.");
                    }
                    if (obj.GetComponent<Collider>() != null)
                    {
                        obj.GetComponent<Collider>().isTrigger = true;
                    }
                    else
                    {
                        obj.AddComponent<Collider>().isTrigger = true;
                    }
                    EditorSceneManager.MarkSceneDirty(obj.scene);
                }
            }
            else
            {
                Debug.LogWarning("No object selected.");
            }
        }

        #endregion
    }
}