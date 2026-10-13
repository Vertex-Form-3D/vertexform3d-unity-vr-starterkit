using System.Collections.Generic;
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

    // Whether the pointer (mouse or controller ray) is over this card right now. Tracked so the card can
    // bring its buttons back the moment a download finishes: until now only a fresh pointer-enter could do
    // that, so anyone who kept pointing at the card while it downloaded had to move away and back to Enter.
    bool _pointerInside;
    public GameObject howerUI;
    public WorldData worlddata = new WorldData();
    public bool InitalizeInStart;
    WorldScreen _worldScreen;

    private void Start()
    {
        clikedBtn.onClick.AddListener(LoadWorld);
        infoBtn.onClick.AddListener(ShowInfo);
        downloadBtn.onClick.AddListener(OnDownloadCliked);
        starBtn.onClick.AddListener(OnTapStar);
        // Always polled: whether counts are available can change after this card exists, because an
        // app's session source (see SessionDirectory) registers once its settings have loaded.
        InvokeRepeating(nameof(SetPlayerCountText), 0, 1);
        if (InitalizeInStart)
        {
            UpdateView(worlddata);
        }
        EnsureHoverVisibleForTouch();
    }

    /// <summary>
    /// <see cref="howerUI"/> wraps the row action buttons; if it stays off until hover, touch never activates it.
    /// Uses <see cref="DesktopMobileControlSettings.UseMobileMenuHoverUx"/> (mobile, not VR).
    /// </summary>
    private void EnsureHoverVisibleForTouch()
    {
        if (howerUI == null) return;
        howerUI.SetActive(DesktopMobileControlSettings.UseMobileMenuHoverUx);
    }

    public void SetWorldData(WorldData wd, WorldScreen worldScreen)
    {
        _worldScreen = worldScreen;
        worlddata = wd.Clone();
        UpdateView(worlddata);
    }

    public void ShowInfo()
    {
        _worldScreen?.ShowWorldDetails(worlddata);
    }

    public void OnTapStar()
    {
        _worldScreen?.ToggleStar(worlddata.worldName, starBtn.GetComponent<Image>());
    }
    void LoadWorld()
    {
        if (!IsPlatformSupported(worlddata))
        {
            MenuManager.Instance?.ShowUnsupportedPlatformPopup(worlddata);
            return;
        }
        SceneLoader.Instance.isCesiumScene = false;
        SceneLoader.Instance.isFlyModeEnabled = worlddata.flyMode;
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.SetAddressableSceneVisuals(true);
        }
        SceneLoader.Instance.LoadScnene(worlddata.worldKey);
    }

    bool IsPlatformSupported(WorldData t) => ScenePlatformSupport.IsPlatformSupported(t);

    public void SetPlayerCountText()
    {
        if (playerCountTxt == null)
            return;

        // No lobby runner and no external source: nothing can know the counts, so hide the label.
        bool showCounts = SessionDirectory.ShowsPlayerCounts;
        if (playerCountTxt.gameObject.activeSelf != showCounts)
            playerCountTxt.gameObject.SetActive(showCounts);
        if (!showCounts)
            return;

        int playersInSession = 0;
        IReadOnlyList<SessionInfoData> sessions = SessionDirectory.Sessions;
        for (int i = 0; i < sessions.Count; i++)
        {
            if (sessions[i].SessionID == worlddata.worldKey)
            {
                playersInSession = sessions[i].PlayerCount;
                break;
            }
        }

        // 10 for the lobby runner (as before), or whatever the app's session source says.
        playerCountTxt.text = $"{playersInSession}/{SessionDirectory.DefaultMaxPlayers}";
    }

    public void UpdateView(WorldData t)
    {
        this.worlddata = t;
        worldNameText.text = t.worldName;
        worldImage.sprite = t.worldImage;
        clikedBtn.image.color = IsPlatformSupported(t) ? Color.white : Color.red;
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

        if (_worldScreen != null)
            _worldScreen.ApplyStarIcon(worlddata.worldName, starBtn.GetComponent<Image>());

        EnsureHoverVisibleForTouch();
    }

    public void RefreshStarIcon(string panelKey, Sprite starSprite, Sprite unStarSprite)
    {
        if (starBtn == null)
            return;

        WorldFavorites.ApplyStarIcon(panelKey, worlddata.worldName, starBtn.GetComponent<Image>(), starSprite, unStarSprite);
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

        // Cleared on failure too. It used to stay true after a failed download, and OnPointerEnter
        // refuses to show the buttons while it is set, so the card went dead until the menu was rebuilt.
        isDownloading = false;

        if (status)
        {
            clikedBtn.gameObject.SetActive(true);
        }
        else
        {
            downloadBtn.gameObject.SetActive(true);
        }

        // Show Enter (or Download again, after a failure) straight away if the user is still pointing at
        // the card, rather than waiting for a pointer-enter that is never going to come.
        if (howerUI != null && (_pointerInside || DesktopMobileControlSettings.UseMobileMenuHoverUx))
            howerUI.SetActive(true);
    }

    public void OnDownloadProgress(string message, float downloadedSizeMB, float totalSizeMB, float downloadPecentage)
    {
        DownloadTextTxt.text = $"Downloading\n{(int)downloadedSizeMB}/{(int)totalSizeMB} MB";
        downloadLoader.fillAmount = (1 - downloadPecentage);
        Debug.Log(downloadPecentage + " " + totalSizeMB + " " + downloadedSizeMB);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _pointerInside = false;
        if (DesktopMobileControlSettings.UseMobileMenuHoverUx)
            return;
        howerUI.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _pointerInside = true;
        if (!isDownloading)
        {
            howerUI.SetActive(true);
        }
    }

    private void OnDisable()
    {
        // Closing the menu while hovering sends no pointer-exit, so reset here or the card would think it
        // is still being pointed at next time the menu opens.
        _pointerInside = false;
    }
}
