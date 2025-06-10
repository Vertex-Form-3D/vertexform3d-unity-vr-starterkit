using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(PhotonView))]
public class XRNetworkSyncEvent : MonoBehaviour
{
    PhotonView photonView;
    public RpcTarget rpcTarget = RpcTarget.AllBuffered;
    public UnityEvent networkEvent;
    void Start()
    {
        photonView=GetComponent<PhotonView>();
    }

    public void SyncEventOverTheNetwork()
    {
        photonView.RPC(nameof(NetworkEvent), rpcTarget);
    }

    [PunRPC]
    public void NetworkEvent()
    {
        networkEvent.Invoke();
    }
}
