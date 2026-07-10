using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.SceneManagement;


[CustomEditor(typeof(UILayoutConfig))]
public class UILayoutConfigEditor : Editor
{
    private SerializedProperty _leftSectionEnabled;
    private SerializedProperty _leftSectionText;
    private SerializedProperty _mainSectionPanelEntries;
    private SerializedProperty _worldCategories;
    private SerializedProperty _rightSectionEnabled;
    private SerializedProperty _mirror;
    private SerializedProperty _showAvatarBodyInFirstPerson;
    private SerializedProperty _avatarDatas;

    private SerializedProperty _legacyShowMainPanel;
    private SerializedProperty _legacyPlacesPanelEnabled;
    private SerializedProperty _legacyGuidePanelEnabled;
    private SerializedProperty _legacyMainTabLabel;
    private SerializedProperty _legacyMainPanelSortOrder;
    private SerializedProperty _legacyPlacesTabLabel;
    private SerializedProperty _legacyPlacesPanelSortOrder;
    private SerializedProperty _legacyGuideTabLabel;
    private SerializedProperty _legacyGuidePanelSortOrder;
    private SerializedProperty _legacyMainSectionPanels;
    private SerializedProperty _legacyCustomMainPanels;

    private bool _foldPlatform = true;
    private bool _foldSettingsUI = true;
    private bool _foldLeft = true;
    private bool _foldMain = true;
    private bool _foldRight = true;
    private bool[] _foldPanelEntries;

    private ReorderableList _panelList;

    private const float PanelListDragHandleWidth = 10f;
    private const float PanelListDragHandleGap = 2f;

    private void OnEnable()
    {
        _leftSectionEnabled = serializedObject.FindProperty("leftSectionEnabled");
        _leftSectionText = serializedObject.FindProperty("leftSectionText");
        _mainSectionPanelEntries = serializedObject.FindProperty("mainSectionPanelEntries");
        _worldCategories = serializedObject.FindProperty("worldCategories");
        _rightSectionEnabled = serializedObject.FindProperty("rightSectionEnabled");
        _mirror = serializedObject.FindProperty("mirror");
        _showAvatarBodyInFirstPerson = serializedObject.FindProperty("showAvatarBodyInFirstPerson");
        _avatarDatas = serializedObject.FindProperty("avatarDatas");

        _legacyShowMainPanel = serializedObject.FindProperty("showMainPanel");
        _legacyPlacesPanelEnabled = serializedObject.FindProperty("placesPanelEnabled");
        _legacyGuidePanelEnabled = serializedObject.FindProperty("guidePanelEnabled");
        _legacyMainTabLabel = serializedObject.FindProperty("mainTabLabel");
        _legacyMainPanelSortOrder = serializedObject.FindProperty("mainPanelSortOrder");
        _legacyPlacesTabLabel = serializedObject.FindProperty("placesTabLabel");
        _legacyPlacesPanelSortOrder = serializedObject.FindProperty("placesPanelSortOrder");
        _legacyGuideTabLabel = serializedObject.FindProperty("guideTabLabel");
        _legacyGuidePanelSortOrder = serializedObject.FindProperty("guidePanelSortOrder");
        _legacyMainSectionPanels = serializedObject.FindProperty("mainSectionPanels");
        _legacyCustomMainPanels = serializedObject.FindProperty("customMainPanels");

        TryMigrateLegacyPanels();
        TryMigrateWorldCategoriesToPlacesPanels();
        RefreshScreenPrefabCache();
        SanitizeAllPanelEntries();
        EnsureFoldoutArray();
        BuildPanelList();
    }

    void RefreshScreenPrefabCache()
    {
        var config = target as UILayoutConfig;
        if (config == null)
            return;

        config.RefreshScreenPrefabCache();
        EditorUtility.SetDirty(config);
        serializedObject.Update();
    }

