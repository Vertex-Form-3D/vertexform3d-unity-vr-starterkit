using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using System.IO;
using UnityEngine;

public class AddressablesBuildEditor : EditorWindow
{
    private Texture2D bannerTexture;

    [MenuItem("VertexForm3D SDK/Build Addressables")]
    public static void ShowWindow()
    {
        AddressablesBuildEditor window = GetWindow<AddressablesBuildEditor>("Build Addressables");
        window.minSize = new Vector2(450, 400); // Adjusted to fit UI elements
        window.Show();
    }

    private void OnEnable()
    {
        // Load the banner from Resources folder
        bannerTexture = Resources.Load<Texture2D>("VF3DBannerEditor");
    }

    private void OnGUI()
    {
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
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // Local Delivery Section
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Local Delivery", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "By default, the framework is set up for Local Delivery, meaning all scenes are built directly into the final .apk file. " +
            "When you press 'Build Scenes,' your scenes will be compiled and loaded from the local path.",
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
        EditorGUILayout.EndVertical();

        GUILayout.Space(15);

        // Centered Build Scenes Button
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Build Addressables", GUILayout.Width(150), GUILayout.Height(30), GUILayout.ExpandWidth(true)))
        {
            Debug.Log("Building scenes...");
            BuildAddressablesAndRenameRemoteCatalog();
            // Add your build logic here
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }
    public static void BuildAddressablesAndRenameRemoteCatalog()
    {
        // Get remote catalog build path from Addressables settings
        string remoteBuildPath = GetRemoteBuildPath();

        if (string.IsNullOrEmpty(remoteBuildPath))
        {
            Debug.LogError("Remote Build Path is not set in Addressables settings.");
            return;
        }

        // Clear old bundles before building
        ClearOldBundles(remoteBuildPath);

        // Clean and build Addressables
        AddressableAssetSettings.CleanPlayerContent();
        AddressableAssetSettings.BuildPlayerContent();

        ProjectDataScriptableObject PSO = Resources.Load("Project Data SO") as ProjectDataScriptableObject;
        // Rename catalog files
        RenameCatalogFiles(remoteBuildPath, PSO.projectData.catalogFileName);

        Debug.Log("Addressables build complete, remote catalog files renamed.");
    }

    private static void ClearOldBundles(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true); // Delete all contents
            Directory.CreateDirectory(path); // Recreate the directory
            Debug.Log($"Cleared old Addressables bundles at: {path}");
        }
        else
        {
            Debug.Log($"No existing Addressables bundle directory found at: {path}");
        }
    }
    private static void RenameCatalogFiles(string buildPath, string newCatalogName)
    {
        if (!Directory.Exists(buildPath))
        {
            Debug.LogError($"Remote Addressables build path not found: {buildPath}");
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

        Debug.Log("Remote catalog files successfully renamed.");
    }

    private static string GetRemoteBuildPath()
    {
        // Get Addressables settings
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("AddressableAssetSettings not found!");
            return null;
        }

        // Get Remote Build Path from the active profile
        string remoteBuildPath = settings.RemoteCatalogBuildPath.GetValue(settings);
        remoteBuildPath = remoteBuildPath.Replace("[UnityEngine.AddressableAssets.Addressables.BuildPath]", "ServerData");

        return remoteBuildPath;
    }

    private static string GetLocalBuildPath()
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("AddressableAssetSettings not found!");
            return null;
        }

        string localBuildPath = settings.profileSettings.GetValueByName(settings.activeProfileId, "Local.BuildPath");
        return localBuildPath;
    }
}
