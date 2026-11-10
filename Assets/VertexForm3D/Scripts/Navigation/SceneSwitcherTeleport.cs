using Fusion;
using Unity.XR.CoreUtils;
using UnityEngine;
using VertexFormCore;
using TMPro;


[RequireComponent(typeof(Collider))]

public class SceneSwitcherTeleport : MonoBehaviour
{
    public string sceneName;
    public TMP_Text sceneNameText;
    [SerializeField] bool flyMode;
    bool switching;

    void Start()
    {
        var col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;

        if (sceneNameText != null)
            sceneNameText.text = sceneName;
    }

    public void SwitchScene()
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[SceneSwitcherTeleport] Scene name is empty. Set Scene Name before using this teleporter.");
            switching = false;
            return;
        }

        if (!ScenePlatformSupport.CanEnterScene(sceneName))
        {
            switching = false;
            return;
        }

        if (SceneLoader.Instance == null)
        {
            Debug.LogWarning("[SceneSwitcherTeleport] SceneLoader is missing. Cannot switch scenes.");
            switching = false;
            return;
        }

        SceneLoader.Instance.isFlyModeEnabled = flyMode;
        SceneLoader.Instance.LoadScnene(sceneName);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (switching || string.IsNullOrWhiteSpace(sceneName))
            return;

        var networkObject = other.GetComponentInParent<NetworkObject>();
        if (networkObject != null)
        {
            if (other.GetComponentInParent<PlayerNetworkSetup>() != null && networkObject.HasInputAuthority)
                QueueSwitch();
            return;
        }

        // Fallback for non-networked XR Origin (local play / WebGL)
        if (other.GetComponentInParent<XROrigin>() != null)
            QueueSwitch();
    }

    void QueueSwitch()
    {
        switching = true;
        Invoke(nameof(SwitchScene), 1f);
    }
}
