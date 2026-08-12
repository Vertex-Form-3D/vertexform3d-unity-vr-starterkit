using UnityEngine;
using UnityEditor;

public class VertexForm3DHelp : EditorWindow
{
    public static void ShowWindow()
    {
        VertexForm3DHelp window = GetWindow<VertexForm3DHelp>();
        VertexFormEditorHeader.ApplyWindowTitle(window, "Help & Support");
        window.minSize = new Vector2(450, 350);
        window.Show();
    }

    private void OnEnable()
    {
        VertexFormEditorHeader.ApplyWindowTitle(this, "Help & Support");
    }

    private void OnGUI()
    {
        VertexFormEditorHeader.Draw(position.width);
        VertexFormEditorHeader.DrawPanelTitle("Help & Support");
        VertexFormEditorHeader.BeginPanelBody();

        EditorGUILayout.LabelField("Need Help? Reach Out to Us!", EditorStyles.boldLabel);
        GUILayout.Space(5);

        // Tutorials Section
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Tutorials", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Follow step-by-step guides to install, configure, and build with Vertex Form 3D.", EditorStyles.wordWrappedLabel);
        if (GUILayout.Button("Open Tutorials", GUILayout.Height(25)))
        {
            Application.OpenURL("https://vertexform3d.com/tutorials/");
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

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

        VertexFormEditorHeader.EndPanelBody();
    }
}
