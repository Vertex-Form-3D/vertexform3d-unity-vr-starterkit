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

        int c = _mainSectionPanels.arraySize;
        if (_foldPanels == null || _foldPanels.Length != c)
        {
            _foldPanels = new bool[Mathf.Max(c, 1)];
            if (c > 0) _foldPanels[0] = true; // expand first panel by default
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawSectionFoldout("Left Section", ref _foldLeft, DrawLeftSection);
        DrawSectionFoldout("Main Section", ref _foldMain, DrawMainSection);
        GUILayout.Space(4);
        DrawSectionFoldout("Right Section", ref _foldRight, DrawRightSection);

        GUILayout.Space(8);
        DrawApplyToSceneBlock();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawApplyToSceneBlock()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Apply to prefabs", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Assigns this config to every prefab that has a MainMap component. Saves the prefab assets.", MessageType.None);
        if (GUILayout.Button("Apply to Prefabs", GUILayout.Height(28)))
            ApplyConfigToPrefabs();
        EditorGUILayout.EndVertical();
        GUILayout.Space(6);
    }

    private void ApplyConfigToPrefabs()
    {
        var config = target as UILayoutConfig;
        if (config == null) return;

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        int appliedCount = 0;

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabRoot == null) continue;

            MainMap mainMaps = prefabRoot.GetComponent<MainMap>();
            if (mainMaps == null) continue;

            var so = new SerializedObject(mainMaps);
            var prop = so.FindProperty("uiLayoutConfig");
            if (prop != null)
            {
                prop.objectReferenceValue = config;
                so.ApplyModifiedProperties();
                appliedCount++;
            }

            EditorUtility.SetDirty(prefabRoot);
        }

        if (appliedCount > 0)
            AssetDatabase.SaveAssets();

        if (appliedCount == 0)
            Debug.LogWarning("UILayoutConfig: No prefab with a MainMap component found in the project.");
        else
            Debug.Log($"UILayoutConfig: Applied to {appliedCount} MainMap component(s) across prefabs.");
    }

    private void DrawSectionFoldout(string title, ref bool foldout, System.Action drawContent)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
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
        int c = _mainSectionPanels.arraySize;
        if (_foldPanels == null || _foldPanels.Length != c)
            System.Array.Resize(ref _foldPanels, Mathf.Max(c, 1));

        for (int i = 0; i < c; i++)
        {
            var panel = _mainSectionPanels.GetArrayElementAtIndex(i);
            var panelName = panel.FindPropertyRelative("panelName").stringValue;
            var panelType = (MainPanelType)panel.FindPropertyRelative("panelType").enumValueIndex;

            string header = $"Panel {i + 1} - {panelName}";

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            _foldPanels[i] = EditorGUILayout.Foldout(_foldPanels[i], header, true);
            if (GUILayout.Button("⋮", GUILayout.Width(22)))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Duplicate"), false, () => DuplicatePanel(i));
                menu.AddItem(new GUIContent("Remove"), false, () => RemovePanel(i));
                menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();

            if (_foldPanels[i])
            {
                EditorGUI.indentLevel++;

                if (panelType == MainPanelType.Main)
                {
                    EditorGUILayout.PropertyField(panel.FindPropertyRelative("backgroundImage"), new GUIContent("Background Image"));
                    EditorGUILayout.PropertyField(panel.FindPropertyRelative("logoImage"), new GUIContent("Logo Image"));
                }
                else if (panelType == MainPanelType.Places)
                {
                    EditorGUILayout.HelpBox("Toggle per-category visibility with Show In Places Nav.", MessageType.None);
                    EditorGUILayout.PropertyField(_worldCategories, new GUIContent("World Categories"), true);
                }
                else
                {
                    EditorGUILayout.PropertyField(panel.FindPropertyRelative("guideOrCustomText"), new GUIContent("Content"));
                }

                EditorGUILayout.PropertyField(panel.FindPropertyRelative("panelName"));
                EditorGUILayout.PropertyField(panel.FindPropertyRelative("panelType"));

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }
    }

    private void AddPanel(MainPanelType type)
    {
        int i = _mainSectionPanels.arraySize;
        _mainSectionPanels.arraySize++;
        var panel = _mainSectionPanels.GetArrayElementAtIndex(i);
        panel.FindPropertyRelative("panelName").stringValue = type == MainPanelType.Main ? "Main" : type == MainPanelType.Places ? "Places" : type == MainPanelType.Guide ? "Guide" : "Custom";
        panel.FindPropertyRelative("panelType").enumValueIndex = (int)type;
        System.Array.Resize(ref _foldPanels, Mathf.Max(_foldPanels?.Length ?? 0, i + 1));
    }

    private void DuplicatePanel(int index)
    {
        _mainSectionPanels.InsertArrayElementAtIndex(index + 1);
        var src = _mainSectionPanels.GetArrayElementAtIndex(index);
        var dst = _mainSectionPanels.GetArrayElementAtIndex(index + 1);
        CopyPanelProperties(src, dst);
        System.Array.Resize(ref _foldPanels, _mainSectionPanels.arraySize);
    }

    private void RemovePanel(int index)
    {
        _mainSectionPanels.DeleteArrayElementAtIndex(index);
        System.Array.Resize(ref _foldPanels, Mathf.Max(_mainSectionPanels.arraySize, 1));
    }

    private void CopyPanelProperties(SerializedProperty src, SerializedProperty dst)
    {
        dst.FindPropertyRelative("panelName").stringValue = src.FindPropertyRelative("panelName").stringValue + " (Copy)";
        dst.FindPropertyRelative("panelType").enumValueIndex = src.FindPropertyRelative("panelType").enumValueIndex;
        dst.FindPropertyRelative("backgroundImage").objectReferenceValue = src.FindPropertyRelative("backgroundImage").objectReferenceValue;
        dst.FindPropertyRelative("logoImage").objectReferenceValue = src.FindPropertyRelative("logoImage").objectReferenceValue;
        dst.FindPropertyRelative("guideOrCustomText").stringValue = src.FindPropertyRelative("guideOrCustomText").stringValue;
    }

    private void DrawRightSection()
    {
        EditorGUILayout.PropertyField(_rightSectionEnabled, new GUIContent("Enable"));
        EditorGUILayout.PropertyField(_mirror, new GUIContent("Mirror"));
        EditorGUILayout.PropertyField(_avatarDatas, new GUIContent("Avatar Datas"));


    }
}
