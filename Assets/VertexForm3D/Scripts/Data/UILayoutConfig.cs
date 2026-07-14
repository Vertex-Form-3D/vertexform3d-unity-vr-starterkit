using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Main UI Database configuration. Defines the three-section layout (Left, Main, Right),
/// platform selection, and settings UI. Use the custom editor via VertexForm3D SDK menu.
/// Main section panels are defined as an ordered list (drag to reorder in the editor).
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

    [Header("Main Section")]
    [Tooltip("Ordered list of main-section panels. List order determines bottom-nav tab order (after Home).")]
    public List<MainSectionPanelEntry> mainSectionPanelEntries = new List<MainSectionPanelEntry>();

    [HideInInspector][SerializeField] GameObject _cachedMainScreenPrefab;
    [HideInInspector][SerializeField] GameObject _cachedWorldScreenPrefab;

    /// <summary>
    /// Scene database (places/worlds per category). Edit under a Places panel entry, or here.
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

    // Legacy fields kept for asset migration in the custom editor.
    [HideInInspector] public bool showMainPanel = true;
    [HideInInspector] public bool placesPanelEnabled = true;
    [HideInInspector] public bool guidePanelEnabled = true;
    [HideInInspector] public string mainTabLabel = "Main";
    [HideInInspector] public int mainPanelSortOrder = 10;
    [HideInInspector] public string placesTabLabel = "Worlds";
    [HideInInspector] public int placesPanelSortOrder = 20;
    [HideInInspector] public string guideTabLabel = "Guide";
    [HideInInspector] public int guidePanelSortOrder = 30;
    [HideInInspector] public List<MainSectionPanelBranding> mainSectionPanels = new List<MainSectionPanelBranding>();
    [HideInInspector] public List<CustomMainPanel> customMainPanels = new List<CustomMainPanel>();

    public MainSectionPanelEntry GetFirstPanelOfType(MainSectionPanelType type)
    {
        if (mainSectionPanelEntries == null)
            return null;

        foreach (var entry in mainSectionPanelEntries)
        {
            if (entry != null && entry.panelType == type)
                return entry;
        }

        return null;
    }

    public MainSectionPanelEntry GetFirstEnabledPanelOfType(MainSectionPanelType type)
    {
        if (mainSectionPanelEntries == null)
            return null;

        foreach (var entry in mainSectionPanelEntries)
        {
            if (entry != null && entry.panelType == type && entry.enabled)
                return entry;
        }

        return null;
    }

    public string GetTabLabel(MainSectionPanelEntry entry)
    {
        if (entry == null)
            return "Panel";

        if (!string.IsNullOrWhiteSpace(entry.tabLabel))
            return entry.tabLabel.Trim();

        return entry.panelType switch
        {
            MainSectionPanelType.Main => "Main",
            MainSectionPanelType.Places => "Worlds",
            MainSectionPanelType.Guide => "Guide",
            MainSectionPanelType.Custom => entry.uiPrefab != null ? entry.uiPrefab.name : "Custom",
            _ => "Panel"
        };
    }

    public bool IsBuiltInPanelEnabled(int index)
    {
        var type = index switch
        {
            MainPanelIndex => MainSectionPanelType.Main,
            PlacesPanelIndex => MainSectionPanelType.Places,
            GuidePanelIndex => MainSectionPanelType.Guide,
            _ => (MainSectionPanelType)(-1)
        };

        if ((int)type < 0)
            return false;

        var entry = GetFirstEnabledPanelOfType(type);
        return entry != null;
    }

    public string GetBuiltInTabLabel(int index)
    {
        var type = index switch
        {
            MainPanelIndex => MainSectionPanelType.Main,
            PlacesPanelIndex => MainSectionPanelType.Places,
            GuidePanelIndex => MainSectionPanelType.Guide,
            _ => (MainSectionPanelType)(-1)
        };

        if ((int)type < 0)
            return "Panel";

        var entry = GetFirstPanelOfType(type);
        return GetTabLabel(entry);
    }

    public int GetPrimaryPanelListIndex(MainSectionPanelType type)
    {
        if (mainSectionPanelEntries == null)
            return -1;

        for (int i = 0; i < mainSectionPanelEntries.Count; i++)
        {
            var entry = mainSectionPanelEntries[i];
            if (entry != null && entry.panelType == type)
                return i;
        }

        return -1;
    }

    public int GetPrimaryMainListIndex() => GetPrimaryPanelListIndex(MainSectionPanelType.Main);

    public int GetPrimaryPlacesListIndex() => GetPrimaryPanelListIndex(MainSectionPanelType.Places);

    public int GetPrimaryGuideListIndex() => GetPrimaryPanelListIndex(MainSectionPanelType.Guide);

    public MainSectionPanelEntry GetPlacesEntryAt(int listIndex)
    {
        if (mainSectionPanelEntries == null || listIndex < 0 || listIndex >= mainSectionPanelEntries.Count)
            return null;

        var entry = mainSectionPanelEntries[listIndex];
        return entry != null && entry.panelType == MainSectionPanelType.Places ? entry : null;
    }

    public GameObject ResolveScreenPrefab(MainSectionPanelEntry entry, MainSectionPanelType type)
    {
        return type switch
        {
            MainSectionPanelType.Main => GetMainScreenPrefab(),
            MainSectionPanelType.Places => GetWorldScreenPrefab(),
            MainSectionPanelType.Custom => entry != null ? entry.uiPrefab : null,
            MainSectionPanelType.Guide => entry != null ? entry.uiPrefab : null,
            _ => null
        };
    }

    public GameObject GetMainScreenPrefab() =>
        PanelScreenPrefabUtility.GetMainScreenPrefab() ?? _cachedMainScreenPrefab;

    public GameObject GetWorldScreenPrefab() =>
        PanelScreenPrefabUtility.GetWorldScreenPrefab() ?? _cachedWorldScreenPrefab;

    public void RefreshScreenPrefabCache()
    {
#if UNITY_EDITOR
        _cachedMainScreenPrefab = PanelScreenPrefabUtility.FindPrefabWithComponentOnRoot<MainScreen>();
        _cachedWorldScreenPrefab = PanelScreenPrefabUtility.FindPrefabWithComponentOnRoot<WorldScreen>();
        PanelScreenPrefabUtility.ClearCache();
#endif
    }

    public void EnsureDefaultPanelEntries()
    {
        if (mainSectionPanelEntries != null && mainSectionPanelEntries.Count > 0)
            return;

        mainSectionPanelEntries = new List<MainSectionPanelEntry>
        {
            new MainSectionPanelEntry
            {
                panelType = MainSectionPanelType.Main,
                tabLabel = "Main",
                enabled = true
            },
            new MainSectionPanelEntry
            {
                panelType = MainSectionPanelType.Places,
                tabLabel = "Worlds",
                enabled = true,
                // Prefer root legacy categories (scenes) over empty defaults when upgrading old assets.
                worldCategories = worldCategories != null && worldCategories.Count > 0
                    ? new List<Category>(worldCategories)
                    : CreateDefaultWorldCategories()
            },
            new MainSectionPanelEntry
            {
                panelType = MainSectionPanelType.Guide,
                tabLabel = "Guide",
                enabled = true
            }
        };
    }

    static List<Category> CreateDefaultWorldCategories()
    {
        return new List<Category>
        {
            new Category { categoryName = "Hubs", showInPlacesNav = true },
            new Category { categoryName = "Geospatial", showInPlacesNav = false },
            new Category { categoryName = "Other", showInPlacesNav = false }
        };
    }

    private void Reset()
    {
        EnsureDefaultPanelEntries();
    }

    public void EnsureBuiltInPanelSlots()
    {
        EnsureDefaultPanelEntries();
    }
}

