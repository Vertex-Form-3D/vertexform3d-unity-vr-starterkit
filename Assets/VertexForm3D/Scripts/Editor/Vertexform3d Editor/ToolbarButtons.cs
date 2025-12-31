#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;
using System.Reflection;
using UnityEngine;

[InitializeOnLoad]
public static class ToolbarButtons
{
    private static VisualElement toolbarUI;
    private static float positionOffset = 180f; // Move closer to Play button
    private static float buttonHeight = 20f; // Button height

    static ToolbarButtons()
    {
        EditorApplication.delayCall += AddToolbarUI;
    }

    static void AddToolbarUI()
    {
        /*if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;*/

        var toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
        if (toolbarType == null) return;

        var toolbars = Resources.FindObjectsOfTypeAll(toolbarType);
        if (toolbars.Length == 0) return;

        var toolbar = toolbars[0];
        var rootField = toolbarType.GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
        if (rootField == null) return;

        var root = rootField.GetValue(toolbar) as VisualElement;
        if (root == null) return;

        var leftContainer = root.Q("ToolbarZoneLeftAlign");
        if (leftContainer == null) return;

        // Remove old UI if it exists to prevent duplication
        if (toolbarUI != null)
        {
            leftContainer.Remove(toolbarUI);
        }

        toolbarUI = new IMGUIContainer(OnGUI);
        toolbarUI.style.marginLeft = positionOffset;
        leftContainer.Add(toolbarUI);
    }

    static void OnGUI()
    {
        /*if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;*/

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("XR Device Simulator", GUILayout.Height(buttonHeight)))
        {
            GameObject g = Object.Instantiate(Resources.Load<GameObject>("CustomEditor/XR Device Simulator"));
            g.name = "XR Device Simulator";
            EditorGUIUtility.PingObject(g);
            Debug.Log("Toolbar button pressed!");
        }
        GUILayout.EndHorizontal();
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.ExitingPlayMode)
        {
            EditorApplication.delayCall += () => AddToolbarUI();
        }
    }
}
#endif