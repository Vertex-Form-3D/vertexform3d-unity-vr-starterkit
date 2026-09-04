using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Self-contained Places/Worlds panel. Each instance owns its categories, world grid,
/// pager, and info overlay. MenuManager only switches to this screen via tab buttons.
/// </summary>
public class WorldScreen : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] Transform categoryParent;
    [SerializeField] Transform worldParent;
    [SerializeField] GameObject categoryPrefab;
    [SerializeField] GameObject worldPrefab;
    [SerializeField] GridScrollViewPager gridScrollViewPager;

    [Header("World Info")]
    [SerializeField] GameObject worldInfoScreen;
    [SerializeField] TextMeshProUGUI worldNameText;
    [SerializeField] TextMeshProUGUI worldDescriptionText;
    [SerializeField] Image worldImage;

    [Header("Favorites")]
    [SerializeField] Sprite starSprite;
    [SerializeField] Sprite unStarSprite;

    [Header("Info Panel Animation")]
    [SerializeField] float infoPanelAnimDuration = 0.22f;

    Coroutine _infoAnim;
    int _placesListIndex = -1;
    string _favoritesPanelKey;
    CategoryItemView _starCategoryItemView;
    readonly List<WorldData> _allWorlds = new List<WorldData>();
    readonly List<WorldData> _favourites = new List<WorldData>();
    bool _categoriesBuilt;

    public int PlacesListIndex => _placesListIndex;

    void Awake()
    {
        ResolveReferences();
        WorldFavorites.FavoritesChanged += OnFavoritesChanged;
    }

    void OnEnable()
    {
        // Every time this panel is shown (tab selected, menu reopened, etc.)
        // reset the info overlay so the user always lands on the worlds list.
        if (worldInfoScreen != null)
            worldInfoScreen.SetActive(false);
    }

    void OnDestroy()
    {
        WorldFavorites.FavoritesChanged -= OnFavoritesChanged;
    }

    void OnFavoritesChanged(string panelKey)
    {
        if (panelKey != GetFavoritesPanelKey())
            return;

        RefreshFavoritesCategory();
        RefreshVisibleStarIcons();
    }

    /// <summary>Called when this panel's tab is selected.</summary>
    public void OnPanelOpened()
    {
        ResolveReferences();
        if (!_categoriesBuilt)
            RefreshCategories();
        else
            RefreshDefaultWorlds();
    }

    void RefreshDefaultWorlds()
    {
        if (_allWorlds.Count == 0)
        {
            RefreshCategories();
            return;
        }

        var allCat = new Category
        {
            categoryName = "All",
            environments = new List<WorldData>(_allWorlds)
        };
        InitWorlds(allCat);
    }

    public void ResolveReferences()
    {
        var resolvedCategoryParent = FindTransform("CategoryParent/Scroll View/Viewport/Content")
            ?? FindTransform("CategoryParent/Content");
        if (resolvedCategoryParent != null)
            categoryParent = resolvedCategoryParent;

        var resolvedWorldParent = FindTransform("WorldParent/Scroll View/Viewport/Content")
            ?? FindTransform("WorldParent/Content");
        if (resolvedWorldParent != null)
            worldParent = resolvedWorldParent;

        if (worldInfoScreen == null)
        {
            var infoRoot = transform.Find("WorldInfoScreen");
            if (infoRoot != null)
                worldInfoScreen = infoRoot.gameObject;
        }

        if (worldInfoScreen != null)
        {
            if (worldNameText == null)
                worldNameText = FindTextInChildren(worldInfoScreen.transform, "WorldName");
            if (worldDescriptionText == null)
                worldDescriptionText = FindTextInChildren(worldInfoScreen.transform, "WorldDescription");

            // Prefer the thumbnail named WorldImage; an older prefab wiring pointed at the
            // full-screen black overlay also named "Image", which hid the sprite.
            var resolvedWorldImage = FindImageInChildren(worldInfoScreen.transform, "WorldImage");
            if (resolvedWorldImage != null)
                worldImage = resolvedWorldImage;
            else if (worldImage == null)
                worldImage = FindImageInChildren(worldInfoScreen.transform, "Image");
        }

        if (gridScrollViewPager == null)
            gridScrollViewPager = GetComponent<GridScrollViewPager>();
        if (gridScrollViewPager == null)
            gridScrollViewPager = GetComponentInChildren<GridScrollViewPager>(true);

        var marker = GetComponent<UILayoutCustomPanelMarker>();
        if (marker != null)
        {
            if (_placesListIndex < 0)
                _placesListIndex = marker.sortOrder;
            if (!string.IsNullOrEmpty(marker.panelKey)
                && marker.panelKey.StartsWith("Places:", StringComparison.Ordinal))
                _favoritesPanelKey = marker.panelKey;
        }
    }

    public string GetFavoritesPanelKey()
    {
        if (!string.IsNullOrEmpty(_favoritesPanelKey))
            return WorldFavorites.NormalizePanelKey(_favoritesPanelKey);

        var cfg = GetLayoutConfig();
        if (cfg == null)
            return WorldFavorites.NormalizePanelKey(null);

        var entry = cfg.GetPlacesEntryAt(_placesListIndex);
        if (entry != null)
            return WorldFavorites.NormalizePanelKey(MenuManager.GetPlacesPanelKey(entry, _placesListIndex));

        int primaryIndex = cfg.GetPrimaryPlacesListIndex();
        if (primaryIndex >= 0 && cfg.mainSectionPanelEntries != null && primaryIndex < cfg.mainSectionPanelEntries.Count)
        {
            entry = cfg.mainSectionPanelEntries[primaryIndex];
            if (entry != null)
                return WorldFavorites.NormalizePanelKey(MenuManager.GetPlacesPanelKey(entry, primaryIndex));
        }

        return WorldFavorites.NormalizePanelKey(null);
    }

    void RefreshVisibleStarIcons()
    {
        if (worldParent == null)
            return;

        string panelKey = GetFavoritesPanelKey();
        foreach (var item in worldParent.GetComponentsInChildren<WorldItemView>(true))
            item.RefreshStarIcon(panelKey, starSprite, unStarSprite);
    }

    Transform FindTransform(string path)
    {
        return transform.Find(path);
    }

    static TextMeshProUGUI FindTextInChildren(Transform root, string objectName)
    {
        if (root == null)
            return null;

        foreach (var text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (text != null && text.name == objectName)
                return text;
        }

        return null;
    }

    static Image FindImageInChildren(Transform root, string objectName)
    {
        if (root == null)
            return null;

        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            if (image != null && image.name == objectName)
                return image;
        }

        return null;
    }

    UILayoutConfig GetLayoutConfig()
    {
        if (ProjectManager.instance != null && ProjectManager.instance.uiLayoutConfig != null)
            return ProjectManager.instance.uiLayoutConfig;

        var mainMap = FindFirstObjectByType<MainMap>();
        return mainMap != null ? mainMap.Config : null;
    }

    public IReadOnlyList<Category> GetWorldCategories()
    {
        var cfg = GetLayoutConfig();
        if (cfg == null)
            return System.Array.Empty<Category>();

        var entry = cfg.GetPlacesEntryAt(_placesListIndex);
        if (entry?.worldCategories != null && entry.worldCategories.Count > 0)
            return entry.worldCategories;

        if (cfg.worldCategories != null && cfg.worldCategories.Count > 0)
            return cfg.worldCategories;

        return System.Array.Empty<Category>();
    }

    public void RefreshCategories()
    {
        ResolveReferences();
        if (categoryParent == null || categoryPrefab == null)
            return;

        ClearChildren(categoryParent);

        var categories = GetWorldCategories();
        bool filterPlacesNav = categories.Count > 0;
        foreach (var cat in categories)
        {
            if (filterPlacesNav && !cat.showInPlacesNav)
                continue;

            var catObj = Instantiate(categoryPrefab, categoryParent);
            catObj.GetComponent<CategoryItemView>().SetCategory(cat, this);
        }

        BuildAllAndFavoritesCategories(categories);
        _categoriesBuilt = true;
    }

    void BuildAllAndFavoritesCategories(IReadOnlyList<Category> categories)
    {
        _allWorlds.Clear();
        foreach (var cat in categories)
        {
            if (cat?.environments == null)
                continue;

            foreach (var world in cat.environments)
            {
                if (world != null && !_allWorlds.Exists(o => o.worldName == world.worldName))
                    _allWorlds.Add(world);
            }
        }

        var allCat = new Category
        {
            categoryName = "All",
            environments = new List<WorldData>(_allWorlds)
        };

        var allCatObj = Instantiate(categoryPrefab, categoryParent);
        allCatObj.GetComponent<CategoryItemView>().SetCategory(allCat, this);
        // Keep "All" pinned as the first category regardless of config order
        allCatObj.transform.SetAsFirstSibling();

        RebuildFavouritesFromStarred();
        InitWorlds(allCat);

        var favCatObj = Instantiate(categoryPrefab, categoryParent);
        _starCategoryItemView = favCatObj.GetComponent<CategoryItemView>();
        var favCategory = new Category
        {
            categoryName = "Favorites",
            environments = new List<WorldData>(_favourites)
        };
        _starCategoryItemView.SetCategory(favCategory, this);
    }

    public void RefreshFavoritesCategory()
    {
        RebuildFavouritesFromStarred();
        if (_starCategoryItemView == null)
            return;

        _starCategoryItemView.category.environments = new List<WorldData>(_favourites);
    }

    void RebuildFavouritesFromStarred()
    {
        _favourites.Clear();
        string panelKey = GetFavoritesPanelKey();
        foreach (var world in _allWorlds)
        {
            if (WorldFavorites.IsStarred(panelKey, world.worldName))
                _favourites.Add(world);
        }
    }

    public void InitWorlds(Category cat)
    {
        ResolveReferences();
        if (worldParent == null || worldPrefab == null || cat?.environments == null)
            return;

        if (gridScrollViewPager != null)
            gridScrollViewPager.ClearAllItems();
        else
            ClearChildren(worldParent);

        foreach (var world in cat.environments)
        {
            var worldObj = Instantiate(worldPrefab, worldParent);
            gridScrollViewPager?.AddItem(worldObj);
            worldObj.GetComponent<WorldItemView>().SetWorldData(world, this);
        }
    }

    public void ToggleStar(string worldName, Image img)
    {
        WorldFavorites.ToggleStar(GetFavoritesPanelKey(), worldName, img, starSprite, unStarSprite);
    }

    public void ApplyStarIcon(string worldName, Image img)
    {
        WorldFavorites.ApplyStarIcon(GetFavoritesPanelKey(), worldName, img, starSprite, unStarSprite);
    }

    public void ShowWorldDetails(WorldData worldData)
    {
        if (worldInfoScreen == null || worldData == null)
            return;

        // Populate data first, then animate in
        if (worldNameText != null)
            worldNameText.text = worldData.worldName;
        if (worldDescriptionText != null)
        {
            worldDescriptionText.text = worldData.worldDescription;
            LayoutRebuilder.ForceRebuildLayoutImmediate(worldDescriptionText.rectTransform);
        }
        if (worldImage != null)
        {
            worldImage.sprite = worldData.worldImage;
            worldImage.enabled = worldData.worldImage != null;
            worldImage.color = worldData.worldImage != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        worldInfoScreen.SetActive(true);

        if (_infoAnim != null)
            StopCoroutine(_infoAnim);
        _infoAnim = StartCoroutine(AnimateInfoPanelIn());
    }

    /// <summary>
    /// Called by the Back (←) button: hides the info overlay and returns to the world list.
    /// </summary>
    public void ShowWorldList()
    {
        if (_infoAnim != null)
            StopCoroutine(_infoAnim);

        if (worldInfoScreen != null)
            worldInfoScreen.SetActive(false);
    }

    /// <summary>
    /// Called by the Close (✕) button: hides the info overlay and closes the entire WorldScreen panel.
    /// </summary>
    public void CloseWorldInfoScreen()
    {
        if (_infoAnim != null)
            StopCoroutine(_infoAnim);

        if (worldInfoScreen != null)
            worldInfoScreen.SetActive(false);

        // Close the whole WorldScreen panel so the user returns to the main menu
        gameObject.SetActive(false);
    }

    IEnumerator AnimateInfoPanelIn()
    {
        var rt = worldInfoScreen.GetComponent<RectTransform>();
        var cg = worldInfoScreen.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = worldInfoScreen.AddComponent<CanvasGroup>();

        // Slide in from the right edge and fade in
        float panelW = rt.rect.width;
        if (panelW <= 0f) panelW = 800f; // fallback before layout pass

        Vector3 startPos = rt.localPosition + new Vector3(panelW * 0.15f, 0f, 0f);
        Vector3 endPos   = rt.localPosition - new Vector3(panelW * 0.15f, 0f, 0f); // reset to 0
        // Keep the destination at current (0,0,0) from anchor
        endPos = new Vector3(0f, rt.localPosition.y, rt.localPosition.z);
        startPos = endPos + new Vector3(panelW * 0.15f, 0f, 0f);

        rt.localPosition = startPos;
        cg.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < infoPanelAnimDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / infoPanelAnimDuration);
            // Ease-out quad
            float eased = 1f - (1f - t) * (1f - t);
            rt.localPosition = Vector3.Lerp(startPos, endPos, eased);
            cg.alpha = eased;
            yield return null;
        }

        rt.localPosition = endPos;
        cg.alpha = 1f;
        _infoAnim = null;
    }

    static void ClearChildren(Transform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }
}
