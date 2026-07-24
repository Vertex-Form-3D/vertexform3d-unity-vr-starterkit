using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Shared Vertex Form editor chrome: Project Setup banner, large panel titles,
/// and branded window titles of the form "Vertex Form > Panel Name".
/// </summary>
public static class VertexFormEditorHeader
{
    private const string HeaderAssetPath = "Assets/VertexForm3D/UI/vertexform-header-for-SDK.png";
    private const string LogoAssetPath = "Assets/VertexForm3D/UI/vertexform-Logo.png";
    private const string BrandedTitlePrefix = "Vertex Form > ";
    private const float PanelTitleBottomSpace = 18f;
    private const int PanelTitleLeftPadding = 14;

    private static Texture2D cachedHeader;
    private static Texture2D cachedLogo;
    private static GUIStyle panelTitleStyle;

    private static Texture2D HeaderTexture
    {
        get
        {
            if (cachedHeader == null)
                cachedHeader = AssetDatabase.LoadAssetAtPath<Texture2D>(HeaderAssetPath);
            return cachedHeader;
        }
    }

    public static Texture2D LogoTexture
    {
        get
        {
            if (cachedLogo == null)
                cachedLogo = AssetDatabase.LoadAssetAtPath<Texture2D>(LogoAssetPath);
            return cachedLogo;
        }
    }

    /// <summary>
    /// Creates a window title: "Vertex Form > {panelName}" (text only; branding banner is drawn in-window).
    /// </summary>
    public static GUIContent CreateWindowTitle(string panelName)
    {
        string text = string.IsNullOrEmpty(panelName)
            ? "Vertex Form"
            : $"{BrandedTitlePrefix}{panelName}";
        return new GUIContent(text);
    }

    /// <summary>
    /// Applies the branded title to an EditorWindow.
    /// </summary>
    public static void ApplyWindowTitle(EditorWindow window, string panelName)
    {
        if (window == null)
            return;

        GUIContent desired = CreateWindowTitle(panelName);
        GUIContent current = window.titleContent;
        if (current != null
            && current.text == desired.text
            && ReferenceEquals(current.image, desired.image))
            return;

        window.titleContent = desired;
    }

    /// <summary>
    /// Opens a ScriptableObject in a Vertex Form branded floating window.
    /// </summary>
    public static void OpenBrandedPropertyEditor(Object asset, string panelName)
    {
        VertexFormScriptableObjectWindow.Open(asset, panelName);
    }

    /// <summary>
    /// Kept for CustomEditor callers. Branding is owned by
    /// <see cref="VertexFormScriptableObjectWindow"/> when opened from the Vertex Form menu.
    /// Selecting the asset in the Project window still uses the normal Inspector.
    /// </summary>
    public static void BrandHostWindow(Object target, string panelName)
    {
        // No-op for docked Inspector. Floating SO panels use VertexFormScriptableObjectWindow.
    }

    private static GUIStyle PanelTitleStyle
    {
        get
        {
            if (panelTitleStyle == null)
            {
                panelTitleStyle = new GUIStyle(EditorStyles.label)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 25,
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = true,
                    padding = new RectOffset(PanelTitleLeftPadding, 0, 4, 0),
                    margin = new RectOffset(4, 4, 6, 0)
                };
            }

            return panelTitleStyle;
        }
    }

    /// <summary>
    /// Draws the large all-caps panel title used across Vertex Form windows
    /// (same style as Creator Toolkit), with space below before content.
    /// </summary>
    public static void DrawPanelTitle(string title)
    {
        if (string.IsNullOrEmpty(title))
            return;

        GUILayout.Label(title.ToUpperInvariant(), PanelTitleStyle);
        GUILayout.Space(PanelTitleBottomSpace);
    }

    /// <summary>
    /// Begins the padded content area used below the panel title so all panel items
    /// share the same horizontal inset as the header. Pair with <see cref="EndPanelBody"/>.
    /// </summary>
    public static void BeginPanelBody()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Space(PanelTitleLeftPadding);
        GUILayout.BeginVertical();
    }

    /// <summary>
    /// Ends the padded content area started by <see cref="BeginPanelBody"/>.
    /// </summary>
    public static void EndPanelBody()
    {
        GUILayout.EndVertical();
        GUILayout.Space(PanelTitleLeftPadding);
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// Width available inside the padded panel body for a given view width.
    /// Use instead of raw position.width when computing layout inside the body.
    /// </summary>
    public static float PanelBodyWidth(float viewWidth)
    {
        return Mathf.Max(0f, viewWidth - (PanelTitleLeftPadding * 2f));
    }

    /// <summary>
    /// Draws the large SDK banner at the top of Vertex Form editor windows.
    /// </summary>
    public static void Draw(float viewWidth)
    {
        Texture2D header = HeaderTexture;

        GUILayout.Space(5);

        if (header == null)
        {
            EditorGUILayout.HelpBox(
                "Header image not found at 'Assets/VertexForm3D/UI/vertexform-header-for-SDK.png'.",
                MessageType.Warning);
            return;
        }

        float available = viewWidth > 1f ? viewWidth : EditorGUIUtility.currentViewWidth;
        float width = Mathf.Min(header.width, available - 20f);
        float height = (width / header.width) * header.height;

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label(header, GUILayout.Width(width), GUILayout.Height(height));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
    }

    /// <summary>
    /// Draws the header banner using the current inspector/editor view width.
    /// </summary>
    public static void Draw()
    {
        Draw(EditorGUIUtility.currentViewWidth);
    }
}
