using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VertextFormCore;

public class BundleSampleUI : MonoBehaviour,IBundleDownloadCallBack
{
    [SerializeField] TMPro.TMP_Text status;
    [SerializeField] TMPro.TMP_Text progress;

    [SerializeField] Button ClearCashButton;

    [SerializeField] BundleManagers bundleManagers;

    [SerializeField] BundleItemView bundleItem;
    [SerializeField] RectTransform bundlesListRoot;
   

    List<string> data;
    ListAdapter<string> listAdapter;
    // Start is called before the first frame update
    void Start()
    {
        ClearCashButton.onClick.AddListener(ClearBundleCash);     
    }

    private void OnEnable()
    {
        bundleManagers.OnCatalogUpdated += OnCatalogUpdated;
    }

    private void OnDisable()
    {
        bundleManagers.OnCatalogUpdated -= OnCatalogUpdated;

    }

    private void OnCatalogUpdated() {
        data = AddresableHelpers.GetRemoteBundlesKey();
        Debuger.Log(this, data.Count + "");
        ListClickedListener listClickedListener = new ListClickedListener(OnBundleItemClicked);
        listAdapter = new ListAdapter<string>(bundleItem, bundlesListRoot, data, listClickedListener);
        listAdapter.CreateViews();
    }

    private void OnBundleItemClicked(int index)
    {
        DownloadBundle(data[index]);
    }

    private void ClearBundleCash()
    {
        Caching.ClearCache();
        AddresableHelpers.ClearBundlePlayerPrefCash();
        listAdapter.UpdateViews();
    }

    private void DownloadBundle(string bundleName)
    {
        bundleManagers.DownloadBundle(bundleName,this);
    }

    public void OnStartDownload()
    {
        Debuger.Log(this, "start download");
    }

    public void OnFinishDownload(bool status)
    {
        Debuger.Log(this, "finish download");
        listAdapter.UpdateViews();
    }

  
    void IBundleDownloadCallBack.OnDownloadProgress(string message, float downloadedSizeMB, float totalSizeMB, float downloadPecentage)
    {
        string textMessage = $"{message}:{(int)downloadedSizeMB} / {(int)totalSizeMB}MB";
        status.text = textMessage;
        progress.text = (float)Math.Round(downloadPecentage, 1) + "";
    }
}
