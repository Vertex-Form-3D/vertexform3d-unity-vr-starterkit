using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Collections;
using Unity.EditorCoroutines.Editor;
using System.IO;
using System;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

[System.Serializable]
public class PackageUpdateInfo
{
    public string version;
    public string releaseNotes;
    public string packageUrl;
}

public class PackageUpdaterWindow : EditorWindow
{
    private const string defaultPackageUrl = "https://storage.googleapis.com/vertexform_package_updater/version.json";
    private const string tempFileName = "downloaded_package.unitypackage";
    private const string jsonUrl = "https://storage.googleapis.com/vertexform_package_updater/version.json";

    private string statusMessage = "Idle";
    private float downloadProgress = 0f;
    private bool isDownloading = false;
    private bool isUpdatingPackages = false;
    private PackageUpdateInfo updateInfo;
    private Vector2 scrollPosition;

    private ListRequest listRequest;

    [MenuItem("VertexForm3D SDK/Package Updater")]
    public static void ShowWindow()
    {
        GetWindow<PackageUpdaterWindow>("Package Updater");
    }

    /// <summary>
    /// Shows the window and triggers an update for the given package URL.
    /// Called by the auto-update checker when user chooses to update.
    /// </summary>
    public static void ShowWindowAndUpdate(string packageUrl, string version = "Latest", string releaseNotes = "Update available from version.json")
    {
        var window = GetWindow<PackageUpdaterWindow>("Package Updater");
        window.Show();

        // Create a temporary PackageUpdateInfo with the URL
        window.updateInfo = new PackageUpdateInfo
        {
            version = version,
            releaseNotes = releaseNotes,
            packageUrl = packageUrl
        };

        // Start the download/import process
        EditorCoroutineUtility.StartCoroutineOwnerless(window.DownloadAndImportPackage());
    }

    private void OnEnable()
    {
        EditorCoroutineUtility.StartCoroutineOwnerless(FetchUpdateInfo());
    }

    private void OnGUI()
    {
        GUILayout.Label("Unity Package Updater", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox("Check the latest package update information below. Click 'Update Package' to download and import the latest version.", MessageType.Info);

        GUILayout.Space(10);

        if (updateInfo != null)
        {
            GUILayout.Label("Update Information", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(100));
            GUILayout.Label($"Version: {updateInfo.version}");
            GUILayout.Label("Release Notes:");
            GUILayout.TextArea(updateInfo.releaseNotes, EditorStyles.label, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }
        else
        {
            GUILayout.Label("Fetching update information...");
        }

        GUILayout.Space(10);

        GUI.enabled = !isDownloading && updateInfo != null;
        if (GUILayout.Button("Update Package (.unitypackage)", GUILayout.Height(40)))
        {
            EditorCoroutineUtility.StartCoroutineOwnerless(DownloadAndImportPackage());
        }
        GUI.enabled = true;

        GUILayout.Space(10);

        GUI.enabled = !isUpdatingPackages;
        if (GUILayout.Button("Update All UPM Packages", GUILayout.Height(30)))
        {
            EditorCoroutineUtility.StartCoroutineOwnerless(UpdateAllUPMPackages());
        }
        GUI.enabled = true;

        GUILayout.Space(20);

        if (isDownloading)
        {
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), downloadProgress, $"Downloading: {Mathf.RoundToInt(downloadProgress * 100)}%");
        }

        GUILayout.Label("Status: " + statusMessage, EditorStyles.miniLabel);
    }

