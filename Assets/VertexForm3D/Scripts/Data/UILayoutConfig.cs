using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Main UI Database configuration. Defines the three-section layout (Left, Main, Right),
/// platform selection, and settings UI. Use the custom editor via VertexForm3D SDK menu.
/// </summary>
[CreateAssetMenu(fileName = "Main UI Database", menuName = "VertexForm3D/Main UI Database", order = 0)]
public class UILayoutConfig : ScriptableObject
{
    [Header("Left Section")]
    public bool leftSectionEnabled = true;
    public string leftSectionText = "";
    public List<LeftSectionItem> leftSectionItems = new List<LeftSectionItem>();

    [Header("Main Section (Panels)")]
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
            mainSectionPanels.Add(new MainSectionPanel
            {
                panelName = "Main",
                panelType = MainPanelType.Main
            });
            mainSectionPanels.Add(new MainSectionPanel
            {
                panelName = "Places",
                panelType = MainPanelType.Places
            });
            mainSectionPanels.Add(new MainSectionPanel
            {
                panelName = "Guide",
                panelType = MainPanelType.Guide
            });
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
/// Panel types: Main (branding/background), Places (category toggles), Guide (read-only), Custom.
/// </summary>
[Serializable]
public class MainSectionPanel
{
    public string panelName = "Panel";
    public MainPanelType panelType = MainPanelType.Main;

    [Header("Main panel")]
    public Sprite backgroundImage;

    public Sprite logoImage;

    [Header("Guide / Custom")]
    [TextArea(2, 4)]
    public string guideOrCustomText = "";
}

public enum MainPanelType
{
    Main,
    Places,
    Guide,
    Custom
}
