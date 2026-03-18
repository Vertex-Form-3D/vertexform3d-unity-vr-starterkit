using System.IO;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SettingsUISO))]
public class SettingsUIEditor : Editor
{
    private const string SettingsUIPrefabFileName = "SettingsUI.prefab";

    private static readonly string DefaultSettingsHelp =
        "These are the default settings that are enabled or disabled by default. " +
        "The settings UI is accessible via the left controller on the Y button.";

    public override void OnInspectorGUI()
    {
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
                        EditorGUIUtility.PingObject(obj);
                        Selection.activeObject = obj;
                    }
                    break;
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("anonymousUserNamePrefix"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultSettings"));

        EditorGUILayout.Space(2);
        EditorGUILayout.HelpBox(DefaultSettingsHelp, MessageType.Info);

        serializedObject.ApplyModifiedProperties();
    }
}
