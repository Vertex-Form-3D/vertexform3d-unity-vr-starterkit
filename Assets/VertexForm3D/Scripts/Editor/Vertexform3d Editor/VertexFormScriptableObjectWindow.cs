using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

/// <summary>
/// Floating EditorWindow that hosts a ScriptableObject inspector with a stable
/// branded title: "[Logo] Vertex Form > Panel Name".
/// Prefer this over <see cref="EditorUtility.OpenPropertyEditor"/> which resets its own title on focus.
/// </summary>
public class VertexFormScriptableObjectWindow : EditorWindow
{
    [SerializeField] private Object targetAsset;
    [SerializeField] private string panelName = "Panel";

    private Editor cachedEditor;
    private Vector2 scrollPosition;
    private Object editorTarget;

    [OnOpenAsset(0)]
    private static bool OnOpenAsset(int instanceId, int line)
    {
        Object asset = EditorUtility.InstanceIDToObject(instanceId);
        if (!TryGetPanelName(asset, out string name))
            return false;

        Open(asset, name);
        return true;
    }

    private static bool TryGetPanelName(Object asset, out string panelName)
    {
        panelName = null;
        if (asset == null)
            return false;

        if (asset is UILayoutConfig)
        {
            panelName = "Main UI Database";
            return true;
        }

        if (asset is Platforms)
        {
            panelName = "Platforms";
            return true;
        }

        if (asset is SettingsUISO)
        {
            panelName = "Settings";
            return true;
        }

        if (asset is ProjectDataScriptableObject)
        {
            panelName = "Project Data";
            return true;
        }

        return false;
    }

    public static void Open(Object asset, string panelName)
    {
        if (asset == null)
            return;

        // Reuse an existing window for the same asset when possible.
        VertexFormScriptableObjectWindow[] openWindows =
            Resources.FindObjectsOfTypeAll<VertexFormScriptableObjectWindow>();
        for (int i = 0; i < openWindows.Length; i++)
        {
            VertexFormScriptableObjectWindow existing = openWindows[i];
            if (existing == null || existing.targetAsset != asset)
                continue;

            existing.panelName = panelName;
            existing.ApplyTitle();
            existing.Focus();
            existing.Repaint();
            return;
        }

        VertexFormScriptableObjectWindow window = CreateInstance<VertexFormScriptableObjectWindow>();
        window.targetAsset = asset;
        window.panelName = panelName;
        window.minSize = new Vector2(420, 360);
        window.ApplyTitle();
        window.Show();
        window.Focus();
    }

    private void OnEnable()
    {
        ApplyTitle();
        RecreateEditorIfNeeded();
    }

    private void OnDisable()
    {
        DestroyEditor();
    }

    private void OnDestroy()
    {
        DestroyEditor();
    }

    private void OnFocus()
    {
        ApplyTitle();
    }

    private void OnInspectorUpdate()
    {
        // Keep title stable if Unity/editor chrome tries to rewrite it.
        ApplyTitle();
    }

    private void ApplyTitle()
    {
        VertexFormEditorHeader.ApplyWindowTitle(this, panelName);
    }

    private void RecreateEditorIfNeeded()
    {
        if (targetAsset == null)
        {
            DestroyEditor();
            return;
        }

        if (cachedEditor != null && editorTarget == targetAsset)
            return;

        DestroyEditor();
        cachedEditor = Editor.CreateEditor(targetAsset);
        editorTarget = targetAsset;
    }

    private void DestroyEditor()
    {
        if (cachedEditor != null)
        {
            DestroyImmediate(cachedEditor);
            cachedEditor = null;
        }

        editorTarget = null;
    }

    private void OnGUI()
    {
        ApplyTitle();

        if (targetAsset == null)
        {
            EditorGUILayout.HelpBox("The ScriptableObject asset is missing.", MessageType.Warning);
            return;
        }

        RecreateEditorIfNeeded();
        if (cachedEditor == null)
        {
            EditorGUILayout.HelpBox("Could not create an inspector for this asset.", MessageType.Error);
            return;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        cachedEditor.OnInspectorGUI();
        EditorGUILayout.EndScrollView();
    }
}
