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
    void Start()
    {
        if (GetComponent<Collider>() != null)
        {
            GetComponent<Collider>().isTrigger = true;
        }
        sceneNameText.text = sceneName;
    }
    public void SwitchScene()
    {
        if (!ScenePlatformSupport.CanEnterScene(sceneName))
            return;

        SceneLoader.Instance.isFlyModeEnabled = flyMode;
        SceneLoader.Instance.LoadScnene(sceneName);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check for Fusion NetworkObject
        if (other.GetComponent<NetworkObject>() != null)
        {
            var networkObject = other.GetComponent<NetworkObject>();
            if (other.GetComponent<PlayerNetworkSetup>() != null && networkObject.HasInputAuthority)
            {
                Invoke(nameof(SwitchScene), 1f);
            }
        }
        else
        {
            // Fallback for non-networked XR Origin (local play)
            if (other.GetComponent<XROrigin>() != null)
            {
                Invoke(nameof(SwitchScene), 1f);
            }
        }

    }
}
