using UnityEditor;
using UnityEngine;

/// <summary>
/// Shared helper that draws the VertexForm SDK header image at the top of editor windows,
/// giving every VertexForm editor panel a consistent branded header.
/// </summary>
public static class VertexFormEditorHeader
{
    private const string HeaderAssetPath = "Assets/VertexForm3D/UI/vertexform-header-for-SDK.png";
    private static Texture2D cachedHeader;

    private static Texture2D HeaderTexture
    {
        get
        {
            if (cachedHeader == null)
            {
                cachedHeader = AssetDatabase.LoadAssetAtPath<Texture2D>(HeaderAssetPath);
            }
            return cachedHeader;
        }
    }

    /// <summary>
    /// Draws the header banner centered at the top of the current editor layout.
    /// </summary>
    /// <param name="viewWidth">Width available for the header (typically the window's position.width).</param>
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
