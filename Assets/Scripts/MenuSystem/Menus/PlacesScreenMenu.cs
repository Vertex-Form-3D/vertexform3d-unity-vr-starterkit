using System.Collections.Generic;
using UnityEngine;

namespace VertextFormCore
{
    public class PlacesScreenMenu : MonoBehaviour
    {
        public static PlacesScreenMenu _instance;
        [SerializeField] SerializedDataBase dataBase;

        [Header("Place List")]
        [SerializeField] PlaceItemView _placePrefab;
        [SerializeField] RectTransform _placeListRoot;


        private void Awake()
        {
            RoomManager.Instance.onJoinedRoomSuccess += OnJoinedRoom;
            RoomManager.Instance.onCashedRoomUpdated += OnCashRoomListUpdated;
            _instance = this;
        }

        private void OnDestroy()
        {
            RoomManager.Instance.onJoinedRoomSuccess -= OnJoinedRoom;
            RoomManager.Instance.onCashedRoomUpdated -= OnCashRoomListUpdated;
        }

        private void Start()
        {
            ListClickedListener listClickedListener = new ListClickedListener(OnPlaceClicked);
            ListAdapter<PlaceData> listAdapter =
                new ListAdapter<PlaceData>(_placePrefab, _placeListRoot, dataBase._placeItemDatas, listClickedListener);
            listAdapter.CreateViews();
        }

        private void OnPlaceClicked(int index)
        {
            Debug.Log($"hub clicked {dataBase._placeItemDatas[index].placeName}");
            SceneLoader.Instance.isCesiumScene = true;
            SceneLoader.Instance.isFlyModeEnabled = dataBase._placeItemDatas[index].flyMode;
            SceneLoader.Instance.cesiumWorldClass = dataBase._placeItemDatas[index].CesiumWorld;
            SceneLoader.Instance.LoadScnene(dataBase._placeItemDatas[index].placeName);
        }

        private void OnCashRoomListUpdated(List<Photon.Realtime.RoomInfo> activeRooms)
        {

        }

        private void OnJoinedRoom(string roomName)
        {
            //PhotonNetwork.LoadLevel(roomName);
            //StartCoroutine(NavigationButton.LoadSceneAsyncCoroutine(roomName));
        }
    }
}