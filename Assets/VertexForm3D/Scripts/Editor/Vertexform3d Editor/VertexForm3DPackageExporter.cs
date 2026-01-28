using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Exports a .unitypackage and generates a version.json manifest next to it.
/// You can then upload both the .unitypackage and version.json to your cloud bucket.
/// </summary>
public class VertexForm3DPackageExporter : EditorWindow
{
    private class AssetItem
    {
        public string path;
        public bool isSelected;
        public bool isFolder;
        public List<AssetItem> children = new List<AssetItem>();
        public AssetItem parent;
        public bool isExpanded = false;

        public AssetItem(string path, bool isFolder, AssetItem parent = null)
        {
            this.path = path;
            this.isFolder = isFolder;
            this.parent = parent;
        }
    }

    private const string PrefKey_PackageName = "VertexForm3D_Exporter_PackageName";
    private const string PrefKey_VersionCode = "VertexForm3D_Exporter_VersionCode";
    private const string PrefKey_ReleaseNotes = "VertexForm3D_Exporter_ReleaseNotes";
    private const string PrefKey_ExportPath = "VertexForm3D_Exporter_ExportPath";
    private const string PrefKey_PackageUrlBase = "VertexForm3D_Exporter_PackageUrlBase";
    private const string PrefKey_ShowAssetSelector = "VertexForm3D_Exporter_ShowAssetSelector";
    private const string PrefKey_ShowOnlySelected = "VertexForm3D_Exporter_ShowOnlySelected";
    private const string PrefKey_IncludeDependencies = "VertexForm3D_Exporter_IncludeDependencies";
    private const string PrefKey_SelectedAssetPaths = "VertexForm3D_Exporter_SelectedAssetPaths";

    private string packageName = "VertexForm3D_SDK";
    private string versionCode = "1.0.0";
    private string releaseNotes = "Switch to OpenXR.\nFixed minor bugs in the rendering pipeline.\nHand Tracking, Teleportation Using Hands.\nCategorized UI.";
    private string exportRelativePath = "Builds"; // relative to project root
    private string packageUrlBase = "https://storage.googleapis.com/your_bucket_name/";

    // Asset tree for selection
    private AssetItem rootItem;
    private Vector2 assetScrollPosition = Vector2.zero;
    private Vector2 mainScrollPosition = Vector2.zero;
    private Dictionary<string, AssetItem> assetItemMap = new Dictionary<string, AssetItem>();
    private bool showAssetSelector = true;
    private bool showOnlySelected = false;
    private string searchFilter = "";
    private bool includeDependencies = true; // Include dependencies by default

    //[MenuItem("VertexForm3D SDK/Export SDK Package", false, 15)]
    public static void ShowWindow()
    {
        GetWindow<VertexForm3DPackageExporter>("Export SDK Package");
    }

    private void OnEnable()
    {
        // Load saved values from EditorPrefs
        packageName = EditorPrefs.GetString(PrefKey_PackageName, "VertexForm3D_SDK");
        versionCode = EditorPrefs.GetString(PrefKey_VersionCode, "1.0.0");
        releaseNotes = EditorPrefs.GetString(PrefKey_ReleaseNotes, "Switch to OpenXR.\nFixed minor bugs in the rendering pipeline.\nHand Tracking, Teleportation Using Hands.\nCategorized UI.");
        exportRelativePath = EditorPrefs.GetString(PrefKey_ExportPath, "Builds");
        packageUrlBase = EditorPrefs.GetString(PrefKey_PackageUrlBase, "https://storage.googleapis.com/your_bucket_name/");
        showAssetSelector = EditorPrefs.GetBool(PrefKey_ShowAssetSelector, true);
        showOnlySelected = EditorPrefs.GetBool(PrefKey_ShowOnlySelected, false);
        includeDependencies = EditorPrefs.GetBool(PrefKey_IncludeDependencies, true);

        // Initialize asset tree
        RefreshAssetList();

        // Restore selection state after building the tree
        RestoreSelectionState();
    }

    private void OnDisable()
    {
        // Save values to EditorPrefs when window closes
        SavePreferences();
        SaveSelectionState();
    }

