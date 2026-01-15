using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Unity.EditorCoroutines.Editor;

/// <summary>
/// Automatically checks for SDK updates when Unity starts.
/// Shows a dialog if a newer version is available on the server.
/// </summary>
[InitializeOnLoad]
public static class VertexForm3DUpdateChecker
{
    private const string LastCheckKey = "VertexForm3D_LastUpdateCheck";
    private const double CheckIntervalHours = 24; // Check once per day
    private static bool hasCheckedThisSession = false;

    static VertexForm3DUpdateChecker()
    {
        // Called whenever Unity loads the editor assemblies
        EditorApplication.update += OnEditorUpdate;
    }

    private static void OnEditorUpdate()
    {
        // Only check once per Unity session
        if (hasCheckedThisSession)
        {
            EditorApplication.update -= OnEditorUpdate;
            return;
        }

        hasCheckedThisSession = true;
        EditorApplication.update -= OnEditorUpdate;

        // Check if we should check now (respects 24-hour interval)
        if (!ShouldCheckNow())
            return;

        // Start async check
        EditorCoroutineUtility.StartCoroutineOwnerless(CheckForUpdatesCoroutine());
    }

    private static bool ShouldCheckNow()
    {
        var lastCheckStr = EditorPrefs.GetString(LastCheckKey, string.Empty);
        if (string.IsNullOrEmpty(lastCheckStr))
            return true;

        if (!double.TryParse(lastCheckStr, out var lastOADate))
            return true;

        var lastTime = DateTime.FromOADate(lastOADate);
        var timeSinceLastCheck = DateTime.UtcNow - lastTime;
        return timeSinceLastCheck.TotalHours >= CheckIntervalHours;
    }

    private static IEnumerator CheckForUpdatesCoroutine()
    {
        // Update last check time
        EditorPrefs.SetString(LastCheckKey, DateTime.UtcNow.ToOADate().ToString());

        // Load project data
        ProjectDataScriptableObject pso = Resources.Load<ProjectDataScriptableObject>("Project Data SO");
        if (pso == null)
        {
            Debug.LogWarning("VertexForm3D: ProjectDataScriptableObject not found. Skipping update check.");
            yield break;
        }

        string localVersion = pso.projectData.currentPackageVersion;
        string versionJsonUrl = pso.projectData.versionJsonUrl;

        if (string.IsNullOrEmpty(localVersion))
        {
            Debug.LogWarning("VertexForm3D: Current package version not set. Please set it in Vertex Form 3D > Addressables Management.");
            yield break;
        }

        if (string.IsNullOrEmpty(versionJsonUrl))
        {
            Debug.LogWarning("VertexForm3D: Version JSON URL not set. Please set it in Vertex Form 3D > Addressables Management.");
            yield break;
        }

        // Fetch version.json from server
        string fullUrl = versionJsonUrl + "?nocache=" + DateTime.UtcNow.Ticks;
        using (UnityWebRequest request = UnityWebRequest.Get(fullUrl))
        {
            request.SetRequestHeader("Cache-Control", "no-cache");
            request.SetRequestHeader("Pragma", "no-cache");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"VertexForm3D: Failed to check for updates: {request.error}");
                yield break;
            }

            try
            {
                string json = request.downloadHandler.text;
                var versionData = VersionJsonParser.ParseVersionJson(json);

                if (versionData == null || versionData.Length == 0)
                {
                    Debug.LogWarning("VertexForm3D: No package data found in version.json");
                    yield break;
                }

                // Find the SDK package (usually the first one, or match by name)
                VersionPackageInfo latestPackage = versionData[0];
                foreach (var pkg in versionData)
                {
                    if (pkg.name.Contains("VertexForm3D") || pkg.name.Contains("Vertex Form 3D"))
                    {
                        latestPackage = pkg;
                        break;
                    }
                }

                string remoteVersion = latestPackage.version;
                string packageUrl = latestPackage.url;
                string releaseNotes = latestPackage.releaseNotes ?? "No release notes available.";

                // Compare versions
                if (IsNewerVersion(remoteVersion, localVersion))
                {
                    // Show update dialog with release notes
                    string message = $"A new SDK version is available!\n\n" +
                                    $"Current Version: {localVersion}\n" +
                                    $"Latest Version: {remoteVersion}\n\n" +
                                    $"Release Notes:\n{releaseNotes}\n\n" +
                                    $"Would you like to update now?";

                    int result = EditorUtility.DisplayDialogComplex(
                        "Vertex Form 3D SDK Update Available",
                        message,
                        "Update Now",
                        "Later",
                        "Open Update Window"
                    );

                    if (result == 0) // Update Now
                    {
                        // Trigger update via PackageUpdaterWindow
                        PackageUpdaterWindow.ShowWindowAndUpdate(packageUrl, remoteVersion, releaseNotes);
                    }
                    else if (result == 2) // Open Update Window
                    {
                        PackageUpdaterWindow.ShowWindow();
                    }
                }
                else
                {
                    Debug.Log($"VertexForm3D SDK is up to date (Version {localVersion}).");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"VertexForm3D: Error parsing version.json: {e.Message}");
            }
        }
    }

    private static bool IsNewerVersion(string remoteVersion, string localVersion)
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

    private static Version ParseVersion(string versionStr)
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
}

/// <summary>
/// Helper class to parse version.json array format:
/// [{ "name": "...", "version": "...", "url": "...", "releaseNotes": "..." }]
/// </summary>
[Serializable]
public class VersionPackageInfo
{
    public string name;
    public string version;
    public string url;
    public string releaseNotes;
}

public static class VersionJsonParser
{
    /// <summary>
    /// Parses version.json array format: [{ "name": "...", "version": "...", "url": "...", "releaseNotes": "..." }]
    /// Unity's JsonUtility doesn't support arrays directly, so we parse manually.
    /// </summary>
    public static VersionPackageInfo[] ParseVersionJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            json = json.Trim();
            if (!json.StartsWith("["))
            {
                Debug.LogWarning("VertexForm3D: version.json should start with '['");
                return null;
            }

            // Remove outer brackets
            string arrayContent = json.TrimStart('[').TrimEnd(']').Trim();
            if (string.IsNullOrEmpty(arrayContent))
                return new VersionPackageInfo[0];

            var packages = new System.Collections.Generic.List<VersionPackageInfo>();

            // Find all JSON objects in the array by looking for balanced braces
            int braceCount = 0;
            int startIndex = -1;

            for (int i = 0; i < arrayContent.Length; i++)
            {
                char c = arrayContent[i];

                if (c == '{')
                {
                    if (braceCount == 0)
                        startIndex = i;
                    braceCount++;
                }
                else if (c == '}')
                {
                    braceCount--;
                    if (braceCount == 0 && startIndex >= 0)
                    {
                        // Found a complete object
                        string objJson = arrayContent.Substring(startIndex, i - startIndex + 1);
                        try
                        {
                            var pkg = JsonUtility.FromJson<VersionPackageInfo>(objJson);
                            if (pkg != null && !string.IsNullOrEmpty(pkg.version))
                            {
                                packages.Add(pkg);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"VertexForm3D: Failed to parse package entry: {ex.Message}");
                        }
                        startIndex = -1;
                    }
                }
            }

            return packages.ToArray();
        }
        catch (Exception e)
        {
            Debug.LogError($"VertexForm3D: Failed to parse version.json: {e.Message}");
            return null;
        }
    }
}

