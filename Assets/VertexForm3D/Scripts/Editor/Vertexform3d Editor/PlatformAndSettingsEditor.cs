using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Platforms))]
public class PlatformAndSettingsEditor : Editor
{
    /// <summary>Below this inspector width, first two guides stack vertically so text stays readable.</summary>
    private const float MinWidthSideBySide = 420f;
    private const string WindowPanelName = "Platforms";

    private static GUIStyle s_RichWordWrap;

    private static GUIStyle RichWordWrapStyle
    {
        get
        {
            if (s_RichWordWrap == null)
                s_RichWordWrap = new GUIStyle(EditorStyles.wordWrappedLabel) { richText = true };
            return s_RichWordWrap;
        }
    }

    private void OnEnable()
    {
        VertexFormEditorHeader.BrandHostWindow(target, WindowPanelName);
    }

    public override void OnInspectorGUI()
    {
        VertexFormEditorHeader.BrandHostWindow(target, WindowPanelName);
        VertexFormEditorHeader.DrawPanelTitle(WindowPanelName);

        serializedObject.Update();

        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

        SerializedProperty platformChoiceProp = serializedObject.FindProperty("platformChoice");
        SerializedProperty webGpuBrowserKindProp = serializedObject.FindProperty("webGpuBrowserKind");

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(platformChoiceProp);
        if (platformChoiceProp.enumValueIndex == (int)platform.Web)
        {
            EditorGUILayout.PropertyField(
                webGpuBrowserKindProp,
                new GUIContent(
                    "Web Browser Kind (for testing)",
                    "When platform is Web, set from WebGL index.html (SendMessage). In Editor, use this for testing."));
            EditorGUILayout.HelpBox("Web browser kind is normally set at runtime from WebGL index.html (SendMessage). Use the field above to test a kind in the Editor.", MessageType.None);
        }
        bool platformSettingsChanged = EditorGUI.EndChangeCheck();

        EditorGUILayout.Space(8);

        var platforms = (Platforms)target;
        List<PlatformSetupGuide> guides = platforms.platformGuides;

        if (guides == null || guides.Count == 0)
        {
            EditorGUILayout.HelpBox("No platform guides configured. Right-click the asset and select Reset to populate defaults.", MessageType.Info);
            serializedObject.ApplyModifiedProperties();
            return;
        }

        EditorGUILayout.LabelField("Platform Setup Guide", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        float viewW = EditorGUIUtility.currentViewWidth;
        float usableW = Mathf.Max(100f, viewW - 20f);
        int helpBoxPadH = EditorStyles.helpBox.padding.left + EditorStyles.helpBox.padding.right;

        if (guides.Count >= 2)
        {
            bool sideBySide = viewW >= MinWidthSideBySide;
            float gutter = 6f;

            if (sideBySide)
            {
                float colOuter = (usableW - gutter) * 0.5f;
                float innerPerCol = Mathf.Max(80f, colOuter - helpBoxPadH);

                EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(colOuter), GUILayout.ExpandWidth(false));
                DrawCardContent(guides[0], innerPerCol);
                EditorGUILayout.EndVertical();
                GUILayout.Space(gutter);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(colOuter), GUILayout.ExpandWidth(false));
                DrawCardContent(guides[1], innerPerCol);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                float innerFull = Mathf.Max(100f, usableW - helpBoxPadH);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                DrawCardContent(guides[0], innerFull);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(6);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                DrawCardContent(guides[1], innerFull);
                EditorGUILayout.EndVertical();
            }

            for (int i = 2; i < guides.Count; i++)
            {
                EditorGUILayout.Space(6);
                float innerFull = Mathf.Max(100f, usableW - helpBoxPadH);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                DrawCardContent(guides[i], innerFull);
                EditorGUILayout.EndVertical();
            }
        }
        else
        {
            float innerFull = Mathf.Max(100f, usableW - helpBoxPadH);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawCardContent(guides[0], innerFull);
            EditorGUILayout.EndVertical();
        }

        serializedObject.ApplyModifiedProperties();

        if (platformSettingsChanged)
        {
            var editedPlatforms = (Platforms)target;
            bool shouldUseMobileControls =
                editedPlatforms.platformChoice == platform.Web &&
                editedPlatforms.webGpuBrowserKind == WebGpuBrowserKind.MobileBrowser;
            DesktopMobileControlSettings.SetUseMobileControls(shouldUseMobileControls);
        }
    }

    /// <param name="innerContentWidth">Width inside the help box for one row (after horizontal padding).</param>
    private static void DrawCardContent(PlatformSetupGuide guide, float innerContentWidth)
    {
        EditorGUILayout.LabelField(guide.title, EditorStyles.boldLabel);
        if (!string.IsNullOrEmpty(guide.subtitle))
            EditorGUILayout.LabelField(guide.subtitle, EditorStyles.miniLabel);

        EditorGUILayout.Space(4);

        const float numCol = 22f;
        float textWidth = Mathf.Max(40f, innerContentWidth - numCol - 8f);

        for (int i = 0; i < guide.steps.Count; i++)
        {
            var content = new GUIContent(guide.steps[i]);
            float lineHeight = RichWordWrapStyle.CalcHeight(content, textWidth);
            lineHeight = Mathf.Max(lineHeight, EditorGUIUtility.singleLineHeight);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(numCol), GUILayout.Height(lineHeight));
            EditorGUILayout.LabelField(content, RichWordWrapStyle, GUILayout.Width(textWidth), GUILayout.Height(lineHeight));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);
        }

        if (!string.IsNullOrEmpty(guide.note))
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(guide.note, MessageType.Info);
        }
    }
}
