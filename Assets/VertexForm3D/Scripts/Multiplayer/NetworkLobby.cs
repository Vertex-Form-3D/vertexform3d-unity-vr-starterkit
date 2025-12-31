using System;
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
    private string region = "in"; // Default region

    public List<SessionInfoData> Sessions = new List<SessionInfoData>();
    public Action OnSessionListChanged;

    private NetworkRunner _runner;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(this);
        }



    }

    private async void Start()
    {
        Debug.Log($"[NetworkLobby - Starting lobby...");
        _runner = gameObject.AddComponent<NetworkRunner>();

        _runner.AddCallbacks(this);
        await JoinLobby(_runner);
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

            Sessions.Add(new SessionInfoData
            {
                SessionID = session.Name,
                PlayerCount = session.PlayerCount,
                MaxPlayers = session.MaxPlayers,
                Scene = session.Properties.ContainsKey("Scene") ? session.Properties["Scene"].PropertyValue.ToString() : "",
                IsOpen = true
            });

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