    private void SavePreferences()
    {
        EditorPrefs.SetString(PrefKey_PackageName, packageName);
        EditorPrefs.SetString(PrefKey_VersionCode, versionCode);
        EditorPrefs.SetString(PrefKey_ReleaseNotes, releaseNotes);
        EditorPrefs.SetString(PrefKey_ExportPath, exportRelativePath);
        EditorPrefs.SetString(PrefKey_PackageUrlBase, packageUrlBase);
        EditorPrefs.SetBool(PrefKey_ShowAssetSelector, showAssetSelector);
        EditorPrefs.SetBool(PrefKey_ShowOnlySelected, showOnlySelected);
        EditorPrefs.SetBool(PrefKey_IncludeDependencies, includeDependencies);
    }

    private void RefreshAssetList()
    {
        const string rootPath = "Assets/VertexForm3D";
        rootItem = new AssetItem(rootPath, true);
        rootItem.isExpanded = true; // Expand root folder so users can see the structure
        assetItemMap.Clear();
        assetItemMap[rootPath] = rootItem;

        string[] allAssets = AssetDatabase.GetAllAssetPaths();

        foreach (string assetPath in allAssets)
        {
            if (assetPath.StartsWith(rootPath + "/") || assetPath == rootPath)
            {
                AddAssetToTree(assetPath, rootPath);
            }
        }

        // Sort children
        SortChildrenRecursive(rootItem);
    }

    private void SaveSelectionState()
    {
        List<string> selectedPaths = GetSelectedAssetPaths();
        string pathsJson = string.Join("|", selectedPaths);
        EditorPrefs.SetString(PrefKey_SelectedAssetPaths, pathsJson);
    }

    private void RestoreSelectionState()
    {
        string savedPathsJson = EditorPrefs.GetString(PrefKey_SelectedAssetPaths, "");
        if (string.IsNullOrEmpty(savedPathsJson))
            return;

        string[] savedPaths = savedPathsJson.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
        HashSet<string> pathsSet = new HashSet<string>(savedPaths);

        // Restore selection for all matching paths
        RestoreSelectionRecursive(rootItem, pathsSet);
    }

    private void RestoreSelectionRecursive(AssetItem item, HashSet<string> selectedPaths)
    {
        if (selectedPaths.Contains(item.path))
        {
            item.isSelected = true;
        }

        foreach (var child in item.children)
        {
            RestoreSelectionRecursive(child, selectedPaths);
        }
    }

    private void AddAssetToTree(string assetPath, string rootPath)
    {
        if (assetItemMap.ContainsKey(assetPath))
            return;

        // Skip the root path itself if it's the exact path
        if (assetPath == rootPath)
            return;

        // Get the relative path from the root
        string relativePath = assetPath.Substring(rootPath.Length + 1);
        string[] parts = relativePath.Split('/');
        string currentPath = rootPath;

        for (int i = 0; i < parts.Length; i++)
        {
            currentPath += "/" + parts[i];

            if (!assetItemMap.ContainsKey(currentPath))
            {
                bool isFolder = Directory.Exists(currentPath);
                AssetItem parent = i > 0 ? assetItemMap[currentPath.Substring(0, currentPath.LastIndexOf('/'))] : rootItem;
                AssetItem item = new AssetItem(currentPath, isFolder, parent);
                assetItemMap[currentPath] = item;
                parent.children.Add(item);
            }
        }
    }

