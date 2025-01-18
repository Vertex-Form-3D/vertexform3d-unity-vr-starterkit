using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ButtonToggle : MonoBehaviour
{
    public List<GameObject> prefabsToToggle;
    public Button toggleButton;

    private void Start()
    {
        toggleButton.onClick.AddListener(ActivatePrefabs);
    }

    // Now this function only activates the prefabs
    private void ActivatePrefabs()
    {
        foreach (var prefab in prefabsToToggle)
        {
            prefab.SetActive(true);
        }
    }

    // New function to deactivate the prefabs
    public void DeactivatePrefabs()
    {
        foreach (var prefab in prefabsToToggle)
        {
            prefab.SetActive(false);
        }
    }

    // Function to add a prefab
    public void AddPrefab(GameObject prefab)
    {
        if (prefab != null)
        {
            prefabsToToggle.Add(prefab);
        }
    }

    // Function to remove a prefab
    public void RemovePrefab(GameObject prefab)
    {
        if (prefab != null && prefabsToToggle.Contains(prefab))
        {
            prefabsToToggle.Remove(prefab);
        }
    }
}