using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TMPro;

/// <summary>
/// Bakes UILayoutConfig panels and built-in tab settings into MainMap / MainMap Desktop prefabs via MenuManager.
/// </summary>
public static class MenuManagerPanelPrefabBaker
{
    static readonly string[] MenuPrefabPaths =
    {
        "Assets/VertexForm3D/Prefabs/UI Prefabs/MainMap.prefab",
        "Assets/VertexForm3D/Prefabs/UI Prefabs/MainMap Desktop.prefab"
    };

    public static IReadOnlyList<string> TargetMenuPrefabPaths => MenuPrefabPaths;

    public static int Bake(MenuManager menuManager, UILayoutConfig config, string hostPrefabAssetPath)
    {
        if (menuManager == null || config == null)
            return 0;

        menuManager.ResolveBottomNavReferences();
        ClearBakedCustomPanels(menuManager);

        menuManager.ApplyBuiltInPanelConfig(config, wireTabButtons: false);

        int added = 0;
        if (config.mainSectionPanelEntries == null || config.mainSectionPanelEntries.Count == 0)
            return added;

        Transform panelParent = menuManager.menuPanelParent != null
            ? menuManager.menuPanelParent
            : menuManager.transform;
        Transform navParent = menuManager.bottomNavContent != null
            ? menuManager.bottomNavContent
            : menuManager.mainTabButton != null ? menuManager.mainTabButton.transform.parent : null;
        GameObject tabTemplate = menuManager.navTabButtonTemplate != null
            ? menuManager.navTabButtonTemplate
            : menuManager.mainTabButton;

        if (navParent == null || tabTemplate == null)
        {
            Debug.LogWarning(
                $"MenuManagerPanelPrefabBaker: Cannot bake panels on '{hostPrefabAssetPath}' — " +
                "could not resolve bottom nav (Content with Main/Worlds/Guide tabs and a Main tab template).");
            return added;
        }

        var usedPanelKeys = new HashSet<string>();
        int primaryMainIndex = config.GetPrimaryMainListIndex();
        int primaryPlacesIndex = config.GetPrimaryPlacesListIndex();
        int primaryGuideIndex = config.GetPrimaryGuideListIndex();

        for (int i = 0; i < config.mainSectionPanelEntries.Count; i++)
        {
            var panelEntry = config.mainSectionPanelEntries[i];
            if (panelEntry == null || !panelEntry.enabled)
                continue;

            switch (panelEntry.panelType)
            {
                case MainSectionPanelType.Main:
                    if (menuManager.mainScreen != null && i == primaryMainIndex)
                        continue;
                    added += BakeTypedPanel(menuManager, config, panelEntry, i, MainSectionPanelType.Main,
                        panelParent, navParent, tabTemplate, usedPanelKeys, hostPrefabAssetPath,
                        useBuiltInTab: i == primaryMainIndex);
                    break;
                case MainSectionPanelType.Places:
                    if (menuManager.worldScreen != null && i == primaryPlacesIndex)
                        continue;
                    added += BakeTypedPanel(menuManager, config, panelEntry, i, MainSectionPanelType.Places,
                        panelParent, navParent, tabTemplate, usedPanelKeys, hostPrefabAssetPath,
                        useBuiltInTab: i == primaryPlacesIndex);
                    break;
                case MainSectionPanelType.Guide:
                    if (menuManager.GuideScreen != null && i == primaryGuideIndex)
                        continue;
                    added += BakeTypedPanel(menuManager, config, panelEntry, i, MainSectionPanelType.Guide,
                        panelParent, navParent, tabTemplate, usedPanelKeys, hostPrefabAssetPath,
                        useBuiltInTab: i == primaryGuideIndex);
                    break;
                case MainSectionPanelType.Custom when panelEntry.uiPrefab != null:
                    added += BakeCustomPanel(menuManager, config, panelEntry, i, panelParent, navParent, tabTemplate, usedPanelKeys, hostPrefabAssetPath);
                    break;
            }
        }

        menuManager.RegisterBakedCustomPanelsFromHierarchy();
        MainMapPanelOrdering.ApplyBottomNavSiblingOrder(menuManager, config);
        EditorUtility.SetDirty(menuManager);
        return added;
    }

