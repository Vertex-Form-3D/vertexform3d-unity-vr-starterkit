using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Collections;
using Unity.EditorCoroutines.Editor;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using Newtonsoft.Json.Linq;

[System.Serializable]
public class PackageUpdateInfo
{
    public string version;
    public string releaseNotes;
    public string packageUrl;
    // Asset paths removed in this version; deleted from the project after this package is imported.
    public List<string> deletedAssets = new List<string>();
}

/// <summary>
/// Static class that automatically resumes package imports after domain reload.
/// This survives domain reloads unlike instance methods.
/// </summary>
[InitializeOnLoad]
public static class PackageImportResumer
{
    private const string PrefKey_HasPackagesToImport = "VertexForm3D_HasPackagesToImport";
    private const string PrefKey_PackagePaths = "VertexForm3D_PackagePaths";
    private const string PrefKey_PackageVersions = "VertexForm3D_PackageVersions";
    private const string PrefKey_CurrentImportIndex = "VertexForm3D_CurrentImportIndex";
    private const string PrefKey_TotalPackages = "VertexForm3D_TotalPackages";
    private const string PrefKey_IsImporting = "VertexForm3D_IsImporting";
    private const string PrefKey_LastResumeAttempt = "VertexForm3D_LastResumeAttempt";
    private const string PrefKey_ExcludeDefaultScenes = "VertexForm3D_ExcludeDefaultScenes";
    // Per-package deleted asset lists. Packages separated by "|||", paths within a package by "|".
    // ("|" is not a legal character in asset paths, so this is unambiguous.)
    private const string PrefKey_DeletedAssets = "VertexForm3D_DeletedAssets";

    // Paths excluded when the "Don't include default scene example updates" toggle is enabled.
    // The actual scene files (addressableScene.unity, HomeScene.unity, LoginScene.unity) are kept;
    // only the example content folders alongside them are removed.
    private static readonly string[] ExcludedAssetPaths = new[]
    {
        "Assets/VertexForm3D/Scenes/Vertex Form 3D Scenes/addressableScene",
        "Assets/VertexForm3D/Scenes/Vertex Form 3D Scenes/Database Scenes",
    };

