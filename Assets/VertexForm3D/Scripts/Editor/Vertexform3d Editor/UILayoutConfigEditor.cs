using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(UILayoutConfig))]
public class UILayoutConfigEditor : Editor
{
    private SerializedProperty _leftSectionEnabled;
    private SerializedProperty _leftSectionText;
    private SerializedProperty _mainSectionPanels;
    private SerializedProperty _worldCategories;
    private SerializedProperty _rightSectionEnabled;
    private SerializedProperty _mirror;
    private SerializedProperty _avatarDatas;

    private bool _foldPlatform = true;
    private bool _foldSettingsUI = true;
    private bool _foldLeft = true;
    private bool _foldMain = true;
    private bool _foldRight = true;
    private bool[] _foldPanels;
    private bool[] _foldAvatars;

    private void OnEnable()
    {
        _leftSectionEnabled = serializedObject.FindProperty("leftSectionEnabled");
        _leftSectionText = serializedObject.FindProperty("leftSectionText");
        _mainSectionPanels = serializedObject.FindProperty("mainSectionPanels");
        _worldCategories = serializedObject.FindProperty("worldCategories");
        _rightSectionEnabled = serializedObject.FindProperty("rightSectionEnabled");
        _mirror = serializedObject.FindProperty("mirror");
        _avatarDatas = serializedObject.FindProperty("avatarDatas");

        serializedObject.Update();
        while (_mainSectionPanels.arraySize > 3)
            _mainSectionPanels.DeleteArrayElementAtIndex(_mainSectionPanels.arraySize - 1);
        while (_mainSectionPanels.arraySize < 3)
            _mainSectionPanels.arraySize++;
        if (serializedObject.ApplyModifiedProperties())
            EditorUtility.SetDirty(target);

        const int panelCount = 3;
        if (_foldPanels == null || _foldPanels.Length != panelCount)
        {
            _foldPanels = new bool[panelCount];
            _foldPanels[0] = true;
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        while (_mainSectionPanels.arraySize > 3)
            _mainSectionPanels.DeleteArrayElementAtIndex(_mainSectionPanels.arraySize - 1);
        while (_mainSectionPanels.arraySize < 3)
            _mainSectionPanels.arraySize++;

        DrawHighlightPrefabButton(MainMapPrefabFileName);
        DrawSectionFoldout("Left Section", ref _foldLeft, DrawLeftSection, useLargeSectionTitle: true);
        DrawSectionFoldout("Main Section", ref _foldMain, DrawMainSection, useLargeSectionTitle: true);
        GUILayout.Space(4);
        DrawSectionFoldout("Right Section", ref _foldRight, DrawRightSection, useLargeSectionTitle: true);

        GUILayout.Space(8);
        DrawApplyToSceneBlock();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawApplyToSceneBlock()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Apply to prefabs", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Saves pending edits to this asset, assigns it on MainMap.prefab, then bakes text/images/section visibility into that prefab (same idea as Apply Project Data).", MessageType.None);
        if (GUILayout.Button("Apply to Prefabs", GUILayout.Height(28)))
            ApplyConfigToPrefabs();
        EditorGUILayout.EndVertical();
        GUILayout.Space(6);
    }

    private const string MainMapPrefabFileName = "MainMap.prefab";

