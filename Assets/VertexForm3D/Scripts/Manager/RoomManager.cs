using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using UnityEngine.SceneManagement;
using Fusion.Photon.Realtime;
using System.Threading.Tasks;
using System.Linq;
using DG.Tweening;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace VertexFormCore
{
    public class RoomManager : MonoBehaviour, INetworkRunnerCallbacks
    {
        public static RoomManager Instance;

        public System.Action<string> onJoinedRoomSuccess;
        public System.Action<List<SessionInfo>> onCashedRoomUpdated;

        public List<SessionInfo> cashedRooms = new List<SessionInfo>();
        public List<roomClass> roomData = new List<roomClass>();

        [SerializeField] private NetworkRunner _runner;
        private string mapName;
        public int numberofPlayers;
        public int numberOfRooms;
        [SerializeField] GameObject genericVRPlayerPrefab;
        [SerializeField] GameObject connectVRPrefab;
        public List<PlayerNetworkSetup> allPlayers = new List<PlayerNetworkSetup>();
        public GameObject localVRPlayer;
        [SerializeField] private PlayerJoinUI playerJoinUI;
        [SerializeField] private MessageScript messageUI;
        [SerializeField] private RectTransform playerJoinHolder;
        [SerializeField] private GameObject addressableSceneVisuals;
        [SerializeField] private CanvasGroup fadeCanvasGroup;

        GameObject connectVRObject;
        public Vector3 spawnPosition;

        // Dictionary to track spawned players
        private Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();

        // Dictionary to track player names
        private Dictionary<PlayerRef, string> playerNames = new Dictionary<PlayerRef, string>();

        // Track players who were already in the room when local player joined (don't show join messages for them)
        private HashSet<PlayerRef> knownPlayers = new HashSet<PlayerRef>();


        public GameObject ConnectVRObject
        {
            get
            {
                if (connectVRObject == null)
                    connectVRObject = Instantiate(connectVRPrefab, spawnPosition, Quaternion.identity);
                return connectVRObject;
            }
        }

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

            ShowLocalTempVRPlayer(true);
            // Fusion doesn't require explicit connection like PUN2
            InvokeRepeating(nameof(GetMultiplayerData), 1, 1);
        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                foreach (var player in playerNames)
                {
                    Debug.Log($"Player {player.Key.PlayerId} name is {player.Value} is in the room");
                }
            }
        }
        public void GetMultiplayerData()
        {
            if (_runner != null)
            {

                numberofPlayers = _runner.SessionInfo != null ? _runner.SessionInfo.PlayerCount : 0;
                numberOfRooms = cashedRooms.Count;
            }
        }
        public void SetAddressableSceneVisuals(bool status)
        {
            // Stop any ongoing fade on this canvas to avoid conflicts
            fadeCanvasGroup.DOKill();

            Sequence seq = DOTween.Sequence().SetEase(Ease.InOutQuad);

            if (status)
            {
                // SHOW SCENE VISUALS FLOW
                // 1. Make sure the black image is fully opaque, then enable scene visuals
                fadeCanvasGroup.alpha = 1f;
                addressableSceneVisuals.SetActive(true);

                // 2. Fade black image out so we can see the scene visuals
                seq.Append(fadeCanvasGroup.DOFade(0f, 2f));
            }
            else
            {
                // HIDE SCENE VISUALS FLOW
                // 1. Start from transparent
                fadeCanvasGroup.alpha = 1f;

                // 2. Fade to black so we hide the scene visuals
                //seq.AppendInterval(2f);
                //seq.Append(fadeCanvasGroup.DOFade(1f, 2f));

                // 3. Once fully black, turn off the scene visuals
                seq.AppendCallback(() => addressableSceneVisuals.SetActive(false));

                // 4. Wait a bit, then fade black image away again
                seq.Append(fadeCanvasGroup.DOFade(0f, 2f));
            }
        }

        public async void ConnectToRoom(string mapName)
        {
            Debug.Log($"[RoomManager] ConnectToRoom called with mapName: {mapName}");

            // Check if NetworkRunner is assigned, create one if not
            if (_runner == null)
            {
                Debug.Log("[RoomManager] NetworkRunner is null, creating one...");
                _runner = gameObject.AddComponent<NetworkRunner>();
                _runner.AddCallbacks(this);
                Debug.Log("[RoomManager] NetworkRunner created and callbacks added");
            }
            else
            {
                Debug.Log("[RoomManager] NetworkRunner already exists, ensuring callbacks are registered");
                // Ensure callbacks are registered
                _runner.AddCallbacks(this);
            }

            Debug.Log($"[RoomManager] NetworkRunner state: {_runner.State}, IsRunning: {_runner.IsRunning}");

            this.mapName = mapName;

            _runner.ProvideInput = true;

            var appSettings = BuildCustomAppSetting("eu");

            // Create scene info for both base scene and addressable scene
            var sceneInfo = new NetworkSceneInfo();

            // Add the base "addressableScene" from build settings
            var baseScene = SceneManager.GetSceneByName("addressableScene");
            if (baseScene.IsValid())
            {
                var baseSceneRef = SceneRef.FromIndex(baseScene.buildIndex);
                sceneInfo.AddSceneRef(baseSceneRef, LoadSceneMode.Single);
                Debug.Log($"Adding base scene to network: {baseScene.name} (build index: {baseScene.buildIndex})");
            }

            // Add the addressable scene with mapName additively
            if (!string.IsNullOrEmpty(mapName))
            {
                var addressableSceneRef = SceneRef.FromPath(mapName);
                sceneInfo.AddSceneRef(addressableSceneRef, LoadSceneMode.Additive);
                Debug.Log($"Adding addressable scene to network: {mapName} (additive)");
            }

            var startGameArgs = new StartGameArgs
            {
                GameMode = GameMode.Shared,
                CustomPhotonAppSettings = appSettings,
                SessionName = mapName,
                Scene = sceneInfo, // Add scene information for both scenes
                SceneManager = _runner.GetComponent<NetworkSceneManagerDefault>(),
                CustomLobbyName = "default_lobby",
                IsVisible = true,
                IsOpen = true
            };

            // Start game session
            Debug.Log($"[RoomManager] About to call StartGame with session name: {mapName}");

            // Add timeout to prevent hanging
            /*StartCoroutine(WaitNCall(4f, () =>
            {
                //SetAddressableSceneVisuals(false);
            }));*/
            var startGameTask = _runner.StartGame(startGameArgs);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30)); // 30 second timeout

            var completedTask = await Task.WhenAny(startGameTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                Debug.LogError("[RoomManager] StartGame timed out after 30 seconds!");
                return;
            }

            var result = startGameTask.Result;
            Debug.Log($"[RoomManager] StartGame completed. Result: {(result.Ok ? "Success" : "Failed")}");

            if (result.Ok)
            {
                Log("Successfully connected to room: " + mapName);
                onJoinedRoomSuccess?.Invoke(mapName);
            }
            else
            {
                Log("Failed to connect to room: " + result.ShutdownReason);
            }
        }

        public void PlayerJoined(PlayerRef player)
        {
            Debug.Log($"[SpawnManager] PlayerJoined called for player: {player}");

            // Validate Runner is available
            if (_runner == null)
            {
                Debug.LogError("[SpawnManager] Runner is null - cannot spawn player!");
                Debug.LogError($"[SpawnManager] SpawnManager GameObject: {gameObject.name}, Active: {gameObject.activeInHierarchy}");
                return;
            }

            Debug.Log($"[SpawnManager] Runner found: {_runner.name}, State: {_runner.State}, IsRunning: {_runner.IsRunning}");

            // Only spawn for the local player
            if (player == _runner.LocalPlayer)
            {
                Debug.Log($"[SpawnManager] Spawning VR player for local player: {player}");
                SetAddressableSceneVisuals(false);
                SpawnLocalVRPlayer(player);
            }
            else
            {
                Debug.Log($"[SpawnManager] Remote player joined: {player} - their client will spawn their own player");
            }
        }

        private void SpawnLocalVRPlayer(PlayerRef player)
        {

            // Get spawn points
            PlayerSpawnPointScript[] playerSpawnPointScripts = FindObjectsByType<PlayerSpawnPointScript>(FindObjectsSortMode.InstanceID);
            int pspIndex = 0;

            Debug.Log($"[SpawnManager] Found {playerSpawnPointScripts.Length} spawn points");

            Vector3 spawnPos = spawnPosition;
            Quaternion spawnRot = Quaternion.identity;

            if (playerSpawnPointScripts.Length > 0)
            {
                // Use a random spawn point or find an available one
                pspIndex = UnityEngine.Random.Range(0, playerSpawnPointScripts.Length);
                PlayerSpawnPointScript pps = playerSpawnPointScripts[pspIndex];
                spawnPos = pps.transform.position;
                spawnRot = pps.transform.rotation;
                Debug.Log($"[SpawnManager] Using spawn point {pspIndex} at position {spawnPos}");
            }
            else
            {
                Debug.Log($"[SpawnManager] No spawn points found, using default position: {spawnPosition}");
            }

            // Position the temp VR object at spawn location
            ConnectVRObject.transform.position = spawnPos;
            ConnectVRObject.transform.rotation = spawnRot;

            // Spawn the VR player
            SpawnVRPlayer(player, spawnPos, spawnRot);
        }

        private void SpawnVRPlayer(PlayerRef player, Vector3 spawnPos, Quaternion spawnRot)
        {
            Debug.Log($"[SpawnManager] SpawnVRPlayer called for player: {player}");

            // Validate prefab
            if (genericVRPlayerPrefab == null)
            {
                Debug.LogError("[SpawnManager] genericVRPlayerPrefab is NULL! Cannot spawn player.");
                return;
            }

            // Check if prefab has NetworkObject component
            NetworkObject prefabNetworkObject = genericVRPlayerPrefab.GetComponent<NetworkObject>();
            if (prefabNetworkObject == null)
            {
                Debug.LogError("[SpawnManager] genericVRPlayerPrefab does not have a NetworkObject component! Cannot spawn player.");
                return;
            }

            Debug.Log($"[SpawnManager] Prefab validation passed. NetworkObject found: {prefabNetworkObject.name}");

            // Hide temp character
            ShowLocalTempVRPlayer(false);

            Debug.Log($"[SpawnManager] Attempting to spawn at position: {spawnPos}, rotation: {spawnRot}");

            // Spawn the VR player using Fusion's spawn method
            NetworkObject vrpNetworkObject = _runner.Spawn(genericVRPlayerPrefab, spawnPos, spawnRot, player);
            Debug.Log("vrpNetworkObject.transform.position: " + vrpNetworkObject.transform.position);
            Debug.Log("vrpNetworkObject.transform.rotation: " + vrpNetworkObject.transform.rotation);
            Debug.Log($"[SpawnManager] Runner.Spawn returned: {(vrpNetworkObject != null ? vrpNetworkObject.name : "NULL")}");

            if (vrpNetworkObject != null)
            {
                // Track the spawned player
                spawnedPlayers[player] = vrpNetworkObject;

                // Store player name from networked property if available
                PlayerNetworkSetup playerSetup = vrpNetworkObject.GetComponent<PlayerNetworkSetup>();

                if (vrpNetworkObject.HasInputAuthority)
                {
                    localVRPlayer = vrpNetworkObject.gameObject;

                    SceneLoader.Instance.ActivateScene();
                    Debug.Log($"[SpawnManager] VR Player spawned successfully! GameObject: {localVRPlayer.name}");

                    if (CesiumSceneHandler.Instance)
                    {
                        CesiumSceneHandler.Instance.refreshTilesAction?.Invoke();
                        Debug.Log("[SpawnManager] Cesium tiles refresh invoked");
                    }
                }
                else
                {
                    Debug.Log($"[SpawnManager] Remote VR Player spawned: {vrpNetworkObject.name}");
                }
            }
            else
            {
                Debug.LogError("[SpawnManager] Failed to spawn VR Player - Runner.Spawn returned null");
            }
        }

        public void ShowLocalTempVRPlayer(bool status)
        {
            Debug.Log("ConnectVRObject.transform.position: " + ConnectVRObject.transform.position);
            Debug.Log("ConnectVRObject.transform.rotation: " + ConnectVRObject.transform.rotation);
            ConnectVRObject.SetActive(status);
            Debug.Log($"[SpawnManager] Temp VR Player visibility set to: {status}");
        }
        private FusionAppSettings BuildCustomAppSetting(string region)
        {
            var appSettings = PhotonAppSettings.Global.AppSettings.GetCopy();
            appSettings.UseNameServer = true;
            appSettings.FixedRegion = region.ToLower();
            return appSettings;
        }
        public void LeaveRoom()
        {
            if (_runner != null && _runner.IsClient)
            {
                _runner.Shutdown();
            }
        }

        public void Log(string message)
        {
            Debug.Log(message);
        }

        #region Fusion Callback Methods 

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"[RoomManager] OnPlayerJoined callback triggered for player: {player}, Total players: {runner.SessionInfo.PlayerCount}");

            StartCoroutine(WaitNCall(2f, () =>
            {
                PlayerJoined(player);
                Debug.Log($"[RoomManager] OnPlayerJoined called for player: {player}, Total players: {runner.SessionInfo.PlayerCount}");
            }));

            // If this is the local player joining, mark all existing players as "known"
            // so we don't show join messages for players who were already in the room
            if (player == runner.LocalPlayer)
            {
                StartCoroutine(MarkExistingPlayersAsKnown(runner));
            }
            else
            {
                // For other players joining, show join message only if they joined after local player
                StartCoroutine(BroadcastPlayerJoined(runner, player));
            }
        }

        IEnumerator MarkExistingPlayersAsKnown(NetworkRunner runner)
        {
            // Wait a bit for all players to be spawned and initialized
            yield return new WaitForSeconds(3f);

            // Mark all existing players as "known" so we don't show join messages for them
            var players = FindObjectsByType<PlayerNetworkSetup>(FindObjectsSortMode.None);
            foreach (var p in players)
            {
                if (p.Object != null && p.Object.InputAuthority != runner.LocalPlayer)
                {
                    knownPlayers.Add(p.Object.InputAuthority);
                    playerNames[p.Object.InputAuthority] = p.PlayerName.ToString();
                    Debug.Log($"[RoomManager] Marked existing player {p.Object.InputAuthority.PlayerId} ({p.PlayerName}) as known");
                }
            }
        }

        IEnumerator BroadcastPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            // Wait for player to be spawned
            yield return new WaitForSeconds(2f);

            string playerName = $"Player {player.PlayerId}";
            float timeout = 10f; // Maximum 10 seconds to wait for player name
            float elapsed = 0f;
            bool foundName = false;

            // Retry getting player name until it's available or timeout
            while (elapsed < timeout && !foundName)
            {
                // Try to get player name from PlayerNetworkSetup
                var players = FindObjectsByType<PlayerNetworkSetup>(FindObjectsSortMode.None);
                foreach (var p in players)
                {
                    if (p.Object != null && p.Object.InputAuthority == player && p.Object.IsValid)
                    {
                        // Check if PlayerName is not empty (networked property is synced)
                        string name = p.PlayerName.ToString();
                        string fallbackName = $"Player {player.PlayerId}";

                        // Accept the name if it's not empty and different from the fallback
                        // Also check that it's not just whitespace and has reasonable length
                        if (!string.IsNullOrWhiteSpace(name) && name != fallbackName && name != "Player" && name.Length > 0)
                        {
                            playerName = name;
                            playerNames[player] = playerName;
                            foundName = true;
                            Debug.Log($"[RoomManager] Found player name: {playerName} for player {player.PlayerId}");
                            break;
                        }
                    }
                }

                if (!foundName)
                {
                    elapsed += 0.5f;
                    yield return new WaitForSeconds(0.5f); // Wait 0.5 seconds before retrying
                }
            }

            if (!foundName)
            {
                Debug.LogWarning($"[RoomManager] Could not get player name for player {player.PlayerId} within timeout, using fallback");
            }

            // Only show join message if this player joined AFTER the local player
            // (i.e., they're not in the knownPlayers set)
            if (!knownPlayers.Contains(player))
            {
                Debug.Log($"[RoomManager] {playerName} joined the Lobby");
                PlayerJoinLeftUI(playerName, true);
                knownPlayers.Add(player); // Mark as known for future reference
            }
            else
            {
                Debug.Log($"[RoomManager] {playerName} was already in the room, skipping join message");
            }
        }
        IEnumerator WaitNCall(float seconds, Action action)
        {
            yield return new WaitForSeconds(seconds);
            action();
        }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            string playerName = $"Player {player.PlayerId}";

            // Try to get player name from stored dictionary first
            if (playerNames.ContainsKey(player))
            {
                playerName = playerNames[player];
            }
            else
            {
                // Try to get from PlayerNetworkSetup if still available
                var players = FindObjectsByType<PlayerNetworkSetup>(FindObjectsSortMode.None);
                foreach (var p in players)
                {
                    if (p.Object != null && p.Object.InputAuthority == player)
                    {
                        playerName = p.PlayerName.ToString();
                        break;
                    }
                }
            }

            // Clean up references
            if (playerNames.ContainsKey(player))
            {
                playerNames.Remove(player);
            }
            if (spawnedPlayers.ContainsKey(player))
            {
                spawnedPlayers.Remove(player);
            }
            if (knownPlayers.Contains(player))
            {
                knownPlayers.Remove(player);
            }

            Debug.Log($"[RoomManager] {playerName} Left the Lobby");
            PlayerJoinLeftUI(playerName, false);
        }
        public void PlayerJoinLeftUI(string playerName, bool isJoined)
        {
            if (localVRPlayer != null)
            {
                playerJoinHolder = localVRPlayer.GetComponent<PlayerNetworkSetup>().notificationParent;
                GameObject obj = Instantiate(messageUI.gameObject, playerJoinHolder);
                if (isJoined)
                {
                    obj.GetComponent<MessageScript>().ShowMessage(playerName + " Join the Place.", Color.green);
                }
                else
                {
                    obj.GetComponent<MessageScript>().ShowMessage(playerName + " Left the Place.", Color.red);
                }
            }
        }
        public void OnConnectedToServer(NetworkRunner runner)
        {
            Debug.Log($"[RoomManager] OnConnectedToServer called - Connected to Fusion server. Runner: {runner.name}, State: {runner.State}");
            Log("Connected to server - Ready to join sessions");
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            Log($"Disconnected from server: {reason}");
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            Debug.LogError($"[RoomManager] OnConnectFailed called - Connection failed: {reason}");
            Log($"Connection failed: {reason}");
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            Log($"Runner shutdown: {shutdownReason}");
            if (_runner == runner)
            {
                _runner = null;
            }
        }

        // Required INetworkRunnerCallbacks methods (empty implementations)
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            cashedRooms.Clear();
            roomData.Clear();

            foreach (SessionInfo session in sessionList)
            {
                cashedRooms.Add(session);

                roomClass rc = new roomClass();
                rc.roomName = session.Name;
                rc.playerCount = session.PlayerCount;
                roomData.Add(rc);
            }

            onCashedRoomUpdated?.Invoke(cashedRooms);
        }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

        #endregion

        public NetworkRunner Runner => _runner;
    }

    [System.Serializable]
    public class roomClass
    {
        public string roomName;
        public int playerCount;
    }
}

#if UNITY_EDITOR

namespace VertexFormCore
{
    [CustomEditor(typeof(RoomManager))]
    public class RoomManagerEditorScript : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("This script is responsible for creating and joining rooms using Fusion 2.", MessageType.Info);

            RoomManager roomManager = (RoomManager)target;

            if (GUILayout.Button("Home"))
            {
                if (VirtualRoomManager.Instance == null) return;
                VirtualRoomManager.Instance.LeaveRoomAndLoadHomeScene();
            }
        }
    }
}
#endif