    /// <summary>
    /// Deletes the default example scene assets if the user has opted out of receiving them.
    /// Called after every package import (which triggers a domain reload) so each newly imported
    /// copy is removed before the next package in the queue is applied.
    /// </summary>
    public static void CleanupExcludedAssetsIfRequested()
    {
        if (!EditorPrefs.GetBool(PrefKey_ExcludeDefaultScenes, false))
            return;

        bool deletedAny = false;
        foreach (string assetPath in ExcludedAssetPaths)
        {
            if (AssetDatabase.LoadMainAssetAtPath(assetPath) == null && !AssetDatabase.IsValidFolder(assetPath))
                continue;

            if (AssetDatabase.DeleteAsset(assetPath))
            {
                Debug.Log($"[PackageImportResumer] Excluded default scene asset removed: {assetPath}");
                deletedAny = true;
            }
            else
            {
                Debug.LogWarning($"[PackageImportResumer] Failed to delete excluded asset: {assetPath}");
            }
        }

        if (deletedAny)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    // Asset paths queued for deletion for the package currently being imported.
    // Applied after the import's domain reload so the freshly imported assets are already present.
    private const string PrefKey_PendingDeletedAssets = "VertexForm3D_PendingDeletedAssets";

    /// <summary>
    /// Deletes the assets that the version.json entry of the just-imported package marked as removed.
    /// The list is stored in EditorPrefs right before AssetDatabase.ImportPackage is called, so it
    /// survives the domain reload triggered by the import.
    ///
    /// Safe to call repeatedly: paths that already don't exist are skipped, paths that fail to delete
    /// (e.g. because Unity is still compiling) are re-stored and retried on the next poll cycle.
    /// </summary>
    public static void ApplyPendingAssetDeletions()
    {
        string pendingStr = EditorPrefs.GetString(PrefKey_PendingDeletedAssets, "");
        if (string.IsNullOrEmpty(pendingStr))
            return;

        // If Unity is still compiling, AssetDatabase mutations will fail. Leave the key in place
        // and let PollForResume retry once compilation settles.
        if (EditorApplication.isCompiling)
        {
            Debug.Log("[PackageImportResumer] Pending asset deletions deferred - Unity is still compiling.");
            return;
        }

        string[] assetPaths = pendingStr.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
        List<string> failedPaths = new List<string>();
        bool deletedAny = false;

        foreach (string assetPath in assetPaths)
        {
            // Safety: only ever delete inside the Assets folder.
            if (!assetPath.StartsWith("Assets/"))
            {
                Debug.LogWarning($"[PackageImportResumer] Ignoring deleted asset path outside Assets/: {assetPath}");
                continue;
            }

            bool exists = AssetDatabase.LoadMainAssetAtPath(assetPath) != null || AssetDatabase.IsValidFolder(assetPath);
            if (!exists)
            {
                Debug.Log($"[PackageImportResumer] Deleted asset already absent, skipping: {assetPath}");
                continue;
            }

            if (AssetDatabase.DeleteAsset(assetPath))
            {
                Debug.Log($"[PackageImportResumer] ✓ Removed asset deleted in this SDK version: {assetPath}");
                deletedAny = true;
            }
            else
            {
                // Re-queue for next retry cycle instead of silently losing the path.
                Debug.LogWarning($"[PackageImportResumer] ✗ Failed to delete removed asset (will retry): {assetPath}");
                failedPaths.Add(assetPath);
            }
        }

        // Re-store any paths that failed so PollForResume retries them.
        if (failedPaths.Count > 0)
        {
            EditorPrefs.SetString(PrefKey_PendingDeletedAssets, string.Join("|", failedPaths));
            Debug.Log($"[PackageImportResumer] {failedPaths.Count} deletion(s) re-queued for retry.");
        }
        else
        {
            EditorPrefs.DeleteKey(PrefKey_PendingDeletedAssets);
        }

        if (deletedAny)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private static void SetPendingAssetDeletions(string deletedAssetsForPackage)
    {
        if (string.IsNullOrEmpty(deletedAssetsForPackage))
        {
            EditorPrefs.DeleteKey(PrefKey_PendingDeletedAssets);
            return;
        }

        EditorPrefs.SetString(PrefKey_PendingDeletedAssets, deletedAssetsForPackage);
        Debug.Log($"[PackageImportResumer] Queued asset deletions for after import: {deletedAssetsForPackage.Replace("|", ", ")}");
    }

    private static bool hasRegisteredUpdateCallback = false;
    private static double lastPollTime = 0;

    static PackageImportResumer()
    {
        // Wait a bit for Unity to fully initialize after domain reload
        EditorApplication.delayCall += CheckAndResumeImport;

        // Also register an update callback as a fallback mechanism (check every 2 seconds)
        if (!hasRegisteredUpdateCallback)
        {
            EditorApplication.update += PollForResume;
            hasRegisteredUpdateCallback = true;
            lastPollTime = EditorApplication.timeSinceStartup;
        }
    }

    private static void PollForResume()
    {
        double currentTime = EditorApplication.timeSinceStartup;
        if (currentTime - lastPollTime < 2.0)
            return;

        lastPollTime = currentTime;

        // Always retry pending asset deletions on every poll cycle, even when there is no active
        // import. This handles the case where a compile error in the imported package prevented
        // the domain-reload path from completing the deletions.
        if (!EditorApplication.isCompiling)
        {
            string pending = EditorPrefs.GetString(PrefKey_PendingDeletedAssets, "");
            if (!string.IsNullOrEmpty(pending))
            {
                Debug.Log("[PackageImportResumer] PollForResume: Retrying deferred asset deletions...");
                ApplyPendingAssetDeletions();
            }
        }

        bool hasPackages = EditorPrefs.GetBool(PrefKey_HasPackagesToImport, false);
        bool isImporting = EditorPrefs.GetBool(PrefKey_IsImporting, false);

        if (hasPackages && isImporting)
        {
            int currentIndex = EditorPrefs.GetInt(PrefKey_CurrentImportIndex, -1);
            int totalPackages = EditorPrefs.GetInt(PrefKey_TotalPackages, -1);

            if (currentIndex >= 0 && currentIndex < totalPackages)
            {
                Debug.Log($"[PackageImportResumer] PollForResume: Fallback check detected pending import ({currentIndex}/{totalPackages}), attempting to resume...");
                CheckAndResumeImport();
            }
        }
    }

    private static void CheckAndResumeImport()
    {
        bool hasPackages = EditorPrefs.GetBool(PrefKey_HasPackagesToImport, false);
        bool isImporting = EditorPrefs.GetBool(PrefKey_IsImporting, false);

        Debug.Log($"[PackageImportResumer] Static constructor called - HasPackages: {hasPackages}, IsImporting: {isImporting}");

        // Strip excluded example scenes from the package that was just imported before we continue.
        CleanupExcludedAssetsIfRequested();

        // Delete assets that the just-imported version marked as removed (version.json "deletedAssets").
        ApplyPendingAssetDeletions();

        if (hasPackages && isImporting)
        {
            int currentIndex = EditorPrefs.GetInt(PrefKey_CurrentImportIndex, -1);
            int totalPackages = EditorPrefs.GetInt(PrefKey_TotalPackages, -1);
            string packagePathsStr = EditorPrefs.GetString(PrefKey_PackagePaths, "");
            string packageVersionsStr = EditorPrefs.GetString(PrefKey_PackageVersions, "");

            Debug.Log($"[PackageImportResumer] Resuming import - CurrentIndex: {currentIndex}, TotalPackages: {totalPackages}");
            Debug.Log($"[PackageImportResumer] PackagePaths length: {packagePathsStr.Length}, PackageVersions length: {packageVersionsStr.Length}");

            if (currentIndex >= 0 && totalPackages > 0 && currentIndex < totalPackages &&
                !string.IsNullOrEmpty(packagePathsStr) && !string.IsNullOrEmpty(packageVersionsStr))
            {
                string[] packagePaths = packagePathsStr.Split(new[] { "|||" }, StringSplitOptions.None);
                string[] packageVersions = packageVersionsStr.Split(new[] { "|||" }, StringSplitOptions.None);

                if (currentIndex < packagePaths.Length && currentIndex < packageVersions.Length)
                {
                    string nextPackagePath = packagePaths[currentIndex];
                    bool fileExists = File.Exists(nextPackagePath);
                    Debug.Log($"[PackageImportResumer] Next package to import: {packageVersions[currentIndex]}");
                    Debug.Log($"[PackageImportResumer] Next package path: {nextPackagePath}");
                    Debug.Log($"[PackageImportResumer] File exists: {fileExists}");

                    if (!fileExists)
                    {
                        Debug.LogError($"[PackageImportResumer] ✗ Package file not found: {nextPackagePath}. Cannot resume import.");
                        ClearImportProgress();
                        return;
                    }

                    Debug.Log($"[PackageImportResumer] ✓ Auto-resuming package import after domain reload. {currentIndex}/{totalPackages} packages imported.");

                    // Use delayCall to ensure Unity is ready
                    EditorApplication.delayCall += () =>
                    {
                        Debug.Log($"[PackageImportResumer] Executing delayed ContinueImportProcess call...");
                        ContinueImportProcess();
                    };
                }
                else
                {
                    Debug.LogWarning($"[PackageImportResumer] Index out of bounds - CurrentIndex: {currentIndex}, PackagePaths length: {packagePaths.Length}, PackageVersions length: {packageVersions.Length}");
                    ClearImportProgress();
                }
            }
            else
            {
                Debug.LogWarning($"[PackageImportResumer] Invalid state - clearing flags. CurrentIndex: {currentIndex}, TotalPackages: {totalPackages}, PathsEmpty: {string.IsNullOrEmpty(packagePathsStr)}, VersionsEmpty: {string.IsNullOrEmpty(packageVersionsStr)}");
                ClearImportProgress();
            }
        }
        else
        {
            Debug.Log($"[PackageImportResumer] No import in progress. HasPackages: {hasPackages}, IsImporting: {isImporting}");
        }
    }

    public static void ContinueImportProcess()
    {
        string packagePathsStr = EditorPrefs.GetString(PrefKey_PackagePaths, "");
        string packageVersionsStr = EditorPrefs.GetString(PrefKey_PackageVersions, "");

        Debug.Log($"[PackageImportResumer] ContinueImportProcess called");
        Debug.Log($"[PackageImportResumer] PackagePaths length: {packagePathsStr.Length}, PackageVersions length: {packageVersionsStr.Length}");

        if (string.IsNullOrEmpty(packagePathsStr) || string.IsNullOrEmpty(packageVersionsStr))
        {
            Debug.LogWarning("[PackageImportResumer] No packages found to import.");
            ClearImportProgress();
            return;
        }

        string[] packagePaths = packagePathsStr.Split(new[] { "|||" }, StringSplitOptions.None);
        string[] packageVersions = packageVersionsStr.Split(new[] { "|||" }, StringSplitOptions.None);
        int currentIndex = EditorPrefs.GetInt(PrefKey_CurrentImportIndex, 0);
        int totalPackages = packagePaths.Length;

        Debug.Log($"[PackageImportResumer] CurrentIndex: {currentIndex}, TotalPackages: {totalPackages}");

        if (currentIndex >= packagePaths.Length || currentIndex >= packageVersions.Length)
        {
            Debug.Log($"[PackageImportResumer] All packages imported. Clearing progress.");
            ClearImportProgress();

            // Refresh update info
            EditorApplication.delayCall += () =>
            {
                var window = EditorWindow.GetWindow<PackageUpdaterWindow>(false);
                VertexFormEditorHeader.ApplyWindowTitle(window, "Package Updater");
                if (window != null)
                {
                    Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutineOwnerless(window.FetchUpdateInfo());
                }
            };
            return;
        }

        string packagePath = packagePaths[currentIndex];
        string packageVersion = packageVersions[currentIndex];

        // Look up the deleted-assets list for this specific package (stored per-package,
        // packages separated by "|||", paths within a package by "|").
        string deletedAssetsForThisPackage = "";
        string deletedAssetsStr = EditorPrefs.GetString(PrefKey_DeletedAssets, "");
        if (!string.IsNullOrEmpty(deletedAssetsStr))
        {
            string[] deletedAssetsPerPackage = deletedAssetsStr.Split(new[] { "|||" }, StringSplitOptions.None);
            if (currentIndex < deletedAssetsPerPackage.Length)
            {
                deletedAssetsForThisPackage = deletedAssetsPerPackage[currentIndex];
            }
        }

        Debug.Log($"[PackageImportResumer] [IMPORT {currentIndex + 1}/{totalPackages}] Importing: {packageVersion}");
        Debug.Log($"[PackageImportResumer] [IMPORT {currentIndex + 1}/{totalPackages}] Path: {packagePath}");

        if (!File.Exists(packagePath))
        {
            Debug.LogError($"[PackageImportResumer] [IMPORT {currentIndex + 1}/{totalPackages}] ✗ Package file not found: {packagePath}");
            ClearImportProgress();
            return;
        }

        // Update progress before importing
        EditorPrefs.SetInt(PrefKey_CurrentImportIndex, currentIndex);
        EditorPrefs.SetBool(PrefKey_HasPackagesToImport, true);
        EditorPrefs.SetBool(PrefKey_IsImporting, true);
        // EditorPrefs are automatically saved when set

        Debug.Log($"[PackageImportResumer] [IMPORT {currentIndex + 1}/{totalPackages}] Progress saved. CurrentIndex: {currentIndex}, TotalPackages: {totalPackages}");

        // Update version BEFORE import (so it's saved even if domain reload happens)
        if (!string.IsNullOrEmpty(packageVersion))
        {
            UpdatePackageVersionStatic(packageVersion);
        }

        // Check if more packages to import BEFORE importing (so we can set the flag correctly)
        bool hasMorePackages = currentIndex + 1 < totalPackages;

        // Update index for next import (will be picked up after domain reload)
        if (hasMorePackages)
        {
            EditorPrefs.SetInt(PrefKey_CurrentImportIndex, currentIndex + 1);
            EditorPrefs.SetBool(PrefKey_HasPackagesToImport, true);
            EditorPrefs.SetBool(PrefKey_IsImporting, true);
            // EditorPrefs are automatically saved when set
            Debug.Log($"[PackageImportResumer] [IMPORT {currentIndex + 1}/{totalPackages}] Updated index to {currentIndex + 1} for next import after domain reload.");
        }

        // Queue this version's asset deletions so they are applied after the import's domain reload.
        // The pending key survives domain reload; PollForResume retries if deletions fail (e.g.
        // because a compile error in the package is blocking AssetDatabase mutations).
        SetPendingAssetDeletions(deletedAssetsForThisPackage);

        // Import the package (this will trigger domain reload when it contains scripts).
        try
        {
            Debug.Log($"[PackageImportResumer] [IMPORT {currentIndex + 1}/{totalPackages}] Calling AssetDatabase.ImportPackage({packagePath}, false)...");
            AssetDatabase.ImportPackage(packagePath, false);
            Debug.Log($"[PackageImportResumer] [IMPORT {currentIndex + 1}/{totalPackages}] AssetDatabase.ImportPackage returned");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PackageImportResumer] [IMPORT {currentIndex + 1}/{totalPackages}] ✗ AssetDatabase.ImportPackage threw an exception: {ex.Message}\n{ex.StackTrace}");
            // Clear progress so we don't loop forever on a broken package.
            // Pending deletions are kept so PollForResume can still apply them.
            ClearImportProgress();
            return;
        }

        // Delete temp file AFTER import (but before domain reload completes)
        // Note: We delete the file we just imported, not the next one
        try
        {
            if (File.Exists(packagePath))
            {
                File.Delete(packagePath);
                Debug.Log($"[PackageImportResumer] [IMPORT {currentIndex + 1}/{totalPackages}] ✓ Temp file deleted: {packagePath}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[PackageImportResumer] Failed to delete temp file: {ex.Message}");
        }

        // Check if more packages to import
        if (hasMorePackages)
        {
            Debug.Log($"[PackageImportResumer] [IMPORT {currentIndex + 1}/{totalPackages}] More packages to import. Will continue after domain reload.");
            Debug.Log($"[PackageImportResumer] [IMPORT {currentIndex + 1}/{totalPackages}] Next package index: {currentIndex + 1}, Remaining: {totalPackages - currentIndex - 1}");
            // The static constructor will automatically resume after domain reload.
            // If no domain reload happens (compile errors), PollForResume picks up after 2 s.
        }
        else
        {
            Debug.Log($"[PackageImportResumer] [IMPORT {currentIndex + 1}/{totalPackages}] All packages imported!");
            CleanupExcludedAssetsIfRequested();
            // If no domain reload occurs (e.g. compile errors in the package), apply deletions
            // here. If a domain reload does occur, CheckAndResumeImport applies them instead.
            ApplyPendingAssetDeletions();
            ClearImportProgress();

            // Refresh update info
            EditorApplication.delayCall += () =>
            {
                var window = EditorWindow.GetWindow<PackageUpdaterWindow>(false);
                VertexFormEditorHeader.ApplyWindowTitle(window, "Package Updater");
                if (window != null)
                {
                    Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutineOwnerless(window.FetchUpdateInfo());
                }
            };
        }
    }

    private static void UpdatePackageVersionStatic(string newVersion)
    {
        Debug.Log($"[PackageImportResumer] [UpdatePackageVersion] Updating version to: {newVersion}");
        ProjectDataScriptableObject pso = Resources.Load<ProjectDataScriptableObject>("Project Data SO");
        if (pso != null)
        {
            string oldVersion = pso.projectData.currentPackageVersion;
            pso.projectData.currentPackageVersion = string.IsNullOrEmpty(newVersion) ? "1.0.0" : newVersion;
            EditorUtility.SetDirty(pso);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PackageImportResumer] [UpdatePackageVersion] ✓ Version updated: {oldVersion} -> {pso.projectData.currentPackageVersion}");
        }
        else
        {
            Debug.LogWarning($"[PackageImportResumer] [UpdatePackageVersion] ✗ ProjectDataScriptableObject not found.");
        }
    }

    public static void ClearImportProgress()
    {
        Debug.Log($"[PackageImportResumer] Clearing import progress flags...");
        EditorPrefs.DeleteKey(PrefKey_HasPackagesToImport);
        EditorPrefs.DeleteKey(PrefKey_PackagePaths);
        EditorPrefs.DeleteKey(PrefKey_PackageVersions);
        EditorPrefs.DeleteKey(PrefKey_CurrentImportIndex);
        EditorPrefs.DeleteKey(PrefKey_TotalPackages);
        EditorPrefs.DeleteKey(PrefKey_IsImporting);
        EditorPrefs.DeleteKey(PrefKey_DeletedAssets);
    }

    public static void SaveDownloadedPackages(List<string> paths, List<string> versions, List<string> deletedAssetsPerPackage = null)
    {
        Debug.Log($"[PackageImportResumer] Saving {paths.Count} package(s) for import...");
        EditorPrefs.SetBool(PrefKey_HasPackagesToImport, true);
        EditorPrefs.SetBool(PrefKey_IsImporting, true);
        EditorPrefs.SetString(PrefKey_PackagePaths, string.Join("|||", paths));
        EditorPrefs.SetString(PrefKey_PackageVersions, string.Join("|||", versions));
        EditorPrefs.SetInt(PrefKey_CurrentImportIndex, 0);
        EditorPrefs.SetInt(PrefKey_TotalPackages, paths.Count);

        // Each entry is that package's deleted asset paths joined by "|" (may be empty).
        if (deletedAssetsPerPackage != null && deletedAssetsPerPackage.Count == paths.Count)
        {
            EditorPrefs.SetString(PrefKey_DeletedAssets, string.Join("|||", deletedAssetsPerPackage));
        }
        else
        {
            EditorPrefs.DeleteKey(PrefKey_DeletedAssets);
        }

        for (int i = 0; i < paths.Count && i < versions.Count; i++)
        {
            Debug.Log($"[PackageImportResumer]   Package {i + 1}: {versions[i]} -> {paths[i]}");
        }
    }
}

public class PackageUpdaterWindow : EditorWindow
{
    private const string defaultPackageUrl = "https://storage.googleapis.com/vertexform_package_updater/Test2/version.json";
    private const string tempFileName = "downloaded_package.unitypackage";
    private const string jsonUrl = "https://storage.googleapis.com/vertexform_package_updater/Test2/version.json";
    private const string openUpmRegistryName = "OpenUPM";
    private const string openUpmRegistryUrl = "https://package.openupm.com";
    private const string webxrPackageId = "com.de-panther.webxr";
    private const string webxrScopePrefix = "com.de-panther";

