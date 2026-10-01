using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using UnityEngine;

[Serializable]
public class SessionInfoData
{
    public string SessionID;
    public int PlayerCount;
    public int MaxPlayers;
    public string Scene;
    public bool IsOpen;
    public string StreetViewType;
    public string WorldKey;
    public string PanoId;
    public float Latitude;
    public float Longitude;
    public string TourTitle;
    public string RoomCode;
}

/// <summary>
/// Joins a lobby and keeps track of all sessions.
/// Check: https://doc.photonengine.com/fusion/current/manual/connection-and-matchmaking/matchmaking
/// "Lobby" is counterintuitive in Photon. Basically it's a list of game sessions. "Joining a lobby" only means that you get access to the list of sessions.
/// ...but you cannot be in a lobby (= receive list of sessions) and be in a session (= being in a game with other people).
/// So this NetworkLobby component has it's own NetworkRunner (= basically a second player) that checks for available sessions. This costs us an additional CCU.
/// Midterm, we should have one NetworkLobby on the masterclient that shares its info to other clients.
/// Longterm, we should run a server that has the list of lobbies and the clients can update from our server.
/// </summary> 
public class NetworkLobby : MonoBehaviour, INetworkRunnerCallbacks
{
    public static NetworkLobby Instance;

    [Header("Configuration")]
    private string region = "eu"; // Default region

    public List<SessionInfoData> Sessions = new List<SessionInfoData>();
    public Action OnSessionListChanged;

    /// <summary>
    /// Raised once per session as the list is rebuilt, after the framework's own fields are filled
    /// and before <see cref="OnSessionListChanged"/>.
    ///
    /// Lets an app read session properties the framework knows nothing about and write them onto
    /// the entry, without editing this file. World Expo uses it for Street View — instance kind,
    /// pano id, coordinates, room code — which used to be inlined here and was therefore destroyed
    /// every time the framework package was imported over it.
    ///
    /// <see cref="SessionInfoData"/> is a class, so a subscriber mutates the entry in place.
    /// Static so a subscriber can attach before this component exists; subscribers must detach in
    /// OnDestroy, or they will be kept alive across play sessions in the editor.
    /// </summary>
    public static event Action<SessionInfo, SessionInfoData> SessionEntryBuilt;

    private NetworkRunner _runner;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            this.transform.parent = null;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(this);
        }



    }

    private void Start()
    {
        StartCoroutine(StartLobbyWhenProjectSettingsReady());
    }

    private IEnumerator StartLobbyWhenProjectSettingsReady()
    {
        int waitedFrames = 0;
        while (ProjectManager.instance == null && waitedFrames++ < 120)
            yield return null;

        if (!ProjectManager.UsesPhotonSessionLobbyRunner)
        {
            Debug.Log("[NetworkLobby] Session lobby runner disabled (Photon CCU: game sessions only).");
            Destroy(this);
            yield break;
        }

        Debug.Log($"[NetworkLobby - Starting lobby...");
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.AddCallbacks(this);
        Task joinTask = JoinLobby(_runner);
        while (!joinTask.IsCompleted)
            yield return null;
    }

    // Photon method that is called automatically, if you are in a lobby and the session list changes.
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Sessions.Clear();

        foreach (SessionInfo session in sessionList)
        {
            // list all properties
            Debug.Log($"[NetworkLobby] Session: {session.Name}");
            foreach (var property in session.Properties)
            {
                Debug.Log($"[NetworkLobby] Session Property: {property.Key} = {property.Value.PropertyValue}");
            }

            var entry = new SessionInfoData
            {
                SessionID = session.Name,
                PlayerCount = session.PlayerCount,
                MaxPlayers = session.MaxPlayers,
                Scene = session.Properties.ContainsKey("Scene") ? session.Properties["Scene"].PropertyValue.ToString() : "",
                IsOpen = session.IsOpen,
            };

            Sessions.Add(entry);

            // Hand the raw session to anything that understands properties this framework does not.
            // A throwing subscriber must not take the whole session list down with it.
            try
            {
                SessionEntryBuilt?.Invoke(session, entry);
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkLobby] A SessionEntryBuilt subscriber threw for session \"{session.Name}\": {e}");
            }

            Debug.Log($"[NetworkLobby] Session: {session.Name} | Scene: {Sessions.Last().Scene} | Players: {session.PlayerCount}/{session.MaxPlayers}"); //| Scene: {session.Properties["Scene"].PropertyValue}");
        }

        OnSessionListChanged?.Invoke();

        Debug.Log($"[NetworkLobby] SESSION LIST UPDATED. Found {sessionList.Count} lobbies.");
    }

    public async Task JoinLobby(NetworkRunner runner)
    {
        // Build custom app settings with the specified region
        var appSettings = BuildCustomAppSetting("eu");

        var result = await runner.JoinSessionLobby(SessionLobby.Custom, "default_lobby", customAppSettings: appSettings);

        if (result.Ok)
        {
            Debug.Log($"[NetworkLobby] Successfully joined lobby in region: {region}");
        }
        else
        {
            Debug.LogError($"Failed to Start: {result.ShutdownReason}");
        }
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

    /// <summary>
    /// Set the region for the lobby connection. Call this before Start() or call ReconnectToRegion() after.
    /// </summary>
    /// <param name="newRegion">Region code (e.g., "us", "eu", "asia", etc.)</param>
    public void SetRegion(string newRegion)
    {
        region = newRegion;
    }

    /// <summary>
    /// Reconnect to the lobby with a new region
    /// </summary>
    /// <param name="newRegion">Region code (e.g., "us", "eu", "asia", etc.)</param>
    public async Task ReconnectToRegion(string newRegion)
    {
        if (_runner != null)
        {
            _runner.Shutdown();
            await Task.Delay(1000); // Wait for shutdown
        }

        region = newRegion;

        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.AddCallbacks(this);
        await JoinLobby(_runner);
    }

    /// <summary>
    /// Shuts the lobby runner down and removes it, then returns.
    ///
    /// The lobby is a second NetworkRunner living alongside the game runner — that is how the world
    /// list can show live player counts. Two runners are two sets of INetworkRunnerCallbacks firing
    /// on whoever is listening, and a PlayerRef issued by one is meaningless to the other. Calling
    /// this before entering a world removes that ambiguity entirely, at the cost of the live counts
    /// while you are in the session.
    ///
    /// Awaiting it matters: the runner must be fully down before a game runner starts, or the two
    /// overlap during exactly the window where a player object is being spawned.
    /// </summary>
    public async Task ShutdownLobbyRunner()
    {
        if (_runner == null)
            return;

        Debug.Log("[NetworkLobby] Shutting down lobby runner before entering game session...");

        // Cleared first so anything that checks _runner during the shutdown sees it as gone.
        NetworkRunner runnerToDestroy = _runner;
        _runner = null;

        if (runnerToDestroy.IsRunning ||
            runnerToDestroy.State == NetworkRunner.States.Starting)
        {
            await runnerToDestroy.Shutdown();
        }

        Destroy(runnerToDestroy);

        Debug.Log("[NetworkLobby] Lobby runner shut down and destroyed.");
    }

    // ======================== UNUSED METHODS ========================

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        /*
        if (runner.IsClient == true && shutdownReason != ShutdownReason.Ok)
        {
            NetworkSettings.Instance.ForceReconnect = true;
        }
        */
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {

    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {

    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {

    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {

    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {

    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {

    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {

    }

    public void OnConnectedToServer(NetworkRunner runner)
    {

    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {

    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {

    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {

    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {

    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {

    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {

    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {

    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {

    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {

    }
}
