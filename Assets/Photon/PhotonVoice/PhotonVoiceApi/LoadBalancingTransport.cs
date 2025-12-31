// -----------------------------------------------------------------------
// <copyright file="LoadBalancingTransport.cs" company="Exit Games GmbH">
//   Photon Voice API Framework for Photon - Copyright (C) 2015 Exit Games GmbH
// </copyright>
// <summary>
//   Extends Photon Realtime API with media streaming functionality.
// </summary>
// <author>developer@photonengine.com</author>
// ----------------------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Realtime;

namespace Photon.Voice
{
    class VoiceEvent
    {
        /// <summary>
        /// Single event used for voice communications.
        /// </summary>
        /// Change if it conflicts with other event codes used in the same Photon room.
        public const byte Code = 202; // all photon voice events use single event code
        public const byte FrameCode = 203; // LoadBalancingTransport2 uses separate code for frame event serialized as byte[]
    }

    /// <summary>
    /// Extends LoadBalancingClient with media streaming functionality.
    /// </summary>
    /// <remarks>
    /// Use your normal LoadBalancing workflow to join a Voice room.
    /// All standard LoadBalancing features are available.
    /// Use <see cref="VoiceClient"/> to work with media streams.
    /// </remarks>
    public class LoadBalancingTransport : LoadBalancingClient, IVoiceTransport, IDisposable
    {
        // Channel is used only by local voice (to specify Enet channel) and ignored by remote voices which are all on the same channel.
        // Join / leave per channel is not supported.
        // It's important to call onJoinAllChannels() instead of onJoinChannel() to avoid ignoring local non-0 channel voices.
        internal const int REMOTE_VOICE_CHANNEL = 0;

        public virtual int GetPayloadFragmentSize(SendFrameParams par)
        {
            // rough estimate, no need to improve because this transport is obsolete
            int overhead = 3 * 2; // possible InterestGroup and Receivers: key, type, value
            if (par.TargetPlayers != null)
            {
                overhead += 3 + par.TargetPlayers.Length; // key, type, compressed length and array
            }

            return 1114 - overhead; // <- protocol 18 theoretical encoded; experimental encrypted: 1115, non-encrypted: 1130
        }

        /// <summary>The <see cref="VoiceClient"></see> implementation associated with this LoadBalancingTransport.</summary>
        public VoiceClient VoiceClient { get { return this.voiceClient; } }

        protected VoiceClient voiceClient;
        private PhotonTransportProtocol protocol;
        protected readonly bool cppCompatibilityMode;
        protected readonly ILogger logger;

        public bool IsChannelJoined(int channelId) { return this.State == ClientState.Joined; }

        /// <summary>
        /// Initializes a new <see cref="LoadBalancingTransport"/>.
        /// </summary>
        /// <param name="logger">ILogger instance. If null, this instance LoadBalancingClient.DebugReturn implementation is used.<see cref="ConnectionProtocol"></see></param>
        /// <param name="connectionProtocol">Connection protocol (UDP or TCP). <see cref="ConnectionProtocol"></see></param>
        /// <param name="cppCompatibilityMode">Use a protocol compatible with Voice C++ API.</param>
        public LoadBalancingTransport(ILogger logger = null, ConnectionProtocol connectionProtocol = ConnectionProtocol.Udp, bool cppCompatibilityMode = false) : base(connectionProtocol)
        {
            if (logger == null)
            {
                logger = new LBCLogger(this);
            }
            this.ClientType = ClientAppType.Voice;
            this.cppCompatibilityMode = cppCompatibilityMode;
            base.EventReceived += onEventActionVoiceClient;
            base.StateChanged += onStateChangeVoiceClient;
            this.voiceClient = new VoiceClient(this, logger);
            // Pre-allocate a channel for each stream type, assuming the recommended channel setup is used (1 = audio, 2 = video, 3 = screen share).
            if (LoadBalancingPeer.ChannelCount < 4)
            {
                this.LoadBalancingPeer.ChannelCount = 4;
            }
            this.protocol = new PhotonTransportProtocol(voiceClient, logger);
            this.logger = logger;
        }

