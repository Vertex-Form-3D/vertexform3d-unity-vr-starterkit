using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Shared Vertex Form editor chrome: Project Setup banner, and branded window titles
/// of the form "[Logo] Vertex Form > Panel Name".
/// </summary>
public static class VertexFormEditorHeader
{
    private const string HeaderAssetPath = "Assets/VertexForm3D/UI/vertexform-header-for-SDK.png";
    private const string LogoAssetPath = "Assets/VertexForm3D/UI/vertexform-Logo.png";
    private const string BrandedTitlePrefix = "Vertex Form > ";

    private static Texture2D cachedHeader;
    private static Texture2D cachedLogo;

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
    /// Creates a window title: "[Logo] Vertex Form > {panelName}".
    /// </summary>
    public static GUIContent CreateWindowTitle(string panelName)
    {
        string text = string.IsNullOrEmpty(panelName)
            ? "Vertex Form"
            : $"{BrandedTitlePrefix}{panelName}";
        return new GUIContent(text, LogoTexture);
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

    /// <summary>
    /// Draws the large SDK banner (intended for Project Setup only).
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
