// ----------------------------------------------------------------------------
// <copyright file="VoiceFollowClient.cs" company="Exit Games GmbH">
// Photon Voice - Copyright (C) 2018 Exit Games GmbH
// </copyright>
// <summary>
// This class can be used to automatically join/leave Voice rooms when
// another network clinet (Leader) joins or leaves its rooms.
// </summary>
// <author>developer@photonengine.com</author>
// ----------------------------------------------------------------------------

using ExitGames.Client.Photon;
using UnityEngine;
using Photon.Realtime;
using Photon.Voice.Unity;
using System;

namespace Photon.Voice
{
    /// <summary>
    /// This class can be used to automatically sync client states between Leader and Voice clients.
    /// </summary>
    abstract public class VoiceFollowClient : VoiceConnection
    {
        abstract protected bool LeaderInRoom { get; }
        abstract protected bool LeaderOfflineMode { get; }
        abstract protected string GetVoiceRoomName();
        abstract protected bool ConnectVoice();

        #region Public Fields

        /// <summary>Auto connect voice client and join a voice room when Leader client is joined to a Leader room </summary>
        public bool AutoConnectAndJoin = true;

        #endregion

        #region Private Fields

        /// <summary>Used as deliberate disconnect / prevents automatic (re)connect when moving to state disconnected.</summary>
        /// <remarks>
        /// After a manualDisconnect, the VoiceFollowClient will go online at the next state change of Leader.
        /// To prevent that, set AutoConnectAndJoin to false.
        /// </remarks>
        private bool manualDisconnect;
        private bool errAuthOrJoin;

        #endregion
        #region Public Methods

        /// <summary>
        /// Connect voice client to Photon servers and join a Voice room
        /// </summary>
        /// <returns>If true, connection command send from client</returns>
        public bool ConnectAndJoinRoom()
        {
            if (!LeaderInRoom)
            {
                this.Logger.Log(LogLevel.Error, "Cannot connect and join if Leader is not joined.");
                return false;
            }
            if (this.ConnectVoice())
            {
                this.manualDisconnect = false;
                return true;
            }
            this.Logger.Log(LogLevel.Error, "Connecting to server failed.");
            return false;
        }

        /// <summary>
        /// Disconnect voice client from all Photon servers
        /// </summary>
        public void Disconnect()
        {
            if (!this.Client.IsConnected)
            {
                this.Logger.Log(LogLevel.Error, "Cannot Disconnect if not connected.");
                return;
            }
            this.manualDisconnect = true;
            this.Client.Disconnect();
        }

        #endregion

        #region Private Methods