        /// <summary>
        /// This method dispatches all available incoming commands and then sends this client's outgoing commands.
        /// Call this method regularly (2 to 20 times a second).
        /// </summary>
        new public void Service()
        {
            base.Service();
            this.voiceClient.Service();
        }

        [Obsolete("Use LoadBalancingPeer::OpChangeGroups().")]
        public virtual bool ChangeAudioGroups(byte[] groupsToRemove, byte[] groupsToAdd)
        {
            return this.LoadBalancingPeer.OpChangeGroups(groupsToRemove, groupsToAdd);
        }

        // Photon transport specific:
        // Empty TargetActors is the same as null: sending to all except the local client.
        // if TargetActors is not null and non-empty, InterestGroup and ReceiverGroup are ignored
        // if TargetActors is null or empty and InterestGroup is set, ReceiverGroup is ignored
        RaiseEventOptions buildEvOptFromTargets(bool targetMe, int[] targetPlayers)
        {
            var opt = new RaiseEventOptions();
            if (targetMe)
            {
                if (targetPlayers == null) // all others and me
                {
                    opt.Receivers = ReceiverGroup.All;
                }
                else if (targetPlayers.Length == 0) // only me
                {
                    opt.TargetActors = new int[] { this.LocalPlayer.ActorNumber };
                }
                else // some others and me
                {
                    opt.TargetActors = new int[targetPlayers.Length + 1];
                    Array.Copy(targetPlayers, opt.TargetActors, targetPlayers.Length);
                    opt.TargetActors[targetPlayers.Length] = this.LocalPlayer.ActorNumber;
                }
            }
            else
            {
                if (opt.TargetActors != null && opt.TargetActors.Length == 0) // Voice Core does not do such calls but better check again because LoadBalancing sends to all except the local client if the list is empty
                {
                    throw new ArgumentException("LoadBalancingTransport: no targets specified in Send* method call");
                }
                opt.TargetActors = targetPlayers;
            }

            return opt;
        }
        #region nonpublic

        public void SendVoiceInfo(LocalVoice voice, int channelId, bool targetMe, int[] targetPlayers)
        {
            object content = protocol.buildVoicesInfo(voice);

            var sendOpt = new SendOptions()
            {
                DeliveryMode = DeliveryMode.Reliable,
                Channel = (byte)channelId,
            };

            var opt = buildEvOptFromTargets(targetMe, targetPlayers);

            this.OpRaiseEvent(VoiceEvent.Code, content, opt, sendOpt);
        }

        public void SendVoiceRemove(LocalVoice voice, int channelId, bool targetMe, int[] targetPlayers)
        {
            object content = protocol.buildVoiceRemoveMessage(voice);
            var sendOpt = new SendOptions()
            {
                DeliveryMode = DeliveryMode.Reliable,
                Channel = (byte)channelId,
            };

            var opt = buildEvOptFromTargets(targetMe, targetPlayers);

            this.OpRaiseEvent(VoiceEvent.Code, content, opt, sendOpt);
        }

        protected virtual byte FrameCode => VoiceEvent.Code;

        protected virtual object buildFrameMessage(byte voiceId, ushort evNumber, byte frNumber, ArraySegment<byte> data, FrameFlags flags)
        {
            return protocol.buildFrameMessage(voiceId, evNumber, frNumber, data, flags);
        }

