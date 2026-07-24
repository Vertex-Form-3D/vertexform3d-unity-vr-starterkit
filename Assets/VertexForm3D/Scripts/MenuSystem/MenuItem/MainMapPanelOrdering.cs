using System;
using System.Collections.Generic;
using UnityEngine;

public enum MainMapPanelKind
{
    Main = 0,
    Places = 1,
    Guide = 2,
    Custom = 3
}

public struct MainMapPanelTabEntry
{
    public int listIndex;
    public MainMapPanelKind kind;
    public string panelKey;
    public GameObject tabButton;
    public GameObject screen;
    public bool enabled;
}

/// <summary>
/// Applies bottom-nav tab order from <see cref="UILayoutConfig.mainSectionPanelEntries"/> list order.
/// Home button stays first.
/// </summary>
public static class MainMapPanelOrdering
{
    public static List<MainMapPanelTabEntry> BuildOrderedTabs(MenuManager menuManager, UILayoutConfig config)
    {
        var entries = new List<MainMapPanelTabEntry>();
        if (menuManager == null || config == null || config.mainSectionPanelEntries == null)
            return entries;

        int primaryMainIndex = config.GetPrimaryMainListIndex();
        int primaryPlacesIndex = config.GetPrimaryPlacesListIndex();
        int primaryGuideIndex = config.GetPrimaryGuideListIndex();

        for (int i = 0; i < config.mainSectionPanelEntries.Count; i++)
        {
            var panel = config.mainSectionPanelEntries[i];
            if (panel == null || !panel.enabled)
                continue;

            switch (panel.panelType)
            {
                case MainSectionPanelType.Main:
                    AddTypedPanelEntry(entries, menuManager, panel, i, primaryMainIndex, MainMapPanelKind.Main,
                        MenuManager.GetMainPanelKey(panel, i), menuManager.mainTabButton, menuManager.mainScreen);
                    break;
                case MainSectionPanelType.Places:
                    AddTypedPanelEntry(entries, menuManager, panel, i, primaryPlacesIndex, MainMapPanelKind.Places,
                        MenuManager.GetPlacesPanelKey(panel, i), menuManager.placesTabButton, menuManager.worldScreen);
                    break;
                case MainSectionPanelType.Guide:
                    AddTypedPanelEntry(entries, menuManager, panel, i, primaryGuideIndex, MainMapPanelKind.Guide,
                        MenuManager.GetGuidePanelKey(panel, i), menuManager.guideTabButton, menuManager.GuideScreen);
                    break;
                case MainSectionPanelType.Custom:
                {
                    string panelKey = MenuManager.GetCustomPanelKey(panel);
                    TryFindPanelTabPair(menuManager, panelKey, out GameObject tab, out GameObject screen);
                    entries.Add(new MainMapPanelTabEntry
                    {
                        listIndex = i,
                        kind = MainMapPanelKind.Custom,
                        panelKey = panelKey,
                        tabButton = tab,
                        screen = screen,
                        enabled = panel.uiPrefab != null && tab != null && screen != null
                    });
                    break;
                }
            }
        }

        entries.Sort(CompareEntries);
        return entries;
    }

    static void AddTypedPanelEntry(
        List<MainMapPanelTabEntry> entries,
        MenuManager menuManager,
        MainSectionPanelEntry panel,
        int listIndex,
        int primaryIndex,
        MainMapPanelKind kind,
        string panelKey,
        GameObject primaryTab,
        GameObject primaryScreen)
    {
        if (listIndex == primaryIndex)
        {
            GameObject resolvedScreen = primaryScreen;
            GameObject resolvedTab = primaryTab;
            if (resolvedScreen == null)
                menuManager.TryFindPanelByKey(panelKey, out resolvedScreen, out resolvedTab);
            AddBuiltInEntry(entries, listIndex, kind, panelKey, resolvedTab ?? primaryTab, resolvedScreen);
            return;
        }

        TryFindPanelTabPair(menuManager, panelKey, out GameObject tab, out GameObject screen);
        entries.Add(new MainMapPanelTabEntry
        {
            listIndex = listIndex,
            kind = kind,
            panelKey = panelKey,
            tabButton = tab,
            screen = screen ?? primaryScreen,
            enabled = tab != null && (screen != null || listIndex == primaryIndex)
        });
    }

    public static void ApplyBottomNavSiblingOrder(MenuManager menuManager, UILayoutConfig config)
    {
        if (menuManager == null || config == null)
            return;

        menuManager.ResolveBottomNavReferences();
        Transform navParent = menuManager.bottomNavContent;
        if (navParent == null)
            return;

        int insertIndex = GetFirstPanelTabSiblingIndex(navParent);
        foreach (var entry in BuildOrderedTabs(menuManager, config))
        {
            if (!entry.enabled || entry.tabButton == null)
                continue;

            entry.tabButton.transform.SetSiblingIndex(insertIndex);
            insertIndex++;
        }
    }

    public static GameObject GetFirstEnabledScreen(MenuManager menuManager, UILayoutConfig config)
    {
        foreach (var entry in BuildOrderedTabs(menuManager, config))
        {
            if (entry.enabled && entry.screen != null)
                return entry.screen;
        }
        return null;
    }

    public static int GetFirstEnabledPanelListIndex(UILayoutConfig config)
    {
        if (config?.mainSectionPanelEntries == null)
            return -1;

        for (int i = 0; i < config.mainSectionPanelEntries.Count; i++)
        {
            var panel = config.mainSectionPanelEntries[i];
            if (panel != null && panel.enabled)
                return i;
        }

        return -1;
    }

    static void AddBuiltInEntry(
        List<MainMapPanelTabEntry> entries,
        int listIndex,
        MainMapPanelKind kind,
        string panelKey,
        GameObject tabButton,
        GameObject screen)
    {
        entries.Add(new MainMapPanelTabEntry
        {
            listIndex = listIndex,
            kind = kind,
            panelKey = panelKey,
            tabButton = tabButton,
            screen = screen,
            enabled = tabButton != null && tabButton.activeSelf && screen != null
        });
    }

    static bool TryFindPanelTabPair(MenuManager menuManager, string panelKey, out GameObject tab, out GameObject screen)
    {
        tab = null;
        screen = null;

        var markers = menuManager.GetMenuRoot().GetComponentsInChildren<UILayoutCustomPanelMarker>(true);
        foreach (var marker in markers)
        {
            if (marker == null || marker.panelKey != panelKey)
                continue;
            if (!marker.transform.IsChildOf(menuManager.GetMenuRoot()))
                continue;

            if (marker.isTabButton)
                tab = marker.gameObject;
            else
                screen = marker.gameObject;
        }

        return tab != null || screen != null;
    }

    static int GetFirstPanelTabSiblingIndex(Transform navParent)
    {
        for (int i = 0; i < navParent.childCount; i++)
        {
            if (navParent.GetChild(i).name == "HomeButton")
                return i + 1;
        }
        return 0;
    }

    static int CompareEntries(MainMapPanelTabEntry a, MainMapPanelTabEntry b)
    {
        int order = a.listIndex.CompareTo(b.listIndex);
        if (order != 0)
            return order;

        order = a.kind.CompareTo(b.kind);
        if (order != 0)
            return order;

        return string.Compare(a.panelKey, b.panelKey, StringComparison.Ordinal);
    }
}
