using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared platform-support checks for scenes/worlds defined in <see cref="UILayoutConfig"/>.
/// A scene is identified by its <see cref="WorldData.worldKey"/> (the scene name passed to SceneLoader).
/// Navigation entry points (buttons, teleporters, world list items) use this so a scene is only
/// entered when the active runtime platform is one of the world's checked "Platform Supported" flags.
/// </summary>
public static class ScenePlatformSupport
{
    /// <summary>
    /// Returns the <see cref="WorldData"/> whose <see cref="WorldData.worldKey"/> matches
    /// <paramref name="sceneKey"/>, searching the layout config's Places categories. Returns null when
    /// no matching world exists (e.g. scenes that aren't listed in the database).
    /// </summary>
    public static WorldData FindWorldData(string sceneKey)
    {
        if (string.IsNullOrEmpty(sceneKey))
            return null;

        var cfg = GetLayoutConfig();
        if (cfg == null)
            return null;

        WorldData match = FindInCategories(cfg.worldCategories, sceneKey);
        if (match != null)
            return match;

        if (cfg.mainSectionPanelEntries != null)
        {
            foreach (var entry in cfg.mainSectionPanelEntries)
            {
                if (entry == null)
                    continue;

                match = FindInCategories(entry.worldCategories, sceneKey);
                if (match != null)
                    return match;
            }
        }

        return null;
    }

    /// <summary>
    /// True when the current runtime platform supports the given world's checked platforms.
    /// Mirrors the platform gating used by the world list (WorldItemView).
    /// </summary>
    public static bool IsPlatformSupported(WorldData world)
    {
        if (world == null)
            return true;

        var pm = ProjectManager.instance;
        if (pm == null || pm.platforms == null)
            return true;

        var pl = pm.platforms;
        if (pl.platformChoice == platform.Web && !world.Web) return false;
        if (pl.platformChoice == platform.VR && !world.VR) return false;
        if (pl.platformChoice == platform.Desktop && !world.Desktop) return false;
        if (pl.webGpuBrowserKind == WebGpuBrowserKind.WebXRBrowser && !world.WebXR) return false;
        if (pl.webGpuBrowserKind == WebGpuBrowserKind.MobileBrowser && !world.Mobile) return false;
        return true;
    }

    /// <summary>
    /// True when the scene identified by <paramref name="sceneKey"/> can be entered on the current
    /// platform. Scenes with no matching world entry are treated as supported (not gated).
    /// <paramref name="world"/> is the matched world data (null when not found).
    /// </summary>
    public static bool IsSceneSupported(string sceneKey, out WorldData world)
    {
        world = FindWorldData(sceneKey);
        return IsPlatformSupported(world);
    }

    /// <summary>
    /// Convenience gate: checks the scene, and when unsupported shows the shared unsupported-platform
    /// popup (via <see cref="MenuManager"/>) and returns false. Returns true when the scene may load.
    /// </summary>
    public static bool CanEnterScene(string sceneKey)
    {
        if (IsSceneSupported(sceneKey, out WorldData world))
            return true;

        if (MenuManager.Instance != null)
            MenuManager.Instance.ShowUnsupportedPlatformPopup(world);
        else
            Debug.LogWarning($"[ScenePlatformSupport] Scene '{sceneKey}' is not supported on the current platform.");

        return false;
    }

    static WorldData FindInCategories(List<Category> categories, string sceneKey)
    {
        if (categories == null)
            return null;

        foreach (var category in categories)
        {
            if (category?.environments == null)
                continue;

            foreach (var world in category.environments)
            {
                if (world != null && world.worldKey == sceneKey)
                    return world;
            }
        }

        return null;
    }

    static UILayoutConfig GetLayoutConfig()
    {
        if (ProjectManager.instance != null && ProjectManager.instance.uiLayoutConfig != null)
            return ProjectManager.instance.uiLayoutConfig;

        var mainMap = Object.FindFirstObjectByType<MainMap>();
        return mainMap != null ? mainMap.Config : null;
    }
}