public enum MainSectionPanelType
{
    Main = 0,
    Places = 1,
    Guide = 2,
    Custom = 3
}

[Serializable]
public class LeftSectionItem
{
    public string label = "About";
    public string id = "about";
}

[Serializable]
public class MainSectionPanelEntry
{
    public MainSectionPanelType panelType = MainSectionPanelType.Custom;
    public bool enabled = true;
    public string tabLabel = "Custom";

    [Tooltip("Main panel branding (applied to the built-in main screen for the first Main panel, or to UI Prefab images when set).")]
    public Sprite backgroundImage;
    public Sprite logoImage;

    [Tooltip("Optional override prefab. When empty, Main/Places panels use the screen prefabs on UILayoutConfig.")]
    public GameObject uiPrefab;

    [Tooltip("World categories shown when this Places panel tab is active.")]
    public List<Category> worldCategories = new List<Category>();
}

/// <summary>Legacy branding row used only for migrating older assets.</summary>
[Serializable]
public class MainSectionPanelBranding
{
    public Sprite backgroundImage;
    public Sprite logoImage;
}

/// <summary>Legacy custom panel row used only for migrating older assets.</summary>
[Serializable]
public class CustomMainPanel
{
    public int sortOrder = 100;
    public bool enabled = true;
    public string tabLabel = "Custom";
    public GameObject uiPrefab;
}
[Serializable]
public class AvatarData
{
    public GameObject head;
    public GameObject body;
}
