using Photon.Pun;
using Unity.XR.CoreUtils;
using UnityEngine;
using VertexFormCore;


[RequireComponent(typeof(Collider))]

public class SceneSwitcherTeleport : MonoBehaviour
{
    public string sceneName;
    [SerializeField] bool flyMode;
    void Start()
    {
        if (GetComponent<Collider>() != null)
        {
            GetComponent<Collider>().isTrigger = true;
        }
    }
    public void SwitchScene()
    {
        SceneLoader.Instance.isFlyModeEnabled = flyMode;
        SceneLoader.Instance.LoadScnene(sceneName);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PhotonView>() != null)
        {
            if (other.GetComponent<PlayerNetworkSetup>() != null&& other.GetComponent<PhotonView>().IsMine)
            {
                Invoke(nameof(SwitchScene), 1f);
            }
        }
        else
        {
            if (other.GetComponent<XROrigin>()!=null)
            {
                Invoke(nameof(SwitchScene), 1f);
            }
        }
        
    }
}
