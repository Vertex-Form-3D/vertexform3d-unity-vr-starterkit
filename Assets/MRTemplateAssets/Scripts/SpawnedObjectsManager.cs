using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

[RequireComponent(typeof(ObjectSpawner))]
public class SpawnedObjectsManager : MonoBehaviour
{
    /// <summary>
    /// UI drop down representing object spawn selection for the scene.
    /// </summary>
    [SerializeField]
    TMP_Dropdown m_ObjectSelectorDropdown;

    /// <summary>
    /// UGUI Button clicked to destroy all spawned objects by this spawner.
    /// </summary>
    [SerializeField]
    Button m_DestroyObjectsButton;

    ObjectSpawner m_Spawner;

    void OnEnable()
    {
        m_Spawner = GetComponent<ObjectSpawner>();
        if (m_Spawner != null)
            m_Spawner.spawnAsChildren = true;

        if (m_ObjectSelectorDropdown != null)
        {
            OnObjectSelectorDropdownValueChanged(m_ObjectSelectorDropdown.value);
            m_ObjectSelectorDropdown.onValueChanged.AddListener(OnObjectSelectorDropdownValueChanged);
        }

        if (m_DestroyObjectsButton != null)
            m_DestroyObjectsButton.onClick.AddListener(OnDestroyObjectsButtonClicked);
    }

    void OnDisable()
    {
        if (m_ObjectSelectorDropdown != null)
            m_ObjectSelectorDropdown.onValueChanged.RemoveListener(OnObjectSelectorDropdownValueChanged);

        if (m_DestroyObjectsButton != null)
            m_DestroyObjectsButton.onClick.RemoveListener(OnDestroyObjectsButtonClicked);
    }

    void OnObjectSelectorDropdownValueChanged(int value)
    {
        if (m_Spawner == null)
            return;

        if (value == 0)
        {
            m_Spawner.RandomizeSpawnOption();
            return;
        }

        m_Spawner.spawnOptionIndex = value - 1;
    }

    void OnDestroyObjectsButtonClicked()
    {
        if (m_Spawner == null)
            return;

        foreach (Transform child in m_Spawner.transform)
        {
            Destroy(child.gameObject);
        }
    }
}