        public void SendFrame(ArraySegment<byte> data, FrameFlags flags, ushort evNumber, byte frNumber, byte voiceId, int channelId, SendFrameParams par)
        {
            object content = buildFrameMessage(voiceId, evNumber, frNumber, data, flags);

            var sendOpt = new SendOptions()
            {
                DeliveryMode = ((flags & FrameFlags.Config) != 0) ? DeliveryMode.Reliable : // config frame should be send in sync with voice info
                    cppCompatibilityMode ?
                        par.Reliable ? DeliveryMode.Reliable : DeliveryMode.Unreliable :
                        par.Reliable ? DeliveryMode.ReliableUnsequenced : DeliveryMode.UnreliableUnsequenced,
                Channel = (byte)channelId,
                Encrypt = par.Encrypt,
            };

            var opt = buildEvOptFromTargets(par.TargetMe, par.TargetPlayers);

            opt.InterestGroup = par.InterestGroup;

            this.OpRaiseEvent(FrameCode, content, opt, sendOpt);
            while (this.LoadBalancingPeer.SendOutgoingCommands()) ;
        }

        public string ChannelIdStr(int channelId) { return null; }
        public string PlayerIdStr(int playerId) { return null; }

        protected virtual void onEventActionVoiceClient(EventData ev)
        {
            // check for voice event first
            if (ev.Code == VoiceEvent.Code)
            {
                // Payloads are arrays. If first array element is 0 than next is event subcode. Otherwise, the event is data frame with voiceId in 1st element.
                protocol.onVoiceEvent(ev[(byte)ParameterCode.CustomEventContent], REMOTE_VOICE_CHANNEL, ev.Sender, ev.Sender == this.LocalPlayer.ActorNumber);
            }
            else
            {
                int playerId;
                switch (ev.Code)
                {
                    case (byte)EventCode.Join:
                        playerId = ev.Sender;
                        if (playerId == this.LocalPlayer.ActorNumber)
                        {
                        }
                        else
                        {
                            this.voiceClient.onPlayerJoin(playerId);
                        }
                        break;
                    case (byte)EventCode.Leave:
                    {
                        playerId = ev.Sender;
                        if (playerId == this.LocalPlayer.ActorNumber)
                        {
                            this.voiceClient.onLeaveAllChannels();
                        }
                        else
                        {
                            this.voiceClient.onPlayerLeave(playerId);
                        }
                    }
                    break;
                }
            }
        }

        void onStateChangeVoiceClient(ClientState fromState, ClientState state)
        {
            switch (fromState)
            {
                case ClientState.Joined:
                    this.voiceClient.onLeaveAllChannels();
                    break;
            }

            switch (state)
            {
                case ClientState.Joined:
                    this.voiceClient.onJoinAllChannels();
                    break;
            }
        }

        #endregion

        /// <summary>
        /// Releases all resources used by the <see cref="LoadBalancingTransport"/> instance.
        /// </summary>
        public void Dispose()
        {
            this.voiceClient.Dispose();
        }

        // Allows the underlying LoadBalancingClient.DebugReturn() logger to be used as a Transport/Voice logger.
        class LBCLogger : ILogger
        {
            LoadBalancingTransport lbt;
            public LBCLogger(LoadBalancingTransport lbt)
            {
                this.lbt = lbt;
            }

            public LogLevel Level
            {
                get
                {
                    if (lbt.LoadBalancingPeer.DebugOut == DebugLevel.INFO) return LogLevel.Info;
                    if (lbt.LoadBalancingPeer.DebugOut == DebugLevel.WARNING) return LogLevel.Warning;
                    if (lbt.LoadBalancingPeer.DebugOut <= DebugLevel.ERROR) return LogLevel.Error;
                    return LogLevel.Trace;
                }
            }

            public void Log(LogLevel level, string fmt, params object[] args)
            {
                if (this.Level >= level)
                {
                    DebugLevel debugOut = DebugLevel.ALL;
                    if (level == LogLevel.Info) debugOut = DebugLevel.INFO;
                    else if (level == LogLevel.Warning) debugOut = DebugLevel.WARNING;
                    else if (level == LogLevel.Error) debugOut = DebugLevel.ERROR;
                    lbt.DebugReturn(debugOut, string.Format(fmt, args));
                }
            }
        }

    }
}
