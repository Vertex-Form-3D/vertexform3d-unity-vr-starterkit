using UnityEngine;
using UnityEngine.UI;

namespace VertextFormCore
{
    public class HubItemView : ViewElement<HubItemData>, IBundleDownloadCallBack
    {
        [SerializeField] TMPro.TMP_Text hubNameTxt;
        [SerializeField] Image hubIMage;
        [SerializeField] TMPro.TMP_Text hubMaxRoomCountTxt;
        [SerializeField] TMPro.TMP_Text DownloadTextTxt;

        [SerializeField] Button clikedBtn;
        [SerializeField] Button downloadClikedBtn;
        public bool isinCache;
        HubItemData data;

        private void Start()
        {
            clikedBtn.onClick.AddListener(() =>
            {
                clikedBtn.interactable = false;
                OnClicked();
            });

            downloadClikedBtn.onClick.AddListener(OnDownloadCliked);
        }


        public override void UpdateView(HubItemData t)
        {
            this.data = t;
            hubNameTxt.text = t.HubName;
            hubIMage.sprite = t.HubImage;
            hubMaxRoomCountTxt.text = t.HubMaxRoomCount + "";
            //check if scene should download or not
            bool sceneIsCashed = IsSceneCashed(t.HubName);
            downloadClikedBtn.gameObject.SetActive(!sceneIsCashed);
            clikedBtn.interactable = (sceneIsCashed);

            //check current download
            var currentDownloadingKey = BundleManagers.instance.CurrentDownloadingBundleKey();
            if (!sceneIsCashed && currentDownloadingKey != null && currentDownloadingKey == t.HubName)
            {
                OnStartDownload();
                BundleManagers.instance.SubscribleToDownloaderCallBack(this);
            }
        }

        private void OnDownloadCliked()
        {
            //downloader
            var addressablesDownloader = BundleManagers.instance;
            addressablesDownloader.DownloadBundle(data.HubName, this);
        }


        public bool IsSceneCashed(string placeName)
        {
            if (data.sceneProvider == SceneProvider.Local)
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
            downloadClikedBtn.gameObject.SetActive(false);
            DownloadTextTxt.gameObject.SetActive(true);
        }

        public void OnFinishDownload(bool status)
        {
            if (status)
            {
                DownloadTextTxt.gameObject.SetActive(false);
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
        }
    }


    [System.Serializable]
    public class HubItemData
    {
        public string HubName;
        public Sprite HubImage;
        public bool flyMode;
        public int HubMaxRoomCount;
        public SceneProvider sceneProvider;
    }

    public enum SceneProvider
    {
        Local, Remote
    }
}