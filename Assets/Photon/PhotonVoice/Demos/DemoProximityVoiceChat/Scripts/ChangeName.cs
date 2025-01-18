using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class ChangeName : MonoBehaviour
{
    private void Start()
    {
        PhotonView photonView = this.GetComponent<PhotonView>();
        this.name = string.Format("ActorNumber {0}", photonView.OwnerActorNr);
    }
}