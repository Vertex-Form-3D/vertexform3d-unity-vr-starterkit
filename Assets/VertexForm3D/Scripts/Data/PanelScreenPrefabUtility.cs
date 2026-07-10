using UnityEngine;

/// <summary>
/// Locates panel screen prefabs by their root <see cref="MainScreen"/> / <see cref="WorldScreen"/> components.
/// </summary>
public static class PanelScreenPrefabUtility
{
    static GameObject _mainScreenPrefab;
    static GameObject _worldScreenPrefab;

    public static GameObject GetMainScreenPrefab()
    {
        if (_mainScreenPrefab == null)
            _mainScreenPrefab = FindPrefabWithComponentOnRoot<MainScreen>();
        return _mainScreenPrefab;
    }

    public static GameObject GetWorldScreenPrefab()
    {
        if (_worldScreenPrefab == null)
            _worldScreenPrefab = FindPrefabWithComponentOnRoot<WorldScreen>();
        return _worldScreenPrefab;
    }

    public static void ClearCache()
    {
        _mainScreenPrefab = null;
        _worldScreenPrefab = null;
    }

    public static GameObject FindPrefabWithComponentOnRoot<T>() where T : Component
    {
#if UNITY_EDITOR
        return FindPrefabWithComponentOnRootEditor<T>();
#else
        return null;
#endif
    }

#if UNITY_EDITOR
    static GameObject FindPrefabWithComponentOnRootEditor<T>() where T : Component
    {
        GameObject childMatch = null;
        foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:Prefab"))
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                continue;

            if (prefab.GetComponent<T>() != null)
                return prefab;

            if (childMatch == null && prefab.GetComponentInChildren<T>(true) != null)
                childMatch = prefab;
        }

        return childMatch;
    }
#endif
}
