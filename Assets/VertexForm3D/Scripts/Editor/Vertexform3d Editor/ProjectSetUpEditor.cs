using UnityEditor;
using UnityEngine;
using System.Linq; // Added for FirstOrDefault

public class ProjectSetUpEditor : EditorWindow
{
    private Vector2 scrollPosition;

    public static void ShowWindow()
    {
        var window = GetWindow<ProjectSetUpEditor>();
        VertexFormEditorHeader.ApplyWindowTitle(window, "Project Setup");
    }

    private void OnEnable()
    {
        VertexFormEditorHeader.ApplyWindowTitle(this, "Project Setup");
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Large banner kept only on Project Setup
        VertexFormEditorHeader.Draw(position.width);

        EditorGUILayout.Space(12);
        VertexFormEditorHeader.DrawPanelTitle("Project Setup");
        VertexFormEditorHeader.BeginPanelBody();

        GUIStyle subHeaderStyle = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold, fontSize = 18 };
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = MakeTex(2, 2, new Color(0.13f, 0.13f, 0.13f)); // Dark gray
        boxStyle.padding = new RectOffset(10, 10, 10, 10);

        GUIStyle textStyle = new GUIStyle(EditorStyles.label)
        {
            wordWrap = true,
            fontSize = 13,
            normal = { textColor = Color.white }
        };

        DrawSection("Project Settings", subHeaderStyle, boxStyle, textStyle,
            "Step 1: Open Player Settings\n" +
            "Go to Project Settings > Player\n\n" +
            "Step 2: Set Application Info\n" +
            "- Product Name\n" +
            "- Company Name\n" +
            "- Bundle Identifier (Package ID)\n\n" +
            "Step 3: Setup Keystore\n" +
            "- Create and store your keystore + password securely.",
            "Open Player Settings",
            () => SettingsService.OpenProjectSettings("Project/Player")
        );

        EditorGUILayout.Space(15);

        GUILayout.BeginVertical(boxStyle);
        GUILayout.Label("Photon Fusion 2 + Voice Setup", subHeaderStyle);
        GUILayout.Space(5);
        GUILayout.Label(
            "Step 1: Visit Photon Dashboard\n" +
            "Visit dashboard.photonengine.com and create a new Fusion application (copy Fusion App ID).\n\n" +
            "Step 2: Import Required Packages\n" +
            "• Photon Fusion 2 (includes networking and voice communication)\n\n" +
            "Step 3: Configure Fusion Settings\n" +
            "Click 'Open Fusion Hub' to configure your Fusion App ID and network settings.\n\n" +
            "Step 4: Configure Voice in Fusion\n" +
            "Click 'Open Fusion Settings' to configure voice settings within Fusion.\n\n" +
            "Step 5: Network Settings\n" +
            "Set your preferred region and configure tick rate (60 Hz recommended for VR).",
            textStyle);
        GUILayout.Space(8);

        if (GUILayout.Button("Open Fusion Hub"))
        {
            bool fusionMenuExists = EditorApplication.ExecuteMenuItem("Tools/Fusion/Fusion Hub");
            if (!fusionMenuExists)
            {
                EditorUtility.DisplayDialog("Fusion Hub Not Found",
                    "Photon Fusion 2 Hub not found. Please ensure Photon Fusion 2 is properly imported.\n\n" +
                    "You can manually configure Fusion by:\n" +
                    "1. Going to Tools > Fusion > Fusion Hub\n" +
                    "2. Or finding the Fusion settings in your project", "OK");
            }
        }

        if (GUILayout.Button("Open Fusion App Settings"))
        {
            string appSettingsPath = "Assets/Photon/Fusion/Resources/PhotonAppSettings.asset";
            var appSettings = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(appSettingsPath);
            if (appSettings != null)
            {
                AssetDatabase.OpenAsset(appSettings);
            }
            else
            {
                EditorUtility.DisplayDialog("Fusion App Settings Not Found",
                    "PhotonAppSettings.asset not found at expected location.\n\n" +
                    "Expected path: " + appSettingsPath + "\n\n" +
                    "Please ensure Photon Fusion 2 is properly imported.", "OK");
            }
        }
        GUILayout.EndVertical();
        VertexFormEditorHeader.EndPanelBody();
        EditorGUILayout.EndScrollView();
    }

    private void DrawSection(string title, GUIStyle titleStyle, GUIStyle boxStyle, GUIStyle textStyle,
        string content, string buttonLabel, System.Action buttonAction)
    {
        GUILayout.BeginVertical(boxStyle);
        GUILayout.Label(title, titleStyle);
        GUILayout.Space(5);
        GUILayout.Label(content, textStyle);
        GUILayout.Space(8);
        if (GUILayout.Button(buttonLabel))
        {
            buttonAction?.Invoke();
        }
        GUILayout.EndVertical();
    }

    // Utility to make a dark background texture
    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}
