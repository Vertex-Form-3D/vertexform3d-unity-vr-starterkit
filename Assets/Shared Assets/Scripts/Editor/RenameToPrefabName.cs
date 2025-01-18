using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RenameToPrefabName : MonoBehaviour
{
    [MenuItem("GameObject/To Prefab Name", false, 1)]
    public static void RenameSelected()
    {
        Object obj = Selection.activeObject;
        GameObject go = (GameObject) obj;

        Rename(go.transform);
    }

    private static void Rename(Transform transform)
    {
        GameObject prefabGO = PrefabUtility.GetCorrespondingObjectFromSource(transform.gameObject);

        //If prefab, rename, else search among children
        if (prefabGO != null)
        {
            Debug.Log("Renaming " + transform.gameObject.name + " to " + prefabGO.name);
            transform.gameObject.name = prefabGO.name;
        }
        else
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Rename(transform.GetChild(i));
            }
        }
    }
}
