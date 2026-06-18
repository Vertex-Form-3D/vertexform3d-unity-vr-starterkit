using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Main UI Database configuration. Defines the three-section layout (Left, Main, Right),
/// platform selection, and settings UI. Use the custom editor via VertexForm3D SDK menu.
/// Main section panels are fixed: [0] Main, [1] Places, [2] Guide.
/// </summary>
[CreateAssetMenu(fileName = "Main UI Database", menuName = "VertexForm3D/Main UI Database", order = 0)]
public class UILayoutConfig : ScriptableObject
{
    public const int MainPanelIndex = 0;
    public const int PlacesPanelIndex = 1;
    public const int GuidePanelIndex = 2;

    [Header("Left Section")]
    public bool leftSectionEnabled = true;
    public string leftSectionText = "";
    public List<LeftSectionItem> leftSectionItems = new List<LeftSectionItem>();

    [Header("Main Section (Panels)")]
    [Tooltip("When false, the Main panel (background/logo) is hidden and Places becomes the default tab. Useful if you'd rather drop users straight into Places.")]
    public bool showMainPanel = true;
    public List<MainSectionPanel> mainSectionPanels = new List<MainSectionPanel>();

    /// <summary>
    /// Scene database (places/worlds per category). Edit in Inspector under the Places panel, or here.
    /// Runtime uses this when assigned on MenuManager; falls back to SerializedDataBase if empty.
    /// </summary>
    [Header("Scene Database (Places)")]
    public List<Category> worldCategories = new List<Category>();

    [Header("Right Section")]
    public bool rightSectionEnabled = true;
    public bool mirror = true;
    [Tooltip("When true, the local player's avatar body is visible in first-person mode on Desktop/Mobile (non-VR). When false, the body is hidden so it doesn't clip the camera.")]
    public bool showAvatarBodyInFirstPerson = true;
    public List<AvatarData> avatarDatas = new List<AvatarData>();

    private void Reset()
    {
        if (worldCategories != null && worldCategories.Count == 0)
        {
            worldCategories.Add(new Category { categoryName = "Hubs", showInPlacesNav = true });
            worldCategories.Add(new Category { categoryName = "Geospatial", showInPlacesNav = false });
            worldCategories.Add(new Category { categoryName = "Other", showInPlacesNav = false });
        }
        if (mainSectionPanels != null && mainSectionPanels.Count == 0)
        {
            mainSectionPanels.Add(new MainSectionPanel());
            mainSectionPanels.Add(new MainSectionPanel());
            mainSectionPanels.Add(new MainSectionPanel());
        }
    }
}

[Serializable]
public class LeftSectionItem
{
    public string label = "About";
    public string id = "about";
}

/// <summary>
/// Per-row data: index 0 = branding (background/logo); 1–2 are used by the layout editor only.
/// </summary>
[Serializable]
public class MainSectionPanel
{
    [Header("Main panel (first slot only)")]
    public Sprite backgroundImage;

    public Sprite logoImage;
}
