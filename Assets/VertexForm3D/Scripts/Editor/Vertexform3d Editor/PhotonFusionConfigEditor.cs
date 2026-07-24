using UnityEngine;
using UnityEditor;
using System.IO;
using Fusion.Photon.Realtime;

[System.Serializable]
public class PhotonConfigData
{
    public string FusionAppId = "";
    public string VoiceAppId = "";
    public string ReadyPlayerMeAppId = "";
    public string AddressablesPath = "";
}

public class PhotonFusionConfigEditor : EditorWindow
{
    private PhotonConfigData configData = new PhotonConfigData();
    private Vector2 scrollPosition;
    private string filePath;
    private const string PhotonAppSettingsPath = "PhotonAppSettings";
    private const string CoreSettingsPath = "CoreSettings";

    [MenuItem("Window/Photon Fusion Config Manager")]
    public static void ShowWindow()
    {
        var window = GetWindow<PhotonFusionConfigEditor>();
        VertexFormEditorHeader.ApplyWindowTitle(window, "Photon Fusion Config");
    }

    private void OnEnable()
    {
        VertexFormEditorHeader.ApplyWindowTitle(this, "Photon Fusion Config");
        filePath = Path.Combine(Application.persistentDataPath, "PhotonFusionConfig.json");
        LoadAllData();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        VertexFormEditorHeader.DrawPanelTitle("Photon Fusion Config");
        VertexFormEditorHeader.BeginPanelBody();

        EditorGUILayout.HelpBox(
            "Manage your Photon Fusion, Voice, and Ready Player Me configuration in one place.\n" +
            "Changes are saved to a JSON file and can be applied to project assets.",
            MessageType.Info);

        EditorGUILayout.Space(10);

        // === Photon Settings Group ===
        EditorGUILayout.LabelField("Photon App Settings", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        configData.FusionAppId = EditorGUILayout.TextField(new GUIContent("Fusion App ID", "Photon Fusion App ID"), configData.FusionAppId);
        configData.VoiceAppId = EditorGUILayout.TextField(new GUIContent("Voice App ID", "Photon Voice App ID"), configData.VoiceAppId);
        EditorGUI.indentLevel--;

        EditorGUILayout.Space(15);

        // === Ready Player Me Settings Group ===
        EditorGUILayout.LabelField("Ready Player Me Settings", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        configData.ReadyPlayerMeAppId = EditorGUILayout.TextField(
            new GUIContent("RPM App ID (Optional)", "Required for partner features or analytics"),
            configData.ReadyPlayerMeAppId);
        EditorGUI.indentLevel--;

        EditorGUILayout.Space(15);

        // === Addressables Settings ===
        EditorGUILayout.LabelField("Addressables", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        configData.AddressablesPath = EditorGUILayout.TextField(
            new GUIContent("Custom Addressables Path (Optional)", "Override default catalog path if needed"),
            configData.AddressablesPath);
        EditorGUI.indentLevel--;

        EditorGUILayout.Space(25);

        // === Action Buttons ===
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Create Default JSON", GUILayout.Height(30)))
        {
            CreateDefaultJson();
        }
        if (GUILayout.Button("Load from JSON", GUILayout.Height(30)))
        {
            LoadFromJson();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save to JSON", GUILayout.Height(30)))
        {
            SaveToJson();
        }
        if (GUILayout.Button("Apply to Project", GUILayout.Height(30)))
        {
            ApplyToProject();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(20);

        // === File Info ===
        EditorGUILayout.HelpBox("JSON File Location:\n" + filePath, MessageType.None);

        // === Asset Status ===
        var photonSettings = Resources.Load<PhotonAppSettings>(PhotonAppSettingsPath);

        if (photonSettings == null)
        {
            EditorGUILayout.HelpBox("⚠ PhotonAppSettings.asset not found in any Resources folder!", MessageType.Error);
        }
        else
        {
            EditorGUILayout.HelpBox("✓ PhotonAppSettings.asset found and ready.", MessageType.Info);
        }

        VertexFormEditorHeader.EndPanelBody();
        EditorGUILayout.EndScrollView();
    }

    private void CreateDefaultJson()
    {
        if (File.Exists(filePath))
        {
            Debug.Log("JSON file already exists: " + filePath);
            return;
        }

        configData = new PhotonConfigData();
        SaveJsonFile();
        Debug.Log("Default configuration JSON created at: " + filePath);
        LoadAllData(); // Refresh UI
    }

    private void LoadFromJson()
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            JsonUtility.FromJsonOverwrite(json, configData);
            Debug.Log("Configuration loaded from JSON: " + filePath);
        }
        else
        {
            EditorUtility.DisplayDialog("File Not Found", "No JSON config file found at:\n" + filePath + "\n\nCreate one first.", "OK");
        }
    }

    private void SaveToJson()
    {
        SaveJsonFile();
        EditorUtility.DisplayDialog("Saved", "Configuration successfully saved to JSON!", "OK");
    }

    private void SaveJsonFile()
    {
        string json = JsonUtility.ToJson(configData, true);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)); // Ensure directory exists
        File.WriteAllText(filePath, json);
        Debug.Log("Config saved to: " + filePath);
    }

    private void ApplyToProject()
    {
        bool modified = false;

        // Update PhotonAppSettings
        var photonSettings = Resources.Load<PhotonAppSettings>(PhotonAppSettingsPath);
        if (photonSettings == null)
        {
            EditorUtility.DisplayDialog("Error", "PhotonAppSettings.asset not found!\nPlace it in a Resources folder (e.g., Assets/Resources/).", "OK");
            return;
        }

        if (!string.IsNullOrEmpty(configData.FusionAppId))
        {
            photonSettings.AppSettings.AppIdFusion = configData.FusionAppId;
            modified = true;
        }
        if (!string.IsNullOrEmpty(configData.VoiceAppId))
        {
            photonSettings.AppSettings.AppIdVoice = configData.VoiceAppId;
            modified = true;
        }

        // Save Addressables path via PlayerPrefs (common practice, or use your own ScriptableObject)
        PlayerPrefs.SetString("Addressables_Path", configData.AddressablesPath);
        PlayerPrefs.Save();

        if (modified)
        {
            EditorUtility.SetDirty(photonSettings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Project settings updated successfully!");
        }

        EditorUtility.DisplayDialog("Success", "All applicable settings have been applied to the project!", "OK");
    }

    private void LoadAllData()
    {
        // First try to load from JSON
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            JsonUtility.FromJsonOverwrite(json, configData);
        }
        else
        {
            // Fallback: load from current project assets (for backward compatibility)
            LoadFromProjectAssets();
        }
    }

    private void LoadFromProjectAssets()
    {
        var photonSettings = Resources.Load<PhotonAppSettings>(PhotonAppSettingsPath);
        if (photonSettings != null)
        {
            configData.FusionAppId = photonSettings.AppSettings.AppIdFusion ?? "";
            configData.VoiceAppId = photonSettings.AppSettings.AppIdVoice ?? "";
        }

        configData.AddressablesPath = PlayerPrefs.GetString("Addressables_Path", "");
    }
}