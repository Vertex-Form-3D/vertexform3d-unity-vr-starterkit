using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
/// <summary>
/// manage bundle workflow
/// </summary>
public class BundleManagers : MonoBehaviour
{
    //1- check catlog updates return list of updates keys using CatalogUpdater.cs
    //2- update palyer prefs of updates 
    //3- download updates bundle using AssestBundleDownloader.cs
    //4- cash bundle ,clear bundle cash
    public Action OnCatalogUpdated;
    private AddressablesDownloader addressablesDownloader;
    public static BundleManagers instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            addressablesDownloader = GetComponent<AddressablesDownloader>();
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
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

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Toolsa/ClearCashedBundle")]
    public static void ClearCashedBundles()
    {
        Caching.ClearCache();
        PlayerPrefs.DeleteAll();

        Addressables.CleanBundleCache().Completed += handle => Debug.Log("Cache cleared.");

    }
#endif
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