    // EditorPrefs keys for resuming after domain reload
    private const string PrefKey_HasPackagesToImport = "VertexForm3D_HasPackagesToImport";
    private const string PrefKey_PackagePaths = "VertexForm3D_PackagePaths"; // Delimited string
    private const string PrefKey_PackageVersions = "VertexForm3D_PackageVersions"; // Delimited string
    private const string PrefKey_CurrentImportIndex = "VertexForm3D_CurrentImportIndex";
    private const string PrefKey_TotalPackages = "VertexForm3D_TotalPackages";
    private const string PrefKey_ExcludeDefaultScenes = "VertexForm3D_ExcludeDefaultScenes";
    private const string PrefKey_DeletedAssets = "VertexForm3D_DeletedAssets";

    private string statusMessage = "Idle";
    private float downloadProgress = 0f;
    private bool isDownloading = false;
    private bool isImporting = false; // Track if we're importing packages
    private bool isUpdatingPackages = false;
    private PackageUpdateInfo updateInfo;
    private Vector2 scrollPosition;
    private bool isUpdateAvailable = false;
    private List<PackageUpdateInfo> availableUpdates = new List<PackageUpdateInfo>(); // All versions to download sequentially
    private int currentUpdateIndex = 0; // Current version being downloaded
    private bool isFetchingUpdateInfo = false; // Track if we're currently fetching update info
    private bool autoStartDownload = false; // Flag to auto-start download after fetch completes
    private bool isInstallingWebXR = false;
    private bool isWebXRInstalled = false;

