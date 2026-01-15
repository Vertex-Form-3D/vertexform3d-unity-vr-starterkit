using Newtonsoft.Json;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VertexFormCore;

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
    public SerializedDataBase dataBase;
    public GameObject[] allScreens;

    [Header("World View UI")]
    public TextMeshProUGUI worldname;
    public TextMeshProUGUI worldDescription;
    public Image worldImage;
    public Sprite starSprite;
    public Sprite unStarSprite;
    [SerializeField] GridScrollViewPager gridScrollViewPager;
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
        Invoke(nameof(InitCatagory), .5f);
    }

    public void OnTapHome()
    {

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
        foreach (var cat in dataBase.worldCategories)
        {
            GameObject catObj = Instantiate(categoryPrefab, categoryParent);
            catObj.GetComponent<CategoryItemView>().SetCategory(cat);
        }
    }

    public void GetAllWorlds()
    {
        allWorlds.Clear();
        foreach (var cat in dataBase.worldCategories)
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
        catObj.GetComponent<CategoryItemView>().SetCategory(allCat);

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
        favcat.GetComponent<CategoryItemView>().SetCategory(starCategoryItemView.category);
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
            worldObj.GetComponent<WorldItemView>().SetWorldData(world);
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
    public void HandleScreen(GameObject screen)
    {
        foreach (var sc in allScreens)
        {
            sc.SetActive(false);
        }
        screen.SetActive(true);
    }
}
