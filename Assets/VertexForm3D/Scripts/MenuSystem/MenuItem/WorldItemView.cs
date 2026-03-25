using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VertexFormCore;

public class WorldItemView : MonoBehaviour, IBundleDownloadCallBack, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] TMPro.TMP_Text worldNameText;
    [SerializeField] Image worldImage;
    [SerializeField] GameObject downloadPanel;
    [SerializeField] Image downloadLoader;
    [SerializeField] TMPro.TMP_Text DownloadTextTxt;
    [SerializeField] TMPro.TMP_Text playerCountTxt;
    [SerializeField] Button clikedBtn;
    [SerializeField] Button infoBtn;
    [SerializeField] Button downloadBtn;
    [SerializeField] Button starBtn;
    public bool isinCache;
    bool isDownloading;
    public GameObject howerUI;
    public WorldData worlddata = new WorldData();
    public bool InitalizeInStart;

    private void Start()
    {
        clikedBtn.onClick.AddListener(LoadWorld);
        infoBtn.onClick.AddListener(ShowInfo);
        downloadBtn.onClick.AddListener(OnDownloadCliked);
        starBtn.onClick.AddListener(OnTapStar);
        if (ProjectManager.UsesPhotonSessionLobbyRunner)
            InvokeRepeating(nameof(SetPlayerCountText), 0, 1);
        else if (playerCountTxt != null)
            playerCountTxt.gameObject.SetActive(false);
        if (InitalizeInStart)
        {
            UpdateView(worlddata);
        }
    }

    public void SetWorldData(WorldData wd)
    {
        worlddata = wd.Clone();
        UpdateView(worlddata);
    }

    public void ShowInfo()
    {
        MenuManager.Instance.ShowWorldDetails(worlddata);
    }

    public void OnTapStar()
    {
        MenuManager.Instance.OnTapStar(worlddata.worldName, starBtn.GetComponent<Image>());
    }
    void LoadWorld()
    {
        SceneLoader.Instance.isCesiumScene = false;
        SceneLoader.Instance.isFlyModeEnabled = worlddata.flyMode;
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.SetAddressableSceneVisuals(true);
        }
        SceneLoader.Instance.LoadScnene(worlddata.worldKey);
    }

    public void SetPlayerCountText()
    {
        if (!ProjectManager.UsesPhotonSessionLobbyRunner || playerCountTxt == null)
            return;

        int playersInSession = 0;
        int maxPlayersInSession = 100; // Default value if session info is not available
        if (NetworkLobby.Instance != null)
        {
            SessionInfoData ssid = NetworkLobby.Instance.Sessions.Find(s => s.SessionID == worlddata.worldKey);
            playersInSession = ssid != null ? ssid.PlayerCount : 0;
            maxPlayersInSession = ssid != null ? ssid.MaxPlayers : 100;
        }
        maxPlayersInSession = 10; //for now making 100 as max players
        playerCountTxt.text = $"{playersInSession}/{maxPlayersInSession}";
    }

    public void UpdateView(WorldData t)
    {
        this.worlddata = t;
        worldNameText.text = t.worldName;
        worldImage.sprite = t.worldImage;
        //placeMaxRoomCountTxt.text = t.maxPlayerCount + "";

        if (worlddata.sceneProvider == SceneProvider.Local)
        {
            downloadBtn.gameObject.SetActive(false);
            clikedBtn.gameObject.SetActive(true);
        }
        else
        {
            AddressableManager.Instance.CheckCacheByLabels(t.worldKey, (b) =>
            {
                bool sceneIsCashed = b;
                downloadBtn.gameObject.SetActive(!sceneIsCashed);
                clikedBtn.gameObject.SetActive(sceneIsCashed);

                //check current download
                var currentDownloadingKey = AddressableManager.Instance.CurrentDownloadingBundleKey();
                if (!sceneIsCashed && currentDownloadingKey != null && currentDownloadingKey == t.worldKey)
                {
                    OnStartDownload();
                    AddressableManager.Instance.SubscribleToDownloaderCallBack(this);
                }
            });
        }

        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.WorldIsStarredOrNot(worlddata.worldName, starBtn.GetComponent<Image>());
        }
    }

    private void OnDownloadCliked()
    {
        //downloader
        isDownloading = true;
        howerUI.SetActive(false);
        var addressablesDownloader = AddressableManager.Instance;
        addressablesDownloader.DownloadBundle(worlddata.worldKey, this);
    }


    public void OnStartDownload()
    {
        clikedBtn.gameObject.SetActive(false);
        downloadBtn.gameObject.SetActive(false);
        downloadPanel.gameObject.SetActive(true);
    }

    public void OnFinishDownload(bool status)
    {
        downloadPanel.gameObject.SetActive(false);
        if (status)
        {
            clikedBtn.gameObject.SetActive(true);
            isDownloading = false;
        }
        else
        {
            downloadBtn.gameObject.SetActive(true);
        }
    }

    public void OnDownloadProgress(string message, float downloadedSizeMB, float totalSizeMB, float downloadPecentage)
    {
        DownloadTextTxt.text = $"Downloading\n{(int)downloadedSizeMB}/{(int)totalSizeMB} MB";
        downloadLoader.fillAmount = (1 - downloadPecentage);
        Debug.Log(downloadPecentage + " " + totalSizeMB + " " + downloadedSizeMB);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        howerUI.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isDownloading)
        {
            howerUI.SetActive(true);
        }
    }
}
