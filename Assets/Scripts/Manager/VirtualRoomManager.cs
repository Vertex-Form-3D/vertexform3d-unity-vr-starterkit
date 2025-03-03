using System.Collections;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using VertextFormCore;

namespace VertextFormCore
{
    public class VirtualRoomManager : MonoBehaviourPunCallbacks
    {
        public static VirtualRoomManager Instance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
        }

        public void LeaveRoomAndLoadHomeScene()
        {
            SpawnManager.Instance.ShowLoaclTempVRPlayer(true);
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
            }
            StartCoroutine(WaitToLeve());
        }

        IEnumerator WaitToLeve()
        {
            Debug.Log("0-> left room");

            while (PhotonNetwork.InRoom)
            {
                Debug.Log("0-> cannot leave room");

                yield return new WaitForSeconds(1f);
            }
            PhotonNetwork.Disconnect();
        }

        #region Photon Callback Methods
        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            Debug.Log(newPlayer.NickName + " joined to: " + PhotonNetwork.CurrentRoom.PlayerCount);
        }

        public override void OnLeftRoom()
        {
            Debug.Log("1-> left room");
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.Log("2-> disconnected");
            PhotonNetwork.LoadLevel("HomeScene");
        }
        #endregion!
    }
}