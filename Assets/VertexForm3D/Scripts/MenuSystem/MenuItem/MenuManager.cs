
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VertexFormCore;
using VertexForm3D.UI;
public class MenuManager : MonoBehaviour
{
    [Header("Built-in screens")]
    public GameObject mainScreen;
    public GameObject worldScreen;
    public GameObject GuideScreen;

    [Header("Bottom navigation")]
    [Tooltip("Parent for menu panel screens (MenuUI Canvas). Custom panel prefabs are instantiated here.")]
    public Transform menuPanelParent;
    [Tooltip("Bottom scroll Content transform where nav tab buttons live.")]
    public Transform bottomNavContent;
    [Tooltip("Template cloned for custom panel tabs. Defaults to Main tab when unset.")]
    public GameObject navTabButtonTemplate;
    [Tooltip("Tab button that opens the Main screen.")]
    public GameObject mainTabButton;
    [Tooltip("Tab button that opens the Places/Worlds screen.")]
    public GameObject placesTabButton;
    [Tooltip("Tab button that opens the Guide screen.")]
    public GameObject guideTabButton;

    readonly List<GameObject> _customScreens = new List<GameObject>();
    readonly List<GameObject> _customTabButtons = new List<GameObject>();
    readonly List<GameObject> _runtimeCustomScreens = new List<GameObject>();
    readonly List<GameObject> _runtimeCustomTabButtons = new List<GameObject>();

    public GameObject[] allScreens;

    [Header("Unsupported Platform Popup")]
    public GameObject platformNotSupportedPopup;
    public TextMeshProUGUI platformNotSupportedText;
    public bool autoClosePopup = true;
    public float autoCloseDelay = 3f;

    public static MenuManager Instance;

    Transform _menuRoot;

    /// <summary>Root of this menu shell (VR MainMap or Desktop MainMap Desktop).</summary>
    public Transform GetMenuRoot()
    {
        if (_menuRoot != null)
            return _menuRoot;

        var mainMap = GetComponentInParent<MainMap>(true);
        if (mainMap != null)
        {
            _menuRoot = mainMap.transform;
            return _menuRoot;
        }

        Transform namedShell = FindNamedMenuShellTransform();
        if (namedShell != null)
        {
            _menuRoot = namedShell;
            return _menuRoot;
        }

        _menuRoot = FindMenuShellFromScreens();
        return _menuRoot;
    }

    Transform FindNamedMenuShellTransform()
    {
        Transform t = transform;
        while (t != null)
        {
            if (t.name == "MainMap" || t.name == "MainMap Desktop")
                return t;
            t = t.parent;
        }

        return null;
    }

    Transform FindMenuShellFromScreens()
    {
        Transform shell = transform;
        Transform t = transform;
        while (t != null)
        {
            if (t.GetComponent<MenuManager>() != null)
                shell = t;
            t = t.parent;
        }

        if (mainScreen != null)
            shell = GetCommonShell(shell, mainScreen.transform);
        if (worldScreen != null)
            shell = GetCommonShell(shell, worldScreen.transform);
        if (GuideScreen != null)
            shell = GetCommonShell(shell, GuideScreen.transform);

        return shell;
    }

    static Transform GetCommonShell(Transform menuBranch, Transform screenBranch)
    {
        Transform candidate = screenBranch;
        while (candidate != null)
        {
            if (menuBranch == candidate || menuBranch.IsChildOf(candidate))
                return candidate;
            candidate = candidate.parent;
        }

        return menuBranch;
    }

    bool BelongsToThisMenu(GameObject go)
    {
        return go != null && go.transform.IsChildOf(GetMenuRoot());
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        ResolveBottomNavReferences();
    }

    public void ResolveBottomNavReferences()
    {
        if (bottomNavContent == null && mainTabButton != null)
            bottomNavContent = mainTabButton.transform.parent;

        if (bottomNavContent == null)
            bottomNavContent = FindBottomNavContentTransform();

        if (bottomNavContent != null)
        {
            if (mainTabButton == null)
            {
                var main = bottomNavContent.Find("Main");
                if (main != null)
                    mainTabButton = main.gameObject;
            }

            if (placesTabButton == null)
            {
                var worlds = bottomNavContent.Find("Worlds");
                if (worlds != null) placesTabButton = worlds.gameObject;
            }
            if (guideTabButton == null)
            {
                var guide = bottomNavContent.Find("Guide");
                if (guide != null) guideTabButton = guide.gameObject;
            }
        }

        if (navTabButtonTemplate == null)
            navTabButtonTemplate = mainTabButton;

        ResolveMenuPanelParent();
    }

