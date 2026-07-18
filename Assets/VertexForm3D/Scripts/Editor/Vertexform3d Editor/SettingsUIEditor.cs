using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(SettingsUISO))]
public class SettingsUIEditor : Editor
{
    private const string SettingsUIPrefabFileName = "SettingsUI.prefab";
    private const string WindowPanelName = "Settings";
    private bool hasInitializedDefaultSettingsFoldout;

    private static readonly string DefaultSettingsHelp =
        "These are the default settings that are enabled or disabled by default. " +
        "The settings UI is accessible via the left controller on the Y button.";

    private static readonly string PhotonCcuHelp =
        "Photon CCU: the session-list lobby uses a second NetworkRunner (one extra CCU) so worlds can show live player counts. " +
        "Choose game-sessions-only to skip that runner and hide counts, freeing a CCU for more players in rooms.";

    private static readonly string IdleTimeoutHelp =
        "Set the number of minutes a user can remain inactive before IdleQuitDetector automatically quits the session or returns them to the Home scene (configured on the Login scene).";

    private void OnEnable()
    {
        hasInitializedDefaultSettingsFoldout = false;
        VertexFormEditorHeader.BrandHostWindow(target, WindowPanelName);
    }

    public override void OnInspectorGUI()
    {
        VertexFormEditorHeader.BrandHostWindow(target, WindowPanelName);
        VertexFormEditorHeader.DrawPanelTitle(WindowPanelName);
        VertexFormEditorHeader.BeginPanelBody();

        serializedObject.Update();

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Highlight Prefab", EditorStyles.miniButton, GUILayout.Width(110)))
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.Equals(Path.GetFileName(path), SettingsUIPrefabFileName, System.StringComparison.OrdinalIgnoreCase))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                    if (obj != null)
                    {
                        PrefabStageUtility.OpenPrefab(path);
                        EditorGUIUtility.PingObject(obj);
                        Selection.activeObject = obj;
                    }
                    break;
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.HelpBox(DefaultSettingsHelp, MessageType.Info);

        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("anonymousUserNamePrefix"));

        SerializedProperty defaultSettingsProperty = serializedObject.FindProperty("defaultSettings");
        if (defaultSettingsProperty != null)
        {
            // Expand once by default, then let the user control foldout state.
            if (!hasInitializedDefaultSettingsFoldout)
            {
                defaultSettingsProperty.isExpanded = true;
                hasInitializedDefaultSettingsFoldout = true;
            }
            EditorGUILayout.PropertyField(defaultSettingsProperty, true);
        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("photonCcuAllocation"));

        EditorGUILayout.Space(2);
        EditorGUILayout.HelpBox(PhotonCcuHelp, MessageType.Info);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("idleTimeoutMinutes"));

        EditorGUILayout.Space(2);
        EditorGUILayout.HelpBox(IdleTimeoutHelp, MessageType.Info);

        serializedObject.ApplyModifiedProperties();

        VertexFormEditorHeader.EndPanelBody();
    }
}
