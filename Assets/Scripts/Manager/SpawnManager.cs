using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;

namespace VertextFormCore
{
    public class SpawnManager : MonoBehaviourPunCallbacks
    {
        [SerializeField] GameObject genericVRPlayerPrefab;
        [SerializeField] GameObject connectVRPrefab;
        public List<PlayerNetworkSetup> allPlayers= new List<PlayerNetworkSetup>();
        public GameObject localVRPlayer;

        GameObject connectVRObject;
        public Vector3 spawnPosition;

        public static SpawnManager Instance;

        public GameObject ConnectVRObject
        {
            get
            {
                if (connectVRObject == null)
                    connectVRObject = Instantiate(connectVRPrefab, spawnPosition, Quaternion.identity);
                return connectVRObject;
            }
        }

        void Awake()
        {
            Instance = this;
        }

        IEnumerator Start()
        {            
            if (!PhotonNetwork.InRoom)
            {
                //show temp chracter
                ShowLoaclTempVRPlayer(true);
            }
            else
            {
                PhotonNetwork.LeaveRoom();
            }

            while (!PhotonNetwork.InRoom)
            {
                Debug.Log("in room");
                yield return new WaitForSeconds(1);
            }

            // Instantiate the late-joining player
            //hide temp charcter
            ShowLoaclTempVRPlayer(false);
            GameObject vrp = PhotonNetwork.Instantiate(genericVRPlayerPrefab.name, spawnPosition, Quaternion.identity);

            if (vrp.GetComponent<PhotonView>().IsMine)
            {
                localVRPlayer = vrp;

                if (CesiumSceneHandler.Instance)
                {
                    CesiumSceneHandler.Instance.refreshTilesAction?.Invoke();
                }
            }
        }

        public void ShowLoaclTempVRPlayer(bool status)
        {
            ConnectVRObject.SetActive(status);
        }
    }
}