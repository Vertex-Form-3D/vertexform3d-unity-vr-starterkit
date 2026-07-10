using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Unity.EditorCoroutines.Editor;
using Process = System.Diagnostics.Process;

/// <summary>
/// Automatically checks for SDK updates every time Unity starts.
/// Shows a dialog if a newer version is available on the server.
/// </summary>
[InitializeOnLoad]
public static class VertexForm3DUpdateChecker
{
    private const string SessionCheckKey = "VertexForm3D_SessionCheck";
    private const string PrefKey_SuppressAutomaticUpdatePrompt = "VertexForm3D_SuppressAutomaticUpdatePrompt";
    private static bool hasCheckedThisSession = false;

    /// <summary>
    /// Clears the "do not show again" preference so startup update prompts resume.
    /// </summary>
    public static void ClearAutomaticUpdatePromptSuppression()
    {
        EditorPrefs.DeleteKey(PrefKey_SuppressAutomaticUpdatePrompt);
    }

    internal static void ApplyDoNotShowAutomaticUpdatePrompt(bool doNotShowAgain)
    {
        if (doNotShowAgain)
            EditorPrefs.SetBool(PrefKey_SuppressAutomaticUpdatePrompt, true);
    }

    static VertexForm3DUpdateChecker()
    {
        // Called whenever Unity loads the editor assemblies
        EditorApplication.update += OnEditorUpdate;
    }

    private static void OnEditorUpdate()
    {
        // Only check once per Unity session (not on every compile)
        if (hasCheckedThisSession)
        {
            EditorApplication.update -= OnEditorUpdate;
            return;
        }

        // Check if we've already checked in this Unity session
        // Use the actual process start time to detect if Unity was restarted vs just recompiled
        try
        {
            DateTime currentProcessStartTime = Process.GetCurrentProcess().StartTime;
            string storedStartTimeStr = EditorPrefs.GetString(SessionCheckKey, string.Empty);

            if (!string.IsNullOrEmpty(storedStartTimeStr))
            {
                if (DateTime.TryParse(storedStartTimeStr, out DateTime storedStartTime))
                {
                    // If the process start time matches, we're in the same Unity session (just recompiled)
                    if (Math.Abs((currentProcessStartTime - storedStartTime).TotalSeconds) < 1.0)
                    {
                        // Same Unity process, just a recompile - skip check
                        EditorApplication.update -= OnEditorUpdate;
                        return;
                    }
                }
            }

            // New Unity session - store the process start time and check for updates
            EditorPrefs.SetString(SessionCheckKey, currentProcessStartTime.ToString("O"));
        }
        catch
        {
            // If we can't get process info, fall back to checking anyway
        }

        hasCheckedThisSession = true;
        EditorApplication.update -= OnEditorUpdate;

        // Start async check (checks every time Unity starts)
        EditorCoroutineUtility.StartCoroutineOwnerless(CheckForUpdatesCoroutine());
    }

    /// <summary>
    /// Manually triggers an update check. Can be called from menu items.
    /// </summary>
    public static void CheckForUpdatesManually()
    {
        EditorCoroutineUtility.StartCoroutineOwnerless(CheckForUpdatesCoroutine(forceCheck: true));
    }

    // [MenuItem("VertexForm3D SDK/Enable SDK Update Startup Prompt", false, 16)]
    // private static void MenuEnableSdkUpdateStartupPrompt()
    // {
    //     ClearAutomaticUpdatePromptSuppression();
    //     Debug.Log("VertexForm3D: Automatic SDK update prompts are enabled again on editor startup.");
    // }

    // [MenuItem("VertexForm3D SDK/Enable SDK Update Startup Prompt", true)]
    // private static bool MenuEnableSdkUpdateStartupPromptValidate()
    // {
    //     return EditorPrefs.GetBool(PrefKey_SuppressAutomaticUpdatePrompt, false);
    // }

