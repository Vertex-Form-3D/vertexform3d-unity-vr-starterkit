using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VertexFormCore.Editor
{
    public class PlayerPositionTab : ITabGroup
    {
        private List<ToolkitItem> items = new List<ToolkitItem>();
        private const string DEFAULT_CATALOG_PATH = "https://storage.googleapis.com/yourproject_bucket/Android/VertexForm3DAddressablesCatalog.json";

        public string Name => "PLAYER POSITION";

        public string Description => "Tools to manageplayer spawnpoints.";

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
    "You can create one or multiple spawn points. If multiple are present, the player will spawn at one randomly. If no spawn points are set, the player will spawn at the default position (0, 0, 0).",
    CreatePlayerSpawnPoint));
            
            items.Add(new ToolkitItem(
    "SittingPoint",
    "Add one or more sitting points to define where the player can sit within the scene.",
    "You can create one or multiple sitting points in the scene. These define the exact locations where the player is allowed to sit during gameplay.",
    CreatePlayerSit));

        }

        #region PLAYER POSITION METHODS

        public void CreatePlayerSpawnPoint()
        {
            GameObject g = Object.Instantiate(Resources.Load<GameObject>("CustomEditor/PlayerSpawnPoint"));
            g.name = "PlayerspwanPoint";
            EditorGUIUtility.PingObject(g);
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