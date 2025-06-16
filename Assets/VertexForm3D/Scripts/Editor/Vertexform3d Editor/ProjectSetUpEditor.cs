using UnityEditor;
using UnityEngine;
using Photon.Pun;

public class ProjectSetUpEditor : EditorWindow
{
    private Texture2D bannerTexture;
    private Vector2 scrollPosition;

    public static void ShowWindow()
    {
        GetWindow<ProjectSetUpEditor>("Project Setup");
    }

    private void OnEnable()
    {
        bannerTexture = Resources.Load<Texture2D>("VF3DBannerEditor");
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        GUILayout.Space(5);

        if (bannerTexture != null)
        {
            float bannerWidth = Mathf.Min(bannerTexture.width, position.width - 10);
            float bannerHeight = (bannerWidth / bannerTexture.width) * bannerTexture.height;
            GUILayout.Label(bannerTexture, GUILayout.Width(bannerWidth), GUILayout.Height(bannerHeight));
        }

        EditorGUILayout.Space(20);

        GUIStyle headerStyle = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold, fontSize = 25 };
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

        GUILayout.Label("PROJECT SETUP", headerStyle);
        EditorGUILayout.Space(15);

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

        DrawSection("Photon Setup", subHeaderStyle, boxStyle, textStyle,
            "Step 1: Visit Photon Dashboard\n" +
            "Visit dashboard.photonengine.com and set up a new app. Copy Photon PUN and Photon Voice API keys.\n\n" +
            "Step 2: Select PhotonServerSettings\n" +
            "Locate the 'PhotonServerSettings' asset by clicking on the button below.\n\n" +
            "Step 3: Enter App ID\n" +
            "Paste your Photon PUN and Photon VOICE App ID field.\n\n" +
            "Step 4: Fixed Region\n" +
            "Dev region and Fixed region should be set to 'eu'.",
            "Open Photon Server Settings",
            () =>
            {
                var serverSettings = PhotonNetwork.PhotonServerSettings;
                if (serverSettings != null)
                    AssetDatabase.OpenAsset(serverSettings);
                else
                    EditorUtility.DisplayDialog("Error", "PhotonServerSettings not found. Make sure PUN2 is imported.", "OK");
            }
        );

        EditorGUILayout.Space(15);

        DrawSection("Cesium Setup", subHeaderStyle, boxStyle, textStyle,
            "Step 1: Open Cesium Panel\n" +
            "Click the Cesium button from the menu.\n\n" +
            "Step 2: Connect to Cesium ion\n" +
            "Tap the Connect button and follow the browser login.\n\n" +
            "Step 3: Authorize Access\n" +
            "Allow permissions on the Cesium website.\n\n" +
            "Step 4: Return to Unity\n" +
            "Unity will auto-complete the link once you return.",
            "Open Cesium Window",
            () => EditorApplication.ExecuteMenuItem("Cesium/Cesium")
        );

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