    private static IEnumerator CheckForUpdatesCoroutine(bool forceCheck = false)
    {
        if (!forceCheck && EditorPrefs.GetBool(PrefKey_SuppressAutomaticUpdatePrompt, false))
            yield break;

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

                // Filter SDK packages
                List<VersionPackageInfo> sdkPackages = new List<VersionPackageInfo>();
                foreach (var pkg in versionData)
                {
                    if (pkg.name != null && (pkg.name.Contains("VertexForm3D") || pkg.name.Contains("Vertex Form 3D")))
                    {
                        sdkPackages.Add(pkg);
                    }
                }

                // If no SDK-specific packages found, use all packages
                if (sdkPackages.Count == 0)
                {
                    sdkPackages.AddRange(versionData);
                }

                // Find all versions newer than current
                List<VersionPackageInfo> newerVersions = new List<VersionPackageInfo>();
                foreach (var pkg in sdkPackages)
                {
                    if (!string.IsNullOrEmpty(pkg.version) && IsNewerVersion(pkg.version, localVersion))
                    {
                        newerVersions.Add(pkg);
                    }
                }

                // Sort versions (oldest first)
                newerVersions.Sort((a, b) =>
                {
                    System.Version vA = ParseVersion(a.version);
                    System.Version vB = ParseVersion(b.version);
                    if (vA != null && vB != null)
                    {
                        return vA.CompareTo(vB);
                    }
                    return string.Compare(a.version ?? "", b.version ?? "", StringComparison.OrdinalIgnoreCase);
                });

                // Find the latest version for display
                VersionPackageInfo latestPackage = newerVersions.Count > 0
                    ? newerVersions[newerVersions.Count - 1]
                    : (sdkPackages.Count > 0 ? sdkPackages[0] : null);

                if (latestPackage == null)
                {
                    Debug.LogWarning("VertexForm3D: No package found in version.json");
                    yield break;
                }

                string remoteVersion = latestPackage.version;
                string packageUrl = latestPackage.url;
                string releaseNotes = latestPackage.releaseNotes ?? "No release notes available.";

                // Check if any updates are available
                if (newerVersions.Count > 0)
                {
                    // Build message showing how many versions will be downloaded
                    string versionsList = "";
                    if (newerVersions.Count <= 5)
                    {
                        versionsList = string.Join(", ", newerVersions.Select(v => v.version));
                    }
                    else
                    {
                        versionsList = $"{newerVersions[0].version} → ... → {newerVersions[newerVersions.Count - 1].version}";
                    }

                    string message = $"New SDK version(s) available!\n\n" +
                                    $"Current Version: {localVersion}\n" +
                                    $"Latest Version: {remoteVersion}\n" +
                                    $"Versions to download: {newerVersions.Count} ({versionsList})\n\n" +
                                    $"Release Notes (Latest):\n{releaseNotes}\n\n" +
                                    $"All intermediate versions will be downloaded sequentially.\n" +
                                    $"Would you like to update now?";

                    VertexForm3DUpdatePromptWindow.ShowPrompt(message);
                }
                else
                {
                    string message = forceCheck
                        ? $"VertexForm3D SDK is up to date!\n\nCurrent Version: {localVersion}\nLatest Version: {remoteVersion}"
                        : $"VertexForm3D SDK is up to date (Version {localVersion}).";

                    if (forceCheck)
                    {
                        EditorUtility.DisplayDialog("Vertex Form 3D SDK Update Check", message, "OK");
                    }
                    else
                    {
                        Debug.Log(message);
                    }
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
            System.Version remoteV = ParseVersion(remoteVersion);
            System.Version localV = ParseVersion(localVersion);

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

    private static System.Version ParseVersion(string versionStr)
    {
        if (string.IsNullOrEmpty(versionStr))
            return null;

        // Remove any "v" prefix
        versionStr = versionStr.TrimStart('v', 'V').Trim();

        // Try parsing as System.Version
        if (System.Version.TryParse(versionStr, out System.Version v))
            return v;

        return null;
    }

}

/// <summary>
/// Startup update prompt with a "do not show again" option (EditorUtility dialogs cannot show toggles).
/// </summary>
public class VertexForm3DUpdatePromptWindow : EditorWindow
{
    private string bodyText;
    private Vector2 scrollPosition;
    private bool doNotShowAgain;
    private GUIStyle richTextWrappedLabel;

    public static void ShowPrompt(string body)
    {
        var window = CreateInstance<VertexForm3DUpdatePromptWindow>();
        window.bodyText = body ?? string.Empty;
        window.titleContent = new GUIContent("Vertex Form 3D SDK Update Available");
        window.minSize = new Vector2(460, 320);
        window.ShowUtility();
    }

    private void OnGUI()
    {
        if (richTextWrappedLabel == null)
        {
            richTextWrappedLabel = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                richText = true
            };
        }

        VertexFormEditorHeader.Draw(position.width);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        EditorGUILayout.LabelField(bodyText, richTextWrappedLabel);
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(6);
        doNotShowAgain = EditorGUILayout.ToggleLeft("Do not show again (automatic startup checks only)", doNotShowAgain);

        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Update Now", GUILayout.Height(30)))
        {
            VertexForm3DUpdateChecker.ApplyDoNotShowAutomaticUpdatePrompt(doNotShowAgain);
            var updater = PackageUpdaterWindow.ShowWindow();
            updater.SetAutoStartDownload(true);
            Close();
        }
        if (GUILayout.Button("Later", GUILayout.Height(30)))
        {
            VertexForm3DUpdateChecker.ApplyDoNotShowAutomaticUpdatePrompt(doNotShowAgain);
            Close();
        }
        if (GUILayout.Button("Open Update Window", GUILayout.Height(30)))
        {
            VertexForm3DUpdateChecker.ApplyDoNotShowAutomaticUpdatePrompt(doNotShowAgain);
            PackageUpdaterWindow.ShowWindow();
            Close();
        }
        EditorGUILayout.EndHorizontal();
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
    // Asset paths (relative to project root, e.g. "Assets/VertexForm3D/...") that were removed
    // in this version. The Package Updater deletes these from user projects after importing.
    public List<string> deletedAssets = new List<string>();
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