    private ListRequest listRequest;

    private static GUIStyle releaseNotesRichTextStyle;

    private static GUIStyle ReleaseNotesRichTextStyle
    {
        get
        {
            if (releaseNotesRichTextStyle == null)
            {
                releaseNotesRichTextStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
                {
                    richText = true,
                    wordWrap = true,
                    padding = new RectOffset(2, 2, 2, 4)
                };
            }

            return releaseNotesRichTextStyle;
        }
    }
    //

    [MenuItem("Vertex Form/Package Updater", false, 14)]
    public static PackageUpdaterWindow ShowWindow()
    {
        var window = GetWindow<PackageUpdaterWindow>();
        VertexFormEditorHeader.ApplyWindowTitle(window, "Package Updater");
        return window;
    }

    /// <summary>
    /// Shows the window and triggers an update for the given package URL.
    /// Called by the auto-update checker when user chooses to update.
    /// </summary>
    public static void ShowWindowAndUpdate(string packageUrl, string version = "Latest", string releaseNotes = "Update available from version.json")
    {
        var window = GetWindow<PackageUpdaterWindow>();
        VertexFormEditorHeader.ApplyWindowTitle(window, "Package Updater");
        window.Show();

        // Create a temporary PackageUpdateInfo with the URL
        window.updateInfo = new PackageUpdateInfo
        {
            version = version,
            releaseNotes = releaseNotes,
            packageUrl = packageUrl
        };

        // Check if update is available
        window.isUpdateAvailable = window.CheckIfUpdateAvailable(version);

        // Start the download process
        EditorCoroutineUtility.StartCoroutineOwnerless(window.DownloadAllPackages());
    }

    private void OnEnable()
    {
        VertexFormEditorHeader.ApplyWindowTitle(this, "Package Updater");
        Debug.Log($"[PackageUpdater] OnEnable called. HasPackagesToImport: {EditorPrefs.GetBool(PrefKey_HasPackagesToImport, false)}");
        RefreshWebXRInstalledState();
        EditorCoroutineUtility.StartCoroutineOwnerless(FetchUpdateInfo());
    }

    /// <summary>
    /// Sets a flag to auto-start download after FetchUpdateInfo completes.
    /// Called by the update checker when user chooses "Update Now".
    /// </summary>
    public void SetAutoStartDownload(bool autoStart)
    {
        autoStartDownload = autoStart;
    }

    private void OnGUI()
    {
        VertexFormEditorHeader.Draw(position.width);
        VertexFormEditorHeader.DrawPanelTitle("Package Updater");

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));

        EditorGUILayout.HelpBox("Check the latest package update information below. Click 'Update Package' to download and import all available newer versions sequentially.", MessageType.Info);

        GUILayout.Space(10);

        if (updateInfo != null)
        {
            GUILayout.Label("Update Information", EditorStyles.boldLabel);

            // Show latest version info
            GUILayout.Label($"Latest Version: {updateInfo.version}");

            // Show all versions that will be downloaded
            if (availableUpdates.Count > 0)
            {
                GUILayout.Space(5);
                GUILayout.Label($"Versions to download ({availableUpdates.Count}):", EditorStyles.boldLabel);
                foreach (var update in availableUpdates)
                {
                    int deletedCount = update.deletedAssets != null ? update.deletedAssets.Count : 0;
                    string label = deletedCount > 0
                        ? $"  → {update.version} (removes {deletedCount} obsolete asset(s))"
                        : $"  → {update.version}";
                    GUILayout.Label(label, EditorStyles.miniLabel);
                }
            }

            GUILayout.Space(5);
            GUILayout.Label("Release Notes (Latest):", EditorStyles.boldLabel);
            GUILayout.Label(updateInfo.releaseNotes ?? string.Empty, ReleaseNotesRichTextStyle);
        }
        else
        {
            GUILayout.Label("Fetching update information...");
        }

        GUILayout.Space(10);

        // Toggle: when enabled, the default example scene assets are removed after each import.
        bool excludeDefaultScenes = EditorPrefs.GetBool(PrefKey_ExcludeDefaultScenes, false);
        bool newExcludeDefaultScenes = EditorGUILayout.ToggleLeft(
            new GUIContent(
                "Don't include default scene example updates",
                "When enabled, 'addressableScene' and the 'Database Scenes' folder under Assets/VertexForm3D/Scenes/Vertex Form 3D Scenes will be removed after each package is imported."),
            excludeDefaultScenes);
        if (newExcludeDefaultScenes != excludeDefaultScenes)
        {
            EditorPrefs.SetBool(PrefKey_ExcludeDefaultScenes, newExcludeDefaultScenes);
        }

        GUILayout.Space(10);

