using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace VertextFormCore
{
    public class RoomManager : MonoBehaviourPunCallbacks

    {
        public static RoomManager Instance;

        //  public bool messageQue = false;  

        public System.Action<string> onJoinedRoomSuccess;
        public System.Action<List<RoomInfo>> onCashedRoomUpdated;

        public List<RoomInfo> cashedRooms = new List<RoomInfo>();
        public List<roomClass> roomData = new List<roomClass>();

        private string mapName;
        public int numberofPlayers;
        public int numberofPlayersOnMaster;
        public int numberOfRooms;
        public int CountOfPlayersInRooms;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }


        void Start()
        {
            Debug.Log("Room Start");
            if (!PhotonNetwork.IsConnected)
            {
                PhotonNetwork.ConnectUsingSettings();
            }
            InvokeRepeating(nameof(GetMultiplayerData), 1, 1);
        }

        public void GetMultiplayerData()
        {
            CountOfPlayersInRooms = PhotonNetwork.CountOfPlayersInRooms;
            numberofPlayers = PhotonNetwork.CountOfPlayers;
            numberOfRooms = PhotonNetwork.CountOfRooms;
            numberofPlayersOnMaster = PhotonNetwork.CountOfPlayersOnMaster;
        }

        public void ConnectToRoom(string mapName)
        {

            this.mapName = mapName;
            ExitGames.Client.Photon.Hashtable expectedCustomRoomProperties = new ExitGames.Client.Photon.Hashtable()
        {
            { MultiplayerVRConstants.MAP_NAME_KEY, mapName }
        };

            StartCoroutine(JoinRoom(expectedCustomRoomProperties));
        }

        public void LeaveRoom()
        {
            if (PhotonNetwork.InRoom)
                PhotonNetwork.LeaveRoom();
        }


        //override On
        IEnumerator JoinRoom(ExitGames.Client.Photon.Hashtable expectedRoomProperties)
        {
            //wait to back to lobby
            while (!PhotonNetwork.InLobby)
                yield return new WaitForSeconds(1);

            PhotonNetwork.JoinRandomRoom(expectedRoomProperties, 0);
        }

        private void CreateRoom()
        {
            string randomRoomName = "Room_" + mapName + Random.Range(0, 10000);
            Debug.Log("CreateRoom " + randomRoomName);

            RoomOptions roomOptions = new RoomOptions();
            roomOptions.MaxPlayers = 20;
            string[] roomPropsInLobby = { MultiplayerVRConstants.MAP_NAME_KEY };

            ExitGames.Client.Photon.Hashtable customRoomProperties = new ExitGames.Client.Photon.Hashtable() {
            { MultiplayerVRConstants.MAP_NAME_KEY, mapName }
        };
            roomOptions.CustomRoomPropertiesForLobby = roomPropsInLobby;
            roomOptions.CustomRoomProperties = customRoomProperties;
            PhotonNetwork.CreateRoom(randomRoomName, roomOptions);
            Log("player attemp to create room " + randomRoomName);
        }

        public void Log(string message)
        {
            Debug.Log(message);
        }

        #region Photon Callback Methods 
        //master server
        public override void OnConnectedToMaster()
        {
            Log("Connected to maseter server Ready To Join Lobby");
            PhotonNetwork.JoinLobby();
        }

        //lobby
        public override void OnJoinedLobby()
        {
            Debug.Log("palyer Joined the lobby.");
        }

        //room
        public override void OnCreatedRoom()
        {
            Debug.Log("A room is created with the name: " + PhotonNetwork.CurrentRoom.Name);
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            Log("player Fail join random room attemp to creating room" + message);
            CreateRoom();
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {

            Debug.Log(newPlayer.NickName + " joined to: " + PhotonNetwork.CurrentRoom.PlayerCount);
        }

        public override void OnLeftRoom()
        {
            Log("Player left current room");
        }

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            roomData.Clear();
            foreach (RoomInfo room in roomList)
            {
                if (room.RemovedFromList)
                {
                    cashedRooms.Remove(room);
                }
                else
                {
                    cashedRooms.Add(room);
                }
                roomClass rc = new roomClass();
                rc.roomName = room.Name;
                rc.playerCount = room.PlayerCount;
                roomData.Add(rc);
            }

            onCashedRoomUpdated?.Invoke(cashedRooms);
        }
        #endregion

    }
    [System.Serializable]
    public class roomClass
    {
        public string roomName;
        public int playerCount;
    }
}