    void ResolveMenuPanelParent()
    {
        if (worldScreen != null)
        {
            Transform screenParent = worldScreen.transform.parent;
            if (screenParent != null && screenParent.name == "Panels")
            {
                menuPanelParent = screenParent;
                return;
            }
        }

        if (menuPanelParent != null)
        {
            var panels = menuPanelParent.Find("Panels");
            if (panels != null)
            {
                menuPanelParent = panels;
                return;
            }
        }

        var panelsTransform = transform.Find("MenuUI/Panels");
        if (panelsTransform == null)
            panelsTransform = GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "Panels");

        if (panelsTransform != null)
            menuPanelParent = panelsTransform;
        else if (menuPanelParent == null && worldScreen != null)
            menuPanelParent = worldScreen.transform.parent;
    }

    Transform FindBottomNavContentTransform()
    {
        var scrollRects = GetComponentsInChildren<ScrollRect>(true);
        foreach (var scrollRect in scrollRects)
        {
            if (scrollRect.content != null && LooksLikeBottomNavContent(scrollRect.content))
                return scrollRect.content;
        }

        var transforms = GetComponentsInChildren<Transform>(true);
        foreach (var t in transforms)
        {
            if (t.name == "Content" && LooksLikeBottomNavContent(t))
                return t;
        }

        foreach (var t in transforms)
        {
            if (t.name != "Main")
                continue;

            var parent = t.parent;
            if (parent != null && LooksLikeBottomNavContent(parent))
                return parent;
        }

        return null;
    }

    static bool LooksLikeBottomNavContent(Transform navContent)
    {
        if (navContent == null)
            return false;

        return navContent.Find("Main") != null
            && (navContent.Find("Worlds") != null || navContent.Find("Guide") != null);
    }
    void Start()
    {
        var cfg = GetLayoutConfig();
        if (cfg != null)
            ApplyPanelConfig(cfg);
        else
            ApplyDefaultPanelVisibility();
    }

    UILayoutConfig GetLayoutConfig()
    {
        if (ProjectManager.instance != null && ProjectManager.instance.uiLayoutConfig != null)
            return ProjectManager.instance.uiLayoutConfig;
        var mainMap = FindFirstObjectByType<MainMap>();
        return mainMap != null ? mainMap.Config : null;
    }

    /// <summary>
    /// Applies built-in panel enable flags, tab labels, custom panel prefabs, and bottom-nav buttons from config.
    /// </summary>
    public void ApplyPanelConfig(UILayoutConfig cfg)
    {
        if (cfg == null) return;
        cfg.EnsureDefaultPanelEntries();

        bool runtimeSetup = Application.isPlaying;
        ApplyBuiltInPanelConfig(cfg, wireTabButtons: runtimeSetup);

        if (runtimeSetup)
            ClearRuntimeCustomPanels();

        RegisterBakedCustomPanelsFromHierarchy();
        EnsureRuntimeTypedPanels(cfg);
        EnsureRuntimeCustomPanels(cfg);
        WirePrimaryBuiltInTabs(cfg);

        MainMapPanelOrdering.ApplyBottomNavSiblingOrder(this, cfg);
        if (runtimeSetup)
            OpenFirstEnabledPanel(cfg);
    }

    /// <summary>
    /// Updates built-in tab visibility and labels. Used at runtime and when baking prefabs in the editor.
    /// </summary>
    public void ApplyBuiltInPanelConfig(UILayoutConfig cfg, bool wireTabButtons)
    {
        if (cfg == null) return;

        int primaryMainIndex = cfg.GetPrimaryMainListIndex();
        int primaryPlacesIndex = cfg.GetPrimaryPlacesListIndex();
        int primaryGuideIndex = cfg.GetPrimaryGuideListIndex();
        var primaryMainEntry = primaryMainIndex >= 0 ? cfg.mainSectionPanelEntries[primaryMainIndex] : null;
        var primaryPlacesEntry = primaryPlacesIndex >= 0 ? cfg.mainSectionPanelEntries[primaryPlacesIndex] : null;
        var primaryGuideEntry = primaryGuideIndex >= 0 ? cfg.mainSectionPanelEntries[primaryGuideIndex] : null;

        ConfigureBuiltInMainPanel(primaryMainEntry, primaryMainIndex, wireTabButtons);
        ConfigureBuiltInPlacesPanel(primaryPlacesEntry, primaryPlacesIndex, wireTabButtons);
        ConfigureBuiltInGuidePanel(primaryGuideEntry, primaryGuideIndex, wireTabButtons);
    }

    public static string GetMainPanelKey(MainSectionPanelEntry entry, int listIndex)
    {
        if (entry == null)
            return $"Main:{listIndex}";

        if (!string.IsNullOrWhiteSpace(entry.tabLabel))
            return $"Main:{entry.tabLabel.Trim()}";

        return $"Main:{listIndex}";
    }

    public static string GetGuidePanelKey(MainSectionPanelEntry entry, int listIndex)
    {
        if (entry == null)
            return $"Guide:{listIndex}";

        if (!string.IsNullOrWhiteSpace(entry.tabLabel))
            return $"Guide:{entry.tabLabel.Trim()}";

        return $"Guide:{listIndex}";
    }

    public static string GetPlacesPanelKey(MainSectionPanelEntry entry, int listIndex)
    {
        if (entry == null)
            return $"Places:{listIndex}";

        if (!string.IsNullOrWhiteSpace(entry.tabLabel))
            return $"Places:{entry.tabLabel.Trim()}";

        return $"Places:{listIndex}";
    }

    public static string GetCustomPanelKey(MainSectionPanelEntry entry)
    {
        if (entry == null)
            return string.Empty;
        if (!string.IsNullOrWhiteSpace(entry.tabLabel))
            return entry.tabLabel.Trim();
        if (entry.uiPrefab != null)
            return entry.uiPrefab.name;
        return "CustomPanel";
    }

    /// <summary>Spawned/baked panel GameObject name, e.g. "WorldsScreen", "GeospatialScreen".</summary>
    public static string GetScreenObjectName(UILayoutConfig cfg, MainSectionPanelEntry entry, MainSectionPanelType type)
    {
        string label = cfg != null ? cfg.GetTabLabel(entry) : null;
        if (string.IsNullOrWhiteSpace(label))
        {
            label = type switch
            {
                MainSectionPanelType.Main => "Main",
                MainSectionPanelType.Places => "Worlds",
                MainSectionPanelType.Guide => "Guide",
                _ => "Custom"
            };
        }

        label = label.Replace(" ", string.Empty);
        if (!label.EndsWith("Screen", StringComparison.OrdinalIgnoreCase))
            label += "Screen";
        return label;
    }

    GameObject GetBuiltInScreen(MainSectionPanelType type)
    {
        return type switch
        {
            MainSectionPanelType.Main => mainScreen,
            MainSectionPanelType.Places => worldScreen,
            MainSectionPanelType.Guide => GuideScreen,
            _ => null
        };
    }

    GameObject GetBuiltInTabButton(MainSectionPanelType type)
    {
        return type switch
        {
            MainSectionPanelType.Main => mainTabButton,
            MainSectionPanelType.Places => placesTabButton,
            MainSectionPanelType.Guide => guideTabButton,
            _ => null
        };
    }

    bool HasBuiltInScreen(UILayoutConfig cfg, MainSectionPanelType type, int listIndex)
    {
        if (cfg == null)
            return GetBuiltInScreen(type) != null;

        return type switch
        {
            MainSectionPanelType.Main => listIndex == cfg.GetPrimaryMainListIndex() && mainScreen != null,
            MainSectionPanelType.Places => listIndex == cfg.GetPrimaryPlacesListIndex() && worldScreen != null,
            MainSectionPanelType.Guide => listIndex == cfg.GetPrimaryGuideListIndex() && GuideScreen != null,
            _ => false
        };
    }

    public bool TryFindPanelByKey(string panelKey, out GameObject screen, out GameObject tabButton)
    {
        screen = null;
        tabButton = null;
        if (string.IsNullOrEmpty(panelKey))
            return false;

        var markers = GetMenuRoot().GetComponentsInChildren<UILayoutCustomPanelMarker>(true);
        foreach (var marker in markers)
        {
            if (marker == null || marker.panelKey != panelKey)
                continue;
            if (!BelongsToThisMenu(marker.gameObject))
                continue;

            if (marker.isTabButton)
                tabButton = marker.gameObject;
            else
                screen = marker.gameObject;
        }

        return screen != null || tabButton != null;
    }

    /// <summary>
    /// Finds custom panels/tabs already baked into the prefab hierarchy.
    /// </summary>
    public void RegisterBakedCustomPanelsFromHierarchy()
    {
        _customScreens.Clear();
        _customTabButtons.Clear();

        var markers = GetMenuRoot().GetComponentsInChildren<UILayoutCustomPanelMarker>(true);
        foreach (var marker in markers)
        {
            if (marker == null || !BelongsToThisMenu(marker.gameObject))
                continue;
            if (marker.isTabButton)
            {
                if (!_customTabButtons.Contains(marker.gameObject))
                    _customTabButtons.Add(marker.gameObject);
            }
            else if (!_customScreens.Contains(marker.gameObject))
            {
                _customScreens.Add(marker.gameObject);
            }
        }
    }

    bool HasBakedPanel(string panelKey)
    {
        var markers = GetMenuRoot().GetComponentsInChildren<UILayoutCustomPanelMarker>(true);
        foreach (var marker in markers)
        {
            if (marker == null || marker.panelKey != panelKey || !BelongsToThisMenu(marker.gameObject))
                continue;

            return true;
        }

        return false;
    }

    bool HasBakedCustomPanel(string panelKey) => HasBakedPanel(panelKey);

    void EnsureRuntimeCustomPanels(UILayoutConfig cfg)
    {
        if (!Application.isPlaying || cfg.mainSectionPanelEntries == null)
            return;

        foreach (var customPanel in GetCustomPanelsInListOrder(cfg))
        {
            if (customPanel == null || !customPanel.enabled || customPanel.uiPrefab == null)
                continue;

            if (HasBakedCustomPanel(GetCustomPanelKey(customPanel)))
                continue;

            SpawnSingleRuntimeCustomPanel(customPanel);
        }
    }

    void EnsureRuntimeTypedPanels(UILayoutConfig cfg)
    {
        if (!Application.isPlaying || cfg.mainSectionPanelEntries == null)
            return;

        for (int i = 0; i < cfg.mainSectionPanelEntries.Count; i++)
        {
            var entry = cfg.mainSectionPanelEntries[i];
            if (entry == null || !entry.enabled)
                continue;

            if (entry.panelType != MainSectionPanelType.Main
                && entry.panelType != MainSectionPanelType.Places
                && entry.panelType != MainSectionPanelType.Guide)
                continue;

            if (HasBuiltInScreen(cfg, entry.panelType, i))
                continue;
            if (HasBakedPanel(GetPanelKey(entry, i, entry.panelType)))
                continue;

            SpawnTypedPanel(cfg, entry, i, entry.panelType, useBuiltInTab: IsPrimaryPanel(cfg, entry.panelType, i));
        }
    }

    static bool IsPrimaryPanel(UILayoutConfig cfg, MainSectionPanelType type, int listIndex)
    {
        return type switch
        {
            MainSectionPanelType.Main => listIndex == cfg.GetPrimaryMainListIndex(),
            MainSectionPanelType.Places => listIndex == cfg.GetPrimaryPlacesListIndex(),
            MainSectionPanelType.Guide => listIndex == cfg.GetPrimaryGuideListIndex(),
            _ => false
        };
    }

    void WirePrimaryBuiltInTabs(UILayoutConfig cfg)
    {
        if (cfg?.mainSectionPanelEntries == null || !Application.isPlaying)
            return;

        WirePrimaryBuiltInTab(cfg, MainSectionPanelType.Main, cfg.GetPrimaryMainListIndex());
        WirePrimaryBuiltInTab(cfg, MainSectionPanelType.Places, cfg.GetPrimaryPlacesListIndex());
        WirePrimaryBuiltInTab(cfg, MainSectionPanelType.Guide, cfg.GetPrimaryGuideListIndex());
    }

    void WirePrimaryBuiltInTab(UILayoutConfig cfg, MainSectionPanelType type, int listIndex)
    {
        if (listIndex < 0 || listIndex >= cfg.mainSectionPanelEntries.Count)
            return;

        var entry = cfg.mainSectionPanelEntries[listIndex];
        if (entry == null || !entry.enabled)
            return;

        GameObject tabButton = GetBuiltInTabButton(type);
        if (tabButton == null)
            return;

        GameObject screen = GetBuiltInScreen(type);
        if (screen == null)
            TryFindPanelByKey(GetPanelKey(entry, listIndex, type), out screen, out _);

        if (screen == null)
            return;

        tabButton.SetActive(true);
        SetTabButtonLabel(tabButton, cfg.GetTabLabel(entry));

        var tabLink = tabButton.GetComponent<MainMapPanelTabButton>();
        if (tabLink == null)
            tabLink = tabButton.AddComponent<MainMapPanelTabButton>();

        if (type == MainSectionPanelType.Places)
            tabLink.SetPlacesPanel(listIndex, this, screen);
        else
            tabLink.SetTargets(this, screen);

        AssignBuiltInScreenField(type, screen);
    }

    void SpawnTypedPanel(UILayoutConfig cfg, MainSectionPanelEntry entry, int listIndex, MainSectionPanelType type, bool useBuiltInTab)
    {
        ResolveBottomNavReferences();

        GameObject screenPrefab = cfg.ResolveScreenPrefab(entry, type);
        if (screenPrefab == null)
        {
            string componentName = type == MainSectionPanelType.Main ? nameof(MainScreen) : nameof(WorldScreen);
            Debug.LogWarning($"MenuManager: Cannot spawn {type} panel at index {listIndex} — no prefab with {componentName} component found.");
            return;
        }

        if (screenPrefab.GetComponentInChildren<MenuManager>(true) != null)
        {
            Debug.LogError($"MenuManager: Cannot spawn {type} panel '{GetPanelKey(entry, listIndex, type)}' — screen prefab contains MenuManager.");
            return;
        }

        Transform panelParent = menuPanelParent != null ? menuPanelParent : transform;
        Transform navParent = bottomNavContent != null ? bottomNavContent : mainTabButton?.transform.parent;
        GameObject tabTemplate = navTabButtonTemplate != null ? navTabButtonTemplate : mainTabButton;
        if (navParent == null || tabTemplate == null)
        {
            Debug.LogWarning("MenuManager: Cannot spawn panels — assign bottomNavContent and navTabButtonTemplate.");
            return;
        }

        string panelKey = GetPanelKey(entry, listIndex, type);
        string tabLabel = cfg.GetTabLabel(entry);

        GameObject screen = Instantiate(screenPrefab, panelParent);
        screen.name = GetScreenObjectName(cfg, entry, type);
        screen.SetActive(false);

        if (type == MainSectionPanelType.Main)
            ApplyMainPanelBranding(screen, entry);

        var screenMarker = screen.GetComponent<UILayoutCustomPanelMarker>();
        if (screenMarker == null)
            screenMarker = screen.AddComponent<UILayoutCustomPanelMarker>();
        screenMarker.isTabButton = false;
        screenMarker.panelKey = panelKey;
        screenMarker.sortOrder = listIndex;

        _runtimeCustomScreens.Add(screen);
        _customScreens.Add(screen);

        GameObject tabButton = useBuiltInTab ? GetBuiltInTabButton(type) : null;
        if (tabButton == null)
        {
            tabButton = Instantiate(tabTemplate, navParent);
            tabButton.name = tabLabel;
            tabButton.SetActive(true);
            SetTabButtonLabel(tabButton, tabLabel);

            var tabMarker = tabButton.GetComponent<UILayoutCustomPanelMarker>();
            if (tabMarker == null)
                tabMarker = tabButton.AddComponent<UILayoutCustomPanelMarker>();
            tabMarker.isTabButton = true;
            tabMarker.panelKey = panelKey;
            tabMarker.sortOrder = listIndex;
            tabMarker.linkedScreen = screen;

            _runtimeCustomTabButtons.Add(tabButton);
            _customTabButtons.Add(tabButton);
        }
        else
        {
            tabButton.SetActive(true);
            SetTabButtonLabel(tabButton, tabLabel);
        }

        screenMarker.linkedTab = tabButton;

        var tabLink = tabButton.GetComponent<MainMapPanelTabButton>();
        if (tabLink == null)
            tabLink = tabButton.AddComponent<MainMapPanelTabButton>();

        if (type == MainSectionPanelType.Places)
            tabLink.SetPlacesPanel(listIndex, this, screen);
        else
            tabLink.SetTargets(this, screen);

        if (useBuiltInTab)
            AssignBuiltInScreenField(type, screen);
    }

    void AssignBuiltInScreenField(MainSectionPanelType type, GameObject screen)
    {
        if (screen == null)
            return;

        switch (type)
        {
            case MainSectionPanelType.Main:
                mainScreen = screen;
                break;
            case MainSectionPanelType.Places:
                worldScreen = screen;
                break;
            case MainSectionPanelType.Guide:
                GuideScreen = screen;
                break;
        }
    }

    public static void ApplyMainPanelBranding(GameObject screen, MainSectionPanelEntry entry)
    {
        if (screen == null || entry == null)
            return;

        var images = screen.GetComponentsInChildren<Image>(true);
        foreach (var image in images)
        {
            if (entry.backgroundImage != null && image.name.IndexOf("Background", StringComparison.OrdinalIgnoreCase) >= 0)
                image.sprite = entry.backgroundImage;
            if (entry.logoImage != null && image.name.IndexOf("Logo", StringComparison.OrdinalIgnoreCase) >= 0)
                image.sprite = entry.logoImage;
        }
    }

    static string GetPanelKey(MainSectionPanelEntry entry, int listIndex, MainSectionPanelType type)
    {
        return type switch
        {
            MainSectionPanelType.Main => GetMainPanelKey(entry, listIndex),
            MainSectionPanelType.Places => GetPlacesPanelKey(entry, listIndex),
            MainSectionPanelType.Guide => GetGuidePanelKey(entry, listIndex),
            _ => GetCustomPanelKey(entry)
        };
    }

    static List<MainSectionPanelEntry> GetCustomPanelsInListOrder(UILayoutConfig cfg)
    {
        var customPanels = new List<MainSectionPanelEntry>();
        if (cfg.mainSectionPanelEntries == null)
            return customPanels;

        foreach (var entry in cfg.mainSectionPanelEntries)
        {
            if (entry != null && entry.panelType == MainSectionPanelType.Custom)
                customPanels.Add(entry);
        }

        return customPanels;
    }

    static int GetPanelListIndex(MainSectionPanelEntry entry)
    {
        if (entry == null)
            return 0;

        var cfg = ProjectManager.instance != null ? ProjectManager.instance.uiLayoutConfig : null;
        if (cfg == null || cfg.mainSectionPanelEntries == null)
            return 0;

        int index = cfg.mainSectionPanelEntries.IndexOf(entry);
        return index >= 0 ? index : 0;
    }

    void ApplyDefaultPanelVisibility()
    {
        ConfigureBuiltInPanel(UILayoutConfig.MainPanelIndex, mainScreen, mainTabButton, true, "Main", true);
        ConfigureBuiltInPanel(UILayoutConfig.PlacesPanelIndex, worldScreen, placesTabButton, true, "Worlds", true);
        ConfigureBuiltInPanel(UILayoutConfig.GuidePanelIndex, GuideScreen, guideTabButton, true, "Guide", true);
    }

    void ConfigureBuiltInMainPanel(MainSectionPanelEntry entry, int listIndex, bool wireTabButtons)
    {
        bool enabled = entry != null && entry.enabled;
        var cfg = GetLayoutConfig();
        string tabLabel = cfg != null ? cfg.GetTabLabel(entry) : "Main";

        if (wireTabButtons && mainScreen != null)
            mainScreen.SetActive(false);

        if (mainTabButton == null)
            return;

        mainTabButton.SetActive(enabled);
        SetTabButtonLabel(mainTabButton, tabLabel);

        if (!enabled || !wireTabButtons || mainScreen == null)
            return;

        var tabLink = mainTabButton.GetComponent<MainMapPanelTabButton>();
        if (tabLink == null)
            tabLink = mainTabButton.AddComponent<MainMapPanelTabButton>();
        tabLink.SetTargets(this, mainScreen);
    }

    void ConfigureBuiltInPlacesPanel(MainSectionPanelEntry entry, int listIndex, bool wireTabButtons)
    {
        bool enabled = entry != null && entry.enabled;
        var cfg = GetLayoutConfig();
        string tabLabel = cfg != null ? cfg.GetTabLabel(entry) : "Worlds";

        if (wireTabButtons && worldScreen != null)
            worldScreen.SetActive(false);

        if (placesTabButton == null)
            return;

        placesTabButton.SetActive(enabled);
        SetTabButtonLabel(placesTabButton, tabLabel);

        if (!enabled || !wireTabButtons)
            return;

        if (worldScreen == null)
            return;

        var tabLink = placesTabButton.GetComponent<MainMapPanelTabButton>();
        if (tabLink == null)
            tabLink = placesTabButton.AddComponent<MainMapPanelTabButton>();

        if (listIndex >= 0)
            tabLink.SetPlacesPanel(listIndex, this, worldScreen);
        else
            tabLink.SetTargets(this, worldScreen);
    }

    void ConfigureBuiltInGuidePanel(MainSectionPanelEntry entry, int listIndex, bool wireTabButtons)
    {
        bool enabled = entry != null && entry.enabled;
        var cfg = GetLayoutConfig();
        string tabLabel = cfg != null ? cfg.GetTabLabel(entry) : "Guide";

        if (wireTabButtons && GuideScreen != null)
            GuideScreen.SetActive(false);

        if (guideTabButton == null)
            return;

        guideTabButton.SetActive(enabled);
        SetTabButtonLabel(guideTabButton, tabLabel);

        if (!enabled || !wireTabButtons || GuideScreen == null)
            return;

        var tabLink = guideTabButton.GetComponent<MainMapPanelTabButton>();
        if (tabLink == null)
            tabLink = guideTabButton.AddComponent<MainMapPanelTabButton>();
        tabLink.SetTargets(this, GuideScreen);
    }

    void ConfigureBuiltInPanel(int panelIndex, GameObject screen, GameObject tabButton, bool enabled, string tabLabel, bool wireTabButtons)
    {
        if (wireTabButtons && screen != null)
            screen.SetActive(false);

        if (tabButton == null)
            return;

        tabButton.SetActive(enabled);
        SetTabButtonLabel(tabButton, tabLabel);

        if (screen == null || !enabled || !wireTabButtons)
            return;

        var button = tabButton.GetComponent<Button>();
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => HandleScreen(screen));
    }

    void SpawnSingleRuntimeCustomPanel(MainSectionPanelEntry customPanel)
    {
        ResolveBottomNavReferences();

        if (customPanel.uiPrefab.GetComponentInChildren<MenuManager>(true) != null)
        {
            Debug.LogError($"MenuManager: Cannot spawn custom panel '{GetCustomPanelKey(customPanel)}' — UI Prefab contains MenuManager. Use a separate panel prefab, not MainMap/MainMap Desktop.");
            return;
        }

        Transform panelParent = menuPanelParent != null ? menuPanelParent : transform;
        Transform navParent = bottomNavContent != null ? bottomNavContent : mainTabButton?.transform.parent;
        GameObject tabTemplate = navTabButtonTemplate != null ? navTabButtonTemplate : mainTabButton;

        if (navParent == null || tabTemplate == null)
        {
            Debug.LogWarning("MenuManager: Cannot spawn custom panels — assign bottomNavContent and navTabButtonTemplate (or mainTabButton).");
            return;
        }

        string panelKey = GetCustomPanelKey(customPanel);
        string tabLabel = string.IsNullOrWhiteSpace(customPanel.tabLabel) ? customPanel.uiPrefab.name : customPanel.tabLabel;
        var cfg = GetLayoutConfig();

        GameObject screen = Instantiate(customPanel.uiPrefab, panelParent);
        screen.name = cfg != null
            ? GetScreenObjectName(cfg, customPanel, MainSectionPanelType.Custom)
            : tabLabel.Replace(" ", string.Empty) + "Screen";
        screen.SetActive(false);

        var screenMarker = screen.GetComponent<UILayoutCustomPanelMarker>();
        if (screenMarker == null)
            screenMarker = screen.AddComponent<UILayoutCustomPanelMarker>();
        screenMarker.isTabButton = false;
        screenMarker.panelKey = panelKey;
        screenMarker.sortOrder = GetPanelListIndex(customPanel);

        _runtimeCustomScreens.Add(screen);
        _customScreens.Add(screen);

        GameObject tabButton = Instantiate(tabTemplate, navParent);
        tabButton.name = tabLabel;
        tabButton.SetActive(true);
        SetTabButtonLabel(tabButton, tabLabel);

        var tabMarker = tabButton.GetComponent<UILayoutCustomPanelMarker>();
        if (tabMarker == null)
            tabMarker = tabButton.AddComponent<UILayoutCustomPanelMarker>();
        tabMarker.isTabButton = true;
        tabMarker.panelKey = panelKey;
        tabMarker.sortOrder = GetPanelListIndex(customPanel);
        tabMarker.linkedScreen = screen;
        screenMarker.linkedTab = tabButton;

        _runtimeCustomTabButtons.Add(tabButton);
        _customTabButtons.Add(tabButton);

        var tabLink = tabButton.GetComponent<MainMapPanelTabButton>();
        if (tabLink == null)
            tabLink = tabButton.AddComponent<MainMapPanelTabButton>();
        tabLink.SetTargets(this, screen);
    }

    void ClearRuntimeCustomPanels()
    {
        foreach (var screen in _runtimeCustomScreens)
        {
            if (screen != null)
                Destroy(screen);
        }
        foreach (var tab in _runtimeCustomTabButtons)
        {
            if (tab != null)
                Destroy(tab);
        }
        _runtimeCustomScreens.Clear();
        _runtimeCustomTabButtons.Clear();
    }

    List<GameObject> CollectAllPanelScreens()
    {
        var screens = new List<GameObject>();

        AddPanelScreen(screens, mainScreen);
        AddPanelScreen(screens, worldScreen);
        AddPanelScreen(screens, GuideScreen);

        if (allScreens != null)
        {
            foreach (var screen in allScreens)
                AddPanelScreen(screens, screen);
        }

        foreach (var screen in _customScreens)
            AddPanelScreen(screens, screen);

        var cfg = GetLayoutConfig();
        if (cfg != null)
        {
            foreach (var entry in MainMapPanelOrdering.BuildOrderedTabs(this, cfg))
                AddPanelScreen(screens, entry.screen);
        }

        return screens;
    }

    static void AddPanelScreen(List<GameObject> screens, GameObject screen)
    {
        if (screen != null && !screens.Contains(screen))
            screens.Add(screen);
    }

    GameObject ResolvePrimaryPlacesScreen()
    {
        if (worldScreen != null)
            return worldScreen;

        var cfg = GetLayoutConfig();
        if (cfg == null)
            return null;

        int primaryPlaces = cfg.GetPrimaryPlacesListIndex();
        if (primaryPlaces < 0 || cfg.mainSectionPanelEntries == null || primaryPlaces >= cfg.mainSectionPanelEntries.Count)
            return null;

        var entry = cfg.mainSectionPanelEntries[primaryPlaces];
        if (entry == null)
            return null;

        TryFindPanelByKey(GetPlacesPanelKey(entry, primaryPlaces), out GameObject screen, out _);
        return screen;
    }

    void OpenFirstEnabledPanel(UILayoutConfig cfg)
    {
        GameObject firstScreen = MainMapPanelOrdering.GetFirstEnabledScreen(this, cfg);
        if (firstScreen != null)
        {
            HandleScreen(firstScreen);
            firstScreen.GetComponent<WorldScreen>()?.OnPanelOpened();
        }
    }

    static void SetTabButtonLabel(GameObject tabButton, string label)
    {
        if (tabButton == null || string.IsNullOrWhiteSpace(label))
            return;

        var text = tabButton.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
            text.text = label;
    }

    public void OnTapHome()
    {
        if (VirtualRoomManager.Instance != null)
            VirtualRoomManager.Instance.LeaveRoomAndLoadHomeScene();
    }
    public void OpenMainScreen()
    {
        GameObject screen = ResolvePrimaryMainScreen();
        if (screen == null)
            return;

        HandleScreen(screen);
    }

    GameObject ResolvePrimaryMainScreen()
    {
        if (mainScreen != null)
            return mainScreen;

        var cfg = GetLayoutConfig();
        if (cfg == null)
            return null;

        int primaryMain = cfg.GetPrimaryMainListIndex();
        if (primaryMain < 0 || cfg.mainSectionPanelEntries == null || primaryMain >= cfg.mainSectionPanelEntries.Count)
            return null;

        var entry = cfg.mainSectionPanelEntries[primaryMain];
        if (entry == null)
            return null;

        TryFindPanelByKey(GetMainPanelKey(entry, primaryMain), out GameObject screen, out _);
        return screen;
    }

    public void OpenWorldScreen()
    {
        var screen = ResolvePrimaryPlacesScreen();
        if (screen == null)
            return;

        HandleScreen(screen);
        screen.GetComponent<WorldScreen>()?.OnPanelOpened();
    }

    public void OpenGuideScreen()
    {
        HandleScreen(GuideScreen);
    }
    public void ShowUnsupportedPlatformPopup(WorldData wd)
    {
        if (platformNotSupportedPopup == null) return;
        var pl = ProjectManager.instance.platforms;
        string currentPlatform = GetCurrentPlatformDisplayName(pl);
        var supported = new List<string>();
        if (wd.Desktop) supported.Add("Desktop");
        if (wd.VR) supported.Add("VR");
        if (wd.WebGPU) supported.Add("Web");
        if (wd.WebXR) supported.Add("WebXR");
        if (wd.Mobile) supported.Add("WebXR/Mobile");
        string supportedList = supported.Count > 0 ? string.Join(", ", supported) : "None";
        if (platformNotSupportedText != null)
            platformNotSupportedText.text = $"Not available on {currentPlatform}.\nPlease use supported platforms that are checked in Platform Supported in world:\n{supportedList}";
        platformNotSupportedPopup.SetActive(true);
        if (autoClosePopup)
        {
            CancelInvoke(nameof(CloseUnsupportedPlatformPopup));
            Invoke(nameof(CloseUnsupportedPlatformPopup), autoCloseDelay);
        }
    }

    public void CloseUnsupportedPlatformPopup()
    {
        if (platformNotSupportedPopup != null)
            platformNotSupportedPopup.SetActive(false);
    }

    string GetCurrentPlatformDisplayName(Platforms pl)
    {
        if (pl.webGpuBrowserKind == WebGpuBrowserKind.WebXRBrowser) return "WebXR";
        if (pl.webGpuBrowserKind == WebGpuBrowserKind.MobileBrowser) return "WebXR/Mobile";
        return pl.platformChoice switch
        {
            platform.VR => "VR",
            platform.Desktop => "Desktop",
            platform.Web => "Web",
            _ => pl.platformChoice.ToString()
        };
    }
    public void HandleScreen(GameObject screen)
    {
        if (screen == null)
            return;

        foreach (var panelScreen in CollectAllPanelScreens())
        {
            if (panelScreen != null && panelScreen != screen)
                panelScreen.SetActive(false);
        }

        screen.SetActive(true);
    }
}
