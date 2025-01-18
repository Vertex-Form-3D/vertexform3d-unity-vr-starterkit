using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.Linq;
using UnityEngine.SceneManagement;

public class MissingScriptRemover : EditorWindow
{
    [MenuItem("Toolsa/Remove GameObjects with Missing Scripts In Active Scene")]
    static void Init()
    {
        MissingScriptRemover window = (MissingScriptRemover)EditorWindow.GetWindow(typeof(MissingScriptRemover));
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Remove GameObjects with Missing Scripts In Active Scene", EditorStyles.boldLabel);

        if (GUILayout.Button("Remove GameObjects with Missing Scripts"))
        {
            RemoveGameObjectsWithMissingScripts();
        }
    }

    void RemoveGameObjectsWithMissingScripts()
    {
        Scene activeScene = EditorSceneManager.GetActiveScene();
        GameObject[] allObjects = activeScene.GetRootGameObjects();

        int removedCount = 0;

        foreach (GameObject go in allObjects)
        {
            RemoveMissingScriptsRecursive(go, ref removedCount);
        }

        Debug.Log($"Removed {removedCount} GameObjects with missing scripts in the active scene.");
    }

    void RemoveMissingScriptsRecursive(GameObject go, ref int removedCount)
    {
        Component[] components = go.GetComponents<Component>();

        // Check if any of the components is null (indicating a missing script)
        if (components.Any(component => component == null))
        {
            Undo.DestroyObjectImmediate(go);
            removedCount++;
            return;
        }

        // Check children
        for (int i = 0; i < go.transform.childCount; i++)
        {
            GameObject child = go.transform.GetChild(i).gameObject;
            RemoveMissingScriptsRecursive(child, ref removedCount);
        }
    }
}