    /// <summary>All prefab assets named MainMap.prefab (avoids LoadPrefabContents on unrelated/broken prefabs).</summary>
    private static List<string> FindMainMapPrefabPaths()
    {
        var paths = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.Equals(Path.GetFileName(path), MainMapPrefabFileName, System.StringComparison.OrdinalIgnoreCase))
                paths.Add(path);
        }
        return paths;
    }

    private void ApplyConfigToPrefabs()
    {
        // Inspector changes are normally applied after OnInspectorGUI; flush now so baking uses latest text/panels.
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);

        var config = target as UILayoutConfig;
        if (config == null) return;

        List<string> mainMapPaths = FindMainMapPrefabPaths();
        if (mainMapPaths.Count == 0)
        {
            Debug.LogWarning($"UILayoutConfig: No '{MainMapPrefabFileName}' found. Rename your menu prefab to MainMap.prefab or assign uiLayoutConfig on it manually.");
            return;
        }

        int prefabsModified = 0;
        int refsUpdated = 0;

        foreach (string path in mainMapPaths)
        {
            GameObject contents = null;
            try
            {
                contents = PrefabUtility.LoadPrefabContents(path);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"UILayoutConfig: Could not open '{path}': {e.Message}");
                continue;
            }

            if (contents == null) continue;

            MainMap[] mainMaps = contents.GetComponentsInChildren<MainMap>(true);
            if (mainMaps.Length == 0)
            {
                PrefabUtility.UnloadPrefabContents(contents);
                Debug.LogWarning($"UILayoutConfig: '{path}' has no MainMap component.");
                continue;
            }

            foreach (MainMap mm in mainMaps)
            {
                var so = new SerializedObject(mm);
                var prop = so.FindProperty("uiLayoutConfig");
                if (prop != null)
                {
                    if (prop.objectReferenceValue != config)
                    {
                        prop.objectReferenceValue = config;
                        so.ApplyModifiedProperties();
                        refsUpdated++;
                    }
                    else
                        so.ApplyModifiedProperties();
                }

                Undo.RecordObject(mm, "Apply UILayoutConfig to MainMap");
                mm.ApplyLayoutFromConfig();
                EditorUtility.SetDirty(mm);
            }

            PrefabUtility.SaveAsPrefabAsset(contents, path);
            prefabsModified++;
            PrefabUtility.UnloadPrefabContents(contents);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"UILayoutConfig: Baked config into {prefabsModified} MainMap.prefab (asset). Reference updates: {refsUpdated}. Ensure ProjectManager in your bootstrap scene uses this same config for Places/avatars.");
    }

    private static void DrawHighlightPrefabButton(string prefabFileName)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Highlight Prefab", EditorStyles.miniButton, GUILayout.Width(110)))
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.Equals(Path.GetFileName(path), prefabFileName, System.StringComparison.OrdinalIgnoreCase))
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
    }

    private static GUIStyle _largeSectionFoldoutStyle;

    private static GUIStyle LargeSectionFoldoutStyle
    {
        get
        {
            if (_largeSectionFoldoutStyle == null)
            {
                _largeSectionFoldoutStyle = new GUIStyle(EditorStyles.foldoutHeader)
                {
                    fontSize = EditorStyles.foldoutHeader.fontSize + 3,
                    fontStyle = FontStyle.Bold
                };
            }
            return _largeSectionFoldoutStyle;
        }
    }

    private void DrawSectionFoldout(string title, ref bool foldout, System.Action drawContent, bool useLargeSectionTitle = false)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        if (useLargeSectionTitle)
            foldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, title, LargeSectionFoldoutStyle);
        else
            foldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, title);
        EditorGUILayout.EndFoldoutHeaderGroup();
        if (foldout)
        {
            EditorGUI.indentLevel++;
            drawContent();
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
        GUILayout.Space(4);
    }


    private const float LeftSectionTextMinHeight = 64f;
    private const float LeftSectionTextMaxHeight = 320f;
    private const float LeftSectionTextLineHeight = 18f;
    private const int CharsPerWrappedLine = 60;

    private void DrawLeftSection()
    {
        EditorGUILayout.PropertyField(_leftSectionEnabled, new GUIContent("Enable"));

        EditorGUILayout.PrefixLabel(new GUIContent("Text", "Supports TextMeshPro rich text (e.g. <b>, <color=#fff>, <size=24>)."));
        string text = _leftSectionText.stringValue ?? "";
        int lineCount = 1;
        foreach (char c in text) if (c == '\n') lineCount++;
        int wrappedLines = Mathf.Max(0, (text.Length - 1) / CharsPerWrappedLine);
        int totalLines = Mathf.Max(lineCount, 1) + wrappedLines;
        float contentHeight = LeftSectionTextLineHeight * totalLines;
        float textAreaHeight = Mathf.Clamp(contentHeight, LeftSectionTextMinHeight, LeftSectionTextMaxHeight);
        _leftSectionText.stringValue = EditorGUILayout.TextArea(text, EditorStyles.textArea, GUILayout.MinHeight(textAreaHeight));
    }

    private void DrawMainSection()
    {
        for (int i = 0; i < 3; i++)
        {
            var panel = _mainSectionPanels.GetArrayElementAtIndex(i);
            string header = i == UILayoutConfig.MainPanelIndex ? "Main"
                : i == UILayoutConfig.PlacesPanelIndex ? "Places"
                : "Guide";

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _foldPanels[i] = EditorGUILayout.Foldout(_foldPanels[i], header, true);

            if (_foldPanels[i])
            {
                EditorGUI.indentLevel++;

                if (i == UILayoutConfig.MainPanelIndex)
                {
                    EditorGUILayout.PropertyField(panel.FindPropertyRelative("backgroundImage"), new GUIContent("Background Image"));
                    EditorGUILayout.PropertyField(panel.FindPropertyRelative("logoImage"), new GUIContent("Logo Image"));
                }
                else if (i == UILayoutConfig.PlacesPanelIndex)
                {
                    EditorGUILayout.HelpBox("Toggle per-category visibility with Show In Places Nav.", MessageType.None);
                    EditorGUILayout.PropertyField(_worldCategories, new GUIContent("World Categories"), true);
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    var lockIcon = EditorGUIUtility.IconContent("AssemblyLock");
                    if (lockIcon != null && lockIcon.image != null)
                        GUILayout.Label(lockIcon, GUILayout.Width(20), GUILayout.Height(20));
                    else
                        GUILayout.Label("🔒", GUILayout.Width(20));
                    EditorGUILayout.LabelField("Do not edit", EditorStyles.boldLabel);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawRightSection()
    {
        EditorGUILayout.PropertyField(_rightSectionEnabled, new GUIContent("Enable"));
        EditorGUILayout.PropertyField(_mirror, new GUIContent("Mirror"));
        EditorGUILayout.PropertyField(_avatarDatas, new GUIContent("Avatar Datas"));


    }
}
