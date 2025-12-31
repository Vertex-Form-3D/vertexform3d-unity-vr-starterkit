using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class AddressableLabelCreator
{
    private const string LABEL_PREFIX = ""; // e.g., "Level_" → "Level_MainMenu"
    private const string FUSION_SCENES_LABEL = "FusionScenes"; // Label for Photon Fusion scenes
    private const string MENU_PATH = "Assets/Addressables/Create and Assign Label Group";

    [MenuItem(MENU_PATH, false, 10)]
    private static void CreateAndAssignLabelGroup()
    {
        var selectedObjects = Selection.objects;
        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            Debug.LogWarning("Please select one or more assets in the Project window.");
            return;
        }

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("Addressable Asset Settings not found. Please initialize Addressables via Window > Asset Management > Addressables.");
            return;
        }

        // Ensure FusionScenes label exists
        if (!settings.GetLabels().Contains(FUSION_SCENES_LABEL))
        {
            settings.AddLabel(FUSION_SCENES_LABEL);
            Debug.Log($"Created missing label: '{FUSION_SCENES_LABEL}");
        }

        int successCount = 0;

        foreach (Object selectedObject in selectedObjects)
        {
            string assetPath = AssetDatabase.GetAssetPath(selectedObject);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogWarning($"Invalid asset: {selectedObject.name}");
                continue;
            }

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
                continue;

            string assetName = selectedObject.name;
            string customLabel = $"{LABEL_PREFIX}{assetName}";
            string address = assetName; // Use clean name as address

            // Create or find entry
            var entry = settings.FindAssetEntry(guid) ?? CreateEntry(settings, guid, assetPath);

            if (entry == null)
            {
                Debug.LogError($"Failed to create Addressable entry for {assetName}");
                continue;
            }

            // Always add the custom label
            if (!settings.GetLabels().Contains(customLabel))
                settings.AddLabel(customLabel);

            entry.SetLabel(customLabel, true, true);
            entry.address = address;

            // Special handling: if this is a scene, also add FusionScenes label
            if (assetPath.EndsWith(".unity"))
            {
                entry.SetLabel(FUSION_SCENES_LABEL, true, true);
                Debug.Log($"Scene detected: Added '{FUSION_SCENES_LABEL}' label to '{assetName}'");
            }

            Debug.Log($"Addressable updated: {assetName} → Address: '{address}', Labels: {string.Join(", ", entry.labels)}");
            successCount++;
        }

        // Save changes
        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, null, true);
        AssetDatabase.SaveAssets();

        Debug.Log($"Successfully processed {successCount}/{selectedObjects.Length} assets. Scene assets also tagged with '{FUSION_SCENES_LABEL}'.");
    }

    private static AddressableAssetEntry CreateEntry(AddressableAssetSettings settings, string guid, string assetPath)
    {
        var defaultGroup = settings.DefaultGroup ?? settings.CreateGroup("Default Local Group", false, false, true, null);
        var entry = settings.CreateOrMoveEntry(guid, defaultGroup, false, false);
        entry.address = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        return entry;
    }

    [MenuItem(MENU_PATH, true)]
    private static bool ValidateCreateAndAssignLabelGroup()
    {
        return Selection.objects != null && Selection.objects.Length > 0;
    }
}