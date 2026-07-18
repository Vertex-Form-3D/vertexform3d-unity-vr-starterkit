using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(ProjectDataScriptableObject))]
public class ProjectDataScriptableObjectEditor : UnityEditor.Editor
{
    private const string MainMapPrefabName = "MainMap";
    private const string DescriptionObjectName = "infoText";
    private const string EmailObjectName = "Email";
    private const string LogoImageName = "VertexForm3D_Logo";
    private const string BackgroundImageName = "VertexForm3D_Background";
    private const string WindowPanelName = "Project Data";

    private void OnEnable()
    {
        VertexFormEditorHeader.BrandHostWindow(target, WindowPanelName);
    }

    public override void OnInspectorGUI()
    {
        VertexFormEditorHeader.BrandHostWindow(target, WindowPanelName);
        VertexFormEditorHeader.DrawPanelTitle(WindowPanelName);
        VertexFormEditorHeader.BeginPanelBody();

        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        if (GUILayout.Button("Apply Project Data to Main Map Prefab", GUILayout.Height(28)))
        {
            ApplyToPrefab();
        }

        VertexFormEditorHeader.EndPanelBody();
    }

    private void ApplyToPrefab()
    {
        var so = (ProjectDataScriptableObject)target;
        if (so?.projectData == null)
        {
            Debug.LogWarning("Project Data SO or projectData is null.");
            return;
        }

        string prefabPath = FindMainMapPrefabPath();
        if (string.IsNullOrEmpty(prefabPath))
        {
            Debug.LogError("MainMap prefab not found in the project. Ensure 'MainMap.prefab' exists under VertexForm3D.");
            return;
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError("Could not load MainMap prefab contents.");
            return;
        }

        bool changed = false;

        // Apply description (infoText - TMP)
        var tmpTexts = prefabRoot.GetComponentsInChildren<TMPro.TMP_Text>(true);
        var infoText = tmpTexts.FirstOrDefault(t => t.gameObject.name == DescriptionObjectName);
        if (infoText != null)
        {
            if (infoText.text != so.projectData.homeSceneData.projectDescription)
            {
                infoText.text = so.projectData.homeSceneData.projectDescription;
                changed = true;
            }
        }
        else
        {
            Debug.LogWarning($"Could not find '{DescriptionObjectName}' in MainMap prefab.");
        }

        // Apply emails (Email - TMP)
        var emailText = tmpTexts.FirstOrDefault(t => t.gameObject.name == EmailObjectName);
        if (emailText != null)
        {
            string emails = so.projectData.homeSceneData.projectEmails ?? "";
            if (emailText.text != emails)
            {
                emailText.text = emails;
                changed = true;
            }
        }
        else
        {
            Debug.LogWarning($"Could not find '{EmailObjectName}' in MainMap prefab.");
        }

        // Apply logo image (VertexForm3D_Logo - Image)
        var images = prefabRoot.GetComponentsInChildren<UnityEngine.UI.Image>(true);
        if (so.projectData.homeSceneData.projectLogo != null)
        {
            var logoImage = images.FirstOrDefault(i => i.gameObject.name == LogoImageName);
            if (logoImage != null)
            {
                if (logoImage.sprite != so.projectData.homeSceneData.projectLogo)
                {
                    logoImage.sprite = so.projectData.homeSceneData.projectLogo;
                    changed = true;
                }
            }
            else
            {
                Debug.LogWarning($"Could not find '{LogoImageName}' in MainMap prefab.");
            }
        }

        // Apply optional background image (VertexForm3D_Background - Image)
        if (so.projectData.homeSceneData.projectBackgroundImage != null)
        {
            var bgImage = images.FirstOrDefault(i => i.gameObject.name == BackgroundImageName);
            if (bgImage != null)
            {
                if (bgImage.sprite != so.projectData.homeSceneData.projectBackgroundImage)
                {
                    bgImage.sprite = so.projectData.homeSceneData.projectBackgroundImage;
                    changed = true;
                }
            }
        }

        if (changed)
        {
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            AssetDatabase.Refresh();
            Debug.Log("Project data applied to MainMap prefab successfully.");
        }
        else
        {
            Debug.Log("No changes to apply (prefab already up to date).");
        }

        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }

    private static string FindMainMapPrefabPath()
    {
        string[] guids = AssetDatabase.FindAssets($"{MainMapPrefabName} t:Prefab");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith($"{MainMapPrefabName}.prefab", System.StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }
        }
        return null;
    }
}
