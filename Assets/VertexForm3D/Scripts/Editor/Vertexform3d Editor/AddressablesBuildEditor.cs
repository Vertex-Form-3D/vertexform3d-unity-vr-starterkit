using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Diagnostics;

public class AddressablesBuildEditor : EditorWindow
{
    private Texture2D bannerTexture;
    private string addressableCatalogFilePath = ""; // Field to store catalog file path input
    private string catalogFileName = "VertexForm3DAddressablesCatalog"; // Field to store catalog file name input
    private bool useOnlyLocalBundles = true; // Boolean to toggle local/remote bundles
    private Vector2 scrollPosition; // Scroll position for scrollable view
    private const string DEFAULT_CATALOG_PATH = "https://storage.googleapis.com/yourproject_bucket/Android/YourProjectAddressablesCatalog.json";

    /*[MenuItem("VertexForm3D SDK/Build Addressables")]
    public static void ShowWindow()
    {
        AddressablesBuildEditor window = GetWindow<AddressablesBuildEditor>("Build Addressables");
        window.minSize = new Vector2(450, 400); // Adjusted to fit UI elements
        window.Show();
    }*/

    private void OnEnable()
    {
        // Load the banner from Resources folder
        bannerTexture = Resources.Load<Texture2D>("VF3DBannerEditor");

        // Load the catalog path and name from AddressableBuildScriptableObject
        ProjectDataScriptableObject pso = Resources.Load<ProjectDataScriptableObject>("Project Data SO");
        if (pso != null)
        {
            addressableCatalogFilePath = string.IsNullOrEmpty(pso.projectData.addressableCatalogFilePath) ? DEFAULT_CATALOG_PATH : pso.projectData.addressableCatalogFilePath;
            catalogFileName = string.IsNullOrEmpty(pso.projectData.catalogFileName) ? "VertexForm3DAddressablesCatalog" : pso.projectData.catalogFileName;
        }
        else
        {
            UnityEngine.Debug.LogWarning("AddressableBuildScriptableObject not found at 'Project Data SO'. Using default catalog path and name.");
            addressableCatalogFilePath = DEFAULT_CATALOG_PATH;
            catalogFileName = "VertexForm3DAddressablesCatalog";
        }

        // Load the onlyLocalBundles setting from ProjectDataScriptableObject
        ProjectDataScriptableObject PSO = Resources.Load<ProjectDataScriptableObject>("Project Data SO");
        if (PSO != null)
        {
            useOnlyLocalBundles = PSO.projectData.onlyLocalBundles;
        }
        else
        {
            UnityEngine.Debug.LogWarning("ProjectDataScriptableObject not found at 'Project Data SO'. Defaulting to remote delivery.");
            useOnlyLocalBundles = false;
        }
    }

    private void OnGUI()
    {
        // Begin scrollable view
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Space(5);

        // Display Banner Image
        if (bannerTexture != null)
        {
            float bannerWidth = Mathf.Min(bannerTexture.width, position.width - 10); // Fit within the window width
            float bannerHeight = (bannerWidth / bannerTexture.width) * bannerTexture.height; // Maintain aspect ratio
            GUILayout.Label(bannerTexture, GUILayout.Width(bannerWidth), GUILayout.Height(bannerHeight));
        }
        else
        {
            EditorGUILayout.HelpBox("Banner image not found. Make sure 'VF3DBannerEditor' is inside the Resources folder.", MessageType.Warning);
        }

        GUILayout.Space(10);

        GUIStyle boldStyle = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold, fontSize = 25 };
        GUILayout.Label("ADDRESSABLE SYSTEM", boldStyle);

        GUILayout.Space(10);

