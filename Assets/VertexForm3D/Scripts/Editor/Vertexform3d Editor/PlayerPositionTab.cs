using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VertexFormCore.Editor
{
    public class PlayerPositionTab : ITabGroup
    {
        private List<ToolkitItem> items = new List<ToolkitItem>();
        private const string DEFAULT_CATALOG_PATH = "https://storage.googleapis.com/yourproject_bucket/Android/VertexForm3DAddressablesCatalog.json";

        public string Name => "PLAYER POSITION";

        public string Description => "Tools to manage player spawn points.";

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
    "PlayerSpawnPoint",
    "Add one or more spawn points to control where the player appears in the scene.",
    "The spawn point is created where you are looking in the Scene view, dropped onto the ground, in the scene you currently have active. " +
    "You can create one or several: players take them in turn (player number modulo the number of points), and anyone beyond that is spread around them. " +
    "If a world has no spawn points, players appear at the default position (0, 0, 0). " +
    "If this scene is an Addressable world, rebuild Addressables (or use the \"Use Asset Database\" play mode script) before testing, or the old scene without your spawn points is what loads.",
    CreatePlayerSpawnPoint));

            items.Add(new ToolkitItem(
    "SittingPoint",
    "Add one or more sitting points to define where the player can sit within the scene.",
    "You can create one or multiple sitting points in the scene. These define the exact locations where the player is allowed to sit during gameplay.",
    CreatePlayerSit));

            items.Add(new ToolkitItem(
    "SeatAllChairs",
    "Put a sitting point on every chair, copying the one you set up.",
    "First add ONE SittingPoint to a chair and set its SitPoint (the arrow is the way the player faces when seated). Then press this: every object named \"chair\" " +
    "(or the objects you have selected in the Hierarchy) gets the same sitting point in the same place. Chairs that already have one are skipped. " +
    "Also in the menu: Vertex Form > Seating > Add Sit Spots To All Chairs. Save the scene afterwards (and rebuild Addressables if it is an Addressable world).",
    SeatingTools.AddToAllChairs));

        }

        #region PLAYER POSITION METHODS

        public void CreatePlayerSpawnPoint()
        {
            var prefab = Resources.Load<GameObject>("CustomEditor/PlayerSpawnPoint");
            if (prefab == null)
            {
                Debug.LogError("[SpawnPoint] Could not find Resources/CustomEditor/PlayerSpawnPoint.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("[SpawnPoint] No loaded active scene to put the spawn point in.");
                return;
            }

            GameObject g = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (g == null) g = Object.Instantiate(prefab);
            g.name = "PlayerSpawnPoint";

            // The component RoomManager looks for. Without it the object is just an empty and is ignored.
            if (g.GetComponentInChildren<PlayerSpawnPointScript>(true) == null)
            {
                g.AddComponent<PlayerSpawnPointScript>();
                Debug.LogWarning("[SpawnPoint] The prefab had no PlayerSpawnPointScript; added one.");
            }

            // Where you are looking, dropped onto the ground, turned to face the same way as the Scene view.
            var view = SceneView.lastActiveSceneView;
            if (view != null)
            {
                Vector3 p = view.pivot;
                if (Physics.Raycast(p + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 200f, ~0, QueryTriggerInteraction.Ignore))
                    p = hit.point;
                g.transform.position = p;
                Vector3 fwd = Vector3.ProjectOnPlane(view.camera.transform.forward, Vector3.up);
                if (fwd.sqrMagnitude > 0.0001f) g.transform.rotation = Quaternion.LookRotation(fwd.normalized, Vector3.up);
            }

            Undo.RegisterCreatedObjectUndo(g, "Create PlayerSpawnPoint");
            Selection.activeGameObject = g;
            EditorGUIUtility.PingObject(g);
            EditorSceneManager.MarkSceneDirty(scene);

            int total = Object.FindObjectsByType<PlayerSpawnPointScript>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            string msg = $"[SpawnPoint] Created at {g.transform.position} in scene \"{scene.name}\" ({total} spawn point(s) in the open scene(s)). Save the scene.";

            // An Addressable scene runs from its BUILT bundle, so a new spawn point does not exist in-game until the content is rebuilt.
            string path = scene.path;
            var settings = AddressableAssetSettingsDefaultObject.SettingsExists ? AddressableAssetSettingsDefaultObject.Settings : null;
            if (settings != null && !string.IsNullOrEmpty(path) && settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path)) != null)
                msg += " This scene is an Addressable: rebuild Addressables before testing, or the old bundle (without this spawn point) is what loads.";
            Debug.Log(msg, g);
        }

        public void CreatePlayerSit()
        {
            GameObject g = Object.Instantiate(Resources.Load<GameObject>("CustomEditor/SitSpotPrefab"));
            g.name = "SittingPrefab";
            EditorGUIUtility.PingObject(g);
        }

        #endregion
    }
}
