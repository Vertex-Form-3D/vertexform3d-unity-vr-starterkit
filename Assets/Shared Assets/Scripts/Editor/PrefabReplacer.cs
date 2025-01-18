using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// This class was used to replace speed tree assets with prefabs.
/// It probably has no more use and should be removed before releasing
/// TODO: Remove
/// </summary>
[ExecuteInEditMode]
public class PrefabReplacer : MonoBehaviour
{
    public Transform root;

    public bool button;

    public List<GameObject> prefabs;
    // Start is called before the first frame update
    void Start()
    {
        button = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (button)
        {
            button = false;
            Replace();
        }
    }

    private void Replace()
    {
        Transform[] existingTrees = new Transform[root.childCount];
            
            
        for (int i = 0; i < root.childCount; i++)
        {
            existingTrees[i] = root.GetChild(i);
        }

        for (int i = 0; i < existingTrees.Length; i++)
        {
            Transform tree = existingTrees[i];

            GameObject prefab = GetPrefab(tree.gameObject);
            if (prefab != null)
            {
                Transform newTree = ((GameObject)PrefabUtility.InstantiatePrefab(prefab)).transform;
                newTree.parent = root;
                newTree.localPosition = tree.localPosition;
                newTree.localRotation = tree.localRotation;
                newTree.localScale = tree.localScale;
            
                DestroyImmediate(tree.gameObject);
            }
            
        }
    }

    private GameObject GetPrefab(GameObject tree)
    {
        string name = tree.name.Split("(")[0].Trim();
        GameObject replacingPrefab = prefabs.Find(go => string.Equals(name, go.name));

        if (replacingPrefab != null)
        {
            Debug.Log("Found prefab for " + name);
        }
        return replacingPrefab;
    }
}
