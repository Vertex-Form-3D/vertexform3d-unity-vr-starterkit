
using Newtonsoft.Json;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VertexFormCore;
using UnityEngine.SceneManagement;
using VertexForm3D.UI;
public class MenuManager : MonoBehaviour
{
    public string worldDataJson
    {
        get
        {
            return PlayerPrefs.GetString("worldDataJson", "");
        }
        set
        {
            PlayerPrefs.SetString("worldDataJson", value);
        }
    }
    List<string> starredWorlds = new List<string>();
    List<WorldData> allWorlds = new List<WorldData>();
    List<WorldData> favourites = new List<WorldData>();
    public CategoryItemView starCategoryItemView;
    public Transform categoryParent;
    public GameObject categoryPrefab;
    public GameObject worldPrefab;
    public Transform worldParent;
    public GameObject mainScreen;
    public GameObject worldScreen;
    public GameObject GuideScreen;
    public GameObject worldInfoScreen;

    [Tooltip("Optional tab/nav button that opens the Main screen. Hidden when UILayoutConfig.showMainPanel is false so users go straight to Places.")]
    public GameObject mainTabButton;
    public IReadOnlyList<Category> ActiveWorldCategories
    {
        get
        {
            if (ProjectManager.instance.uiLayoutConfig != null && ProjectManager.instance.uiLayoutConfig.worldCategories != null && ProjectManager.instance.uiLayoutConfig.worldCategories.Count > 0)
                return ProjectManager.instance.uiLayoutConfig.worldCategories;
            return System.Array.Empty<Category>();
        }
    }
    public GameObject[] allScreens;

    [Header("World View UI")]
    public TextMeshProUGUI worldname;
    public TextMeshProUGUI worldDescription;
    public Image worldImage;
    public Sprite starSprite;
    public Sprite unStarSprite;
    [SerializeField] GridScrollViewPager gridScrollViewPager;

    [Header("Unsupported Platform Popup")]
    public GameObject platformNotSupportedPopup;
    public TextMeshProUGUI platformNotSupportedText;
    public bool autoClosePopup = true;
    public float autoCloseDelay = 3f;

