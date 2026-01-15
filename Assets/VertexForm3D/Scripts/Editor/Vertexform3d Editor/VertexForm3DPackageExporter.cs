using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Exports a .unitypackage and generates a version.json manifest next to it.
/// You can then upload both the .unitypackage and version.json to your cloud bucket.
/// </summary>
public class VertexForm3DPackageExporter : EditorWindow
{
    private string packageName = "VertexForm3D_SDK";
    private string versionCode = "1.0.0";
    private string releaseNotes = "Switch to OpenXR.\nFixed minor bugs in the rendering pipeline.\nHand Tracking, Teleportation Using Hands.\nCategorized UI.";
    private string exportRelativePath = "Builds"; // relative to project root
    private string packageUrlBase = "https://storage.googleapis.com/your_bucket_name/";


    public static void ShowWindow()
    {
        GetWindow<VertexForm3DPackageExporter>("Export SDK Package");
    }

    private void OnGUI()
    {
        GUILayout.Label("Export Vertex Form 3D SDK", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "This tool will:\n" +
            "1) Export a .unitypackage from the selected folder.\n" +
            "2) Create a version.json file next to the package with name, version, and URL.\n\n" +
            "After export, upload both files to your cloud bucket.",
            MessageType.Info);

        EditorGUILayout.Space();

        packageName = EditorGUILayout.TextField("Package Name", packageName);
        versionCode = EditorGUILayout.TextField("Version Code", versionCode);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Release Notes", EditorStyles.boldLabel);
        releaseNotes = EditorGUILayout.TextArea(releaseNotes, GUILayout.Height(100));

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Export Location (Relative to Project Root)", EditorStyles.boldLabel);
        exportRelativePath = EditorGUILayout.TextField("Folder", exportRelativePath);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Remote URL Settings", EditorStyles.boldLabel);
        packageUrlBase = EditorGUILayout.TextField("Package URL Base", packageUrlBase);

        EditorGUILayout.Space();

        if (GUILayout.Button("Export Package + Generate version.json", GUILayout.Height(35)))
        {
            ExportPackageAndCreateManifest();
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

        // Folder in project to export (adjust as needed)
        string sdkRoot = "Assets/VertexForm3D";
        if (!AssetDatabase.IsValidFolder(sdkRoot))
        {
            EditorUtility.DisplayDialog("Error", $"SDK root folder not found at '{sdkRoot}'.", "OK");
            return;
        }

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
            AssetDatabase.ExportPackage(
                sdkRoot,
                packageProjectRelativePath,
                ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);

            Debug.Log($"Vertex Form 3D SDK exported to: {packageFullPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to export package: {e.Message}");
            EditorUtility.DisplayDialog("Error", "Failed to export package. See console for details.", "OK");
            return;
        }

        // Build version.json content (array of packages)
        string packageUrl = CombineUrl(packageUrlBase, packageFileName);
        string jsonContent = BuildVersionJson(packageName, versionCode, packageUrl, releaseNotes);

        string versionJsonPath = Path.Combine(exportFolderFullPath, "version.json");
        try
        {
            File.WriteAllText(versionJsonPath, jsonContent);
            Debug.Log($"version.json created at: {versionJsonPath}");
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
            $"Created manifest:\n{versionJsonPath}",
            "OK");
    }

    private static string CombineUrl(string baseUrl, string fileName)
    {
        if (string.IsNullOrEmpty(baseUrl))
            return fileName;

        baseUrl = baseUrl.TrimEnd('/', '\\');
        return $"{baseUrl}/{fileName}";
    }

    private static string BuildVersionJson(string name, string version, string url, string releaseNotes)
    {
        // Simple JSON array with a single package entry:
        // [
        //   { "name": "...", "version": "...", "url": "...", "releaseNotes": "..." }
        // ]
        string safeName = EscapeJsonString(name);
        string safeVersion = EscapeJsonString(version);
        string safeUrl = EscapeJsonString(url);
        string safeReleaseNotes = EscapeJsonString(releaseNotes ?? "No release notes available.");

        return "[\n" +
               "  {\n" +
               $"    \"name\": \"{safeName}\",\n" +
               $"    \"version\": \"{safeVersion}\",\n" +
               $"    \"url\": \"{safeUrl}\",\n" +
               $"    \"releaseNotes\": \"{safeReleaseNotes}\"\n" +
               "  }\n" +
               "]";
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


