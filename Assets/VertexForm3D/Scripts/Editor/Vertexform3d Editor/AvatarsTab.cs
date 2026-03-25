using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VertexFormCore.Editor
{
    public class AvatarsTab : ITabGroup
    {
        private const string AvatarSelection3DResourcePath = "CustomEditor/AvatarSelection3D";

        private List<ToolkitItem> items = new List<ToolkitItem>();

        public string Name => "AVATARS";

        public string Description => "Tools for avatar setup and in-scene avatar selection.";

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
                "Avatar Selection 3D\n(Home Scene Only)",
                "Home scene only: Spawns the 3D avatar selection rig from Resources into the active scene. Wire references in the inspector if needed after placement.",
                "",
                SpawnAvatarSelection3D));
        }

        private static void SpawnAvatarSelection3D()
        {
            GameObject prefab = Resources.Load<GameObject>(AvatarSelection3DResourcePath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog(
                    "Avatar Selection 3D",
                    $"Could not load prefab at Resources path \"{AvatarSelection3DResourcePath}\". Ensure AvatarSelection3D.prefab exists under a Resources folder.",
                    "OK");
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "AvatarSelection3D";
            Undo.RegisterCreatedObjectUndo(instance, "Spawn Avatar Selection 3D");
            Selection.activeGameObject = instance;
            EditorGUIUtility.PingObject(instance);
        }
    }
}
