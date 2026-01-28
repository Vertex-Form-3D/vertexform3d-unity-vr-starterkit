using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Diagnostics;

public class AddressablesBuildEditor : EditorWindow
{
    private Texture2D bannerTexture;
    private string addressableCatalogFilePath = "";
    private string catalogFileName = "VertexForm3DAddressablesCatalog";
    private bool useOnlyLocalBundles = true;
    private Vector2 scrollPosition;
    private const string DEFAULT_CATALOG_PATH = "https://storage.googleapis.com/yourproject_bucket/Android/YourProjectAddressablesCatalog.json";

    public static void ShowWindow()
    {
        AddressablesBuildEditor window = GetWindow<AddressablesBuildEditor>("VertexForm3D Addressables");
        window.minSize = new Vector2(500, 600);
        window.Show();
    }

    private void OnEnable()
    {
        bannerTexture = Resources.Load<Texture2D>("VF3DBannerEditor");
        ProjectDataScriptableObject pso = Resources.Load<ProjectDataScriptableObject>("Project Data SO");
        if (pso != null)
        {
            addressableCatalogFilePath = string.IsNullOrEmpty(pso.projectData.addressableCatalogFilePath) ? DEFAULT_CATALOG_PATH : pso.projectData.addressableCatalogFilePath;
            catalogFileName = string.IsNullOrEmpty(pso.projectData.catalogFileName) ? "VertexForm3DAddressablesCatalog" : pso.projectData.catalogFileName;
            useOnlyLocalBundles = pso.projectData.onlyLocalBundles;
        }
        else
        {
            UnityEngine.Debug.LogWarning("ProjectDataScriptableObject not found at 'Project Data SO'. Using defaults.");
            addressableCatalogFilePath = DEFAULT_CATALOG_PATH;
            catalogFileName = "VertexForm3DAddressablesCatalog";
            useOnlyLocalBundles = true;
        }
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));

        // Styles
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 20, alignment = TextAnchor.MiddleCenter, margin = new RectOffset(0, 0, 10, 10) };
        GUIStyle sectionStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, margin = new RectOffset(10, 10, 5, 5) };
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 12, padding = new RectOffset(10, 10, 5, 5), margin = new RectOffset(10, 10, 5, 5) };

        // Banner
        if (bannerTexture != null)
        {
            float bannerWidth = Mathf.Min(bannerTexture.width * 0.8f, position.width - 20);
            float bannerHeight = (bannerWidth / bannerTexture.width) * bannerTexture.height;
            bannerWidth /= 1.2f;
            bannerHeight /= 1.2f;
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(bannerTexture, GUILayout.Width(bannerWidth), GUILayout.Height(bannerHeight));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("Banner image not found. Place 'VF3DBannerEditor' in the Resources folder.", MessageType.Warning);
        }

        // Title
        GUILayout.Label("Addressables Management", titleStyle);
        EditorGUILayout.HelpBox("This window manages the Addressables system for your project, allowing you to configure, build, and manage local and remote asset bundles.", MessageType.Info, true);

        // Build Button
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(new GUIContent("Build Addressables", "Build Addressable assets and rename remote catalog files if applicable"), buttonStyle, GUILayout.Width(400), GUILayout.Height(40)))
        {
            UnityEngine.Debug.Log("Building addressables...");
            BuildAddressablesAndRenameRemoteCatalog();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        EditorGUILayout.Space(10);

        // Tutorials Button
        if (GUILayout.Button(new GUIContent("View Tutorials", "Visit VertexForm3D tutorials for detailed guides on Addressables setup"), buttonStyle, GUILayout.Height(30)))
        {
            Application.OpenURL("https://vertexform3d.com/tutorials/");
        }

        // Addressables Management Buttons
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(new GUIContent("Addressables Groups", "Open Addressables Groups window"), buttonStyle, GUILayout.Height(30)))
        {
            EditorApplication.ExecuteMenuItem("Window/Asset Management/Addressables/Groups");
        }
        if (GUILayout.Button(new GUIContent("Addressables Profiles", "Open Addressables Profiles window"), buttonStyle, GUILayout.Height(30)))
        {
            EditorApplication.ExecuteMenuItem("Window/Asset Management/Addressables/Profiles");
        }
        if (GUILayout.Button(new GUIContent("Open Catalog Folder", "Open the catalog folder"), buttonStyle, GUILayout.Height(30)))
        {
            OpenCatalogFolder();
        }
        GUILayout.EndHorizontal();
        EditorGUILayout.Space(10);

        // Delivery Mode Section
        EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));
        GUILayout.Label("Bundle Delivery Mode", sectionStyle);
        EditorGUILayout.HelpBox("Choose whether to use local bundles (included in the .apk) or remote bundles (hosted on a cloud server). Changing this setting requires clearing the build cache to avoid issues.", MessageType.Info);
        bool newUseOnlyLocalBundles = EditorGUILayout.Toggle(new GUIContent("Use Local Bundles Only", "Enable to include all assets in the .apk. Disable to use cloud-hosted remote bundles."), useOnlyLocalBundles);
        if (newUseOnlyLocalBundles != useOnlyLocalBundles)
        {
            useOnlyLocalBundles = newUseOnlyLocalBundles;
            SaveBundleDeliveryMode();
        }
        EditorGUILayout.Space(10);
        EditorGUILayout.EndVertical();

        // Local Delivery Section
        EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));
        GUILayout.Label("Local Delivery", sectionStyle);
        EditorGUILayout.HelpBox(
            "Local delivery builds all scenes directly into the .apk file, suitable for smaller apps or offline use. " +
            "Ensure you clear the build cache (Addressables Groups > Build > Clear Build Cache > All) after switching delivery modes to prevent asset conflicts.",
            MessageType.Info);
        if (GUILayout.Button(new GUIContent("Create Local Group", "Create a new Addressable group configured for local delivery"), buttonStyle, GUILayout.Height(30)))
        {
            EditorApplication.ExecuteMenuItem("Window/Asset Management/Addressables/Groups");
            CreateLocalGroup();
        }
        EditorGUILayout.Space(5);
        EditorGUILayout.EndVertical();

        // Remote Delivery Section
        EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));
        GUILayout.Label("Remote Delivery", sectionStyle);
        EditorGUILayout.HelpBox(
            "Remote delivery offloads large assets to a cloud server, reducing app size. Configure Addressable Groups and Profiles, build bundles, and upload them to your cloud provider.",
            MessageType.Info);
        if (GUILayout.Button(new GUIContent("Create Remote Group", "Create a new Addressable group configured for remote delivery"), buttonStyle, GUILayout.Height(30)))
        {
            EditorApplication.ExecuteMenuItem("Window/Asset Management/Addressables/Groups");
            CreateRemoteGroup();
        }
        EditorGUILayout.Space(5);
        EditorGUILayout.EndVertical();

        // Catalog Settings Section
        EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));
        GUILayout.Label("Catalog Settings", sectionStyle);
        EditorGUILayout.HelpBox(
            "Specify the public URL and name for the Addressable catalog JSON file used in remote delivery. " +
            "Upload the catalog file to your cloud provider and paste the public URL here.",
            MessageType.Info);
        addressableCatalogFilePath = EditorGUILayout.TextField(new GUIContent("Catalog File Path", "Enter the public URL or local path to the Addressable catalog JSON file"), addressableCatalogFilePath);
        catalogFileName = EditorGUILayout.TextField(new GUIContent("Catalog File Name", "Enter the name of the catalog file (without .json extension)"), catalogFileName);
        if (GUILayout.Button(new GUIContent("Save Catalog Settings", "Save the catalog file path and name to the Project Data SO"), buttonStyle, GUILayout.Height(30)))
        {
            SaveCatalogSettings();
        }
        EditorGUILayout.Space(5);
        EditorGUILayout.EndVertical();

        // Clear Cache Section
        EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));
        GUILayout.Label("Cache Management", sectionStyle);
        EditorGUILayout.HelpBox(
            "Clear cached bundles to remove outdated assets from the local cache. This ensures the latest bundles are loaded during development or after updates.",
            MessageType.Info);
        if (GUILayout.Button(new GUIContent("Clear Cached Bundles", "Delete all locally cached Addressable bundles and reset PlayerPrefs"), buttonStyle, GUILayout.Height(30)))
        {
            ClearCashedBundles();
        }
        EditorGUILayout.Space(5);
        EditorGUILayout.EndVertical();

        // Package Version Section
        // EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));
        // GUILayout.Label("SDK Version Settings", sectionStyle);
        // EditorGUILayout.HelpBox(
        //     "Set the current SDK version installed in this project. This version will be checked against the server on Unity startup to notify you of available updates.",
        //     MessageType.Info);
        // currentPackageVersion = EditorGUILayout.TextField(new GUIContent("Current Package Version", "The version of Vertex Form 3D SDK currently installed (e.g., 1.0.0)"), currentPackageVersion);
        // versionJsonUrl = EditorGUILayout.TextField(new GUIContent("Version JSON URL", "The URL to the version.json file on your cloud bucket"), versionJsonUrl);
        // if (GUILayout.Button(new GUIContent("Save Version Settings", "Save the current package version and version JSON URL to the Project Data SO"), buttonStyle, GUILayout.Height(30)))
        // {
        //     SaveVersionSettings();
        // }
        // EditorGUILayout.Space(5);
        // EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);
        EditorGUILayout.EndScrollView();
    }

    private void CreateRemoteGroup()
    {
        CreateAddressableGroup(true);
    }

    private void CreateLocalGroup()
    {
        CreateAddressableGroup(false);
    }

    private void CreateAddressableGroup(bool remote)
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            UnityEngine.Debug.LogError("AddressableAssetSettings not found!");
            return;
        }

        AddressableAssetGroup newGroup = settings.CreateGroup(remote ? "Remote Group" : "Local Group", false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
        BundledAssetGroupSchema bundledSchema = newGroup.GetSchema<BundledAssetGroupSchema>();
        if (bundledSchema != null)
        {
            bundledSchema.BuildPath.SetVariableByName(settings, remote ? "Remote.BuildPath" : "Local.BuildPath");
            bundledSchema.LoadPath.SetVariableByName(settings, remote ? "Remote.LoadPath" : "Local.LoadPath");
            bundledSchema.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
            bundledSchema.UseAssetBundleCache = true;
            bundledSchema.UseAssetBundleCrc = false;
            bundledSchema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.FileNameHash;
            bundledSchema.IncludeAddressInCatalog = true;
            bundledSchema.IncludeGUIDInCatalog = true;
            bundledSchema.IncludeLabelsInCatalog = true;
            bundledSchema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel;
            bundledSchema.AssetBundledCacheClearBehavior = BundledAssetGroupSchema.CacheClearBehavior.ClearWhenSpaceIsNeededInCache;
        }

        EditorUtility.SetDirty(settings);
        UnityEngine.Debug.Log($"Created new Addressable group '{(remote ? "Remote Group" : "Local Group")}' with specified settings.");
    }

    private void SaveCatalogSettings()
    {
        ProjectDataScriptableObject pso = Resources.Load<ProjectDataScriptableObject>("Project Data SO");
        if (pso != null)
        {
            pso.projectData.addressableCatalogFilePath = string.IsNullOrEmpty(addressableCatalogFilePath) ? DEFAULT_CATALOG_PATH : addressableCatalogFilePath;
            pso.projectData.catalogFileName = string.IsNullOrEmpty(catalogFileName) ? "VertexForm3DAddressablesCatalog" : catalogFileName;
            EditorUtility.SetDirty(pso);
            UnityEngine.Debug.Log($" Mischief managed! Catalog settings saved: Path = {pso.projectData.addressableCatalogFilePath}, Name = {pso.projectData.catalogFileName}");
        }
        else
        {
            UnityEngine.Debug.LogError("ProjectDataScriptableObject not found at 'Project Data SO'.");
        }
    }

    private void SaveBundleDeliveryMode()
    {
        ProjectDataScriptableObject PSO = Resources.Load<ProjectDataScriptableObject>("Project Data SO");
        if (PSO != null)
        {
            PSO.projectData.onlyLocalBundles = useOnlyLocalBundles;
            EditorUtility.SetDirty(PSO);
            UnityEngine.Debug.Log($"Bundle delivery mode saved: onlyLocalBundles = {useOnlyLocalBundles}");
        }
        else
        {
            UnityEngine.Debug.LogError("ProjectDataScriptableObject not found at 'Project Data SO'.");
        }
    }

    private void SaveVersionSettings()
    {
        ProjectDataScriptableObject pso = Resources.Load<ProjectDataScriptableObject>("Project Data SO");
        if (pso != null)
        {
            EditorUtility.SetDirty(pso);
            AssetDatabase.SaveAssets();
            UnityEngine.Debug.Log($"Version settings saved: Current Version = {pso.projectData.currentPackageVersion}");
        }
        else
        {
            UnityEngine.Debug.LogError("ProjectDataScriptableObject not found at 'Project Data SO'.");
        }
    }

    public static void BuildAddressablesAndRenameRemoteCatalog()
    {
        string remoteBuildPath = GetRemoteBuildPath();
        if (string.IsNullOrEmpty(remoteBuildPath))
        {
            UnityEngine.Debug.LogError("Remote Build Path is not set in Addressables settings.");
            return;
        }

        ClearOldBundles(remoteBuildPath);
        AddressableAssetSettings.CleanPlayerContent();
        AddressableAssetSettings.BuildPlayerContent();

        ProjectDataScriptableObject PSO = Resources.Load<ProjectDataScriptableObject>("Project Data SO");
        RenameCatalogFiles(remoteBuildPath, PSO.projectData.catalogFileName);

        UnityEngine.Debug.Log("Addressables build complete, remote catalog files renamed.");
    }

    private static void ClearOldBundles(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
            Directory.CreateDirectory(path);
            UnityEngine.Debug.Log($"Cleared old Addressables bundles at: {path}");
        }
        else
        {
            UnityEngine.Debug.Log($"No existing Addressables bundle directory found at: {path}");
        }
    }

    private static void RenameCatalogFiles(string buildPath, string newCatalogName)
    {
        if (!Directory.Exists(buildPath))
        {
            UnityEngine.Debug.LogError($"Remote Addressables build path not found: {buildPath}");
            return;
        }

        string[] files = Directory.GetFiles(buildPath, "catalog_*");
        foreach (var file in files)
        {
            string directory = Path.GetDirectoryName(file);
            string extension = Path.GetExtension(file);
            if (file.Contains(".hash"))
            {
                File.Move(file, Path.Combine(directory, $"{newCatalogName}.hash"));
            }
            else if (file.Contains(".json"))
            {
                File.Move(file, Path.Combine(directory, $"{newCatalogName}.json"));
            }
        }

        UnityEngine.Debug.Log("Remote catalog files successfully renamed.");
    }

    private static string GetRemoteBuildPath()
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            UnityEngine.Debug.LogError("AddressableAssetSettings not found!");
            return null;
        }

        string remoteBuildPath = settings.RemoteCatalogBuildPath.GetValue(settings);
        remoteBuildPath = remoteBuildPath.Replace("[UnityEngine.AddressableAssets.Addressables.BuildPath]", "ServerData");
        return remoteBuildPath;
    }

    public void SetRemoteBuildPath()
    {
        ProjectDataScriptableObject pso = Resources.Load<ProjectDataScriptableObject>("Project Data SO");
        pso.projectData.addressableCatalogFilePath = string.IsNullOrEmpty(addressableCatalogFilePath) ? DEFAULT_CATALOG_PATH : addressableCatalogFilePath;
    }

    public static void CenterWindow()
    {
        var window = GetWindow<AddressablesBuildEditor>();
        var position = window.position;
        var screenWidth = Screen.currentResolution.width;
        var screenHeight = Screen.currentResolution.height;

        position.x = (screenWidth - position.width) / 2;
        position.y = (screenHeight - position.height) / 2;
        window.position = position;
    }

    public static void ClearCashedBundles()
    {
        Caching.ClearCache();
        PlayerPrefs.DeleteAll();
        Addressables.CleanBundleCache().Completed += handle => UnityEngine.Debug.Log("Cache cleared.");
    }

    private void OpenCatalogFolder()
    {
        string remoteBuildPath = GetRemoteBuildPath();
        if (string.IsNullOrEmpty(remoteBuildPath))
        {
            EditorUtility.DisplayDialog("Error", "Remote Build Path is not set in Addressables settings.", "OK");
            return;
        }

        if (!Directory.Exists(remoteBuildPath))
        {
            EditorUtility.DisplayDialog("Error", $"Catalog folder not found at: {remoteBuildPath}", "OK");
            return;
        }

        string absolutePath = Path.GetFullPath(remoteBuildPath);
        UnityEngine.Debug.Log($"Opening catalog folder at: {absolutePath}");

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = absolutePath,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        catch (System.Exception ex)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to open folder: {ex.Message}", "OK");
        }
    }
}