    private void SortChildrenRecursive(AssetItem item)
    {
        item.children.Sort((a, b) =>
        {
            // Folders first, then files
            if (a.isFolder != b.isFolder)
                return a.isFolder ? -1 : 1;
            return a.path.CompareTo(b.path);
        });

        foreach (var child in item.children)
        {
            if (child.isFolder)
            {
                SortChildrenRecursive(child);
            }
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Export Vertex Form 3D SDK", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Main scroll view for entire window content
        mainScrollPosition = EditorGUILayout.BeginScrollView(mainScrollPosition);

        EditorGUILayout.HelpBox(
            "This tool will:\n" +
            "1) Export a .unitypackage from the selected files/folders.\n" +
            "2) Create a version.json file next to the package with name, version, and URL.\n\n" +
            "Select files/folders using the asset selector below.\n" +
            "After export, upload both files to your cloud bucket.",
            MessageType.Info);

        EditorGUILayout.Space();

        // Asset Selector Section
        EditorGUILayout.BeginHorizontal();
        showAssetSelector = EditorGUILayout.Foldout(showAssetSelector, "Asset Selector", true);
        if (GUILayout.Button("Refresh", GUILayout.Width(60)))
        {
            // Save current selection before refreshing
            SaveSelectionState();
            RefreshAssetList();
            // Restore selection after refreshing
            RestoreSelectionState();
            Repaint();
        }
        EditorGUILayout.EndHorizontal();

        if (showAssetSelector)
        {
            EditorGUILayout.Space(5);

            // Toolbar
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            showOnlySelected = GUILayout.Toggle(showOnlySelected, "Show Selected Only", EditorStyles.toolbarButton);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // Search bar
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Search:", GUILayout.Width(50));
            searchFilter = EditorGUILayout.TextField(searchFilter);
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                searchFilter = "";
            }
            EditorGUILayout.EndHorizontal();

            // Selection info
            int selectedCount = GetSelectedCount();
            EditorGUILayout.LabelField($"Selected: {selectedCount} items", EditorStyles.helpBox);

            // Action buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All"))
            {
                SelectAllRecursive(rootItem, true);
                SaveSelectionState();
                Repaint();
            }
            if (GUILayout.Button("Deselect All"))
            {
                SelectAllRecursive(rootItem, false);
                SaveSelectionState();
                Repaint();
            }
            if (GUILayout.Button("Invert Selection"))
            {
                InvertSelectionRecursive(rootItem);
                SaveSelectionState();
                Repaint();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Asset list with scroll view
            assetScrollPosition = EditorGUILayout.BeginScrollView(assetScrollPosition, GUILayout.Height(300));

            if (rootItem != null)
            {
                DrawAssetItem(rootItem, 0);
            }

            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.Space();

        string newPackageName = EditorGUILayout.TextField("Package Name", packageName);
        if (newPackageName != packageName)
        {
            packageName = newPackageName;
            EditorPrefs.SetString(PrefKey_PackageName, packageName);
        }

        string newVersionCode = EditorGUILayout.TextField("Version Code", versionCode);
        if (newVersionCode != versionCode)
        {
            versionCode = newVersionCode;
            EditorPrefs.SetString(PrefKey_VersionCode, versionCode);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Release Notes", EditorStyles.boldLabel);
        string newReleaseNotes = EditorGUILayout.TextArea(releaseNotes, GUILayout.Height(100));
        if (newReleaseNotes != releaseNotes)
        {
            releaseNotes = newReleaseNotes;
            EditorPrefs.SetString(PrefKey_ReleaseNotes, releaseNotes);
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Export Location (Relative to Project Root)", EditorStyles.boldLabel);
        string newExportPath = EditorGUILayout.TextField("Folder", exportRelativePath);
        if (newExportPath != exportRelativePath)
        {
            exportRelativePath = newExportPath;
            EditorPrefs.SetString(PrefKey_ExportPath, exportRelativePath);
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Remote URL Settings", EditorStyles.boldLabel);
        string newPackageUrlBase = EditorGUILayout.TextField("Package URL Base", packageUrlBase);
        if (newPackageUrlBase != packageUrlBase)
        {
            packageUrlBase = newPackageUrlBase;
            EditorPrefs.SetString(PrefKey_PackageUrlBase, packageUrlBase);
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Export Options", EditorStyles.boldLabel);
        bool newIncludeDependencies = EditorGUILayout.Toggle("Include Dependencies", includeDependencies);
        if (newIncludeDependencies != includeDependencies)
        {
            includeDependencies = newIncludeDependencies;
            EditorPrefs.SetBool(PrefKey_IncludeDependencies, includeDependencies);
        }
        EditorGUILayout.HelpBox(
            includeDependencies
                ? "Dependencies will be automatically included (materials, textures, scripts, etc. that selected assets depend on)."
                : "Only the selected files/folders will be exported. Dependencies will NOT be included.",
            MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("Export Package + Generate version.json", GUILayout.Height(35)))
        {
            ExportPackageAndCreateManifest();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawAssetItem(AssetItem item, int indent)
    {
        // Apply search filter
        if (!string.IsNullOrEmpty(searchFilter))
        {
            if (!item.path.ToLower().Contains(searchFilter.ToLower()))
            {
                // Check if any children match
                bool hasMatchingChild = HasMatchingChild(item);
                if (!hasMatchingChild)
                    return;
            }
        }

        // Apply "show only selected" filter
        if (showOnlySelected && !item.isSelected)
        {
            bool hasSelectedChild = HasSelectedChild(item);
            if (!hasSelectedChild)
                return;
        }

        Rect rect = EditorGUILayout.GetControlRect();
        float x = rect.x + indent * 10f;
        float foldoutWidth = 18f;
        float toggleWidth = 18f;

        // Expand/Collapse button for folders
        if (item.isFolder)
        {
            Rect foldoutRect = new Rect(x, rect.y, foldoutWidth, rect.height);
            item.isExpanded = EditorGUI.Foldout(foldoutRect, item.isExpanded, "", true);
            x = foldoutRect.xMax;
        }
        else
        {
            x += foldoutWidth;
        }

        // Toggle button
        Rect toggleRect = new Rect(x, rect.y, toggleWidth, rect.height);
        bool newSelection = EditorGUI.Toggle(toggleRect, item.isSelected);

        if (newSelection != item.isSelected)
        {
            SetSelectionRecursive(item, newSelection);
            SaveSelectionState(); // Save selection state when it changes
            Repaint();
        }

        x += toggleWidth;

        // Icon and name
        string displayName = Path.GetFileName(item.path);
        if (string.IsNullOrEmpty(displayName))
            displayName = item.path;

        GUIContent content = new GUIContent(
            displayName,
            item.isFolder ? GetFolderIcon() : AssetDatabase.GetCachedIcon(item.path)
        );

        Rect labelRect = new Rect(x, rect.y, rect.width - (x - rect.x), rect.height);
        EditorGUI.LabelField(labelRect, content);

        // Draw children if expanded
        if (item.isFolder && item.isExpanded)
        {
            foreach (var child in item.children)
            {
                DrawAssetItem(child, indent + 1);
            }
        }
    }

    private Texture GetFolderIcon()
    {
        return EditorGUIUtility.IconContent("Folder Icon").image;
    }

    private void SetSelectionRecursive(AssetItem item, bool selected)
    {
        item.isSelected = selected;

        // If selecting a folder, select all children
        if (item.isFolder)
        {
            foreach (var child in item.children)
            {
                SetSelectionRecursive(child, selected);
            }
        }

        // Update parent state based on children
        UpdateParentSelection(item);
    }

    private void UpdateParentSelection(AssetItem item)
    {
        if (item.parent == null)
            return;

        // Check if all siblings are selected
        bool allSiblingsSelected = true;
        bool anySiblingSelected = false;

        foreach (var sibling in item.parent.children)
        {
            if (sibling.isSelected)
                anySiblingSelected = true;
            else
                allSiblingsSelected = false;
        }

        // Update parent based on children state
        if (allSiblingsSelected)
        {
            item.parent.isSelected = true;
        }
        else if (anySiblingSelected)
        {
            // Mixed state - leave parent unselected if not all children are selected
        }

        UpdateParentSelection(item.parent);
    }

    private void SelectAllRecursive(AssetItem item, bool selected)
    {
        item.isSelected = selected;
        foreach (var child in item.children)
        {
            SelectAllRecursive(child, selected);
        }
    }

    private void InvertSelectionRecursive(AssetItem item)
    {
        item.isSelected = !item.isSelected;
        foreach (var child in item.children)
        {
            InvertSelectionRecursive(child);
        }
    }

    private int GetSelectedCount()
    {
        int count = 0;
        CountSelectedRecursive(rootItem, ref count);
        return count;
    }

    private void CountSelectedRecursive(AssetItem item, ref int count)
    {
        if (item.isSelected)
            count++;

        foreach (var child in item.children)
        {
            CountSelectedRecursive(child, ref count);
        }
    }

    private bool HasMatchingChild(AssetItem item)
    {
        if (!item.isFolder)
            return false;

        foreach (var child in item.children)
        {
            if (child.path.ToLower().Contains(searchFilter.ToLower()))
                return true;
            if (HasMatchingChild(child))
                return true;
        }
        return false;
    }

    private bool HasSelectedChild(AssetItem item)
    {
        if (item.isSelected)
            return true;

        if (!item.isFolder)
            return false;

        foreach (var child in item.children)
        {
            if (HasSelectedChild(child))
                return true;
        }
        return false;
    }

    private List<string> GetSelectedAssetPaths()
    {
        List<string> selectedPaths = new List<string>();
        CollectSelectedPaths(rootItem, selectedPaths);
        return selectedPaths;
    }

    private void CollectSelectedPaths(AssetItem item, List<string> paths)
    {
        if (item.isSelected)
        {
            paths.Add(item.path);
        }

        foreach (var child in item.children)
        {
            CollectSelectedPaths(child, paths);
        }
    }

    private void ExportPackageAndCreateManifest()
    {
        if (string.IsNullOrEmpty(packageName))
        {
            EditorUtility.DisplayDialog("Error", "Package Name cannot be empty.", "OK");
            return;
        }

        if (string.IsNullOrEmpty(versionCode))
        {
            EditorUtility.DisplayDialog("Error", "Version Code cannot be empty.", "OK");
            return;
        }

        // Get selected assets from the tree
        List<string> selectedAssetPaths = GetSelectedAssetPaths();
        if (selectedAssetPaths.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "No assets selected. Please select files or folders to export using the Asset Selector.", "OK");
            return;
        }

        // Validate that all selected paths exist
        foreach (string assetPath in selectedAssetPaths)
        {
            if (string.IsNullOrEmpty(assetPath) || !AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath))
            {
                EditorUtility.DisplayDialog("Error", $"Invalid asset path: '{assetPath}'.", "OK");
                return;
            }
        }

        // Convert list to array for ExportPackage
        string[] assetPathsArray = selectedAssetPaths.ToArray();

        // Ensure export folder exists on disk
        string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
        string exportFolderFullPath = Path.Combine(projectRoot, exportRelativePath);
        if (!Directory.Exists(exportFolderFullPath))
        {
            Directory.CreateDirectory(exportFolderFullPath);
        }

        // Build file names
        string packageFileName = $"{packageName}_v{versionCode}.unitypackage";
        string packageProjectRelativePath = Path.Combine(exportRelativePath, packageFileName).Replace("\\", "/");
        string packageFullPath = Path.Combine(projectRoot, packageProjectRelativePath);

        // Export the .unitypackage
        try
        {
            ExportPackageOptions options = ExportPackageOptions.Recurse;
            if (includeDependencies)
            {
                options |= ExportPackageOptions.IncludeDependencies;
            }

            AssetDatabase.ExportPackage(
                assetPathsArray,
                packageProjectRelativePath,
                options);

            Debug.Log($"Vertex Form 3D SDK exported to: {packageFullPath}");
            Debug.Log($"Exported {selectedAssetPaths.Count} selected asset(s):\n{string.Join("\n", selectedAssetPaths)}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to export package: {e.Message}");
            EditorUtility.DisplayDialog("Error", "Failed to export package. See console for details.", "OK");
            return;
        }

        // Build version.json content (array of packages)
        string packageUrl = CombineUrl(packageUrlBase, packageFileName);
        string versionJsonPath = Path.Combine(exportFolderFullPath, "version.json");

        // Read existing versions if version.json exists
        List<VersionPackageInfo> existingVersions = new List<VersionPackageInfo>();
        if (File.Exists(versionJsonPath))
        {
            try
            {
                string existingJson = File.ReadAllText(versionJsonPath);
                var parsedVersions = VersionJsonParser.ParseVersionJson(existingJson);
                if (parsedVersions != null && parsedVersions.Length > 0)
                {
                    existingVersions.AddRange(parsedVersions);
                    Debug.Log($"Found {existingVersions.Count} existing version(s) in version.json");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to read existing version.json: {e.Message}. Creating new file.");
            }
        }

        // Check if this version already exists
        bool versionExists = existingVersions.Any(v => v.version == versionCode &&
            (v.name == packageName || string.IsNullOrEmpty(v.name)));

        bool shouldAddVersion = true;

        if (versionExists)
        {
            int result = EditorUtility.DisplayDialogComplex(
                "Version Already Exists",
                $"Version {versionCode} already exists in version.json.\n\n" +
                $"Do you want to:\n" +
                $"• Update: Replace the existing entry with new data\n" +
                $"• Skip: Keep existing entry, don't add duplicate\n" +
                $"• Cancel: Abort the export",
                "Update",
                "Cancel",
                "Skip"
            );

            if (result == 1) // Cancel
            {
                return;
            }
            else if (result == 2) // Skip
            {
                Debug.Log($"Skipping version.json update - version {versionCode} already exists.");
                shouldAddVersion = false;
            }
            else // Update (result == 0)
            {
                // Remove existing version entry
                existingVersions.RemoveAll(v => v.version == versionCode &&
                    (v.name == packageName || string.IsNullOrEmpty(v.name)));
                Debug.Log($"Updating existing version entry for {versionCode}");
            }
        }

        // Build the new version entry and add it if needed
        if (shouldAddVersion)
        {
            VersionPackageInfo newVersion = new VersionPackageInfo
            {
                name = packageName,
                version = versionCode,
                url = packageUrl,
                releaseNotes = releaseNotes ?? "No release notes available."
            };
            existingVersions.Add(newVersion);
        }

        // Sort versions by version number (oldest first)
        existingVersions.Sort((a, b) =>
        {
            System.Version vA = ParseVersion(a.version);
            System.Version vB = ParseVersion(b.version);
            if (vA != null && vB != null)
            {
                return vA.CompareTo(vB);
            }
            return string.Compare(a.version ?? "", b.version ?? "", StringComparison.OrdinalIgnoreCase);
        });

        // Build JSON content
        string jsonContent = BuildVersionJsonFromList(existingVersions);

        try
        {
            File.WriteAllText(versionJsonPath, jsonContent);
            Debug.Log($"version.json updated at: {versionJsonPath} ({existingVersions.Count} version(s) total)");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to write version.json: {e.Message}");
            EditorUtility.DisplayDialog("Error", "Failed to write version.json. See console for details.", "OK");
            return;
        }

        EditorUtility.DisplayDialog(
            "Export Complete",
            $"Exported package:\n{packageFullPath}\n\n" +
            $"Created manifest:\n{versionJsonPath}\n\n" +
            $"Exported {selectedAssetPaths.Count} asset(s).",
            "OK");
    }

    private static string CombineUrl(string baseUrl, string fileName)
    {
        if (string.IsNullOrEmpty(baseUrl))
            return fileName;

        baseUrl = baseUrl.TrimEnd('/', '\\');
        return $"{baseUrl}/{fileName}";
    }

    /// <summary>
    /// Builds version.json from a list of VersionPackageInfo objects.
    /// </summary>
    private static string BuildVersionJsonFromList(List<VersionPackageInfo> versions)
    {
        if (versions == null || versions.Count == 0)
        {
            return "[]";
        }

        System.Text.StringBuilder json = new System.Text.StringBuilder();
        json.Append("[\n");

        for (int i = 0; i < versions.Count; i++)
        {
            var v = versions[i];
            string safeName = EscapeJsonString(v.name ?? "");
            string safeVersion = EscapeJsonString(v.version ?? "");
            string safeUrl = EscapeJsonString(v.url ?? "");
            string safeReleaseNotes = EscapeJsonString(v.releaseNotes ?? "No release notes available.");

            json.Append("  {\n");
            json.Append($"    \"name\": \"{safeName}\",\n");
            json.Append($"    \"version\": \"{safeVersion}\",\n");
            json.Append($"    \"url\": \"{safeUrl}\",\n");
            json.Append($"    \"releaseNotes\": \"{safeReleaseNotes}\"\n");
            json.Append("  }");

            if (i < versions.Count - 1)
            {
                json.Append(",");
            }
            json.Append("\n");
        }

        json.Append("]");
        return json.ToString();
    }

    /// <summary>
    /// Legacy method for backward compatibility. Use BuildVersionJsonFromList instead.
    /// </summary>
    private static string BuildVersionJson(string name, string version, string url, string releaseNotes)
    {
        List<VersionPackageInfo> versions = new List<VersionPackageInfo>
        {
            new VersionPackageInfo
            {
                name = name,
                version = version,
                url = url,
                releaseNotes = releaseNotes ?? "No release notes available."
            }
        };
        return BuildVersionJsonFromList(versions);
    }

    /// <summary>
    /// Parses a version string to System.Version object.
    /// </summary>
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

    private static string EscapeJsonString(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
}

