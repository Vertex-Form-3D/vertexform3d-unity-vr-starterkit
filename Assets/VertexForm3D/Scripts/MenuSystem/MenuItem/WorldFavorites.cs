using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Starred worlds per Places panel (e.g. Worlds vs Geospatial).
/// VR and Desktop share favorites when they use the same panel key.
/// </summary>
public static class WorldFavorites
{
    const string PrefsKey = "worldFavoritesByPanel";
    const string LegacyPrefsKey = "worldDataJson";
    const string DefaultPanelKey = "Places:Worlds";

    static Dictionary<string, List<string>> _byPanel;
    static bool _loaded;

    /// <summary>Fired with the panel key whose favorites changed.</summary>
    public static event Action<string> FavoritesChanged;

    public static bool IsStarred(string panelKey, string worldName)
    {
        if (string.IsNullOrEmpty(worldName))
            return false;

        EnsureLoaded();
        return GetList(panelKey).Contains(worldName);
    }

    public static void ApplyStarIcon(string panelKey, string worldName, Image img, Sprite starSprite, Sprite unStarSprite)
    {
        if (img == null)
            return;

        img.sprite = IsStarred(panelKey, worldName) ? starSprite : unStarSprite;
    }

    public static void ToggleStar(string panelKey, string worldName, Image img, Sprite starSprite, Sprite unStarSprite)
    {
        if (string.IsNullOrEmpty(worldName))
            return;

        EnsureLoaded();
        string key = NormalizePanelKey(panelKey);
        var starred = GetList(key);

        if (starred.Contains(worldName))
        {
            starred.Remove(worldName);
            if (img != null && unStarSprite != null)
                img.sprite = unStarSprite;
        }
        else
        {
            starred.Add(worldName);
            if (img != null && starSprite != null)
                img.sprite = starSprite;
        }

        Save();
        FavoritesChanged?.Invoke(key);
    }

    public static string NormalizePanelKey(string panelKey)
    {
        if (string.IsNullOrWhiteSpace(panelKey))
            return DefaultPanelKey;
        return panelKey.Trim();
    }

    static List<string> GetList(string panelKey)
    {
        EnsureLoaded();
        string key = NormalizePanelKey(panelKey);
        if (!_byPanel.TryGetValue(key, out var list))
        {
            list = new List<string>();
            _byPanel[key] = list;
        }
        return list;
    }

    static void EnsureLoaded()
    {
        if (_loaded)
            return;

        _loaded = true;
        _byPanel = new Dictionary<string, List<string>>();

        string json = PlayerPrefs.GetString(PrefsKey, "");
        if (!string.IsNullOrEmpty(json))
        {
            _byPanel = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json)
                ?? new Dictionary<string, List<string>>();
        }

        MigrateLegacyFavorites();
    }

    static void MigrateLegacyFavorites()
    {
        string legacyJson = PlayerPrefs.GetString(LegacyPrefsKey, "");
        if (string.IsNullOrEmpty(legacyJson))
            return;

        var legacyList = JsonConvert.DeserializeObject<List<string>>(legacyJson);
        if (legacyList == null || legacyList.Count == 0)
        {
            PlayerPrefs.DeleteKey(LegacyPrefsKey);
            return;
        }

        if (!_byPanel.TryGetValue(DefaultPanelKey, out var existing) || existing.Count == 0)
            _byPanel[DefaultPanelKey] = legacyList;

        PlayerPrefs.DeleteKey(LegacyPrefsKey);
        Save();
    }

    static void Save()
    {
        PlayerPrefs.SetString(PrefsKey, JsonConvert.SerializeObject(_byPanel));
    }
}
