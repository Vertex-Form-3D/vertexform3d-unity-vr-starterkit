using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressablesDownloader : MonoBehaviour
{
    public Action OnDownloadStart;
    public Action<bool> OnDownloadFinish;
    public Action<string, float, float, float> OnDownloadProgress;

    public bool isDownloading = false;
    public string downloadingBundlekey;
    public bool isinCache;
    public string addressableKey;
    public bool check;
    public long cacheSize;
    public bool isCatelogUpdated;
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

        DownloadCatalogFile();

    }

    string catalogFilePath;
    public void DownloadCatalogFile()
    {
        Debug.Log("DownloadCatalogFile");
        if (!isDownloading)
        {
            isDownloading = true;
#if UNITY_EDITOR
            catalogFilePath = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings.profileSettings.GetValueByName(UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings.activeProfileId, "Remote.LoadPath");
            catalogFilePath = catalogFilePath.Replace("[BuildTarget]", UnityEditor.EditorUserBuildSettings.activeBuildTarget.ToString());
            catalogFilePath = catalogFilePath + "/" + ProjectManager.instance.settingsUI.addressableCatalogFileName + ".json";
            AsyncOperationHandle DownloadingCatalog = Addressables.LoadContentCatalogAsync(catalogFilePath, true);
            DownloadingCatalog.Completed += OnCatalogDownload;
#else
            catalogFilePath = ProjectManager.instance.settingsUI.addressableCatalogFilePath;
            AsyncOperationHandle DownloadingCatalog = Addressables.LoadContentCatalogAsync(catalogFilePath, true);
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
        CheckForCatalogUpdates();
        Debug.Log("Addressables.InitializeAsync");
    }

    private void Update()
    {
        if (isCatelogUpdated)
        {
            if (check)
            {
                CheckAddressableInCacheOrNot(addressableKey, null);
                check = false;
            }
        }
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
        float totalSizeMB = bundleSize / (1024.0f * 1024.0f); // Convert bytes to megabytes
        Debug.Log("Total size of " + addressableKey + " is " + totalSizeMB + " MB");
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


    void CheckForCatalogUpdates()
    {
        Debug.Log("Checking For Catalog Updates");

        StartCoroutine(GetLastModifiedTime(catalogFilePath));
        return;
    }

    string catalogModifyTime
    {
        get
        {
            return PlayerPrefs.GetString("catalogModifyTime", "");
        }
        set
        {
            PlayerPrefs.SetString("catalogModifyTime", value);
        }
    }
    private IEnumerator GetLastModifiedTime(string url)
    {
        string fullUrl = url + "?nocache=" + DateTime.UtcNow.Ticks;

        UnityWebRequest request = UnityWebRequest.Head(fullUrl);
        request.SetRequestHeader("Cache-Control", "no-cache");
        request.SetRequestHeader("Pragma", "no-cache");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string lastModified = request.GetResponseHeader("Last-Modified");
            if (!string.IsNullOrEmpty(lastModified))
            {
                DateTime lastModifiedTime = DateTime.Parse(lastModified).ToLocalTime();
                Debug.Log("Last Modified Time: " + lastModifiedTime);

                if (string.IsNullOrEmpty(catalogModifyTime))
                {
                    catalogModifyTime = lastModifiedTime.ToString();
                }
                else
                {
                    if (catalogModifyTime != lastModifiedTime.ToString())
                    {
                        Caching.ClearCache();
                        Addressables.CleanBundleCache().Completed += handle => Debug.Log("Cache cleared.");
                        Resources.UnloadUnusedAssets();
                        catalogModifyTime = lastModifiedTime.ToString();
                    }
                }
            }
            else
            {
                Debug.LogWarning("Last-Modified header not found.");
            }
        }
        else
        {
            Debug.LogError("Error: " + request.error);
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

