using UnityEngine;
using UnityEditor;

public class VertexForm3DHelp : EditorWindow
{
    private Texture2D bannerTexture;

    [MenuItem("VertexForm3D SDK/Help")]
    public static void ShowWindow()
    {
        VertexForm3DHelp window = GetWindow<VertexForm3DHelp>("Help & Support");
        window.minSize = new Vector2(450, 350); // Adjusted window size
        window.Show();
    }

    private void OnEnable()
    {
        // Load the banner from the Resources folder
        bannerTexture = Resources.Load<Texture2D>("VF3DBannerEditor");
    }

    private void OnGUI()
    {
        GUILayout.Space(5);

        // Display Banner Image
        if (bannerTexture != null)
        {
            float bannerWidth = Mathf.Min(bannerTexture.width, position.width - 10); // Fit within window width
            float bannerHeight = (bannerWidth / bannerTexture.width) * bannerTexture.height; // Maintain aspect ratio
            GUILayout.Label(bannerTexture, GUILayout.Width(bannerWidth), GUILayout.Height(bannerHeight));
        }
        else
        {
            EditorGUILayout.HelpBox("Banner image not found. Make sure 'VF3DBannerEditor' is inside the Resources folder.", MessageType.Warning);
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        EditorGUILayout.LabelField("Need Help? Reach Out to Us!", EditorStyles.boldLabel);
        GUILayout.Space(5);

        // Discord Section
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Join Our Discord", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Connect with our community and get real-time support on Discord.", EditorStyles.wordWrappedLabel);
        if (GUILayout.Button("Join Discord", GUILayout.Height(25)))
        {
            Application.OpenURL("https://discord.me/vf3d");
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // GitHub Discussions Section
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Start a Discussion on GitHub", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Have a question or feature request? Start a discussion on GitHub.", EditorStyles.wordWrappedLabel);
        if (GUILayout.Button("Open GitHub Discussions", GUILayout.Height(25)))
        {
            Application.OpenURL("https://github.com/Vertex-Form-3D/vertexform3d-unity-vr-starterkit/discussions");
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // Email Support Section
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Email Support", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("For direct inquiries, send us an email.", EditorStyles.wordWrappedLabel);
        if (GUILayout.Button("Contact Us", GUILayout.Height(25)))
        {
            Application.OpenURL("https://vertexform3d.com/contact/");
        }
        EditorGUILayout.EndVertical();
    }
}
