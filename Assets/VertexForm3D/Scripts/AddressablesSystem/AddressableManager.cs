using System;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
#endif
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
/// <summary>
/// manage bundle workflow
/// </summary>
public class AddressableManager : MonoBehaviour
{
    private AddressablesDownloader addressablesDownloader;
    public static AddressableManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            addressablesDownloader = GetComponent<AddressablesDownloader>();
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool DownloadBundle(string key, IBundleDownloadCallBack bundleCallBack)
    {
        if (addressablesDownloader.isDownloading)
        {
            if (key == addressablesDownloader.downloadingBundlekey)
            {
                //todo: handle the bundle is alredy downloading
            }
            else
            {
                //todo:handle download is blocked and other bundle is dowmloading
            }
            return false;
        }
        else
        {
            addressablesDownloader.OnDownloadStart = bundleCallBack.OnStartDownload;
            addressablesDownloader.OnDownloadFinish = bundleCallBack.OnFinishDownload;
            addressablesDownloader.OnDownloadProgress = bundleCallBack.OnDownloadProgress;
            addressablesDownloader.DownloadBundle(key);
            return true;
        }
    }

    public void SubscribleToDownloaderCallBack(IBundleDownloadCallBack bundleCallBack)
    {
        if (addressablesDownloader.isDownloading)
        {
            addressablesDownloader.OnDownloadStart = bundleCallBack.OnStartDownload;
            addressablesDownloader.OnDownloadFinish = bundleCallBack.OnFinishDownload;
            addressablesDownloader.OnDownloadProgress = bundleCallBack.OnDownloadProgress;
        }
    }

    public string CurrentDownloadingBundleKey()
    {
        return addressablesDownloader.downloadingBundlekey;
    }


    #region CACHE_CHECKING

    public void CheckCacheByLabels(string key, Action<bool> inCacheAction)
    {
        string label = key;
#if UNITY_EDITOR
        string playModeScript = PlayModeScriptChecker.GetCurrentPlayModeScript();
        Debug.Log($"Current Play Mode Script: {playModeScript}");
        if (playModeScript == "Use Asset Database (fastest)")
        {
            if (inCacheAction != null)
            {
                inCacheAction?.Invoke(true);
            }
            return;
        }
#endif
        // Check download size for the label
        var sizeOp = Addressables.GetDownloadSizeAsync(label);
        sizeOp.Completed += (op) =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                long downloadSize = op.Result;
                Debug.Log($"Label '{label}' {(downloadSize == 0 ? "is cached" : $"needs download: {downloadSize} bytes")}");
                if (inCacheAction != null)
                {
                    inCacheAction?.Invoke(downloadSize == 0);
                }

                // Detailed check: Get all locations for the label
                var locationOp = Addressables.LoadResourceLocationsAsync(label);
                locationOp.Completed += (locOp) =>
                {
                    if (locOp.Status == AsyncOperationStatus.Succeeded)
                    {
                        foreach (var location in locOp.Result)
                        {
                            CheckBundleCacheStatus(location, $"Label '{label}' asset: {location.PrimaryKey}");
                        }
                    }
                    else
                    {
                        Debug.LogError($"Failed to load resource locations for label '{label}': {locOp.OperationException}");
                    }
                };
            }
            else
            {
                Debug.LogError($"Failed to get download size for label '{label}': {op.OperationException}");
            }
        };
    }

    void CheckBundleCacheStatus(IResourceLocation location, string identifier)
    {
#if !UNITY_WEBGL
        string bundleUrl = location.InternalId;
        string hash = location.Data != null ? (location.Data as ILocationSizeData)?.ComputeSize(location, null).ToString() : "";

        if (!string.IsNullOrEmpty(bundleUrl))
        {
            Cache cache = Caching.GetCacheByPath(Caching.currentCacheForWriting.path);
            bool isCached = Caching.IsVersionCached(bundleUrl, Hash128.Parse(hash));
            Debug.Log($"{identifier} bundle '{bundleUrl}' is {(isCached ? "cached" : "not cached")}.");
        }
        else
        {
            Debug.LogWarning($"No valid bundle URL for {identifier}.");
        }

        if (location.Dependencies != null && location.Dependencies.Count > 0)
        {
            foreach (var dep in location.Dependencies)
            {
                string depUrl = dep.InternalId;
                string depHash = dep.Data != null ? (dep.Data as ILocationSizeData)?.ComputeSize(dep, null).ToString() : "";
                if (!string.IsNullOrEmpty(depUrl))
                {
                    bool depCached = Caching.IsVersionCached(depUrl, Hash128.Parse(depHash));
                    Debug.Log($"Dependency bundle '{depUrl}' for {identifier} is {(depCached ? "cached" : "not cached")}.");
                }
            }
        }
#else
        Debug.Log($"[AddressableManager] Cache check skipped on WebGL for: {identifier}");
#endif
    }

    #endregion
}

public interface IBundleDownloadCallBack
{
    void OnStartDownload();
    void OnFinishDownload(bool status);
    void OnDownloadProgress(string message, float size, float totalSize, float downloadPecentage);
}

public enum CashStatus
{
    cased = 1, NotCased = 0
}


#if UNITY_EDITOR
public class PlayModeScriptChecker
{
    public static string GetCurrentPlayModeScript()
    {
        // Get the Addressables settings
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            return "Addressable settings not found.";
        }

        // Check the active Play Mode data builder (Play Mode Script)
        var activeBuilder = settings.ActivePlayModeDataBuilder;
        if (activeBuilder == null)
        {
            return "No active Play Mode Script set.";
        }

        // Return the name of the Play Mode Script
        return activeBuilder.Name;
    }
}
#endif