    static int BakeTypedPanel(
        MenuManager menuManager,
        UILayoutConfig config,
        MainSectionPanelEntry entry,
        int listIndex,
        MainSectionPanelType type,
        Transform panelParent,
        Transform navParent,
        GameObject tabTemplate,
        HashSet<string> usedPanelKeys,
        string hostPrefabAssetPath,
        bool useBuiltInTab)
    {
        string panelKey = type switch
        {
            MainSectionPanelType.Main => MenuManager.GetMainPanelKey(entry, listIndex),
            MainSectionPanelType.Places => MenuManager.GetPlacesPanelKey(entry, listIndex),
            MainSectionPanelType.Guide => MenuManager.GetGuidePanelKey(entry, listIndex),
            _ => MenuManager.GetCustomPanelKey(entry)
        };

        if (!usedPanelKeys.Add(panelKey))
        {
            Debug.LogWarning($"MenuManagerPanelPrefabBaker: Skipping duplicate {type} panel '{panelKey}' on '{hostPrefabAssetPath}'.");
            return 0;
        }

        GameObject screenPrefab = config.ResolveScreenPrefab(entry, type);
        if (screenPrefab == null)
        {
            string componentName = type == MainSectionPanelType.Main ? nameof(MainScreen) : nameof(WorldScreen);
            Debug.LogWarning($"MenuManagerPanelPrefabBaker: Cannot bake {type} panel '{panelKey}' — no prefab with {componentName} component found.");
            return 0;
        }

        if (WouldCreatePrefabCycle(hostPrefabAssetPath, screenPrefab))
        {
            Debug.LogError($"MenuManagerPanelPrefabBaker: Cannot bake {type} panel '{panelKey}' into '{hostPrefabAssetPath}'. Use a separate screen prefab.");
            return 0;
        }

        string tabLabel = config.GetTabLabel(entry);
        GameObject screen = InstantiatePanelPrefab(screenPrefab, panelParent);
        if (screen == null)
            return 0;

        screen.name = MenuManager.GetScreenObjectName(config, entry, type);
        screen.SetActive(false);

        if (type == MainSectionPanelType.Main)
            MenuManager.ApplyMainPanelBranding(screen, entry);

        var screenMarker = screen.GetComponent<UILayoutCustomPanelMarker>();
        if (screenMarker == null)
            screenMarker = screen.AddComponent<UILayoutCustomPanelMarker>();
        screenMarker.isTabButton = false;
        screenMarker.panelKey = panelKey;
        screenMarker.sortOrder = listIndex;

        GameObject tabButton = useBuiltInTab ? GetBuiltInTabButton(menuManager, type) : null;
        if (tabButton == null)
        {
            tabButton = UnityEngine.Object.Instantiate(tabTemplate, navParent);
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
            tabLink.SetPlacesPanel(listIndex, menuManager, screen);
        else
            tabLink.SetTargets(menuManager, screen);

        return 1;
    }

    static GameObject GetBuiltInTabButton(MenuManager menuManager, MainSectionPanelType type)
    {
        return type switch
        {
            MainSectionPanelType.Main => menuManager.mainTabButton,
            MainSectionPanelType.Places => menuManager.placesTabButton,
            MainSectionPanelType.Guide => menuManager.guideTabButton,
            _ => null
        };
    }

    static int BakeAdditionalTypedPanel(
        MenuManager menuManager,
        UILayoutConfig config,
        MainSectionPanelEntry entry,
        int listIndex,
        MainSectionPanelType type,
        Transform panelParent,
        Transform navParent,
        GameObject tabTemplate,
        HashSet<string> usedPanelKeys,
        string hostPrefabAssetPath)
    {
        return BakeTypedPanel(menuManager, config, entry, listIndex, type, panelParent, navParent, tabTemplate,
            usedPanelKeys, hostPrefabAssetPath, useBuiltInTab: false);
    }

    static int BakeCustomPanel(
        MenuManager menuManager,
        UILayoutConfig config,
        MainSectionPanelEntry customPanel,
        int listIndex,
        Transform panelParent,
        Transform navParent,
        GameObject tabTemplate,
        HashSet<string> usedPanelKeys,
        string hostPrefabAssetPath)
    {
        string panelKey = MenuManager.GetCustomPanelKey(customPanel);
        if (!usedPanelKeys.Add(panelKey))
        {
            Debug.LogWarning($"MenuManagerPanelPrefabBaker: Skipping duplicate custom panel '{panelKey}' on '{hostPrefabAssetPath}'.");
            return 0;
        }

        if (WouldCreatePrefabCycle(hostPrefabAssetPath, customPanel.uiPrefab))
        {
            Debug.LogError(
                $"MenuManagerPanelPrefabBaker: Cannot bake custom panel '{panelKey}' into '{hostPrefabAssetPath}'. " +
                $"The UI Prefab must be a separate panel prefab — do not assign MainMap or MainMap Desktop as the custom panel prefab.");
            return 0;
        }

        string tabLabel = config.GetTabLabel(customPanel);

        GameObject screen = InstantiatePanelPrefab(customPanel.uiPrefab, panelParent);
        if (screen == null)
            return 0;

        screen.name = MenuManager.GetScreenObjectName(config, customPanel, MainSectionPanelType.Custom);
        screen.SetActive(false);

        var screenMarker = screen.GetComponent<UILayoutCustomPanelMarker>();
        if (screenMarker == null)
            screenMarker = screen.AddComponent<UILayoutCustomPanelMarker>();
        screenMarker.isTabButton = false;
        screenMarker.panelKey = panelKey;
        screenMarker.sortOrder = listIndex;

        GameObject tabButton = UnityEngine.Object.Instantiate(tabTemplate, navParent);
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

        screenMarker.linkedTab = tabButton;

        var tabLink = tabButton.GetComponent<MainMapPanelTabButton>();
        if (tabLink == null)
            tabLink = tabButton.AddComponent<MainMapPanelTabButton>();
        tabLink.SetTargets(menuManager, screen);

        return 1;
    }

    public static bool WouldCreatePrefabCycle(string hostPrefabAssetPath, GameObject uiPrefab)
    {
        if (uiPrefab == null || string.IsNullOrEmpty(hostPrefabAssetPath))
            return false;

        string sourcePath = AssetDatabase.GetAssetPath(uiPrefab);
        if (string.IsNullOrEmpty(sourcePath))
            return false;

        if (string.Equals(NormalizeAssetPath(hostPrefabAssetPath), NormalizeAssetPath(sourcePath), StringComparison.OrdinalIgnoreCase))
            return true;

        foreach (string menuPath in MenuPrefabPaths)
        {
            if (string.Equals(NormalizeAssetPath(sourcePath), NormalizeAssetPath(menuPath), StringComparison.OrdinalIgnoreCase)
                && string.Equals(NormalizeAssetPath(hostPrefabAssetPath), NormalizeAssetPath(menuPath), StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public static bool IsMenuShellPrefab(GameObject prefab)
    {
        if (prefab == null) return false;
        string path = AssetDatabase.GetAssetPath(prefab);
        if (string.IsNullOrEmpty(path)) return false;

        foreach (string menuPath in MenuPrefabPaths)
        {
            if (string.Equals(NormalizeAssetPath(path), NormalizeAssetPath(menuPath), StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    static GameObject InstantiatePanelPrefab(GameObject prefab, Transform parent)
    {
        var instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        if (instance == null)
            instance = UnityEngine.Object.Instantiate(prefab, parent);
        return instance;
    }

    static string NormalizeAssetPath(string path) => path?.Replace('\\', '/');

    static void ClearBakedCustomPanels(MenuManager menuManager)
    {
        if (menuManager == null)
            return;

        menuManager.ResolveBottomNavReferences();

        Transform prefabRoot = menuManager.GetMenuRoot();

        var markers = prefabRoot.GetComponentsInChildren<UILayoutCustomPanelMarker>(true);
        for (int i = markers.Length - 1; i >= 0; i--)
        {
            if (markers[i] != null)
                UnityEngine.Object.DestroyImmediate(markers[i].gameObject);
        }

        var tabLinks = prefabRoot.GetComponentsInChildren<MainMapPanelTabButton>(true);
        for (int i = tabLinks.Length - 1; i >= 0; i--)
        {
            if (tabLinks[i] != null && tabLinks[i].gameObject != menuManager.mainTabButton && tabLinks[i].gameObject != menuManager.placesTabButton && tabLinks[i].gameObject != menuManager.guideTabButton)
                UnityEngine.Object.DestroyImmediate(tabLinks[i].gameObject);
        }

        ClearOrphanCustomScreens(menuManager);
        ClearOrphanCustomTabs(menuManager);
    }

    static void ClearOrphanCustomScreens(MenuManager menuManager)
    {
        Transform panelParent = menuManager.menuPanelParent != null
            ? menuManager.menuPanelParent
            : menuManager.transform;

        var keepScreens = new HashSet<GameObject>();
        if (menuManager.mainScreen != null) keepScreens.Add(menuManager.mainScreen);
        if (menuManager.worldScreen != null) keepScreens.Add(menuManager.worldScreen);
        if (menuManager.GuideScreen != null) keepScreens.Add(menuManager.GuideScreen);

        for (int i = panelParent.childCount - 1; i >= 0; i--)
        {
            GameObject child = panelParent.GetChild(i).gameObject;
            if (!keepScreens.Contains(child))
                UnityEngine.Object.DestroyImmediate(child);
        }
    }

    static void ClearOrphanCustomTabs(MenuManager menuManager)
    {
        Transform navParent = menuManager.bottomNavContent;
        if (navParent == null)
            return;

        var keepTabs = new HashSet<GameObject>();
        if (menuManager.mainTabButton != null) keepTabs.Add(menuManager.mainTabButton);
        if (menuManager.placesTabButton != null) keepTabs.Add(menuManager.placesTabButton);
        if (menuManager.guideTabButton != null) keepTabs.Add(menuManager.guideTabButton);

        for (int i = navParent.childCount - 1; i >= 0; i--)
        {
            GameObject child = navParent.GetChild(i).gameObject;
            if (child.name == "HomeButton")
                continue;
            if (!keepTabs.Contains(child))
                UnityEngine.Object.DestroyImmediate(child);
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
}
