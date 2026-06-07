using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Imports a .unitypackage while programmatically deselecting assets, matching the
/// behavior of unchecking items in Unity's interactive Import Package dialog.
/// Uses Unity's internal PackageUtility API via reflection.
/// </summary>
internal static class SelectivePackageImporter
{
    private static Type packageUtilityType;
    private static MethodInfo extractAndPrepareAssetListMethod;
    private static MethodInfo importPackageAssetsImmediatelyMethod;
    private static FieldInfo destinationAssetPathField;
    private static FieldInfo exportedAssetPathField;
    private static FieldInfo enabledStatusField;
    private static bool initialized;
    private static bool isSupported;

    // Matches UnityEditor.PackageImportTreeView.EnabledState.Disabled
    private const int EnabledStateDisabled = -1;

    public static bool IsSupported
    {
        get
        {
            EnsureInitialized();
            return isSupported;
        }
    }

    public static bool ShouldExcludeProtectedScene(string assetPath, string[] protectedFolders, string[] protectedFiles)
    {
        if (string.IsNullOrEmpty(assetPath))
            return false;

        assetPath = assetPath.Replace('\\', '/');

        foreach (string file in protectedFiles)
        {
            if (string.Equals(assetPath, file, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        foreach (string folder in protectedFolders)
        {
            if (string.Equals(assetPath, folder, StringComparison.OrdinalIgnoreCase)
                || string.Equals(assetPath, folder + ".meta", StringComparison.OrdinalIgnoreCase)
                || assetPath.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Imports a package while excluding assets whose destination paths match the protected lists.
    /// </summary>
    public static bool TryImportPackage(
        string packagePath,
        string[] protectedFolders,
        string[] protectedFiles,
        out int excludedCount,
        out int importedCount,
        out string error)
    {
        excludedCount = 0;
        importedCount = 0;
        error = null;

        EnsureInitialized();
        if (!isSupported)
        {
            error = "UnityEditor.PackageUtility selective import API was not found.";
            return false;
        }

        if (!File.Exists(packagePath))
        {
            error = $"Package file not found: {packagePath}";
            return false;
        }

        try
        {
            object packageIconPath = string.Empty;
            object packageManagerDependenciesPath = string.Empty;
            object[] extractArgs = { packagePath, packageIconPath, packageManagerDependenciesPath };

            Array items = extractAndPrepareAssetListMethod.Invoke(null, extractArgs) as Array;
            if (items == null || items.Length == 0)
            {
                Debug.Log("[SelectivePackageImporter] Package has no importable assets.");
                return true;
            }

            foreach (object item in items)
            {
                string destinationPath = destinationAssetPathField.GetValue(item) as string;
                string exportedPath = exportedAssetPathField != null
                    ? exportedAssetPathField.GetValue(item) as string
                    : null;

                bool shouldExclude = ShouldExcludeProtectedScene(destinationPath, protectedFolders, protectedFiles)
                    || ShouldExcludeProtectedScene(exportedPath, protectedFolders, protectedFiles);

                if (!shouldExclude)
                    continue;

                enabledStatusField.SetValue(item, EnabledStateDisabled);
                excludedCount++;
            }

            importedCount = 0;
            foreach (object item in items)
            {
                if ((int)enabledStatusField.GetValue(item) > 0)
                    importedCount++;
            }

            if (excludedCount == 0)
            {
                Debug.LogWarning("[SelectivePackageImporter] Toggle is on but no Database Scenes assets were found in the package to exclude. Check package paths.");
            }

            if (importedCount == 0)
            {
                Debug.Log($"[SelectivePackageImporter] All assets were excluded ({excludedCount} Database Scenes asset(s) skipped).");
                return true;
            }

            string packageName = Path.GetFileNameWithoutExtension(packagePath);
            importPackageAssetsImmediatelyMethod.Invoke(null, new object[] { packageName, items });

            Debug.Log($"[SelectivePackageImporter] Imported {importedCount} asset(s), skipped {excludedCount} Database Scenes asset(s).");
            return true;
        }
        catch (TargetInvocationException ex)
        {
            error = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static void EnsureInitialized()
    {
        if (initialized)
            return;

        initialized = true;

        packageUtilityType = FindEditorType("UnityEditor.PackageUtility");
        if (packageUtilityType == null)
            return;

        extractAndPrepareAssetListMethod = packageUtilityType.GetMethod(
            "ExtractAndPrepareAssetList",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        importPackageAssetsImmediatelyMethod = packageUtilityType.GetMethod(
            "ImportPackageAssetsImmediately",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? packageUtilityType.GetMethod(
                "ImportPackageAssets",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        Type importPackageItemType = FindEditorType("UnityEditor.ImportPackageItem");
        if (importPackageItemType == null)
            return;

        destinationAssetPathField = importPackageItemType.GetField("destinationAssetPath");
        exportedAssetPathField = importPackageItemType.GetField("exportedAssetPath");
        enabledStatusField = importPackageItemType.GetField("enabledStatus");

        isSupported = extractAndPrepareAssetListMethod != null
            && importPackageAssetsImmediatelyMethod != null
            && destinationAssetPathField != null
            && enabledStatusField != null;
    }

    private static Type FindEditorType(string fullName)
    {
        Type direct = Type.GetType(fullName + ", UnityEditor");
        if (direct != null)
            return direct;

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!assembly.GetName().Name.StartsWith("UnityEditor", StringComparison.Ordinal))
                continue;

            Type type = assembly.GetType(fullName, false);
            if (type != null)
                return type;
        }

        return null;
    }
}
