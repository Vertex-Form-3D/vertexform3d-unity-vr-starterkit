#if FUSION_WEAVER
namespace Photon.Voice.Fusion
{
    using global::Fusion;
    using global::Fusion.Sockets;
    using PhotonAppSettings = global::Fusion.Photon.Realtime.PhotonAppSettings;
    using System.Collections.Generic;
    using Realtime;
    using ExitGames.Client.Photon;
    using UnityEngine;
    using Unity;
    using System;
    using LogLevel = Photon.Voice.LogLevel;

    [AddComponentMenu("Photon Voice/Fusion/Fusion Voice Client")]
    [RequireComponent(typeof(NetworkRunner))]
    public class FusionVoiceClient : VoiceFollowClient, INetworkRunnerCallbacks
    {
        // abstract VoiceFollowClient implementation
        protected override bool LeaderInRoom => this.networkRunner.SessionInfo.IsValid;
        protected override bool LeaderOfflineMode => networkRunner.GameMode == GameMode.Single;

#region Private Fields

        private NetworkRunner networkRunner;

        private EnterRoomParams voiceRoomParams = new EnterRoomParams
        {
            RoomOptions = new RoomOptions { IsVisible = false }
        };

        bool voiceFollowClientStarted = false;
#endregion

#region Properties

        /// <summary>
        /// Whether or not to use the Voice AppId and all the other AppSettings from Fusion's RealtimeAppSettings ScriptableObject singleton in the Voice client/app.
        /// </summary>
        [field: SerializeField]
        public bool UseFusionAppSettings = true;

        /// <summary>
        /// Whether or not to use the same AuthenticationValues used in Fusion client/app in Voice client/app as well.
        /// This means that the same UserID will be used in both clients.
        /// If custom authentication is used and setup in Fusion AppId from dashboard, the same configuration should be done for the Voice AppId.
        /// </summary>
        [field: SerializeField]
        public bool UseFusionAuthValues = true;

#endregion

#region Private Methods

        protected override void Start()
        {
            // skip "Temporary Runner Prefab"
            if (this.networkRunner.State == NetworkRunner.States.Shutdown)
            {
                return;
            }
            // Actual start code if the runner is already connecting
            VoiceFollowClientStart();
        }

        // Starts the VoiceFollowClient and add the recorder.
        //  Can be either be called from Start, or once the local player has joined the session, if the NetworkRunner was not yet starting during the Start() call
        void VoiceFollowClientStart() {
            if (voiceFollowClientStarted) return;

            voiceFollowClientStarted = true;
            base.Start();

            if (this.UsePrimaryRecorder)
            {
                if (this.PrimaryRecorder != null)
                {
                    AddRecorder(this.PrimaryRecorder);
                }
                else
                {
                    this.Logger.Log(LogLevel.Error, "Primary Recorder is not set.");
                }
            }
        }