        // Local/Remote Bundle Toggle
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Bundle Delivery Mode", EditorStyles.boldLabel);
        bool newUseOnlyLocalBundles = EditorGUILayout.Toggle("Use Only Local Bundles", useOnlyLocalBundles);
        if (newUseOnlyLocalBundles != useOnlyLocalBundles)
        {
            useOnlyLocalBundles = newUseOnlyLocalBundles;
            SaveBundleDeliveryMode();
        }
        GUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // Local Delivery Section
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Local Delivery", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "Whenever you change the groups from Remote to local or local to remote then make sure to do clear build cache.Go to addressables group> build> clear build cache> and press All and Build pipeline cache button.Otherwise you might be et issue in addressables." +
            EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField(
            "By default, the framework is set up for Local Delivery, meaning all scenes are built directly into the final .apk file. " +
            "When you press 'Build Addressables,' your scenes will be compiled and loaded from the local path.",
            EditorStyles.wordWrappedLabel);
        if (GUILayout.Button("Visit Tutorials", GUILayout.Height(25)))
        {
            Application.OpenURL("https://vertexform3d.com/tutorials/");
        }

        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // Remote Delivery Section
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Remote Delivery", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "As your app grows, switching to Remote Delivery is recommended to offload large environments to the cloud, " +
            "keeping the local app size small.\n\n" +
            "To enable Remote Delivery, update the settings in both the Addressable Groups and the Database. Once configured, " +
            "clicking 'Publish' will build your scenes and store them in the 'Built' folder. You can then upload these files " +
            "to the cloud provider of your choice.",
            EditorStyles.wordWrappedLabel);
        if (GUILayout.Button("Visit Tutorials", GUILayout.Height(25), GUILayout.ExpandWidth(true)))
        {
            Application.OpenURL("https://vertexform3d.com/tutorials/");
        }
        GUILayout.Space(20);
        EditorGUILayout.LabelField(
            "Open the Addressables Groups window to configure and manage your asset groups for Remote Delivery. This allows you to organize assets, set build and load paths, and optimize bundle creation.",
            EditorStyles.wordWrappedLabel);
        if (GUILayout.Button("Open Addressables Groups", GUILayout.Height(25)))
        {
            EditorApplication.ExecuteMenuItem("Window/Asset Management/Addressables/Groups");
        }
        GUILayout.Space(20);
        EditorGUILayout.LabelField(
            "Open the Addressables Profiles window to manage build and load paths for your addressable assets. This allows you to configure profiles for different environments, such as local or remote hosting.",
            EditorStyles.wordWrappedLabel);
        if (GUILayout.Button("Open Addressables Profiles", GUILayout.Height(25)))
        {
            EditorApplication.ExecuteMenuItem("Window/Asset Management/Addressables/Profiles");
        }
        GUILayout.Space(20);
        EditorGUILayout.LabelField(
            "Open the catalogFolder where all addressables bundles saved.",
            EditorStyles.wordWrappedLabel);
        if (GUILayout.Button("Open bundles Folder", GUILayout.Height(25)))
        {
            OpenCatalogFolder();
        }
        EditorGUILayout.EndVertical();

        // Addressable Catalog File Input Section
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Addressable Catalog File", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("go to the path where you uploaded addressableCatalog json file copy its public URL and paste it here if you are using remote addressables",
            EditorStyles.wordWrappedLabel);
        addressableCatalogFilePath = EditorGUILayout.TextField("Catalog File Path:", addressableCatalogFilePath);
        catalogFileName = EditorGUILayout.TextField("Catalog File Name:", catalogFileName);
        if (GUILayout.Button("Save Catalog Settings", GUILayout.Height(25)))
        {
            SaveCatalogSettings();
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        EditorGUILayout.EndVertical();

        GUILayout.Space(15);

        EditorGUILayout.LabelField("Clear Cached Bundles", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "It deletes downloaded AssetBundles stored in the local cache. This is useful during development or when updating bundles to ensure that Unity loads the most recent versions instead of outdated cached data.",
            EditorStyles.wordWrappedLabel);
        if (GUILayout.Button("Clear CachedBundles", GUILayout.Height(25)))
        {
            ClearCashedBundles();
        }
        GUILayout.Space(15);

        // Centered Build Scenes Button
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Build Addressables", GUILayout.Width(150), GUILayout.Height(30), GUILayout.ExpandWidth(true)))
        {
            UnityEngine.Debug.Log("Building scenes...");
            BuildAddressablesAndRenameRemoteCatalog();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        // End scrollable view
        EditorGUILayout.EndScrollView();
    }

    private void SaveCatalogSettings()
    {
        ProjectDataScriptableObject pso = Resources.Load<ProjectDataScriptableObject>("Project Data SO");
        if (pso != null)
        {
            pso.projectData.addressableCatalogFilePath = string.IsNullOrEmpty(addressableCatalogFilePath) ? DEFAULT_CATALOG_PATH : addressableCatalogFilePath;
            pso.projectData.catalogFileName = string.IsNullOrEmpty(catalogFileName) ? "VertexForm3DAddressablesCatalog" : catalogFileName;
            EditorUtility.SetDirty(pso); // Mark the ScriptableObject as modified to save changes
            UnityEngine.Debug.Log($"Catalog settings saved: Path = {pso.projectData.addressableCatalogFilePath}, Name = {pso.projectData.catalogFileName}");
        }
        else
        {
            UnityEngine.Debug.LogError("AddressableBuildScriptableObject not found at 'Project Data SO'.");
        }
    }

    private void SaveBundleDeliveryMode()
    {
        ProjectDataScriptableObject PSO = Resources.Load<ProjectDataScriptableObject>("Project Data SO");
        if (PSO != null)
        {
            PSO.projectData.onlyLocalBundles = useOnlyLocalBundles;
            EditorUtility.SetDirty(PSO); // Mark the ScriptableObject as modified to save changes
            UnityEngine.Debug.Log($"Bundle delivery mode saved: onlyLocalBundles = {useOnlyLocalBundles}");
        }
        else
        {
            UnityEngine.Debug.LogError("ProjectDataScriptableObject not found at 'Project Data SO'.");
        }
    }

    public static void BuildAddressablesAndRenameRemoteCatalog()
    {
        // Get remote catalog build path from Addressables settings
        string remoteBuildPath = GetRemoteBuildPath();

        if (string.IsNullOrEmpty(remoteBuildPath))
        {
            UnityEngine.Debug.LogError("Remote Build Path is not set in Addressables settings.");
            return;
        }

        // Clear old bundles before building
        ClearOldBundles(remoteBuildPath);

        // Clean and build Addressables
        AddressableAssetSettings.CleanPlayerContent();
        AddressableAssetSettings.BuildPlayerContent();

        ProjectDataScriptableObject PSO = Resources.Load<ProjectDataScriptableObject>("Project Data SO");
        // Rename catalog files
        RenameCatalogFiles(remoteBuildPath, PSO.projectData.catalogFileName);

        UnityEngine.Debug.Log("Addressables build complete, remote catalog files renamed.");
    }

    private static void ClearOldBundles(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true); // Delete all contents
            Directory.CreateDirectory(path); // Recreate the directory
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
        // Get Addressables settings
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            UnityEngine.Debug.LogError("AddressableAssetSettings not found!");
            return null;
        }

        // Get Remote Build Path from the active profile
        string remoteBuildPath = settings.RemoteCatalogBuildPath.GetValue(settings);
        remoteBuildPath = remoteBuildPath.Replace("[UnityEngine.AddressableAssets.Addressables.BuildPath]", "ServerData");

        return remoteBuildPath;
    }

    public void SetRemoteBuildPath()
    {
        ProjectDataScriptableObject pso = Resources.Load<ProjectDataScriptableObject>("Project Data SO");
        pso.projectData.addressableCatalogFilePath = string.IsNullOrEmpty(addressableCatalogFilePath) ? DEFAULT_CATALOG_PATH : addressableCatalogFilePath;
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

        // Convert to absolute path if necessary
        string absolutePath = Path.GetFullPath(remoteBuildPath);
        UnityEngine.Debug.Log($"Opening catalog folder at: {absolutePath}");

        // Open the folder in the system's file explorer
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