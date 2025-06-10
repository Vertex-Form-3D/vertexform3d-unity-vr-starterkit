using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;

namespace VertexFormCore
{
    public class SpawnManager : MonoBehaviourPunCallbacks
    {
        [SerializeField] GameObject genericVRPlayerPrefab;
        [SerializeField] GameObject connectVRPrefab;
        public List<PlayerNetworkSetup> allPlayers = new List<PlayerNetworkSetup>();
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

            while (!SceneLoader.Instance.sceneIsLoaded)
            {
                yield return new WaitForSeconds(.2f);
            }
            PlayerSpawnPointScript[] playerSpawnPointScripts = FindObjectsByType<PlayerSpawnPointScript>(FindObjectsSortMode.InstanceID);
            int pspIndex=0;
            if (playerSpawnPointScripts.Length > 0)
            {
                // Set the position of the local player to the first spawn point
                pspIndex = Random.Range(0, playerSpawnPointScripts.Length);
                PlayerSpawnPointScript pps = playerSpawnPointScripts[pspIndex];
                ConnectVRObject.transform.position = pps.transform.position;
                ConnectVRObject.transform.rotation = pps.transform.rotation;
            }
            else
            {
                // If no spawn points are found, use the default spawn position
                ConnectVRObject.transform.position = spawnPosition;
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
                if (playerSpawnPointScripts.Length > 0)
                {
                    // Set the position of the local player to the first spawn point
                    PlayerSpawnPointScript pps = playerSpawnPointScripts[pspIndex];
                    localVRPlayer.transform.position = pps.transform.position;
                    localVRPlayer.transform.rotation= pps.transform.rotation;
                }
                else
                {
                    // If no spawn points are found, use the default spawn position
                    localVRPlayer.transform.position = spawnPosition;
                }
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