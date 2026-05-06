using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VertexFormCore
{
    /// <summary>
    /// Legacy standalone scene database. Prefer editing worlds under Main UI Database → Places panel (UILayoutConfig.worldCategories).
    /// </summary>
    [CreateAssetMenu(menuName = "Data", fileName = "DataBase", order = 1)]
    public class SerializedDataBase : ScriptableObject
    {
        [SerializeField] public List<Category> worldCategories;
    }
}

[System.Serializable]
public class Category
{
    public string categoryName;
    [Tooltip("If false, this category is hidden from the Places tabs (worlds still load for All/Favorites).")]
    public bool showInPlacesNav = true;
    public List<WorldData> environments = new List<WorldData>();
    public Category Clone()
    {
        Category clone = new Category
        {
            categoryName = this.categoryName,
            showInPlacesNav = this.showInPlacesNav,
            environments = new List<WorldData>()
        };

        foreach (var world in this.environments)
        {
            clone.environments.Add(world.Clone()); // Deep copy each WorldData
        }

        return clone;
    }
}
[System.Serializable]
public class WorldData
{
    public string worldName;
    public string worldKey;
    public string worldImagePath;
    [TextArea(3, 10)]
    public string worldDescription;
    public SceneProvider sceneProvider;
    public Sprite worldImage;
    public bool flyMode = true;
    [Header("Platform Supported")]
    public bool Desktop = true;
    public bool VR = true;
    public bool WebGPU = true;
    public bool WebXR = true;
    // Creates a deep copy to ensure no shared references
    public WorldData Clone()
    {
        return new WorldData
        {
            worldName = this.worldName, // String is immutable
            worldKey = this.worldKey, // String is immutable
            worldImagePath = this.worldImagePath, // String is immutable
            worldDescription = this.worldDescription, // String is immutable
            sceneProvider = this.sceneProvider, // Enum is value type
            worldImage = this.worldImage, // Sprite is reference type; assuming read-only usage
            flyMode = this.flyMode, // Bool is value type
            Desktop = this.Desktop,
            VR = this.VR,
            WebGPU = this.WebGPU,
            WebXR = this.WebXR
        };
    }
}
public enum SceneProvider
{
    Local, Remote
}