    public static MenuManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }
    void Start()
    {
        LoadWorldData();
        ApplyMainPanelVisibility();
        Invoke(nameof(InitCatagory), .5f);
    }

    /// <summary>
    /// Honors <see cref="UILayoutConfig.showMainPanel"/>: when false, hides the Main tab button
    /// and opens the Places (world) screen instead of Main on startup so users skip the landing screen.
    /// </summary>
    void ApplyMainPanelVisibility()
    {
        var cfg = ProjectManager.instance != null ? ProjectManager.instance.uiLayoutConfig : null;
        bool showMain = cfg == null || cfg.showMainPanel;

        if (mainTabButton != null) mainTabButton.SetActive(showMain);

        if (!showMain && worldScreen != null)
            OpenWorldScreen();
    }

    public void OnTapHome()
    {
        // VirtualRoomManager.Instance.LeaveRoomAndLoadHomeScene();
    }
    public void OpenMainScreen()
    {
        HandleScreen(mainScreen);
    }

    public void OpenWorldScreen()
    {
        HandleScreen(worldScreen);
    }

    public void OpenGuideScreen()
    {
        HandleScreen(GuideScreen);
    }
    public void LoadWorldData()
    {
        if (!string.IsNullOrEmpty(worldDataJson))
        {
            starredWorlds = JsonConvert.DeserializeObject<List<string>>(worldDataJson);
        }
    }

    public void WorldIsStarredOrNot(string worldName, Image img)
    {
        if (starredWorlds.Contains(worldName))
        {
            img.sprite = starSprite;
        }
        else
        {
            img.sprite = unStarSprite;
        }
    }
    public void OnTapStar(string worldName, Image img)
    {
        string world = starredWorlds.Find(o => o.Equals(worldName));
        if (string.IsNullOrEmpty(world))
        {
            img.sprite = starSprite;
            starredWorlds.Add(worldName);
        }
        else
        {
            img.sprite = unStarSprite;
            starredWorlds.Remove(worldName);
        }
        favourites = new List<WorldData>();
        foreach (var fav in allWorlds)
        {
            if (starredWorlds.Contains(fav.worldName))
            {
                favourites.Add(fav);
            }
        }
        starCategoryItemView.category.environments = favourites;
        worldDataJson = JsonConvert.SerializeObject(starredWorlds);
    }


    public void InitCatagory()
    {
        foreach (Transform category in categoryParent)
        {
            Destroy(category.gameObject);
        }
        GetAllWorlds();
        bool filterPlacesNav = ProjectManager.instance.uiLayoutConfig != null && ProjectManager.instance.uiLayoutConfig.worldCategories != null &&
                               ProjectManager.instance.uiLayoutConfig.worldCategories.Count > 0;
        foreach (var cat in ActiveWorldCategories)
        {
            if (filterPlacesNav && !cat.showInPlacesNav) continue;
            GameObject catObj = Instantiate(categoryPrefab, categoryParent);
            catObj.GetComponent<CategoryItemView>().SetCategory(cat, this);
        }
    }

    public void GetAllWorlds()
    {
        allWorlds.Clear();
        foreach (var cat in ActiveWorldCategories)
        {
            foreach (var world in cat.environments)
            {
                if (!allWorlds.Exists(o => o.worldName == world.worldName))
                {
                    allWorlds.Add(world);
                }
            }
        }
        GameObject catObj = Instantiate(categoryPrefab, categoryParent);
        Category allCat = new Category();
        allCat.categoryName = "All Places";
        allCat.environments = allWorlds;
        catObj.GetComponent<CategoryItemView>().SetCategory(allCat, this);

        foreach (var world in allWorlds)
        {
            if (starredWorlds.Contains(world.worldName))
            {
                if (!favourites.Exists(o => o.worldName == world.worldName))
                {
                    favourites.Add(world);
                }
            }
        }

        InitWorlds(allCat);

        GameObject favcat = Instantiate(categoryPrefab, categoryParent);
        starCategoryItemView = favcat.GetComponent<CategoryItemView>();
        starCategoryItemView.category = new Category();
        starCategoryItemView.category.categoryName = "Favorites";
        starCategoryItemView.category.environments = favourites;
        favcat.GetComponent<CategoryItemView>().SetCategory(starCategoryItemView.category, this);
    }
    public void OnTapCategory(Category cat)
    {
        foreach (Transform world in worldParent)
        {
            Destroy(world.gameObject);
        }
        InitWorlds(cat);
    }
    public void InitWorlds(Category cat)
    {
        foreach (Transform world in worldParent)
        {
            Destroy(world.gameObject);
        }
        gridScrollViewPager.ClearAllItems();
        foreach (var world in cat.environments)
        {
            GameObject worldObj = Instantiate(worldPrefab, worldParent);
            gridScrollViewPager.AddItem(worldObj);
            worldObj.GetComponent<WorldItemView>().SetWorldData(world, this);
        }
    }

    public void ShowWorldDetails(WorldData wd)
    {
        worldInfoScreen.SetActive(true);
        worldname.text = wd.worldName;
        worldDescription.text = wd.worldDescription;
        LayoutRebuilder.ForceRebuildLayoutImmediate(worldDescription.rectTransform);
        worldImage.sprite = wd.worldImage;
    }

    public void CloseWorldInfoScreen()
    {
        worldInfoScreen.SetActive(false);
    }

    public void ShowUnsupportedPlatformPopup(WorldData wd)
    {
        if (platformNotSupportedPopup == null) return;
        var pl = ProjectManager.instance.platforms;
        string currentPlatform = GetCurrentPlatformDisplayName(pl);
        var supported = new List<string>();
        if (wd.Desktop) supported.Add("Desktop");
        if (wd.VR) supported.Add("VR");
        if (wd.WebGPU) supported.Add("WebGPU");
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
            platform.WebGPU => "WebGPU",
            _ => pl.platformChoice.ToString()
        };
    }
    public void HandleScreen(GameObject screen)
    {
        foreach (var sc in allScreens)
        {
            sc.SetActive(false);
        }
        screen.SetActive(true);
    }
}
