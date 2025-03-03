using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressablesDownloader : MonoBehaviour
{
    public Action OnDownloadStart;
    public Action<bool> OnDownloadFinish;
    public Action<string, float, float, float> OnDownloadProgress;

    public bool isDownloading = false;
    public string downloadingBundlekey;
    //private MonoBehaviour context;

    /*public AddressablesDownloader(MonoBehaviour context) {
        this.context = context;        
    }*/
    public bool isinCache;
    public string addressableKey;
    public bool check;
    public long cacheSize;
    public bool isCatelogUpdated;
    public string key;
    public long bundleSize;
    public static AddressablesDownloader Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Application.targetFrameRate = 75;
        if (ProjectManager.instance.projectDataSO.projectData.onlyLocalBundles)
        {
            Addressables.InitializeAsync();
            isCatelogUpdated = true;
            isDownloading = false;
        }
        else
        {
            DownloadCatalogFile();
        }
    }

    public void DownloadCatalogFile()
    {
        Debug.Log("DownloadCatalogFile");
        if (!isDownloading)
        {
            isDownloading = true;
#if UNITY_EDITOR
            string catalogFilePath = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings.profileSettings.GetValueByName(UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings.activeProfileId, "Remote.LoadPath");
            catalogFilePath = catalogFilePath.Replace("[BuildTarget]", UnityEditor.EditorUserBuildSettings.activeBuildTarget.ToString());
            catalogFilePath = catalogFilePath + "/" + ProjectManager.instance.projectDataSO.projectData.catalogFileName + ".json";
            AsyncOperationHandle DownloadingCatalog = Addressables.LoadContentCatalogAsync(catalogFilePath, true);
            DownloadingCatalog.Completed += OnCatalogDownload;
#else
            AddressableBuildScriptableObject buildScriptableObject = Resources.Load("BuildVersion/BuildVersion") as AddressableBuildScriptableObject;
                      AsyncOperationHandle DownloadingCatalog = Addressables.LoadContentCatalogAsync(buildScriptableObject.addressableCatalogFilePath,true);
                      DownloadingCatalog.Completed += OnCatalogDownload;
#endif
        }
    }

    void OnCatalogDownload(AsyncOperationHandle handle)
    {
        Debug.Log("OnCatalogDownload called");
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log("OnCatalogDownload Succeeded");
            StartCoroutine(CheckCatalogs());
        }
        else
        {
            isDownloading = false;
        }
    }
    IEnumerator CheckCatalogs()
    {
        Debug.Log("CheckCatalogs");
        yield return Addressables.InitializeAsync();
        isCatelogUpdated = true;
        isDownloading = false;
        Debug.Log("Addressables.InitializeAsync");
    }
    public IEnumerator CheckAddressableInCache()
    {
        var asc = Addressables.GetDownloadSizeAsync(addressableKey);
        yield return asc;
        cacheSize = asc.Result;
        isinCache = asc.Result == 0;
    }
    public bool DownloadBundle(string bundleKey)
    {
        if (!isDownloading)
        {
            Debuger.Log(this, "bundle start downloading" + bundleKey);
            StartCoroutine(DownloadBundleIE(bundleKey));
            return isDownloading;
        }
        else
        {
            Debuger.Log(this, "download blocked cause other bundle is downloading");
            return false;
        }
    }

    public void CheckAddressableInCacheOrNot(string addressableKey, Action<bool> cacheAction)
    {
        StartCoroutine(IECheckAddressableInCacheOrNot(addressableKey, cacheAction));
    }

    IEnumerator IECheckAddressableInCacheOrNot(string addressableKey, Action<bool> cacheAction)
    {
        var downloadSize = Addressables.GetDownloadSizeAsync(addressableKey);
        yield return downloadSize;
        bundleSize = downloadSize.Result;
        bool isInCache = downloadSize.Result == 0;
        if (isInCache)
        {
            cacheAction?.Invoke(true);
            Debug.Log(addressableKey + " is in Cache!!!");
        }
        else
        {
            cacheAction?.Invoke(false);
            Debug.Log(addressableKey + " is not in Cache!!!");
        }
    }
    IEnumerator DownloadBundleIE(string key)
    {
        isDownloading = true;
        downloadingBundlekey = key;
        OnDownloadStart?.Invoke();
        AsyncOperationHandle handle = Addressables.DownloadDependenciesAsync(key.ToString(), false); // Download dependencies

        while (handle.GetDownloadStatus().TotalBytes == 0 && !handle.GetDownloadStatus().IsDone)
        {
            Debug.Log("0");
            yield return new WaitForSeconds(1);
        }

        float totalSizeMB = handle.GetDownloadStatus().TotalBytes / (1024.0f * 1024.0f); // Convert bytes to megabytes

        while (!handle.IsDone)
        {
            float downloadedSizeMB = handle.GetDownloadStatus().Percent * totalSizeMB;
            //float size = $"{(int)downloadedSizeMB} / {(int)totalSizeMB}MB";
            float progress = handle.GetDownloadStatus().Percent;
            //Debug.Log(progress + " " + totalSizeMB);
            OnDownloadProgress?.Invoke(key, downloadedSizeMB, totalSizeMB, progress);
            yield return new WaitForSeconds(1);
        }
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            PlayerPrefs.SetInt(key, (int)CashStatus.cased);
            OnDownloadFinish?.Invoke(true);
        }
        else
        {
            OnDownloadFinish?.Invoke(false);
        }
        Addressables.Release(handle);
        isDownloading = false;
        downloadingBundlekey = null;
    }
}

