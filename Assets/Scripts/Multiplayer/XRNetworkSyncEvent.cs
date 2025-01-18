using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

public class XRNetworkSyncEvent : MonoBehaviour
{
    PhotonView photonView;
    public UnityEvent networkEvent;
    void Start()
    {
        photonView=GetComponent<PhotonView>();
    }

    public void SyncEventOverTheNetwork()
    {
        photonView.RPC(nameof(NetworkEvent), RpcTarget.AllBuffered);
    }

    [PunRPC]
    public void NetworkEvent()
    {
        networkEvent.Invoke();
    }
}
