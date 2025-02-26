using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using System.IO;
using UnityEngine;

public class AddressablesBuildEditor : EditorWindow
{
    [MenuItem("VertexForm3D SDK/Build Addressables and Rename Catalog")]
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
