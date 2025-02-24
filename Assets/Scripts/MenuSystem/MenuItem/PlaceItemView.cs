using UnityEngine;
using UnityEngine.UI;

namespace VertextFormCore
{
    public class PlaceItemView : ViewElement<PlaceData>, IBundleDownloadCallBack
    {
        [SerializeField] TMPro.TMP_Text placeNameTxt;
        [SerializeField] Image placeIMage;
        [SerializeField] TMPro.TMP_Text placeMaxRoomCountTxt;
        [SerializeField] TMPro.TMP_Text DownloadTextTxt;
        [SerializeField] Button clikedBtn;
        [SerializeField] Button downloadClikedBtn;
        public bool isinCache;

        private PlaceData placeData;

        private void Start()
        {
            clikedBtn.onClick.AddListener(OnClicked);
            downloadClikedBtn.onClick.AddListener(OnDownloadCliked);
        }


        public override void UpdateView(PlaceData t)
        {
            this.placeData = t;
            placeNameTxt.text = t.placeName;
            placeIMage.sprite = t.placeImage;
            placeMaxRoomCountTxt.text = t.maxPlaceRoomCount + "";

            //check if scene should download or not
            bool sceneIsCashed = IsSceneCashed(t.placeName);
            downloadClikedBtn.gameObject.SetActive(!sceneIsCashed);
            clikedBtn.interactable = (sceneIsCashed);

            //check current download
            var currentDownloadingKey = BundleManagers.instance.CurrentDownloadingBundleKey();
            if (!sceneIsCashed && currentDownloadingKey != null && currentDownloadingKey == t.placeName)
            {
                OnStartDownload();
                BundleManagers.instance.SubscribleToDownloaderCallBack(this);
            }
        }

        private void OnDownloadCliked()
        {
            //downloader
            var addressablesDownloader = BundleManagers.instance;
            addressablesDownloader.DownloadBundle(placeData.placeName, this);
        }

        public bool IsSceneCashed(string placeName)
        {
            if (placeData.sceneProvider == SceneProvider.Local)
            {
                return true;
            }
            else
            {
                CashStatus cashStatus = (CashStatus)PlayerPrefs.GetInt(placeName, (int)CashStatus.NotCased);
                return cashStatus == CashStatus.cased;
            }
        }

        public void OnStartDownload()
        {
            clikedBtn.gameObject.SetActive(false);
            downloadClikedBtn.gameObject.SetActive(false);
            DownloadTextTxt.gameObject.SetActive(true);
        }

        public void OnFinishDownload(bool status)
        {
            if (status)
            {
                DownloadTextTxt.gameObject.SetActive(false);
                clikedBtn.gameObject.SetActive(true);
                clikedBtn.interactable = (true);
            }
            else
            {
                downloadClikedBtn.gameObject.SetActive(true);
                DownloadTextTxt.gameObject.SetActive(false);
            }
        }

        public void OnDownloadProgress(string message, float downloadedSizeMB, float totalSizeMB, float downloadPecentage)
        {
            DownloadTextTxt.text = $"Downloading\n{(int)downloadedSizeMB}/{(int)totalSizeMB} MB";
            Debug.Log(downloadPecentage + " " + totalSizeMB + " " + downloadedSizeMB);
        }
    }


    [System.Serializable]
    public class PlaceData
    {
        public SceneProvider sceneProvider;
        public string placeName;
        public Sprite placeImage;
        public bool flyMode = true;
        public int maxPlaceRoomCount;
        public CesiumWorldClass CesiumWorld;
    }
}