    private IEnumerator FetchUpdateInfo()
    {
        statusMessage = "Fetching update information...";
        string fullUrl = jsonUrl + "?nocache=" + DateTime.UtcNow.Ticks;

        using (UnityWebRequest request = UnityWebRequest.Get(fullUrl))
        {
            request.SetRequestHeader("Cache-Control", "no-cache");
            request.SetRequestHeader("Pragma", "no-cache");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to fetch JSON: {request.error}");
                statusMessage = "Failed to fetch update info.";
                updateInfo = null;
                yield break;
            }

            try
            {
                string json = request.downloadHandler.text;

                // Try parsing as array format first (new format)
                var packages = VersionJsonParser.ParseVersionJson(json);
                if (packages != null && packages.Length > 0)
                {
                    // Find the SDK package (prefer one with VertexForm3D in name, otherwise use first)
                    VersionPackageInfo packageInfo = packages[0];
                    foreach (var pkg in packages)
                    {
                        if (pkg.name != null && (pkg.name.Contains("VertexForm3D") || pkg.name.Contains("Vertex Form 3D")))
                        {
                            packageInfo = pkg;
                            break;
                        }
                    }

                    // Convert to PackageUpdateInfo format
                    updateInfo = new PackageUpdateInfo
                    {
                        version = packageInfo.version ?? "Unknown",
                        releaseNotes = packageInfo.releaseNotes ?? "No release notes available.",
                        packageUrl = packageInfo.url ?? defaultPackageUrl
                    };

                    statusMessage = "Update info loaded.";
                    Debug.Log($"Fetched update info: Version {updateInfo.version}");
                }
                else
                {
                    // Fallback to old single-object format for backward compatibility
                    updateInfo = JsonUtility.FromJson<PackageUpdateInfo>(json);
                    statusMessage = "Update info loaded.";
                    Debug.Log($"Fetched update info: Version {updateInfo.version}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error parsing JSON: {e.Message}");
                statusMessage = "Error parsing update info.";
                updateInfo = null;
            }
        }
        Repaint();
    }

    private IEnumerator DownloadAndImportPackage()
    {
        isDownloading = true;
        downloadProgress = 0f;
        statusMessage = "Starting download...";

        string tempPath = Path.Combine(Application.temporaryCachePath, tempFileName);
        string packageUrl = updateInfo?.packageUrl ?? defaultPackageUrl;

        using (UnityWebRequest uwr = UnityWebRequest.Get(packageUrl))
        {
            var request = uwr.SendWebRequest();

            while (!request.isDone)
            {
                downloadProgress = uwr.downloadProgress;
                statusMessage = "Downloading package...";
                Repaint();
                yield return null;
            }

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Download failed: {uwr.error}");
                statusMessage = "Download failed.";
                isDownloading = false;
                yield break;
            }

            try
            {
                File.WriteAllBytes(tempPath, uwr.downloadHandler.data);
                Debug.Log($"Package saved to: {tempPath}");
                statusMessage = "Download complete. Importing...";
                AssetDatabase.ImportPackage(tempPath, false);
                statusMessage = "Import complete.";
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving or importing package: {e.Message}");
                statusMessage = "Import failed.";
            }
        }

        isDownloading = false;
        downloadProgress = 0f;
        Repaint();
    }

    private IEnumerator UpdateAllUPMPackages()
    {
        statusMessage = "Scanning installed packages...";
        isUpdatingPackages = true;

        listRequest = Client.List(true);
        while (!listRequest.IsCompleted)
            yield return null;

        if (listRequest.Status != StatusCode.Success)
        {
            Debug.LogError("Failed to list packages.");
            statusMessage = "Failed to list packages.";
            isUpdatingPackages = false;
            yield break;
        }

        var updatedCount = 0;

        foreach (var package in listRequest.Result)
        {
            if (!package.isDirectDependency || package.source != PackageSource.Registry)
                continue;

            string packageName = package.name;
            string currentVersion = package.version;

            // Search for latest available version
            var searchRequest = Client.Search(packageName);
            while (!searchRequest.IsCompleted)
                yield return null;

            if (searchRequest.Status != StatusCode.Success || searchRequest.Result == null)
            {
                Debug.LogWarning($"❌ Failed to search versions for {packageName}: {searchRequest?.Error?.message}");
                continue;
            }

            string latestCompatibleVersion = null;
            foreach (var found in searchRequest.Result)
            {
                if (found.name == packageName)
                {
                    latestCompatibleVersion = found.versions.latestCompatible;
                    break;
                }
            }

            if (string.IsNullOrEmpty(latestCompatibleVersion) || latestCompatibleVersion == currentVersion)
            {
                Debug.Log($"⏭ {packageName} is already up to date ({currentVersion}).");
                continue;
            }

            string fullId = $"{packageName}@{latestCompatibleVersion}";
            var addRequest = Client.Add(fullId);
            while (!addRequest.IsCompleted)
                yield return null;

            if (addRequest.Status == StatusCode.Success)
            {
                Debug.Log($"✅ Updated {packageName} to {latestCompatibleVersion}");
                updatedCount++;
            }
            else
            {
                Debug.LogWarning($"❌ Could not update {packageName}: {addRequest.Error.message}");
            }
        }

        statusMessage = updatedCount > 0
            ? $"Updated {updatedCount} packages successfully."
            : "All UPM packages are already up to date.";

        isUpdatingPackages = false;
        Repaint();
    }
}