        protected virtual void Start()
        {
            this.manualDisconnect = false;

            this.FollowLeader(); // in case this is enabled or activated late
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override void OnOperationResponseReceived(OperationResponse operationResponse)
        {
            // the base method only logs some error cases. this class re-implements that, so we deliberately skip calling the base method
            //base.OnOperationResponseReceived(operationResponse);

            if (operationResponse.ReturnCode != ErrorCode.Ok)
            {
                switch (operationResponse.OperationCode)
                {
                    case OperationCode.Authenticate:
                    case OperationCode.AuthenticateOnce:
                        this.Logger.Log(LogLevel.Error, "Setting AutoConnectAndJoin to false because authentication failed. Error: {0}. Message: {1}.", operationResponse.ReturnCode, operationResponse.DebugMessage);
                        this.errAuthOrJoin = true;
                        break;

                    case OperationCode.JoinGame:
                        this.Logger.Log(LogLevel.Error, "Failed to join room. RoomName: '{2}' Region: {3} Error: {0}. Message: {1}.", operationResponse.ReturnCode, operationResponse.DebugMessage, GetVoiceRoomName(), this.Client.CloudRegion);

                        // TODO: replace the following with a cooldown time. check error code if this is a temporary issue and if so, the client can try again
                        this.errAuthOrJoin = true;    // prevents re-connecting without game logic doing something
                        this.manualDisconnect = true;       // do a deliberate (manual) disconnect now as the client is already online
                        this.Disconnect();
                        break;

                    default:
                        this.Logger.Log(LogLevel.Error, "Operation {0} response error code {1} message {2}", operationResponse.OperationCode, operationResponse.ReturnCode, operationResponse.DebugMessage);
                        break;
                }
            }
        }

        protected void LeaderStateChanged(ClientState toState)
        {
            this.Logger.Log(LogLevel.Info, "OnLeaderStateChanged to {0}", toState);
            if (toState == ClientState.Joined)
            {
                //clear the error state so Voice can try to connect once
                this.errAuthOrJoin = false;
            }
            this.FollowLeader(toState);
        }

        protected override void OnVoiceStateChanged(ClientState fromState, ClientState toState)
        {
            base.OnVoiceStateChanged(fromState, toState);
            if (toState == ClientState.Disconnected)
            {
                // for a manual / deliberate disconnect, skip this specific voice-client disconnected state-change (to avoid re-connect)
                if (this.manualDisconnect)
                {
                    this.manualDisconnect = false;
                    return;     // skipping the FollowLeader()-call actually keeps this from immediate re-connect.
                }

                // TODO test rejoins and maybe add more cases we could recover
                if (this.Client.DisconnectedCause == DisconnectCause.ClientTimeout)
                {
                    bool result = this.Client.ReconnectAndRejoin();
                    if (result)
                    {
                        return;
                    }
                }

                if (this.Client.DisconnectedCause == DisconnectCause.DnsExceptionOnConnect)
                {
                    Debug.LogWarning($"Voice Disconnected and will not immediately reconnect. Cause: {this.Client.DisconnectedCause}");
                    return;
                }
            }

            this.Logger.Log(LogLevel.Debug, "OnVoiceStateChanged  from {0} to {1}", fromState, toState);
            this.FollowLeader(toState);
        }

        private void ConnectOrJoinVoice()
        {
            switch (this.ClientState)
            {
                case ClientState.PeerCreated:
                case ClientState.Disconnected:
                    this.Logger.Log(LogLevel.Info, "Leader joined room, now connecting Voice client");
                    if (!this.ConnectVoice())
                    {
                        this.Logger.Log(LogLevel.Error, "Connecting to server failed.");
                    }
                    break;
                case ClientState.ConnectedToMasterServer:
                    this.Logger.Log(LogLevel.Info, "Leader joined room, now joining Voice room");
                    if (!this.JoinVoiceRoom(GetVoiceRoomName()))
                    {
                        this.Logger.Log(LogLevel.Error, "Joining a voice room failed.");
                    }
                    break;
                default:
                    this.Logger.Log(LogLevel.Warning, "Leader joined room, Voice client is busy ({0}). Is this expected?", this.ClientState);
                    break;
            }
        }

        protected virtual bool JoinVoiceRoom(string voiceRoomName)
        {
            if (string.IsNullOrEmpty(voiceRoomName))
            {
                this.Logger.Log(LogLevel.Error, "Voice room name is null or empty.");
                return false;
            }

            var roomParams = new EnterRoomParams
            {
                RoomOptions = new RoomOptions { IsVisible = false, PlayerTtl = 2000 },
                RoomName = voiceRoomName
            };

            Debug.Log($"Calling OpJoinOrCreateRoom for room name '{voiceRoomName}' region {this.Client.CloudRegion}.");  // TODO: remove when done debugging VoiceFollowClient
            return this.Client.OpJoinOrCreateRoom(roomParams);
        }

        private void FollowLeader(ClientState toState)
        {
            switch (toState)
            {
                case ClientState.Joined:
                case ClientState.Disconnected:
                case ClientState.ConnectedToMasterServer:
                    this.Logger.Log(LogLevel.Debug, $"FollowLeader for state {toState}");
                    this.FollowLeader();
                    break;
            }
        }

        private void FollowLeader()
        {
            // no matter what usually happens, if there was a deliberate disconnect call, don't react to state changes
            if (this.manualDisconnect)
            {
                return;
            }

            // setting errAuthOrJoin to true should keep the client from automatically joining the lead room (unless this client is already on the way).
            if (this.AutoConnectAndJoin && !this.errAuthOrJoin || this.Client.IsConnected)
            {
                // if Leader is NOT in an online room, voice should disconnect
                if (!LeaderInRoom || LeaderOfflineMode)
                {
                    // voice client always disconnects for Leader's offline mode
                    if (this.Client.IsConnected && this.Client.State != ClientState.Disconnecting)
                    {
                        // this.Client.Disconnect();
                        // Remove the player from the room to avoid an error when reconnecting without rejoining with the same UserId
                        this.Client.OpLeaveRoom(false);
                    }
                    return;
                }
                // as Leader is in an online room (checked above), voice might have to follow (and check if in the correct room)
                if (!this.Client.InRoom)
                {
                    // Leader is in a room but the voice client not. follow with next steps!
                    this.ConnectOrJoinVoice();
                    return;
                }

                // if Leader is in a room and voice, too, make sure the voice room has the expected room name
                string expectedRoomName = GetVoiceRoomName();
                string currentRoomName = this.Client.CurrentRoom.Name;
                if (string.IsNullOrEmpty(currentRoomName) || !currentRoomName.Equals(expectedRoomName))
                {
                    this.Logger.Log(LogLevel.Warning,
                        "Voice room mismatch: Expected:\"{0}\" Current:\"{1}\", leaving the second to join the first.",
                        expectedRoomName, currentRoomName);
                    if (!this.Client.OpLeaveRoom(false))
                    {
                        this.Logger.Log(LogLevel.Error, "Leaving the current voice room failed.");
                    }
                }

                // if both clients are in matching rooms, everything is fine.
            }
        }

        #endregion
    }
}