    void BuildPanelList()
    {
        _panelList = new ReorderableList(serializedObject, _mainSectionPanelEntries, true, true, true, true)
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Panels (drag to reorder)"),
            drawElementCallback = DrawPanelListElement,
            elementHeightCallback = GetPanelListElementHeight,
            onAddDropdownCallback = ShowAddPanelMenu
        };
    }

    void TryMigrateLegacyPanels()
    {
        serializedObject.Update();
        if (_mainSectionPanelEntries.arraySize > 0)
            return;

        bool hasLegacyData = _legacyShowMainPanel != null && (
            _legacyShowMainPanel.boolValue ||
            _legacyPlacesPanelEnabled.boolValue ||
            _legacyGuidePanelEnabled.boolValue ||
            (_legacyCustomMainPanels != null && _legacyCustomMainPanels.arraySize > 0));

        if (!hasLegacyData)
        {
            (target as UILayoutConfig)?.EnsureDefaultPanelEntries();
            serializedObject.Update();
            if (_mainSectionPanelEntries.arraySize > 0)
                return;
        }

        var migrationItems = new List<(int sortOrder, Action addEntry)>();

        migrationItems.Add((_legacyMainPanelSortOrder.intValue, () =>
        {
            AddPanelEntry(MainSectionPanelType.Main, _legacyMainTabLabel.stringValue, _legacyShowMainPanel.boolValue, brandingIndex: 0);
        }
        ));
        migrationItems.Add((_legacyPlacesPanelSortOrder.intValue, () =>
        {
            AddPanelEntry(MainSectionPanelType.Places, _legacyPlacesTabLabel.stringValue, _legacyPlacesPanelEnabled.boolValue);
        }
        ));
        migrationItems.Add((_legacyGuidePanelSortOrder.intValue, () =>
        {
            AddPanelEntry(MainSectionPanelType.Guide, _legacyGuideTabLabel.stringValue, _legacyGuidePanelEnabled.boolValue);
        }
        ));

        if (_legacyCustomMainPanels != null)
        {
            for (int i = 0; i < _legacyCustomMainPanels.arraySize; i++)
            {
                var legacy = _legacyCustomMainPanels.GetArrayElementAtIndex(i);
                int sortOrder = legacy.FindPropertyRelative("sortOrder").intValue;
                int capturedIndex = i;
                migrationItems.Add((sortOrder, () => AddLegacyCustomPanelEntry(_legacyCustomMainPanels.GetArrayElementAtIndex(capturedIndex))));
            }
        }

        migrationItems.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));
        foreach (var item in migrationItems)
            item.addEntry();

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    void AddPanelEntry(MainSectionPanelType type, string tabLabel, bool enabled, int brandingIndex = -1, GameObject uiPrefab = null)
    {
        int index = _mainSectionPanelEntries.arraySize;
        _mainSectionPanelEntries.InsertArrayElementAtIndex(index);
        var element = _mainSectionPanelEntries.GetArrayElementAtIndex(index);
        element.FindPropertyRelative("panelType").enumValueIndex = (int)type;
        element.FindPropertyRelative("enabled").boolValue = enabled;
        element.FindPropertyRelative("tabLabel").stringValue = tabLabel ?? GetDefaultTabLabel(type);
        SanitizePanelEntry(element, type, uiPrefab);

        if (type == MainSectionPanelType.Main && brandingIndex >= 0 &&
            _legacyMainSectionPanels != null && brandingIndex < _legacyMainSectionPanels.arraySize)
        {
            var branding = _legacyMainSectionPanels.GetArrayElementAtIndex(brandingIndex);
            element.FindPropertyRelative("backgroundImage").objectReferenceValue =
                branding.FindPropertyRelative("backgroundImage").objectReferenceValue;
            element.FindPropertyRelative("logoImage").objectReferenceValue =
                branding.FindPropertyRelative("logoImage").objectReferenceValue;
        }
        else if (type == MainSectionPanelType.Main)
        {
            element.FindPropertyRelative("backgroundImage").objectReferenceValue = null;
            element.FindPropertyRelative("logoImage").objectReferenceValue = null;
        }
    }

    void SanitizeAllPanelEntries()
    {
        if (_mainSectionPanelEntries == null)
            return;

        bool changed = false;
        for (int i = 0; i < _mainSectionPanelEntries.arraySize; i++)
        {
            var element = _mainSectionPanelEntries.GetArrayElementAtIndex(i);
            var type = (MainSectionPanelType)element.FindPropertyRelative("panelType").enumValueIndex;
            if (SanitizePanelEntry(element, type))
                changed = true;
        }

        if (changed)
        {
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }

    bool SanitizePanelEntry(SerializedProperty element, MainSectionPanelType type, GameObject customUiPrefab = null)
    {
        bool changed = false;

        var uiPrefabProp = element.FindPropertyRelative("uiPrefab");
        var backgroundProp = element.FindPropertyRelative("backgroundImage");
        var logoProp = element.FindPropertyRelative("logoImage");
        var categoriesProp = element.FindPropertyRelative("worldCategories");

        switch (type)
        {
            case MainSectionPanelType.Main:
                if (uiPrefabProp.objectReferenceValue != null)
                {
                    uiPrefabProp.objectReferenceValue = null;
                    changed = true;
                }
                if (categoriesProp.arraySize > 0)
                {
                    categoriesProp.ClearArray();
                    changed = true;
                }
                break;

            case MainSectionPanelType.Places:
                if (uiPrefabProp.objectReferenceValue != null)
                {
                    uiPrefabProp.objectReferenceValue = null;
                    changed = true;
                }
                if (backgroundProp.objectReferenceValue != null)
                {
                    backgroundProp.objectReferenceValue = null;
                    changed = true;
                }
                if (logoProp.objectReferenceValue != null)
                {
                    logoProp.objectReferenceValue = null;
                    changed = true;
                }
                if (categoriesProp.arraySize == 0)
                {
                    SeedDefaultWorldCategories(categoriesProp);
                    changed = true;
                }
                break;

            case MainSectionPanelType.Guide:
                if (uiPrefabProp.objectReferenceValue != null)
                {
                    uiPrefabProp.objectReferenceValue = null;
                    changed = true;
                }
                if (backgroundProp.objectReferenceValue != null)
                {
                    backgroundProp.objectReferenceValue = null;
                    changed = true;
                }
                if (logoProp.objectReferenceValue != null)
                {
                    logoProp.objectReferenceValue = null;
                    changed = true;
                }
                if (categoriesProp.arraySize > 0)
                {
                    categoriesProp.ClearArray();
                    changed = true;
                }
                break;

            case MainSectionPanelType.Custom:
                if (customUiPrefab != null && uiPrefabProp.objectReferenceValue != customUiPrefab)
                {
                    uiPrefabProp.objectReferenceValue = customUiPrefab;
                    changed = true;
                }
                if (backgroundProp.objectReferenceValue != null)
                {
                    backgroundProp.objectReferenceValue = null;
                    changed = true;
                }
                if (logoProp.objectReferenceValue != null)
                {
                    logoProp.objectReferenceValue = null;
                    changed = true;
                }
                if (categoriesProp.arraySize > 0)
                {
                    categoriesProp.ClearArray();
                    changed = true;
                }
                break;
        }

        return changed;
    }

    void TryMigrateWorldCategoriesToPlacesPanels()
    {
        var config = target as UILayoutConfig;
        if (config == null || config.worldCategories == null || config.worldCategories.Count == 0)
            return;

        if (config.mainSectionPanelEntries == null)
            return;

        foreach (var entry in config.mainSectionPanelEntries)
        {
            if (entry == null || entry.panelType != MainSectionPanelType.Places)
                continue;
            if (entry.worldCategories != null && entry.worldCategories.Count > 0)
                continue;

            entry.worldCategories = new List<Category>(config.worldCategories);
            EditorUtility.SetDirty(config);
            serializedObject.Update();
            break;
        }
    }

    static void SeedDefaultWorldCategories(SerializedProperty worldCategoriesProp)
    {
        if (worldCategoriesProp == null || worldCategoriesProp.arraySize > 0)
            return;

        worldCategoriesProp.arraySize = 3;
        worldCategoriesProp.GetArrayElementAtIndex(0).FindPropertyRelative("categoryName").stringValue = "Hubs";
        worldCategoriesProp.GetArrayElementAtIndex(0).FindPropertyRelative("showInPlacesNav").boolValue = true;
        worldCategoriesProp.GetArrayElementAtIndex(1).FindPropertyRelative("categoryName").stringValue = "Geospatial";
        worldCategoriesProp.GetArrayElementAtIndex(1).FindPropertyRelative("showInPlacesNav").boolValue = false;
        worldCategoriesProp.GetArrayElementAtIndex(2).FindPropertyRelative("categoryName").stringValue = "Other";
        worldCategoriesProp.GetArrayElementAtIndex(2).FindPropertyRelative("showInPlacesNav").boolValue = false;
    }

    void AddLegacyCustomPanelEntry(SerializedProperty legacy)
    {
        AddPanelEntry(
            MainSectionPanelType.Custom,
            legacy.FindPropertyRelative("tabLabel").stringValue,
            legacy.FindPropertyRelative("enabled").boolValue,
            uiPrefab: legacy.FindPropertyRelative("uiPrefab").objectReferenceValue as GameObject);
    }

    void EnsureFoldoutArray()
    {
        int count = _mainSectionPanelEntries != null ? _mainSectionPanelEntries.arraySize : 0;
        if (_foldPanelEntries == null || _foldPanelEntries.Length != count)
        {
            var previous = _foldPanelEntries;
            _foldPanelEntries = new bool[count];
            if (previous != null)
            {
                for (int i = 0; i < Mathf.Min(previous.Length, count); i++)
                    _foldPanelEntries[i] = previous[i];
            }

            if (count > 0)
                _foldPanelEntries[0] = true;
        }
    }

    public override void OnInspectorGUI()
    {
        VertexFormEditorHeader.Draw();

        serializedObject.Update();
        EnsureFoldoutArray();
        if (_panelList != null)
            _panelList.serializedProperty = _mainSectionPanelEntries;

        DrawHighlightPrefabButtons();
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
        EditorGUILayout.HelpBox("Saves pending edits to this asset, then bakes into prefabs:\n• MainMap / Visual Element prefabs (root MainMap): left/right sections, logo, background\n• Any prefab with MenuManager (MainMap, MainMap Desktop): panel tabs + custom panels/buttons", MessageType.None);
        if (GUILayout.Button("Apply to Prefabs", GUILayout.Height(28)))
            ApplyConfigToPrefabs();
        EditorGUILayout.EndVertical();
        GUILayout.Space(6);
    }

    /// <summary>Prefab assets where the root has a MainMap component (visual/branding prefabs).</summary>
    private static List<string> FindMainMapPrefabPaths()
    {
        var paths = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabAsset == null)
                continue;
            if (prefabAsset.GetComponent<MainMap>() != null)
                paths.Add(path);
        }
        return paths;
    }

    private void ApplyConfigToPrefabs()
    {
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);

        var config = target as UILayoutConfig;
        if (config == null) return;

        config.RefreshScreenPrefabCache();
        EditorUtility.SetDirty(config);

        List<string> mainMapPaths = FindMainMapPrefabPaths();
        if (mainMapPaths.Count == 0)
        {
            Debug.LogWarning("UILayoutConfig: No prefabs found where the prefab root has MainMap. Assign uiLayoutConfig manually if needed.");
        }

        int prefabsModified = 0;
        int refsUpdated = 0;

        var currentPrefabStage = PrefabStageUtility.GetCurrentPrefabStage();

        foreach (string path in mainMapPaths)
        {
            GameObject contents = null;
            bool usingOpenPrefabStage = currentPrefabStage != null &&
                                        string.Equals(currentPrefabStage.assetPath, path, System.StringComparison.OrdinalIgnoreCase);

            if (usingOpenPrefabStage)
            {
                contents = currentPrefabStage.prefabContentsRoot;
            }
            else
            {
                try
                {
                    contents = PrefabUtility.LoadPrefabContents(path);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"UILayoutConfig: Could not open '{path}': {e.Message}");
                    continue;
                }
            }

            if (contents == null) continue;

            MainMap rootMainMap = contents.GetComponent<MainMap>();
            if (rootMainMap == null)
            {
                if (!usingOpenPrefabStage)
                    PrefabUtility.UnloadPrefabContents(contents);
                Debug.LogWarning($"UILayoutConfig: '{path}' has no MainMap on the prefab root. Skipping apply.");
                continue;
            }

            MainMap mm = rootMainMap;
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

            if (usingOpenPrefabStage)
            {
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                prefabsModified++;
            }
            else
            {
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                prefabsModified++;
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        int menuPrefabsModified = 0;
        int customPanelsBaked = 0;
        menuPrefabsModified = ApplyConfigToMenuManagerPrefabs(config, ref customPanelsBaked);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"UILayoutConfig: Baked branding into {prefabsModified} MainMap prefab(s). Updated {menuPrefabsModified} MenuManager prefab(s) with {customPanelsBaked} custom panel(s). Reference updates: {refsUpdated}.");
    }

    static int ApplyConfigToMenuManagerPrefabs(UILayoutConfig config, ref int customPanelsBaked)
    {
        int prefabsModified = 0;
        var currentPrefabStage = PrefabStageUtility.GetCurrentPrefabStage();

        foreach (string path in MenuManagerPanelPrefabBaker.TargetMenuPrefabPaths)
        {
            if (!File.Exists(path))
                continue;

            GameObject contents = null;
            bool usingOpenPrefabStage = currentPrefabStage != null &&
                                        string.Equals(currentPrefabStage.assetPath, path, StringComparison.OrdinalIgnoreCase);

            if (usingOpenPrefabStage)
                contents = currentPrefabStage.prefabContentsRoot;
            else
            {
                try
                {
                    contents = PrefabUtility.LoadPrefabContents(path);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"UILayoutConfig: Could not open '{path}': {e.Message}");
                    continue;
                }
            }

            if (contents == null) continue;

            var menuManager = contents.GetComponentInChildren<MenuManager>(true);
            if (menuManager == null)
            {
                if (!usingOpenPrefabStage)
                    PrefabUtility.UnloadPrefabContents(contents);
                Debug.LogWarning($"UILayoutConfig: '{path}' has no MenuManager. Skipping panel bake.");
                continue;
            }

            customPanelsBaked += MenuManagerPanelPrefabBaker.Bake(menuManager, config, path);

            if (usingOpenPrefabStage)
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            else
            {
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                PrefabUtility.UnloadPrefabContents(contents);
            }

            prefabsModified++;
        }

        if (prefabsModified == 0)
            Debug.LogWarning("UILayoutConfig: MainMap / MainMap Desktop prefabs were not found or could not be updated.");

        return prefabsModified;
    }

    private static void DrawHighlightPrefabButtons()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        DrawHighlightPrefabButton(requireDesktopInName: true, "Highlight Desktop Prefab");
        GUILayout.Space(6);
        DrawHighlightPrefabButton(requireDesktopInName: false, "Highlight VR Prefab");
        EditorGUILayout.EndHorizontal();
    }

    private static void DrawHighlightPrefabButton(bool requireDesktopInName, string buttonLabel)
    {
        if (GUILayout.Button(buttonLabel, EditorStyles.miniButton))
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefabAsset == null)
                    continue;

                if (prefabAsset.GetComponentInChildren<MenuManager>(true) == null)
                    continue;

                string fileName = Path.GetFileNameWithoutExtension(path);
                bool hasDesktopInName = fileName.IndexOf("Desktop", System.StringComparison.OrdinalIgnoreCase) >= 0;
                if (hasDesktopInName != requireDesktopInName)
                    continue;

                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
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
        var config = target as UILayoutConfig;
        EditorGUILayout.HelpBox(
            "Drag panels to set bottom-nav tab order (after Home). The first Main/Places panel uses the built-in screens in MainMap. " +
            "Additional Places panels use the WorldScreen prefab; each panel has its own categories and world list via WorldScreen.",
            MessageType.None);

        if (config != null)
        {
            var mainPrefab = config.GetMainScreenPrefab();
            var worldPrefab = config.GetWorldScreenPrefab();
            EditorGUILayout.LabelField("Detected Main Screen Prefab",
                mainPrefab != null ? mainPrefab.name : "(not found)", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("Detected World Screen Prefab",
                worldPrefab != null ? worldPrefab.name : "(not found)", EditorStyles.miniLabel);
        }

        EditorGUILayout.Space(4);
        _panelList.DoLayoutList();
        DrawPanelValidationWarnings();
    }

    void ShowAddPanelMenu(Rect buttonRect, ReorderableList list)
    {
        var menu = new GenericMenu();
        menu.AddItem(new GUIContent("Main Panel"), false, () => AddNewPanel(MainSectionPanelType.Main));
        menu.AddItem(new GUIContent("Places Panel"), false, () => AddNewPanel(MainSectionPanelType.Places));
        menu.AddItem(new GUIContent("Guide Panel"), false, () => AddNewPanel(MainSectionPanelType.Guide));
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Custom Panel"), false, () => AddNewPanel(MainSectionPanelType.Custom));
        menu.ShowAsContext();
    }

    void AddNewPanel(MainSectionPanelType type)
    {
        serializedObject.Update();
        AddPanelEntry(type, GetDefaultTabLabel(type), enabled: true);
        serializedObject.ApplyModifiedProperties();
        EnsureFoldoutArray();
        if (_foldPanelEntries != null && _foldPanelEntries.Length > 0)
            _foldPanelEntries[_foldPanelEntries.Length - 1] = true;
    }

    static string GetDefaultTabLabel(MainSectionPanelType type)
    {
        return type switch
        {
            MainSectionPanelType.Main => "Main",
            MainSectionPanelType.Places => "Worlds",
            MainSectionPanelType.Guide => "Guide",
            MainSectionPanelType.Custom => "Custom",
            _ => "Panel"
        };
    }

    static string GetPanelTypeTitle(MainSectionPanelType type)
    {
        return type switch
        {
            MainSectionPanelType.Main => "Main Panel",
            MainSectionPanelType.Places => "Places Panel",
            MainSectionPanelType.Guide => "Guide Panel",
            MainSectionPanelType.Custom => "Custom Panel",
            _ => "Panel"
        };
    }

    static string GetPanelDisplayName(SerializedProperty element)
    {
        if (element == null)
            return "Panel";

        string tabLabel = element.FindPropertyRelative("tabLabel").stringValue;
        if (!string.IsNullOrWhiteSpace(tabLabel))
            return tabLabel.Trim();

        var type = (MainSectionPanelType)element.FindPropertyRelative("panelType").enumValueIndex;
        return GetDefaultTabLabel(type);
    }

    float GetPanelListElementHeight(int index)
    {
        if (_foldPanelEntries == null || index >= _foldPanelEntries.Length || !_foldPanelEntries[index])
            return EditorGUIUtility.singleLineHeight + 6f;

        var element = _mainSectionPanelEntries.GetArrayElementAtIndex(index);
        var type = (MainSectionPanelType)element.FindPropertyRelative("panelType").enumValueIndex;
        float height = EditorGUIUtility.singleLineHeight + 6f;
        height += (EditorGUIUtility.singleLineHeight + 2f) * 2f;
        if (type == MainSectionPanelType.Main)
            height += (EditorGUIUtility.singleLineHeight + 2f) * 2f;
        else if (type == MainSectionPanelType.Places)
            height += EditorGUIUtility.singleLineHeight + 2f + EditorGUI.GetPropertyHeight(element.FindPropertyRelative("worldCategories"), true);
        else if (type == MainSectionPanelType.Guide)
            height += EditorGUIUtility.singleLineHeight * 2f + 2f;
        else if (type == MainSectionPanelType.Custom)
            height += EditorGUIUtility.singleLineHeight + 2f;

        return height + 8f;
    }

    void DrawPanelListElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        rect.x += PanelListDragHandleWidth + PanelListDragHandleGap;
        rect.width -= PanelListDragHandleWidth + PanelListDragHandleGap;

        var element = _mainSectionPanelEntries.GetArrayElementAtIndex(index);
        var panelType = (MainSectionPanelType)element.FindPropertyRelative("panelType").enumValueIndex;
        string header = $"{index + 1}. {GetPanelDisplayName(element)}";

        rect.y += 2f;
        rect.height = EditorGUIUtility.singleLineHeight;
        _foldPanelEntries[index] = EditorGUI.Foldout(rect, _foldPanelEntries[index], header, true);
        if (!_foldPanelEntries[index])
            return;

        EditorGUI.indentLevel++;
        float y = rect.y + EditorGUIUtility.singleLineHeight + 2f;
        float width = rect.width - 4f;
        float lineHeight = EditorGUIUtility.singleLineHeight;

        DrawProperty(rect.x, ref y, width, lineHeight, element.FindPropertyRelative("enabled"), "Enabled");
        DrawProperty(rect.x, ref y, width, lineHeight, element.FindPropertyRelative("tabLabel"), "Tab Label");

        switch (panelType)
        {
            case MainSectionPanelType.Main:
                DrawProperty(rect.x, ref y, width, lineHeight, element.FindPropertyRelative("backgroundImage"), "Background Image");
                DrawProperty(rect.x, ref y, width, lineHeight, element.FindPropertyRelative("logoImage"), "Logo Image");
                break;
            case MainSectionPanelType.Places:
                {
                    EditorGUI.LabelField(new Rect(rect.x, y, width, lineHeight), "World Categories", EditorStyles.miniLabel);
                    y += lineHeight + 2f;
                    var categories = element.FindPropertyRelative("worldCategories");
                    float categoriesHeight = EditorGUI.GetPropertyHeight(categories, true);
                    EditorGUI.PropertyField(new Rect(rect.x, y, width, categoriesHeight), categories, GUIContent.none, true);
                    y += categoriesHeight;
                    break;
                }
            case MainSectionPanelType.Guide:
                EditorGUI.HelpBox(new Rect(rect.x, y, width, lineHeight * 2f),
                    "First Guide panel uses the built-in Guide screen in MainMap.", MessageType.None);
                break;
            case MainSectionPanelType.Custom:
                DrawProperty(rect.x, ref y, width, lineHeight, element.FindPropertyRelative("uiPrefab"), "UI Prefab");
                break;
        }

        EditorGUI.indentLevel--;
    }

    static void DrawProperty(float x, ref float y, float width, float lineHeight, SerializedProperty property, string label)
    {
        EditorGUI.PropertyField(new Rect(x, y, width, lineHeight), property, new GUIContent(label));
        y += lineHeight + 2f;
    }

    void DrawPanelValidationWarnings()
    {
        if (_mainSectionPanelEntries == null)
            return;

        var primaryCounts = new Dictionary<MainSectionPanelType, int>();
        for (int i = 0; i < _mainSectionPanelEntries.arraySize; i++)
        {
            var element = _mainSectionPanelEntries.GetArrayElementAtIndex(i);
            var type = (MainSectionPanelType)element.FindPropertyRelative("panelType").enumValueIndex;
            if (type == MainSectionPanelType.Custom)
                continue;

            if (!primaryCounts.ContainsKey(type))
                primaryCounts[type] = i;
        }

        for (int i = 0; i < _mainSectionPanelEntries.arraySize; i++)
        {
            var element = _mainSectionPanelEntries.GetArrayElementAtIndex(i);
            var type = (MainSectionPanelType)element.FindPropertyRelative("panelType").enumValueIndex;
            if (type == MainSectionPanelType.Custom)
                ValidateCustomPanelPrefab(element, i);
            else if (primaryCounts.TryGetValue(type, out int primaryIndex) && i != primaryIndex)
                ValidateExtraTypedPanel(element, type, i);
        }
    }

    void ValidateExtraTypedPanel(SerializedProperty element, MainSectionPanelType type, int index)
    {
        var config = target as UILayoutConfig;
        if (config == null)
            return;

        GameObject prefab = type switch
        {
            MainSectionPanelType.Main => config.GetMainScreenPrefab(),
            MainSectionPanelType.Places => config.GetWorldScreenPrefab(),
            _ => null
        };

        var overridePrefab = element.FindPropertyRelative("uiPrefab").objectReferenceValue as GameObject;
        if (type == MainSectionPanelType.Custom && overridePrefab != null)
            prefab = overridePrefab;

        if (prefab == null)
        {
            string componentName = type == MainSectionPanelType.Main ? nameof(MainScreen) : nameof(WorldScreen);
            EditorGUILayout.HelpBox(
                $"Panel [{index + 1}] ({GetPanelDisplayName(element)}): no prefab with a {componentName} component was found in the project.",
                MessageType.Warning);
            return;
        }

        if (MenuManagerPanelPrefabBaker.IsMenuShellPrefab(prefab))
        {
            EditorGUILayout.HelpBox(
                $"Panel [{index + 1}] ({GetPanelDisplayName(element)}): screen prefab must not be the MainMap shell prefab.",
                MessageType.Error);
        }
    }

    void ValidateCustomPanelPrefab(SerializedProperty element, int index)
    {
        var prefab = element.FindPropertyRelative("uiPrefab").objectReferenceValue as GameObject;
        if (prefab == null) return;

        if (MenuManagerPanelPrefabBaker.IsMenuShellPrefab(prefab))
        {
            EditorGUILayout.HelpBox(
                $"Panel [{index + 1}] ({GetPanelDisplayName(element)}): UI Prefab must be a separate panel prefab, not MainMap.",
                MessageType.Error);
        }
    }

    private void DrawRightSection()
    {
        EditorGUILayout.PropertyField(_rightSectionEnabled, new GUIContent("Enable"));
        EditorGUILayout.PropertyField(_mirror, new GUIContent("Mirror"));
        EditorGUILayout.PropertyField(_showAvatarBodyInFirstPerson,
            new GUIContent("Show Avatar Body In First Person",
                "Desktop/Mobile only. When enabled, the local player's avatar body remains visible in first-person view. Disable to hide the body and avoid camera clipping."));
        EditorGUILayout.PropertyField(_avatarDatas, new GUIContent("Avatar Datas"));
    }
}
