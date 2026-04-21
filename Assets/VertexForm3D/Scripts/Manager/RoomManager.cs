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
using Photon.Voice.Fusion;

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
        /// <summary>Local spawned object: VR player prefab or spectator prefab depending on mode.</summary>
        public GameObject localVRPlayer;
        /// <summary>True when local client is in Spectator mode (localVRPlayer is spectator prefab, no PlayerNetworkSetup).</summary>

        /// <summary>Returns PlayerNetworkSetup of the local VR player, or null if local is spectator or not spawned.</summary>
        public PlayerNetworkSetup GetLocalPlayerSetup() => localVRPlayer != null ? localVRPlayer.GetComponent<PlayerNetworkSetup>() : null;
        [SerializeField] private MessageScript messageUI;
        [SerializeField] private RectTransform playerJoinHolder;
        [SerializeField] private GameObject addressableSceneVisuals;
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField] private FusionVoiceClient fusionVoiceClient;
        private bool _isReturningHomeFromDisconnect;

        GameObject connectVRObject;
        public Vector3 spawnPosition;

        // Dictionary to track spawned players
        private Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();

        // Dictionary to track player names
        private Dictionary<PlayerRef, string> playerNames = new Dictionary<PlayerRef, string>();

        // Track players who were already in the room when local player joined (don't show join messages for them)
        private HashSet<PlayerRef> knownPlayers = new HashSet<PlayerRef>();
        // Track which players are spectators – don't show their join/leave to other players
        private HashSet<PlayerRef> knownSpectators = new HashSet<PlayerRef>();

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

            // Find FusionVoiceClient if not assigned
            if (fusionVoiceClient == null)
            {
                fusionVoiceClient = FindFirstObjectByType<FusionVoiceClient>();
                if (fusionVoiceClient != null)
                {
                    Debug.Log("[RoomManager] FusionVoiceClient found");
                }
                else
                {
                    Debug.LogWarning("[RoomManager] FusionVoiceClient not found in scene");
                }
            }
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

        public void LeaveRoomAndLoadOnBoardingScene()
        {
            VirtualRoomManager.Instance.LeaveRoomAndLoadOnBoardingScene();
        }

        /// <summary>
        /// Connects to a Fusion room/session with the specified addressable scene.
        /// 
        /// WORKFLOW (Option B - Fusion Loads Addressable Scene):
        /// 1. SceneLoader loads only the base "addressableScene"
        /// 2. ConnectToRoom is called with the addressable scene name/key
        /// 3. Scene is registered with CustomNetworkSceneManager
        /// 4. Fusion loads the addressable scene for all clients via CustomNetworkSceneManager
        /// 5. NetworkObjects in the addressable scene are properly networked
        /// 
        /// This ensures cross-platform compatibility and proper NetworkObject synchronization.
        /// </summary>
        /// <param name="mapName">The addressable scene key/name (e.g., "Journeys Experience")</param>
        public async void ConnectToRoom(string mapName)
        {
            Debug.Log($"[RoomManager] ConnectToRoom called with session name: {mapName}");

            // Check if NetworkRunner is assigned, create one if not
            if (_runner == null)
            {
                Debug.Log("[RoomManager] NetworkRunner is null, creating one...");
                _runner = gameObject.AddComponent<NetworkRunner>();

                // Add our custom scene manager instead of the default one
                var sceneManager = _runner.GetComponent<CustomNetworkSceneManager>();
                if (sceneManager == null)
                {
                    sceneManager = _runner.gameObject.AddComponent<CustomNetworkSceneManager>();
                    Debug.Log("[RoomManager] CustomNetworkSceneManager added");
                }

                _runner.AddCallbacks(this);
                Debug.Log("[RoomManager] NetworkRunner created and callbacks added");
            }
            else
            {
                Debug.Log("[RoomManager] NetworkRunner already exists, ensuring callbacks are registered");
                // Ensure callbacks are registered
                _runner.AddCallbacks(this);

                // Make sure we have the custom scene manager
                var sceneManager = _runner.GetComponent<CustomNetworkSceneManager>();
                if (sceneManager == null)
                {
                    sceneManager = _runner.gameObject.AddComponent<CustomNetworkSceneManager>();
                    Debug.Log("[RoomManager] CustomNetworkSceneManager added to existing runner");
                }
            }

            Debug.Log($"[RoomManager] NetworkRunner state: {_runner.State}, IsRunning: {_runner.IsRunning}");

            this.mapName = mapName;

            _runner.ProvideInput = true;

            var appSettings = BuildCustomAppSetting("eu");

            // Create scene info for both base scene and addressable scene
            var sceneInfo = new NetworkSceneInfo();

            // Base "addressableScene" must be in Editor Build Settings. Use build index so Fusion loads it
            // via SceneManager (not Addressables). FromPath would require that path in Fusion's addressable list.
            var baseScene = SceneManager.GetSceneByName("addressableScene");
            if (baseScene.IsValid())
            {
                int baseBuildIndex = SceneUtility.GetBuildIndexByScenePath(baseScene.path);
                SceneRef baseSceneRef = baseBuildIndex >= 0
                    ? SceneRef.FromIndex(baseBuildIndex)
                    : SceneRef.FromPath(baseScene.path);
                sceneInfo.AddSceneRef(baseSceneRef, LoadSceneMode.Single);
                Debug.Log($"Adding base scene to network: {baseScene.name} (path: {baseScene.path}, SceneRef: {(baseBuildIndex >= 0 ? $"index {baseBuildIndex}" : "path")})");
            }

            // Register and add the addressable scene for Fusion to load
            string sessionName = mapName;
            if (!string.IsNullOrEmpty(mapName))
            {
                // Register the addressable scene with our custom scene manager
                // This tells Fusion where to find the scene for loading
                CustomNetworkSceneManager.RegisterAddressableScene(mapName);
                Debug.Log($"[RoomManager] Registered addressable scene: {mapName}");

                // Add addressable scene to NetworkSceneInfo
                // Fusion will load this scene using CustomNetworkSceneManager
                var addressableSceneRef = SceneRef.FromPath(mapName);
                sceneInfo.AddSceneRef(addressableSceneRef, LoadSceneMode.Additive);
                Debug.Log($"[RoomManager] Adding addressable scene to NetworkSceneInfo: {mapName} (additive mode)");
            }

            var startGameArgs = new StartGameArgs
            {
                GameMode = GameMode.Shared,
                CustomPhotonAppSettings = appSettings,
                SessionName = sessionName, // Use scene name for the room, not the path
                Scene = sceneInfo, // Add scene information for both scenes
                SceneManager = _runner.GetComponent<CustomNetworkSceneManager>(),
                CustomLobbyName = "default_lobby",
                IsVisible = true,
                IsOpen = true
            };

            // Start game session
            Debug.Log($"[RoomManager] About to call StartGame with session name: {sessionName} (scene path: {mapName})");

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
                ConnectToRoom(mapName); // Retry connection
                return;
            }

            var result = startGameTask.Result;
            Debug.Log($"[RoomManager] StartGame completed. Result: {(result.Ok ? "Success" : "Failed")}");

            if (result.Ok)
            {
                Log("Successfully connected to room: " + sessionName);
                onJoinedRoomSuccess?.Invoke(sessionName);
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


            GameObject prefabToSpawn = genericVRPlayerPrefab;

            if (prefabToSpawn == null)
            {
                return;
            }

            NetworkObject prefabNetworkObject = prefabToSpawn.GetComponent<NetworkObject>();
            if (prefabNetworkObject == null)
            {
                Debug.LogError($"[SpawnManager] Prefab does not have a NetworkObject component! Cannot spawn.");
                return;
            }


            ShowLocalTempVRPlayer(false);

            NetworkObject vrpNetworkObject = _runner.Spawn(prefabToSpawn, spawnPos, spawnRot, player);
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

                    // Manually join voice lobby now that local player is spawned (with a small delay)
                    StartCoroutine(JoinVoiceLobbyDelayed(1f));
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

        /// <summary>
        /// Manually join the voice lobby after the room is ready (with delay)
        /// </summary>
        private IEnumerator JoinVoiceLobbyDelayed(float delay)
        {
            Debug.Log($"[RoomManager] Waiting {delay} seconds before joining voice lobby...");
            yield return new WaitForSeconds(delay);
            JoinVoiceLobby();
        }

        /// <summary>
        /// Manually join the voice lobby after the room is ready
        /// </summary>
        public void JoinVoiceLobby()
        {
            if (fusionVoiceClient == null)
            {
                fusionVoiceClient = FindFirstObjectByType<FusionVoiceClient>();
                if (fusionVoiceClient == null)
                {
                    Debug.LogError("[RoomManager] Cannot join voice lobby - FusionVoiceClient not found!");
                    return;
                }
            }

            // Ensure AutoConnectAndJoin is disabled (should already be disabled)
            if (fusionVoiceClient.AutoConnectAndJoin)
            {
                Debug.LogWarning("[RoomManager] AutoConnectAndJoin is enabled. Disabling it for manual control.");
                fusionVoiceClient.AutoConnectAndJoin = false;
            }

            // Check if already connected
            if (fusionVoiceClient.Client != null && fusionVoiceClient.Client.State == Photon.Realtime.ClientState.Joined)
            {
                Debug.Log("[RoomManager] Voice client already joined to voice room");
                EnableVoiceRecorder();
                return;
            }

            // Manually connect and join the voice room
            Debug.Log("[RoomManager] Manually joining voice lobby...");
            bool success = fusionVoiceClient.ConnectAndJoinRoom();

            if (success)
            {
                Debug.Log("[RoomManager] Voice lobby join command sent successfully");
                // Enable recorder after a short delay to ensure connection is established
                StartCoroutine(EnableVoiceRecorderDelayed(2f));
            }
            else
            {
                Debug.LogError("[RoomManager] Failed to send voice lobby join command");
            }
        }

        /// <summary>
        /// Enable voice recorder with a delay
        /// </summary>
        private IEnumerator EnableVoiceRecorderDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);
            EnableVoiceRecorder();
        }

        /// <summary>
        /// Wire Photon recorders after voice join: recording on, <see cref="Recorder.TransmitEnabled"/> follows
        /// the local player's runtime mute toggle when available (same as <see cref="PlayerUIManager"/> / in-session UI),
        /// otherwise <see cref="SettingClass.micType"/> defaults.
        /// </summary>
        private void EnableVoiceRecorder()
        {
            bool transmit = VoiceTransmitDesiredForLocalPlayer();
            Debug.Log($"[RoomManager] Apply voice recorders after join — transmit={(transmit ? "on" : "off (muted)")}");

            void Apply(Photon.Voice.Unity.Recorder recorder, string label)
            {
                if (recorder == null)
                    return;
                recorder.RecordingEnabled = true;
                recorder.TransmitEnabled = transmit;
                Debug.Log($"[RoomManager] {label} — RecordingEnabled={recorder.RecordingEnabled}, TransmitEnabled={recorder.TransmitEnabled}");
            }

            if (fusionVoiceClient != null)
                Apply(fusionVoiceClient.PrimaryRecorder, "Primary Recorder");

            if (VoiceRecorderManager.Instance != null)
                Apply(VoiceRecorderManager.Instance.recorder, "VoiceRecorderManager recorder");

            var players = FindObjectsByType<PlayerNetworkSetup>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                if (player.Object != null && player.Object.HasInputAuthority)
                {
                    var recorder = player.GetComponentInChildren<Photon.Voice.Unity.Recorder>();
                    if (recorder != null)
                        Apply(recorder, $"Local player ({player.PlayerName})");
                }
            }
        }

        /// <summary>Fallback when local <see cref="PlayerUIManager"/> is not available yet (e.g. spectator).</summary>
        private static bool DefaultVoiceTransmitFromProjectSettings()
        {
            if (ProjectManager.instance == null || ProjectManager.instance.settingsUI == null ||
                ProjectManager.instance.settingsUI.defaultSettings == null)
                return false;

            return ProjectManager.instance.settingsUI.defaultSettings.micType == micType.unmute;
        }

        /// <summary>
        /// Must match in-session mute UI: toggling unmute does not write <see cref="SettingClass.micType"/>, so delayed
        /// <see cref="EnableVoiceRecorder"/> must not overwrite <see cref="Recorder.TransmitEnabled"/> from defaults alone.
        /// </summary>
        private bool VoiceTransmitDesiredForLocalPlayer()
        {
            var local = GetLocalPlayerSetup();
            if (local != null && local.playerUIManager != null)
                return local.playerUIManager.IsLocalVoiceUnmuted;

            return DefaultVoiceTransmitFromProjectSettings();
        }

        private FusionAppSettings BuildCustomAppSetting(string region)
        {
            var appSettings = PhotonAppSettings.Global.AppSettings.GetCopy();
            appSettings.UseNameServer = true;
            appSettings.FixedRegion = region.ToLower();
#if UNITY_WEBGL && !UNITY_EDITOR
            appSettings.Protocol = ExitGames.Client.Photon.ConnectionProtocol.WebSocketSecure;
#endif
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
            // Release any grabbed interactables that were held by this player (so they don't stay stuck)
            var interactables = FindObjectsByType<XRGrabNetworkInteractable>(FindObjectsSortMode.None);
            foreach (var interactable in interactables)
                interactable.ReleaseIfHeldByPlayer(player);

            if (knownSpectators.Contains(player))
            {
                knownSpectators.Remove(player);
                knownPlayers.Remove(player);
                playerNames.Remove(player);
                spawnedPlayers.Remove(player);
                Debug.Log($"[RoomManager] Spectator left the Lobby (no message shown)");
                return;
            }

            string playerName = $"Player {player.PlayerId}";
            if (playerNames.ContainsKey(player))
                playerName = playerNames[player];
            else
            {
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

            if (playerNames.ContainsKey(player)) playerNames.Remove(player);
            if (spawnedPlayers.ContainsKey(player)) spawnedPlayers.Remove(player);
            if (knownPlayers.Contains(player)) knownPlayers.Remove(player);

            Debug.Log($"[RoomManager] {playerName} Left the Lobby");
            PlayerJoinLeftUI(playerName, false);
        }
        public void PlayerJoinLeftUI(string playerName, bool isJoined)
        {
            if (localVRPlayer == null) return;
            var playerSetup = GetLocalPlayerSetup();
            if (playerSetup == null || (playerSetup.notificationParentDesktop == null && playerSetup.notificationParentVR == null)) return;
            if (ProjectManager.instance.platforms.IsDesktopStylePlatform())
            {
                playerJoinHolder = playerSetup.notificationParentDesktop;
            }
            else
            {
                playerJoinHolder = playerSetup.notificationParentVR;
            }
            GameObject obj = Instantiate(messageUI.gameObject, playerJoinHolder);
            if (isJoined)
                obj.GetComponent<MessageScript>().ShowMessage(playerName + " Join the Place.", Color.green);
            else
                obj.GetComponent<MessageScript>().ShowMessage(playerName + " Left the Place.", Color.red);
        }
        public void OnConnectedToServer(NetworkRunner runner)
        {
            Debug.Log($"[RoomManager] OnConnectedToServer called - Connected to Fusion server. Runner: {runner.name}, State: {runner.State}");
            Log("Connected to server - Ready to join sessions");
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            Log($"Disconnected from server: {reason}");
            HandleUnexpectedDisconnect($"Disconnected from server: {reason}");
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            Debug.LogError($"[RoomManager] OnConnectFailed called - Connection failed: {reason}");
            Log($"Connection failed: {reason}");
            HandleUnexpectedDisconnect($"Connection failed: {reason}");
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            Log($"Runner shutdown: {shutdownReason}");

            // Avoid scene fallback on user-initiated / normal shutdown.
            if (shutdownReason != ShutdownReason.Ok)
            {
                HandleUnexpectedDisconnect($"Runner shutdown: {shutdownReason}");
            }

            if (_runner == runner)
            {
                _runner = null;
            }
        }

        private void HandleUnexpectedDisconnect(string reason)
        {
            if (_isReturningHomeFromDisconnect)
            {
                return;
            }

            if (VirtualRoomManager.Instance == null)
            {
                Debug.LogWarning($"[RoomManager] Cannot return home after disconnect ({reason}) because VirtualRoomManager is missing.");
                return;
            }

            _isReturningHomeFromDisconnect = true;
            Debug.LogWarning($"[RoomManager] Unexpected network disconnect detected. Showing popup and returning to HomeScene. Reason: {reason}");

            var loadingScreen = LoadingScreen.Instance != null ? LoadingScreen.Instance : FindFirstObjectByType<LoadingScreen>();
            if (loadingScreen != null)
            {
                loadingScreen.ShowDisconnectPopupAndLoadHome(reason);
                return;
            }

            Debug.LogWarning("[RoomManager] LoadingScreen not found, returning to HomeScene immediately.");
            VirtualRoomManager.Instance.LeaveRoomAndLoadHomeScene();
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

        public void OnSceneLoadStart(NetworkRunner runner)
        {
            Debug.Log($"[RoomManager] OnSceneLoadStart - Fusion is loading scene(s)");
        }

        public void OnSceneLoadDone(NetworkRunner runner)
        {
            Debug.Log($"[RoomManager] OnSceneLoadDone - Fusion finished loading scene(s)");

            // Notify SceneLoader that the addressable scene is fully loaded by Fusion
            if (SceneLoader.Instance != null && !string.IsNullOrEmpty(mapName))
            {
                SceneLoader.Instance.OnFusionSceneLoaded(mapName);
            }
        }

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