        protected override void Awake()
        {
            base.Awake();

            this.networkRunner = this.GetComponent<NetworkRunner>();
            VoiceRegisterCustomTypes();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override Speaker InstantiateSpeakerForRemoteVoice(int playerId, byte voiceId, object userData)
        {
            if (userData == null) // Recorder w/o VoiceNetworkObject: probably created due to this.UsePrimaryRecorder = true
            {
                this.Logger.Log(LogLevel.Info, "Creating Speaker for remote voice {0}/{1} FusionVoiceClient Primary Recorder (userData == null).", playerId, voiceId);
                return this.InstantiateSpeakerPrefab(this.gameObject, true);
            }

            if (!(userData is NetworkId))
            {
                this.Logger.Log(LogLevel.Warning, "UserData ({0}) is not of type NetworkId. Remote voice {1}/{2} not linked. Do you have a Recorder not used with a VoiceNetworkObject? is this expected?",
                    userData == null ? "null" : userData.ToString(), playerId, voiceId);
                return null;
            }
            NetworkId networkId = (NetworkId)userData;
            if (!networkId.IsValid)
            {
                this.Logger.Log(LogLevel.Warning, "NetworkId is not valid ({0}). Remote voice {1}/{2} not linked.", networkId, playerId, voiceId);
                return null;
            }
            VoiceNetworkObject voiceNetworkObject = this.networkRunner.TryGetNetworkedBehaviourFromNetworkedObjectRef<VoiceNetworkObject>(networkId);
            if (ReferenceEquals(null, voiceNetworkObject) || !voiceNetworkObject)
            {
                this.Logger.Log(LogLevel.Warning, "No voiceNetworkObject found with ID {0}. Remote voice {1}/{2} not linked.", networkId, playerId, voiceId);
                return null;
            }
            this.Logger.Log(LogLevel.Info, "Using VoiceNetworkObject {0} Speaker for remote voice  p#{1} v#{2}.", userData, playerId, voiceId);
            return voiceNetworkObject.SpeakerInUse;
        }

        private string fusionOfflineVoiceRoomName;
        private string FusionOfflineVoiceRoomName
        {
            get
            {
                if (fusionOfflineVoiceRoomName == null)
                {
                    fusionOfflineVoiceRoomName = string.Format("fusion_offline_{0}_voice", Guid.NewGuid());
                }
                return fusionOfflineVoiceRoomName;
            }
        }

        // abstract VoiceFollowClient implementation
        protected override string GetVoiceRoomName()
        {
            return networkRunner.GameMode == GameMode.Single || !this.networkRunner.SessionInfo.IsValid ?
                FusionOfflineVoiceRoomName :
                string.Format("{0}_voice", this.networkRunner.SessionInfo.Name);
        }

        // abstract VoiceFollowClient implementation
        protected override bool ConnectVoice()
        {
            AppSettings settings = new AppSettings();
            if (this.UseFusionAppSettings)
            {
#if FUSION2
                settings.AppIdVoice = PhotonAppSettings.Global.AppSettings.AppIdVoice;
                settings.AppVersion = PhotonAppSettings.Global.AppSettings.AppVersion;
                settings.FixedRegion = PhotonAppSettings.Global.AppSettings.FixedRegion;
                settings.UseNameServer = PhotonAppSettings.Global.AppSettings.UseNameServer;
                settings.Server = PhotonAppSettings.Global.AppSettings.Server;
                settings.Port = PhotonAppSettings.Global.AppSettings.Port;
                settings.ProxyServer = PhotonAppSettings.Global.AppSettings.ProxyServer;
                settings.BestRegionSummaryFromStorage = PhotonAppSettings.Global.AppSettings.BestRegionSummaryFromStorage;
                settings.EnableLobbyStatistics = false;
                settings.EnableProtocolFallback = PhotonAppSettings.Global.AppSettings.EnableProtocolFallback;
                settings.Protocol = PhotonAppSettings.Global.AppSettings.Protocol;
                settings.AuthMode = (AuthModeOption)(int)PhotonAppSettings.Global.AppSettings.AuthMode;
                settings.NetworkLogging = PhotonAppSettings.Global.AppSettings.NetworkLogging;
#else
                settings.AppIdVoice = PhotonAppSettings.Instance.AppSettings.AppIdVoice;
                settings.AppVersion = PhotonAppSettings.Instance.AppSettings.AppVersion;
                settings.FixedRegion = PhotonAppSettings.Instance.AppSettings.FixedRegion;
                settings.UseNameServer = PhotonAppSettings.Instance.AppSettings.UseNameServer;
                settings.Server = PhotonAppSettings.Instance.AppSettings.Server;
                settings.Port = PhotonAppSettings.Instance.AppSettings.Port;
                settings.ProxyServer = PhotonAppSettings.Instance.AppSettings.ProxyServer;
                settings.BestRegionSummaryFromStorage = PhotonAppSettings.Instance.AppSettings.BestRegionSummaryFromStorage;
                settings.EnableLobbyStatistics = false;
                settings.EnableProtocolFallback = PhotonAppSettings.Instance.AppSettings.EnableProtocolFallback;
                settings.Protocol = PhotonAppSettings.Instance.AppSettings.Protocol;
                settings.AuthMode = (AuthModeOption)(int)PhotonAppSettings.Instance.AppSettings.AuthMode;
                settings.NetworkLogging = PhotonAppSettings.Instance.AppSettings.NetworkLogging;
#endif
            }
            else
            {
                this.Settings.CopyTo(settings);
            }
            string fusionRegion = this.networkRunner.SessionInfo.Region;
            if (string.IsNullOrEmpty(fusionRegion))
            {
                this.Logger.Log(LogLevel.Warning, "Unexpected: fusion region is empty.");
                if (!string.IsNullOrEmpty(settings.FixedRegion))
                {
                    this.Logger.Log(LogLevel.Warning, "Unexpected: fusion region is empty while voice region is set to \"{0}\". Setting it to null now.", settings.FixedRegion);
                    settings.FixedRegion = null;
                }
            }
            else if (!string.Equals(settings.FixedRegion, fusionRegion, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(settings.FixedRegion))
                {
                    this.Logger.Log(LogLevel.Info, "Setting voice region to \"{0}\" to match fusion region.", fusionRegion);
                }
                else
                {
                    this.Logger.Log(LogLevel.Info, "Switching voice region to \"{0}\" from \"{1}\" to match fusion region.", fusionRegion, settings.FixedRegion);
                }
                settings.FixedRegion = fusionRegion;
            }
            if (this.UseFusionAuthValues && this.networkRunner.AuthenticationValues != null)
            {
                this.Client.AuthValues = new AuthenticationValues(this.networkRunner.AuthenticationValues.UserId)
                {
                    AuthGetParameters = this.networkRunner.AuthenticationValues.AuthGetParameters,
                    AuthType = (CustomAuthenticationType)(int)this.networkRunner.AuthenticationValues.AuthType
                };
                if (this.networkRunner.AuthenticationValues.AuthPostData != null)
                {
                    if (this.networkRunner.AuthenticationValues.AuthPostData is byte[] byteData)
                    {
                        this.Client.AuthValues.SetAuthPostData(byteData);
                    }
                    else if (this.networkRunner.AuthenticationValues.AuthPostData is string stringData)
                    {
                        this.Client.AuthValues.SetAuthPostData(stringData);
                    }
                    else if (this.networkRunner.AuthenticationValues.AuthPostData is Dictionary<string, object> dictData)
                    {
                        this.Client.AuthValues.SetAuthPostData(dictData);
                    }
                }
            }
            return this.ConnectUsingSettings(settings);
        }

        private static void VoiceRegisterCustomTypes()
        {
            PhotonPeer.RegisterType(typeof(NetworkId), FusionNetworkIdTypeCode, SerializeFusionNetworkId, DeserializeFusionNetworkId);
        }

        private const byte FusionNetworkIdTypeCode = 0; // we need to make sure this does not clash with other custom types?

        private static object DeserializeFusionNetworkId(StreamBuffer instream, short length)
        {
            NetworkId networkId = new NetworkId();
            lock (memCompressedUInt64)
            {
                ulong ul = ReadCompressedUInt64(instream);
                networkId.Raw = (uint)ul;
            }
            return networkId;
        }

        private static ulong ReadCompressedUInt64(StreamBuffer stream)
        {
            ulong value = 0;
            int shift = 0;

            byte[] data = stream.GetBuffer();
            int offset = stream.Position;

            while (shift != 70)
            {
                if (offset >= data.Length)
                {
                    throw new System.IO.EndOfStreamException("Failed to read full ulong.");
                }

                byte b = data[offset];
                offset++;

                value |= (ulong)(b & 0x7F) << shift;
                shift += 7;
                if ((b & 0x80) == 0)
                {
                    break;
                }
            }

            stream.Position = offset;
            return value;
        }

        private static byte[] memCompressedUInt64 = new byte[10];

        private static int WriteCompressedUInt64(StreamBuffer stream, ulong value)
        {
            int count = 0;
            lock (memCompressedUInt64)
            {
                // put values in an array of bytes with variable length encoding
                memCompressedUInt64[count] = (byte)(value & 0x7F);
                value = value >> 7;
                while (value > 0)
                {
                    memCompressedUInt64[count] |= 0x80;
                    memCompressedUInt64[++count] = (byte)(value & 0x7F);
                    value = value >> 7;
                }
                count++;

                stream.Write(memCompressedUInt64, 0, count);
            }
            return count;
        }

        private static short SerializeFusionNetworkId(StreamBuffer outstream, object customobject)
        {
            NetworkId networkId = (NetworkId) customobject;
            return (short)WriteCompressedUInt64(outstream, networkId.Raw);
        }

#endregion

#region INetworkRunnerCallbacks

        void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            this.Logger.Log(LogLevel.Info, "OnPlayerJoined {0}", player);
            if (runner.LocalPlayer == player)
            {
                // Will call the VoicefollowClient start code if the runner was not yet connecting during start (not needed in normal cases)
                VoiceFollowClientStart();
                this.Logger.Log(LogLevel.Info, "Local player joined, calling VoiceConnectOrJoinRoom");
                LeaderStateChanged(ClientState.Joined);
            }
        }

        void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            this.Logger.Log(LogLevel.Info, "OnPlayerLeft {0}", player);
            if (runner.LocalPlayer == player)
            {
                this.Logger.Log(LogLevel.Info, "Local player left, calling VoiceDisconnect");
                LeaderStateChanged(ClientState.Disconnected);
            }
        }

        void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
        {
        }

        void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {
        }

        void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
        }

        void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner)
        {
            LeaderStateChanged(ClientState.ConnectedToMasterServer);
        }

#if FUSION2
        void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
#else
        void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner)
#endif
        {
            LeaderStateChanged(ClientState.Disconnected);
        }

        void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {
        }

        void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
        }

        void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {
        }

        void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
        }

        void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {
        }

        void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {
        }


        void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner)
        {
        }

        void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner)
        {
        }

 #if FUSION2
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
        }

        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
        }

        void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey reliableKey, ArraySegment<byte> data)
        {
        }

        void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey reliableKey, float progress)
        {
        }

#else
        void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data)
        {
        }
#endif
#endregion
    }
}
#endif