using System.Collections.Generic;
using UnityEngine;

namespace VertextFormCore
{
    public class HubsScreenMenu : MonoBehaviour
    {

        public static HubsScreenMenu _instance;
        [SerializeField] SerializedDataBase dataBase;
        [Header("HUB LIST")]
        [SerializeField] HubItemView _hubPrefab;
        [SerializeField] RectTransform _hubsListRoot;
        [SerializeField] HubItemView _hubCustomFirstView;
        [SerializeField] RectTransform _hubsCustomFirstRoot;


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
            ListClickedListener listClickedListener = new ListClickedListener(OnHubClicked);
            ListAdapterCustomFirstElementView<HubItemData> listAdapter = new ListAdapterCustomFirstElementView<HubItemData>(_hubCustomFirstView, _hubsCustomFirstRoot, _hubPrefab, _hubsListRoot, dataBase._hubItemDatas, listClickedListener);
            listAdapter.CreateViews();
        }

        private void OnHubClicked(int index)
        {
            Debug.Log($"hub clicked {dataBase._hubItemDatas[index].HubName}");
            SceneLoader.Instance.isCesiumScene = false;
            SceneLoader.Instance.isFlyModeEnabled = dataBase._hubItemDatas[index].flyMode;
            SceneLoader.Instance.LoadScnene(dataBase._hubItemDatas[index].HubName);
        }

        private void OnCashRoomListUpdated(List<Photon.Realtime.RoomInfo> activeRooms)
        {

        }

        internal void OnJoinedRoom(string roomName)
        {

        }
    }
}