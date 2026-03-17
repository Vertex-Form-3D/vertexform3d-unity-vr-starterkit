using System.Collections;
using UnityEngine;
using VertexFormCore;
using UnityEngine.SceneManagement;

namespace VertexFormCore
{
    public class VirtualRoomManager : MonoBehaviour
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
            RoomManager.Instance.ShowLocalTempVRPlayer(true);

            // Use Fusion RoomManager to leave room
            if (RoomManager.Instance != null)
            {
                RoomManager.Instance.LeaveRoom();
            }

            StartCoroutine(WaitToLeave());
        }

        IEnumerator WaitToLeave()
        {
            Debug.Log("0-> Leaving Fusion room");

            // Wait for Fusion runner to shutdown
            while (RoomManager.Instance != null && RoomManager.Instance.Runner != null && RoomManager.Instance.Runner.IsClient)
            {
                Debug.Log("0-> Waiting for Fusion room to disconnect");
                yield return new WaitForSeconds(1f);
            }

            Debug.Log("1-> Successfully left Fusion room");
            LoadHomeScene();
        }
        public void LeaveRoomAndLoadOnBoardingScene()
        {
            RoomManager.Instance.ShowLocalTempVRPlayer(true);

            // Use Fusion RoomManager to leave room
            if (RoomManager.Instance != null)
            {
                RoomManager.Instance.LeaveRoom();
            }

            StartCoroutine(WaitToLeaveOnBoardingScene());
        }
        IEnumerator WaitToLeaveOnBoardingScene()
        {
            Debug.Log("0-> Leaving Fusion room");

            // Wait for Fusion runner to shutdown
            while (RoomManager.Instance != null && RoomManager.Instance.Runner != null && RoomManager.Instance.Runner.IsClient)
            {
                Debug.Log("0-> Waiting for Fusion room to disconnect");
                yield return new WaitForSeconds(1f);
            }

            Debug.Log("1-> Successfully left Fusion room");
            ProjectManager.instance.platformAndSettings.mode = Mode.OnBoarding;
            SceneManager.LoadScene(0);
        }
        private void LoadHomeScene()
        {

            SceneManager.LoadScene(1); // Load scene at index 1 (home scene)

        }

        // Method to handle when a player joins (can be called by RoomManager)
        public void OnPlayerJoinedRoom(string playerName, int totalPlayers)
        {
            Debug.Log($"{playerName} joined room. Total players: {totalPlayers}");
        }

        // Method to handle when a player leaves (can be called by RoomManager)
        public void OnPlayerLeftRoom(int remainingPlayers)
        {
            Debug.Log($"Player left room. Remaining players: {remainingPlayers}");
        }
    }
}