        GUI.enabled = !isDownloading && !isImporting && isUpdateAvailable;
        if (GUILayout.Button("Update Package (.unitypackage)", GUILayout.Height(40)))
        {
            EditorCoroutineUtility.StartCoroutineOwnerless(DownloadAllPackages());
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

        bool canInstallWebXR = !isDownloading && !isImporting && !isUpdatingPackages && !isInstallingWebXR && !isWebXRInstalled;
        GUI.enabled = canInstallWebXR;
        string webxrButtonLabel = isWebXRInstalled
            ? "WebXR Package Already Installed"
            : "Install WebXR Package (Scoped Registry)";
        if (GUILayout.Button(webxrButtonLabel, GUILayout.Height(30)))
        {
            EditorCoroutineUtility.StartCoroutineOwnerless(InstallWebXRPackage());
        }
        GUI.enabled = true;

        GUILayout.Space(10);

        if (isDownloading || isImporting)
        {
            string progressText;
            if (isDownloading)
            {
                progressText = availableUpdates.Count > 0 && currentUpdateIndex < availableUpdates.Count
                    ? $"Downloading {currentUpdateIndex + 1}/{availableUpdates.Count}: {availableUpdates[currentUpdateIndex].version} - {Mathf.RoundToInt(downloadProgress * 100)}%"
                    : $"Downloading: {Mathf.RoundToInt(downloadProgress * 100)}%";
            }
            else
            {
                int currentIndex = EditorPrefs.GetInt(PrefKey_CurrentImportIndex, 0);
                int totalPackages = EditorPrefs.GetInt(PrefKey_TotalPackages, 0);
                string packagePaths = EditorPrefs.GetString(PrefKey_PackagePaths, "");
                string packageVersions = EditorPrefs.GetString(PrefKey_PackageVersions, "");

                if (!string.IsNullOrEmpty(packageVersions) && currentIndex < totalPackages)
                {
                    string[] versions = packageVersions.Split(new[] { "|||" }, StringSplitOptions.None);
                    if (currentIndex < versions.Length)
                    {
                        progressText = $"Importing {currentIndex + 1}/{totalPackages}: {versions[currentIndex]}";
                    }
                    else
                    {
                        progressText = $"Importing {currentIndex + 1}/{totalPackages}...";
                    }
                }
                else
                {
                    progressText = "Importing packages...";
                }
            }
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), downloadProgress, progressText);
        }

        EditorGUILayout.EndScrollView();

        GUILayout.Label("Status: " + statusMessage, EditorStyles.miniLabel);
    }

    public IEnumerator FetchUpdateInfo()
    {
        isFetchingUpdateInfo = true;
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
                    // Get current local version
                    ProjectDataScriptableObject pso = Resources.Load<ProjectDataScriptableObject>("Project Data SO");
                    string localVersion = pso != null && !string.IsNullOrEmpty(pso.projectData.currentPackageVersion)
                        ? pso.projectData.currentPackageVersion
                        : "0.0.0";

                    // Filter SDK packages and collect all versions
                    List<VersionPackageInfo> sdkPackages = new List<VersionPackageInfo>();
                    foreach (var pkg in packages)
                    {
                        if (pkg.name != null && (pkg.name.Contains("VertexForm3D") || pkg.name.Contains("Vertex Form 3D")))
                        {
                            sdkPackages.Add(pkg);
                        }
                    }

                    // If no SDK-specific packages found, use all packages
                    if (sdkPackages.Count == 0)
                    {
                        sdkPackages.AddRange(packages);
                    }

                    // Filter versions newer than current and sort them
                    availableUpdates.Clear();
                    foreach (var pkg in sdkPackages)
                    {
                        if (!string.IsNullOrEmpty(pkg.version) && IsNewerVersion(pkg.version, localVersion))
                        {
                            availableUpdates.Add(new PackageUpdateInfo
                            {
                                version = pkg.version,
                                releaseNotes = pkg.releaseNotes ?? "No release notes available.",
                                packageUrl = pkg.url ?? defaultPackageUrl,
                                deletedAssets = pkg.deletedAssets ?? new List<string>()
                            });
                        }
                    }

                    // Sort versions in ascending order (oldest first)
                    availableUpdates.Sort((a, b) =>
                    {
                        System.Version vA = ParseVersion(a.version);
                        System.Version vB = ParseVersion(b.version);
                        if (vA != null && vB != null)
                        {
                            return vA.CompareTo(vB);
                        }
                        return string.Compare(a.version, b.version, StringComparison.OrdinalIgnoreCase);
                    });

                    // Set the latest version info for display
                    if (availableUpdates.Count > 0)
                    {
                        updateInfo = availableUpdates[availableUpdates.Count - 1]; // Latest version
                        isUpdateAvailable = true;
                        statusMessage = $"Update info loaded. {availableUpdates.Count} newer version(s) available.";
                        Debug.Log($"Fetched update info: {availableUpdates.Count} version(s) to download. Latest: {updateInfo.version}");
                    }
                    else
                    {
                        // No updates available, but show latest version info
                        if (sdkPackages.Count > 0)
                        {
                            // Find the latest version from all packages
                            VersionPackageInfo latestPkg = sdkPackages[0];
                            foreach (var pkg in sdkPackages)
                            {
                                if (IsNewerVersion(pkg.version, latestPkg.version))
                                {
                                    latestPkg = pkg;
                                }
                            }
                            updateInfo = new PackageUpdateInfo
                            {
                                version = latestPkg.version ?? "Unknown",
                                releaseNotes = latestPkg.releaseNotes ?? "No release notes available.",
                                packageUrl = latestPkg.url ?? defaultPackageUrl
                            };
                        }
                        isUpdateAvailable = false;
                        statusMessage = "Update info loaded. You have the latest version.";
                        Debug.Log($"Fetched update info: Already up to date. Current: {localVersion}");
                    }
                }
                else
                {
                    // Fallback to old single-object format for backward compatibility
                    updateInfo = JsonUtility.FromJson<PackageUpdateInfo>(json);
                    availableUpdates.Clear();

                    // Check if update is available
                    isUpdateAvailable = CheckIfUpdateAvailable(updateInfo.version);
                    if (isUpdateAvailable)
                    {
                        availableUpdates.Add(updateInfo);
                    }

                    statusMessage = isUpdateAvailable
                        ? "Update info loaded. Update available."
                        : "Update info loaded. You have the latest version.";
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

        isFetchingUpdateInfo = false;

        // Auto-start download if requested (from update checker)
        if (autoStartDownload && isUpdateAvailable && availableUpdates.Count > 0)
        {
            autoStartDownload = false; // Reset flag
            EditorCoroutineUtility.StartCoroutineOwnerless(DownloadAllPackages());
        }

        Repaint();
    }

    /// <summary>
    /// Downloads all packages first, then imports them sequentially.
    /// This two-phase approach allows resuming after domain reloads.
    /// </summary>
    private IEnumerator DownloadAllPackages()
    {
        Debug.Log($"[PackageUpdater] ===== DownloadAllPackages START =====");
        Debug.Log($"[PackageUpdater] AvailableUpdates count: {availableUpdates.Count}");

        isDownloading = true;
        downloadProgress = 0f;
        currentUpdateIndex = 0;

        // If availableUpdates is empty but updateInfo exists, add it (backward compatibility)
        if (availableUpdates.Count == 0 && updateInfo != null)
        {
            Debug.Log($"[PackageUpdater] Adding updateInfo to availableUpdates (backward compatibility)");
            availableUpdates.Add(updateInfo);
        }

        if (availableUpdates.Count == 0)
        {
            Debug.LogWarning("[PackageUpdater] No updates available to download.");
            statusMessage = "No updates available.";
            isDownloading = false;
            yield break;
        }

        statusMessage = $"Downloading all packages: {availableUpdates.Count} version(s)...";
        Debug.Log($"[PackageUpdater] Starting download of {availableUpdates.Count} version(s)");
        for (int idx = 0; idx < availableUpdates.Count; idx++)
        {
            Debug.Log($"[PackageUpdater]   - Update {idx + 1}: Version {availableUpdates[idx].version}, URL: {availableUpdates[idx].packageUrl}");
        }

        List<string> downloadedPaths = new List<string>();
        List<string> downloadedVersions = new List<string>();
        List<string> downloadedDeletedAssets = new List<string>(); // per-package, "|"-joined paths

        // Phase 1: Download all packages first
        for (int i = 0; i < availableUpdates.Count; i++)
        {
            currentUpdateIndex = i;
            PackageUpdateInfo currentUpdate = availableUpdates[i];

            statusMessage = $"Downloading {i + 1}/{availableUpdates.Count}: {currentUpdate.version}...";

            string tempPath = Path.Combine(Application.temporaryCachePath, $"downloaded_package_v{currentUpdate.version.Replace(".", "_")}.unitypackage");
            string packageUrl = currentUpdate.packageUrl ?? defaultPackageUrl;


            // Download the package
            using (UnityWebRequest uwr = UnityWebRequest.Get(packageUrl))
            {
                var request = uwr.SendWebRequest();

                while (!request.isDone)
                {
                    downloadProgress = (i + uwr.downloadProgress) / availableUpdates.Count;
                    statusMessage = $"Downloading {i + 1}/{availableUpdates.Count}: {currentUpdate.version} - {Mathf.RoundToInt(uwr.downloadProgress * 100)}%";
                    Repaint();
                    yield return null;
                }

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    statusMessage = $"Download failed for version {currentUpdate.version}. Stopping download process.";
                    isDownloading = false;
                    yield break;
                }


                // Save the downloaded package file
                try
                {
                    File.WriteAllBytes(tempPath, uwr.downloadHandler.data);
                    long fileSize = new FileInfo(tempPath).Length;
                    downloadedPaths.Add(tempPath);
                    downloadedVersions.Add(currentUpdate.version);
                    downloadedDeletedAssets.Add(currentUpdate.deletedAssets != null
                        ? string.Join("|", currentUpdate.deletedAssets)
                        : "");
                    Debug.Log($"[PackageUpdater] [DOWNLOAD {i + 1}/{availableUpdates.Count}] ✓ Package saved: {tempPath} (Size: {fileSize} bytes)");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PackageUpdater] [DOWNLOAD {i + 1}/{availableUpdates.Count}] ✗ Error saving package: {e.Message}\n{e.StackTrace}");
                    statusMessage = $"Save failed for version {currentUpdate.version}. Stopping download process.";
                    isDownloading = false;
                    yield break;
                }
            }
        }

        // All downloads completed
        statusMessage = $"All {downloadedVersions.Count} package(s) downloaded. Starting import...";
        Debug.Log($"[PackageUpdater] ===== All downloads completed: {downloadedVersions.Count} package(s) =====");
        for (int i = 0; i < downloadedVersions.Count; i++)
        {
            Debug.Log($"[PackageUpdater]   Downloaded {i + 1}: {downloadedVersions[i]} -> {downloadedPaths[i]}");
        }

        isDownloading = false;
        downloadProgress = 0f;

        // Save the list of downloaded packages to EditorPrefs for resuming after domain reload
        PackageImportResumer.SaveDownloadedPackages(downloadedPaths, downloadedVersions, downloadedDeletedAssets);

        // Phase 2: Start importing packages sequentially
        // The static PackageImportResumer will handle the import process and resume after domain reloads
        Debug.Log($"[PackageUpdater] Starting import phase via PackageImportResumer...");

        // Start the first import immediately
        EditorApplication.delayCall += PackageImportResumer.ContinueImportProcess;
    }

    /// <summary>
    /// Imports packages sequentially. Can resume after domain reload.
    /// </summary>
    private IEnumerator ImportPackagesSequentially()
    {
        Debug.Log($"[PackageUpdater] ===== ImportPackagesSequentially START =====");
        isImporting = true;

        // Load the list of downloaded packages from EditorPrefs
        string packagePathsStr = EditorPrefs.GetString(PrefKey_PackagePaths, "");
        string packageVersionsStr = EditorPrefs.GetString(PrefKey_PackageVersions, "");

        Debug.Log($"[PackageUpdater] [IMPORT] Loading from EditorPrefs:");
        Debug.Log($"[PackageUpdater] [IMPORT]   PackagePaths length: {packagePathsStr.Length}");
        Debug.Log($"[PackageUpdater] [IMPORT]   PackageVersions length: {packageVersionsStr.Length}");

        if (string.IsNullOrEmpty(packagePathsStr) || string.IsNullOrEmpty(packageVersionsStr))
        {
            Debug.LogWarning($"[PackageUpdater] [IMPORT] ✗ No packages found to import. Paths empty: {string.IsNullOrEmpty(packagePathsStr)}, Versions empty: {string.IsNullOrEmpty(packageVersionsStr)}");
            statusMessage = "No packages to import.";
            isImporting = false;
            PackageImportResumer.ClearImportProgress();
            yield break;
        }

        string[] packagePaths = packagePathsStr.Split(new[] { "|||" }, StringSplitOptions.None);
        string[] packageVersions = packageVersionsStr.Split(new[] { "|||" }, StringSplitOptions.None);

        int currentIndex = EditorPrefs.GetInt(PrefKey_CurrentImportIndex, 0);
        int totalPackages = packagePaths.Length;

        Debug.Log($"[PackageUpdater] [IMPORT] Parsed packages: {totalPackages} total");
        Debug.Log($"[PackageUpdater] [IMPORT] Starting from index: {currentIndex}");
        for (int idx = 0; idx < packageVersions.Length; idx++)
        {
            Debug.Log($"[PackageUpdater] [IMPORT]   Package {idx + 1}: {packageVersions[idx]} -> {packagePaths[idx]} (Exists: {File.Exists(packagePaths[idx])})");
        }

        if (packagePaths.Length != packageVersions.Length)
        {
            Debug.LogError($"[PackageUpdater] [IMPORT] ✗ Mismatch: {packagePaths.Length} paths but {packageVersions.Length} versions!");
        }

        // Import each package sequentially
        for (int i = currentIndex; i < packagePaths.Length && i < packageVersions.Length; i++)
        {
            string packagePath = packagePaths[i];
            string packageVersion = packageVersions[i];

            Debug.Log($"[PackageUpdater] [IMPORT {i + 1}/{totalPackages}] ===== Starting import: {packageVersion} =====");
            Debug.Log($"[PackageUpdater] [IMPORT {i + 1}/{totalPackages}] Package path: {packagePath}");

            // Check if file still exists
            if (!File.Exists(packagePath))
            {
                Debug.LogError($"[PackageUpdater] [IMPORT {i + 1}/{totalPackages}] ✗ Package file not found: {packagePath}");
                Debug.LogError($"[PackageUpdater] [IMPORT {i + 1}/{totalPackages}] Temp directory exists: {Directory.Exists(Application.temporaryCachePath)}");
                Debug.LogError($"[PackageUpdater] [IMPORT {i + 1}/{totalPackages}] Temp directory: {Application.temporaryCachePath}");
                statusMessage = $"Package file not found for version {packageVersion}. Stopping import.";
                isImporting = false;
                PackageImportResumer.ClearImportProgress();
                yield break;
            }

            long fileSize = new FileInfo(packagePath).Length;
            Debug.Log($"[PackageUpdater] [IMPORT {i + 1}/{totalPackages}] ✓ File exists. Size: {fileSize} bytes");

            statusMessage = $"Importing {i + 1}/{totalPackages}: {packageVersion}...";
            Debug.Log($"[PackageUpdater] [IMPORT {i + 1}/{totalPackages}] Setting status message and repainting...");
            Repaint();

            // Save progress before importing (in case of domain reload)
            Debug.Log($"[PackageUpdater] [IMPORT {i + 1}/{totalPackages}] Saving progress: CurrentIndex={i}, HasPackagesToImport=true");
            EditorPrefs.SetInt(PrefKey_CurrentImportIndex, i);
            EditorPrefs.SetBool(PrefKey_HasPackagesToImport, true);

            Debug.Log($"[PackageUpdater] [IMPORT {i + 1}/{totalPackages}] Calling AssetDatabase.ImportPackage({packagePath}, false)...");

            // Import the package (this may trigger domain reload)
            AssetDatabase.ImportPackage(packagePath, false);

            Debug.Log($"[PackageUpdater] [IMPORT {i + 1}/{totalPackages}] AssetDatabase.ImportPackage returned. Refreshing asset database...");

            // Wait for import to complete and asset database to refresh
            AssetDatabase.Refresh();
            Debug.Log($"[PackageUpdater] [IMPORT {i + 1}/{totalPackages}] AssetDatabase.Refresh() called. Waiting 0.2s...");
            yield return new WaitForSeconds(0.2f); // Allow time for domain reload if it happens

            Debug.Log($"[PackageUpdater] [IMPORT {i + 1}/{totalPackages}] Wait complete. Checking if still in same domain...");

            // Update the current package version after successful import
            if (!string.IsNullOrEmpty(packageVersion))
            {
                Debug.Log($"[PackageUpdater] [IMPORT {i + 1}/{totalPackages}] Updating package version to: {packageVersion}");
                UpdatePackageVersion(packageVersion);
                Debug.Log($"[PackageUpdater] [IMPORT {i + 1}/{totalPackages}] ✓ Successfully updated to version {packageVersion}");

                // Ensure the version is saved before proceeding
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[PackageUpdater] [IMPORT {i + 1}/{totalPackages}] Assets saved and refreshed. Waiting 0.1s...");
                yield return new WaitForSeconds(0.1f);
            }

            // Clean up temp file after successful import
            try
            {
                if (File.Exists(packagePath))
                {
                    File.Delete(packagePath);
                    Debug.Log($"[PackageUpdater] [IMPORT {i + 1}/{totalPackages}] ✓ Temp file deleted: {packagePath}");
                }
                else
                {
                    Debug.LogWarning($"[PackageUpdater] [IMPORT {i + 1}/{totalPackages}] Temp file already deleted: {packagePath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PackageUpdater] [IMPORT {i + 1}/{totalPackages}] Failed to delete temp file: {ex.Message}\n{ex.StackTrace}");
            }

            // Update progress
            int nextIndex = i + 1;
            Debug.Log($"[PackageUpdater] [IMPORT {i + 1}/{totalPackages}] Updating progress: CurrentIndex={nextIndex}");
            EditorPrefs.SetInt(PrefKey_CurrentImportIndex, nextIndex);
            EditorPrefs.SetBool(PrefKey_HasPackagesToImport, nextIndex < totalPackages);

            Debug.Log($"[PackageUpdater] [IMPORT {i + 1}/{totalPackages}] ✓ Import complete. Progress saved.");

            if (nextIndex < totalPackages)
            {
                Debug.Log($"[PackageUpdater] [IMPORT {i + 1}/{totalPackages}] More packages to import. Continuing...");
            }
            else
            {
                Debug.Log($"[PackageUpdater] [IMPORT {i + 1}/{totalPackages}] All packages imported!");
            }
        }

        // All imports completed
        if (packageVersions.Length > 0)
        {
            string finalVersion = packageVersions[packageVersions.Length - 1];
            statusMessage = $"Successfully updated to version {finalVersion} ({totalPackages} version(s) installed).";
            Debug.Log($"[PackageUpdater] ===== Sequential import complete. Final version: {finalVersion} =====");
        }
        else
        {
            Debug.LogWarning($"[PackageUpdater] No versions in packageVersions array!");
        }

        // Clear import progress flags
        Debug.Log($"[PackageUpdater] Clearing import progress flags...");
        PackageImportResumer.ClearImportProgress();

        // Ensure final version is saved
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        yield return new WaitForSeconds(0.2f);

        // Refresh update availability by re-fetching update info
        availableUpdates.Clear();
        isUpdateAvailable = false;
        updateInfo = null;

        Debug.Log($"[PackageUpdater] Re-fetching update info...");
        // Re-fetch update info to reflect the new current version
        EditorCoroutineUtility.StartCoroutineOwnerless(FetchUpdateInfo());

        isImporting = false;
        downloadProgress = 0f;
        Debug.Log($"[PackageUpdater] ===== ImportPackagesSequentially END =====");
        Repaint();
    }

    /// <summary>
    /// Saves the list of downloaded packages to EditorPrefs for resuming after domain reload.
    /// </summary>
    private void SaveDownloadedPackages(List<string> paths, List<string> versions)
    {
        Debug.Log($"[PackageUpdater] ===== SaveDownloadedPackages START =====");
        Debug.Log($"[PackageUpdater] Saving {paths.Count} package(s) to EditorPrefs:");

        for (int i = 0; i < paths.Count && i < versions.Count; i++)
        {
            Debug.Log($"[PackageUpdater]   Package {i + 1}: {versions[i]} -> {paths[i]}");
        }

        string pathsStr = string.Join("|||", paths);
        string versionsStr = string.Join("|||", versions);

        EditorPrefs.SetBool(PrefKey_HasPackagesToImport, true);
        EditorPrefs.SetString(PrefKey_PackagePaths, pathsStr);
        EditorPrefs.SetString(PrefKey_PackageVersions, versionsStr);
        EditorPrefs.SetInt(PrefKey_CurrentImportIndex, 0);
        EditorPrefs.SetInt(PrefKey_TotalPackages, paths.Count);

        Debug.Log($"[PackageUpdater] ✓ Saved to EditorPrefs:");
        Debug.Log($"[PackageUpdater]   HasPackagesToImport: {EditorPrefs.GetBool(PrefKey_HasPackagesToImport)}");
        Debug.Log($"[PackageUpdater]   CurrentImportIndex: {EditorPrefs.GetInt(PrefKey_CurrentImportIndex)}");
        Debug.Log($"[PackageUpdater]   TotalPackages: {EditorPrefs.GetInt(PrefKey_TotalPackages)}");
        Debug.Log($"[PackageUpdater]   PackagePaths length: {pathsStr.Length}");
        Debug.Log($"[PackageUpdater]   PackageVersions length: {versionsStr.Length}");
        Debug.Log($"[PackageUpdater] ===== SaveDownloadedPackages END =====");
    }

    /// <summary>
    /// Clears the saved import progress from EditorPrefs.
    /// </summary>
    private void ClearImportProgress()
    {
        Debug.Log($"[PackageUpdater] ===== ClearImportProgress START =====");
        Debug.Log($"[PackageUpdater] Current EditorPrefs values before clearing:");
        Debug.Log($"[PackageUpdater]   HasPackagesToImport: {EditorPrefs.GetBool(PrefKey_HasPackagesToImport, false)}");
        Debug.Log($"[PackageUpdater]   CurrentImportIndex: {EditorPrefs.GetInt(PrefKey_CurrentImportIndex, -1)}");
        Debug.Log($"[PackageUpdater]   TotalPackages: {EditorPrefs.GetInt(PrefKey_TotalPackages, -1)}");

        EditorPrefs.DeleteKey(PrefKey_HasPackagesToImport);
        EditorPrefs.DeleteKey(PrefKey_PackagePaths);
        EditorPrefs.DeleteKey(PrefKey_PackageVersions);
        EditorPrefs.DeleteKey(PrefKey_CurrentImportIndex);
        EditorPrefs.DeleteKey(PrefKey_TotalPackages);
        EditorPrefs.DeleteKey(PrefKey_DeletedAssets);

        Debug.Log($"[PackageUpdater] ✓ All import progress keys deleted from EditorPrefs");
        Debug.Log($"[PackageUpdater] ===== ClearImportProgress END =====");
    }

    /// <summary>
    /// Checks if the remote version is newer than the local version.
    /// </summary>
    private bool CheckIfUpdateAvailable(string remoteVersion)
    {
        if (string.IsNullOrEmpty(remoteVersion))
            return false;

        ProjectDataScriptableObject pso = Resources.Load<ProjectDataScriptableObject>("Project Data SO");
        if (pso == null || string.IsNullOrEmpty(pso.projectData.currentPackageVersion))
        {
            // If we can't get local version, assume update is available
            return true;
        }

        string localVersion = pso.projectData.currentPackageVersion;
        return IsNewerVersion(remoteVersion, localVersion);
    }

    /// <summary>
    /// Compares two version strings to determine if remote is newer than local.
    /// </summary>
    private bool IsNewerVersion(string remoteVersion, string localVersion)
    {
        try
        {
            // Try semantic version comparison (e.g., "1.2.3")
            Version remoteV = ParseVersion(remoteVersion);
            Version localV = ParseVersion(localVersion);

            if (remoteV != null && localV != null)
            {
                return remoteV > localV;
            }

            // Fallback to string comparison
            return string.Compare(remoteVersion, localVersion, StringComparison.OrdinalIgnoreCase) > 0;
        }
        catch
        {
            // If parsing fails, use string comparison
            return string.Compare(remoteVersion, localVersion, StringComparison.OrdinalIgnoreCase) > 0;
        }
    }

    /// <summary>
    /// Parses a version string to System.Version object.
    /// </summary>
    private Version ParseVersion(string versionStr)
    {
        if (string.IsNullOrEmpty(versionStr))
            return null;

        // Remove any "v" prefix
        versionStr = versionStr.TrimStart('v', 'V').Trim();

        // Try parsing as System.Version
        if (Version.TryParse(versionStr, out Version v))
            return v;

        return null;
    }

    /// <summary>
    /// Updates the current package version in ProjectDataScriptableObject after successful package import.
    /// </summary>
    private void UpdatePackageVersion(string newVersion)
    {
        Debug.Log($"[PackageUpdater] [UpdatePackageVersion] Updating version to: {newVersion}");
        ProjectDataScriptableObject pso = Resources.Load<ProjectDataScriptableObject>("Project Data SO");
        if (pso != null)
        {
            string oldVersion = pso.projectData.currentPackageVersion;
            pso.projectData.currentPackageVersion = string.IsNullOrEmpty(newVersion) ? "1.0.0" : newVersion;
            EditorUtility.SetDirty(pso);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PackageUpdater] [UpdatePackageVersion] ✓ Version updated: {oldVersion} -> {pso.projectData.currentPackageVersion}");
        }
        else
        {
            Debug.LogWarning($"[PackageUpdater] [UpdatePackageVersion] ✗ ProjectDataScriptableObject not found at 'Project Data SO'. Version not updated.");
        }
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

    private IEnumerator InstallWebXRPackage()
    {
        isInstallingWebXR = true;
        statusMessage = "Checking scoped registry for WebXR...";
        Repaint();

        if (!EnsureWebXRScopedRegistry())
        {
            statusMessage = "Failed to configure OpenUPM scoped registry.";
            isInstallingWebXR = false;
            Repaint();
            yield break;
        }

        statusMessage = $"Installing {webxrPackageId}...";
        Repaint();

        var addRequest = Client.Add(webxrPackageId);
        while (!addRequest.IsCompleted)
        {
            yield return null;
        }

        if (addRequest.Status == StatusCode.Success)
        {
            statusMessage = "WebXR package installed successfully.";
            isWebXRInstalled = true;
            Debug.Log($"[PackageUpdater] Installed {webxrPackageId} successfully.");
        }
        else
        {
            string errorMessage = addRequest.Error != null ? addRequest.Error.message : "Unknown error";
            statusMessage = $"Failed to install {webxrPackageId}.";
            Debug.LogError($"[PackageUpdater] Failed to install {webxrPackageId}: {errorMessage}");
        }

        isInstallingWebXR = false;
        Repaint();
    }

    private bool EnsureWebXRScopedRegistry()
    {
        try
        {
            string manifestPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Packages", "manifest.json");
            if (!File.Exists(manifestPath))
            {
                Debug.LogError($"[PackageUpdater] manifest.json not found at: {manifestPath}");
                return false;
            }

            string manifestText = File.ReadAllText(manifestPath);
            JObject manifestJson = JObject.Parse(manifestText);

            JArray scopedRegistries = manifestJson["scopedRegistries"] as JArray;
            if (scopedRegistries == null)
            {
                scopedRegistries = new JArray();
                manifestJson["scopedRegistries"] = scopedRegistries;
            }

            JObject openUpmRegistry = null;
            foreach (JToken registryToken in scopedRegistries)
            {
                if (!(registryToken is JObject registryObject))
                    continue;

                string url = registryObject.Value<string>("url");
                string name = registryObject.Value<string>("name");
                if (string.Equals(url, openUpmRegistryUrl, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, openUpmRegistryName, StringComparison.OrdinalIgnoreCase))
                {
                    openUpmRegistry = registryObject;
                    break;
                }
            }

            bool manifestChanged = false;

            if (openUpmRegistry == null)
            {
                openUpmRegistry = new JObject
                {
                    ["name"] = openUpmRegistryName,
                    ["url"] = openUpmRegistryUrl,
                    ["scopes"] = new JArray(webxrScopePrefix)
                };
                scopedRegistries.Add(openUpmRegistry);
                manifestChanged = true;
            }
            else
            {
                JArray scopes = openUpmRegistry["scopes"] as JArray;
                if (scopes == null)
                {
                    scopes = new JArray();
                    openUpmRegistry["scopes"] = scopes;
                    manifestChanged = true;
                }

                bool hasScope = false;
                foreach (JToken scopeToken in scopes)
                {
                    string scopeValue = scopeToken.ToString();
                    if (string.Equals(scopeValue, webxrScopePrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        hasScope = true;
                        break;
                    }
                }

                if (!hasScope)
                {
                    scopes.Add(webxrScopePrefix);
                    manifestChanged = true;
                }
            }

            if (manifestChanged)
            {
                File.WriteAllText(manifestPath, manifestJson.ToString());
                Debug.Log("[PackageUpdater] Updated manifest.json with OpenUPM scoped registry for WebXR.");
                AssetDatabase.Refresh();
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[PackageUpdater] Failed to update scoped registry: {e.Message}");
            return false;
        }
    }

    private void RefreshWebXRInstalledState()
    {
        UnityEditor.PackageManager.PackageInfo[] packages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
        isWebXRInstalled = packages != null && packages.Any(pkg => pkg.name == webxrPackageId);
    }
}
