// -----------------------------------------------------------------------
// <copyright file="VoiceClient.cs" company="Exit Games GmbH">
//   Photon Voice API Framework for Photon - Copyright (C) 2017 Exit Games GmbH
// </copyright>
// <summary>
//   Photon data streaming support.
// </summary>
// <author>developer@photonengine.com</author>
// ----------------------------------------------------------------------------

using System;
using System.Linq;
using System.Collections.Generic;

namespace Photon.Voice
{
    public enum LogLevel
    {
        // start with 1 to match DebgOut enum previously used for level control in Unity integration
        Error = 1,
        Warning = 2,
        Info = 3,
        Debug = 4,
        Trace = 5,
    }

    public interface ILogger
    {
        // Voice can checks the logger level to avoid unnecessary log methods calls.
        // Return LogLevel.Trace if the logger level is unknown.
        LogLevel Level { get; }

        // Must check the level itself
        void Log(LogLevel level, string fmt, params object[] args);
    }


    public readonly struct SendFrameParams
    {
        public bool TargetMe { get; }
        public int[] TargetPlayers { get; }
        public byte InterestGroup { get; }
        public bool Reliable { get; }
        public bool Encrypt { get; }

        public SendFrameParams(bool targetMe, int[] targetPlayers, byte interestGroup, bool reliable, bool encrypt)
        {
            TargetMe = targetMe;
            TargetPlayers = targetPlayers;
            InterestGroup = interestGroup;
            Reliable = reliable;
            Encrypt = encrypt;
        }
    }

    public interface IVoiceTransport
    {
        bool IsChannelJoined(int channelId);
        // if targetMe == false and targetPlayers != null, targetPlayers are targeted
        // if targetMe == false and targetPlayers == null, all but local player are targeted
        // if targetMe == true and targetPlayers != null, targetPlayers and local player are targeted
        // if targetMe == true and targetPlayers == null, all players are targeted
        // Transport should not modify targetPlayers.
        void SendVoiceInfo(LocalVoice voice, int channelId, bool targetMe, int[] targetPlayers);
        void SendVoiceRemove(LocalVoice voice, int channelId, bool targetMe, int[] targetPlayers);
        void SendFrame(ArraySegment<byte> data, FrameFlags flags, ushort evNumber, byte frNumber, byte voiceId, int channelId, SendFrameParams par);
        string ChannelIdStr(int channelId);
        string PlayerIdStr(int playerId);
        // The maximum length of the frame data array that fits into one network packet.
        // Return <= 0 to avoid fragementing.
        int GetPayloadFragmentSize(SendFrameParams par);
    }

    /// <summary>
    /// Voice client interact with other clients on network via IVoiceTransport.
    /// </summary>
    public class VoiceClient : IDisposable
    {
        internal IVoiceTransport transport;
        internal ILogger logger;

#if UNITY_WEBGL && !UNITY_EDITOR // always disabled, ignore setter
        public bool ThreadingEnabled { get => false; set { } }
#else
        public bool ThreadingEnabled { get; set; } = true;
#endif
        /// <summary>Lost events counter (the number of empty frames sent to the deocder).</summary>
        public int EventsLost { get; internal set; }

        /// <summary>Lost frames counter (the number of empty frames sent to the deocder).</summary>
        public int FramesLost { get; internal set; }

        /// <summary>The counter of assembled frames, fragments of which are partially missing.</summary>
        public int FramesFragPart { get; internal set; }

        /// <summary>Recovered frames counter.</summary>
        public int FramesRecovered { get; internal set; }

        /// <summary>Counter of late (incorrectly ordered) frames.</summary>
        public int FramesLate { get; internal set; }

        /// <summary>Received frames counter.</summary>
        public int FramesReceived { get; private set; }

        /// <summary>Received FEC events counter.</summary>
        public int FramesReceivedFEC { get; internal set; }

        /// <summary>FEC recorery attempts counter.</summary>
        public int FramesTryFEC { get; internal set; }

        /// <summary>Received events for fragmented frames counter.</summary>
        public int FramesReceivedFragments { get; internal set; }

        /// <summary>Assembled fragmented frames counter.</summary>
        public int FramesReceivedFragmented { get; internal set; }

        /// <summary>Sent frames counter.</summary>
        public int FramesSent { get { int x = 0; foreach (var v in this.localVoices) { x += v.Value.FramesSent; } return x; } }

        /// <summary>Sent frames bytes counter.</summary>
        public int FramesSentBytes { get { int x = 0; foreach (var v in this.localVoices) { x += v.Value.FramesSentBytes; } return x; } }

        /// <summary>Average time required voice packet to return to sender.</summary>
        public int RoundTripTime { get; private set; }

        /// <summary>Average round trip time variation.</summary>
        public int RoundTripTimeVariance { get; private set; }

        /// <summary>Do not log warning when duplicate info received.</summary>
        public bool SuppressInfoDuplicateWarning { get; set; }

        /// <summary>Remote voice info event delegate.</summary>
        public delegate void RemoteVoiceInfoDelegate(int channelId, int playerId, byte voiceId, VoiceInfo voiceInfo, ref RemoteVoiceOptions options);

        /// <summary>
        /// Register a method to be called when remote voice info arrived (after join or new new remote voice creation).
        /// Metod parameters: (int channelId, int playerId, byte voiceId, VoiceInfo voiceInfo, ref RemoteVoiceOptions options);
        /// </summary>
        public RemoteVoiceInfoDelegate OnRemoteVoiceInfoAction { get; set; }

        /// <summary>Lost frames simulation ratio.</summary>
        public int DebugLostPercent { get; set; }

        /// <summary>Iterates through copy of all local voices list.</summary>
        public IEnumerable<LocalVoice> LocalVoices
        {
            get
            {
                var res = new LocalVoice[this.localVoices.Count];
                this.localVoices.Values.CopyTo(res, 0);
                return res;
            }
        }

        /// <summary>Iterates through copy of all local voices list of given channel.</summary>
        public IEnumerable<LocalVoice> LocalVoicesInChannel(int channelId)
        {
            List<LocalVoice> channelVoices;
            if (this.localVoicesPerChannel.TryGetValue(channelId, out channelVoices))
            {
                var res = new LocalVoice[channelVoices.Count];
                channelVoices.CopyTo(res, 0);
                return res;
            }
            else
            {
                return new LocalVoice[0];
            }
        }

        /// <summary>Iterates through all remote voices infos.</summary>
        public IEnumerable<RemoteVoiceInfo> RemoteVoiceInfos
        {
            get
            {
                foreach (var playerVoices in this.remoteVoices)
                {
                    foreach (var voice in playerVoices.Value)
                    {
                        yield return new RemoteVoiceInfo(voice.Value.channelId, playerVoices.Key, voice.Key, voice.Value.Info);
                    }
                }
            }
        }

        public void LogSpacingProfiles()
        {
            foreach (var voice in this.localVoices)
            {
                voice.Value.SendSpacingProfileStart(); // in case it's not started yet
                this.logger.Log(LogLevel.Info, voice.Value.LogPrefix + " ev. prof.: " + voice.Value.SendSpacingProfileDump);
            }
            foreach (var playerVoices in this.remoteVoices)
            {
                foreach (var voice in playerVoices.Value)
                {
                    voice.Value.ReceiveSpacingProfileStart(); // in case it's not started yet
                    this.logger.Log(LogLevel.Info, voice.Value.LogPrefix + " ev. prof.: " + voice.Value.ReceiveSpacingProfileDump);
                }
            }
        }

        public void LogStats()
        {
            int dc = FrameBuffer.statDisposerCreated;
            int dd = FrameBuffer.statDisposerDisposed;
            int pp = FrameBuffer.statPinned;
            int pu = FrameBuffer.statUnpinned;
            this.logger.Log(LogLevel.Info, "[PV] FrameBuffer stats Disposer: " + dc + " - " + dd + " = " + (dc - dd));
            this.logger.Log(LogLevel.Info, "[PV] FrameBuffer stats Pinned: " + pp + " - " + pu + " = " + (pp - pu));
        }

        public void SetRemoteVoiceDelayFrames(Codec codec, int delayFrames)
        {
            remoteVoiceDelayFramesPerCodec[codec] = delayFrames;
            foreach (var playerVoices in this.remoteVoices)
            {
                foreach (var voice in playerVoices.Value)
                {
                    if (codec == voice.Value.Info.Codec)
                    {
                        voice.Value.DelayFrames = delayFrames;
                    }
                }
            }
        }

        public void SetRemoteVoiceDelayFrames(Func<Codec, bool> predicate, int delayFrames)
        {
            foreach (var c in System.Enum.GetValues(typeof(Photon.Voice.Codec)))
            {
                var codec = (Photon.Voice.Codec)c;
                if (predicate(codec))
                {
                    remoteVoiceDelayFramesPerCodec[codec] = delayFrames;
                }
            }

            foreach (var playerVoices in this.remoteVoices)
            {
                foreach (var voice in playerVoices.Value)
                {
                    if (predicate(voice.Value.Info.Codec))
                    {
                        voice.Value.DelayFrames = delayFrames;
                    }
                }
            }

        }

        // store delay to apply on new remote voices
        private Dictionary<Codec, int> remoteVoiceDelayFramesPerCodec = new Dictionary<Codec, int>();

        public struct CreateOptions
        {
            public byte VoiceIDMin;
            public byte VoiceIDMax;

            static public CreateOptions Default = new CreateOptions()
            {
                VoiceIDMin = 1, // 0 means invalid id
                VoiceIDMax = 15 // preserve ids for other clients creating voices for the same player (server plugin)
            };
        }

        /// <summary>Creates VoiceClient instance</summary>
        public VoiceClient(IVoiceTransport transport, ILogger logger, CreateOptions opt = default(CreateOptions))
        {
            this.transport = transport;
            this.logger = logger;
            if (opt.Equals(default(CreateOptions)))
            {
                opt = CreateOptions.Default;
            }
            this.voiceIDMin = opt.VoiceIDMin;
            this.voiceIDMax = opt.VoiceIDMax;
            this.voiceIdLast = this.voiceIDMax;
        }

        /// <summary>
        /// This method dispatches all available incoming commands and then sends this client's outgoing commands.
        /// Call this method regularly (2..20 times a second).
        /// </summary>
        public void Service()
        {
            foreach (var v in localVoices)
            {
                v.Value.service();
            }
        }

        private LocalVoice createLocalVoice(int channelId, Func<byte, int, LocalVoice> voiceFactory)
        {
            var newId = getNewVoiceId();
            if (newId != 0)
            {
                LocalVoice v = voiceFactory(newId, channelId);
                if (v != null)
                {
                    addVoice(newId, channelId, v);
                    this.logger.Log(LogLevel.Info, v.LogPrefix + " added enc: " + v.Info.ToString());
                    return v;
                }
            }

            return null;
        }
        /// <summary>
        /// Creates basic outgoing stream w/o data processing support. Provided encoder should generate output data stream.
        /// </summary>
        /// <param name="voiceInfo">Outgoing stream parameters.</param>
        /// <param name="channelId">Transport channel specific to transport.</param>
        /// <param name="options">Voice creation options.</param>
        /// <returns>Outgoing stream handler.</returns>
        public LocalVoice CreateLocalVoice(VoiceInfo voiceInfo, int channelId, VoiceCreateOptions options = default(VoiceCreateOptions))
        {
            return (LocalVoice)createLocalVoice(channelId, (vId, chId) => new LocalVoice(this, vId, voiceInfo, channelId, options));
        }

        public LocalVoiceAudio<T> CreateLocalVoiceAudio<T>(VoiceInfo voiceInfo, IAudioDesc audioSourceDesc, int channelId, VoiceCreateOptions options = default(VoiceCreateOptions))
        {
            return (LocalVoiceAudio<T>)createLocalVoice(channelId, (vId, chId) => LocalVoiceAudio<T>.Create(this, vId, voiceInfo, audioSourceDesc, channelId, options));
        }

        /// <summary>
        /// Creates outgoing audio stream of type automatically assigned and adds procedures (callback or serviceable) for consuming given audio source data.
        /// Adds audio specific features (e.g. resampling, level meter) to processing pipeline and to returning stream handler.
        /// </summary>
        /// <param name="voiceInfo">Outgoing stream parameters.</param>
        /// <param name="source">Streaming audio source.</param>
        /// <param name="sampleType">Voice's audio sample type. If does not match source audio sample type, conversion will occur.</param>
        /// <param name="channelId">Transport channel specific to transport.</param>
        /// <param name="options">Voice creation options.</param>
        /// <returns>Outgoing stream handler.</returns>
        /// <remarks>
        /// audioSourceDesc.SamplingRate and voiceInfo.SamplingRate may do not match. Automatic resampling will occur in this case.
        /// </remarks>
        public LocalVoice CreateLocalVoiceAudioFromSource(VoiceInfo voiceInfo, IAudioDesc source, AudioSampleType sampleType, int channelId, VoiceCreateOptions options = default(VoiceCreateOptions))
        {
            // resolve AudioSampleType.Source to concrete type for encoder creation
            if (sampleType == AudioSampleType.Source)
            {
                if (source is IAudioPusher<float> || source is IAudioReader<float>)
                {
                    sampleType = AudioSampleType.Float;
                }
                else if (source is IAudioPusher<short> || source is IAudioReader<short>)
                {
                    sampleType = AudioSampleType.Short;
                }
            }

            if (options.Encoder == null)
            {
                switch (sampleType)
                {
                    case AudioSampleType.Float:
                        options.Encoder = Platform.CreateDefaultAudioEncoder<float>(logger, voiceInfo);
                        break;
                    case AudioSampleType.Short:
                        options.Encoder = Platform.CreateDefaultAudioEncoder<short>(logger, voiceInfo);
                        break;
                }
            }

            if (source is IAudioPusher<float>)
            {
                if (sampleType == AudioSampleType.Short)
                {
                    logger.Log(LogLevel.Info, "[PV] Creating local voice with source samples type conversion from IAudioPusher float to short.");
                    var localVoice = CreateLocalVoiceAudio<short>(voiceInfo, source, channelId, options);
                    // we can safely reuse the same buffer in callbacks from native code
                    //
                    var sourceBufFactory = new ArrayPoolSet<float>(1, "Source Float To Short", localVoice.OptimalSourceFrameSize, 5);
                    ((IAudioPusher<float>)source).SetCallback(buf =>
                    {
                        var shortBuf = localVoice.BufferFactory.New(buf.Length);
                        AudioUtil.Convert(buf, shortBuf, buf.Length);
                        sourceBufFactory.Free(buf, buf.Length);
                        localVoice.PushDataAsync(shortBuf);
                    }, sourceBufFactory, localVoice.OptimalSourceFrameSize);
                    return localVoice;
                }
                else
                {
                    var localVoice = CreateLocalVoiceAudio<float>(voiceInfo, source, channelId, options);
                    ((IAudioPusher<float>)source).SetCallback(buf => localVoice.PushDataAsync(buf), localVoice.BufferFactory, localVoice.OptimalSourceFrameSize);
                    return localVoice;
                }
            }
            else if (source is IAudioPusher<short>)
            {
                if (sampleType == AudioSampleType.Float)
                {
                    logger.Log(LogLevel.Info, "[PV] Creating local voice with source samples type conversion from IAudioPusher short to float.");
                    var localVoice = CreateLocalVoiceAudio<float>(voiceInfo, source, channelId, options);
                    // we can safely reuse the same buffer in callbacks from native code
                    //
                    var sourceBufFactory = new ArrayPoolSet<short>(1, "Source Short To Float", localVoice.OptimalSourceFrameSize, 5);
                    ((IAudioPusher<short>)source).SetCallback(buf =>
                    {
                        var floatBuf = localVoice.BufferFactory.New(buf.Length);
                        AudioUtil.Convert(buf, floatBuf, buf.Length);
                        sourceBufFactory.Free(buf, buf.Length);
                        localVoice.PushDataAsync(floatBuf);
                    }, sourceBufFactory, localVoice.OptimalSourceFrameSize);
                    return localVoice;
                }
                else
                {
                    var localVoice = CreateLocalVoiceAudio<short>(voiceInfo, source, channelId, options);
                    ((IAudioPusher<short>)source).SetCallback(buf => localVoice.PushDataAsync(buf), localVoice.BufferFactory, localVoice.OptimalSourceFrameSize);
                    return localVoice;
                }
            }
            else if (source is IAudioReader<float>)
            {
                if (sampleType == AudioSampleType.Short)
                {
                    logger.Log(LogLevel.Info, "[PV] Creating local voice with source samples type conversion from IAudioReader float to short.");
                    var localVoice = CreateLocalVoiceAudio<short>(voiceInfo, source, channelId, options);
                    localVoice.LocalUserServiceable = new BufferReaderPushAdapterAsyncPoolFloatToShort(source as IAudioReader<float>);
                    return localVoice;
                }
                else
                {
                    var localVoice = CreateLocalVoiceAudio<float>(voiceInfo, source, channelId, options);
                    localVoice.LocalUserServiceable = new BufferReaderPushAdapterAsyncPool<float>(source as IAudioReader<float>);
                    return localVoice;
                }
            }
            else if (source is IAudioReader<short>)
            {
                if (sampleType == AudioSampleType.Float)
                {
                    logger.Log(LogLevel.Info, "[PV] Creating local voice with source samples type conversion from IAudioReader short to float.");
                    var localVoice = CreateLocalVoiceAudio<float>(voiceInfo, source, channelId, options);
                    localVoice.LocalUserServiceable = new BufferReaderPushAdapterAsyncPoolShortToFloat(source as IAudioReader<short>);
                    return localVoice;
                }
                else
                {
                    var localVoice = CreateLocalVoiceAudio<short>(voiceInfo, source, channelId, options);
                    localVoice.LocalUserServiceable = new BufferReaderPushAdapterAsyncPool<short>(source as IAudioReader<short>);
                    return localVoice;
                }
            }
            else
            {
                logger.Log(LogLevel.Error, "[PV] CreateLocalVoiceAudioFromSource does not support Voice.IAudioDesc of type {0}", source.GetType());
                return LocalVoiceAudioDummy.Dummy;
            }
        }

#if PHOTON_VOICE_VIDEO_ENABLE
        /// <summary>
        /// Creates outgoing video stream consuming sequence of image buffers.
        /// </summary>
        /// <param name="voiceInfo">Outgoing stream parameters.</param>
        /// <param name="recorder">Video recorder.</param>
        /// <param name="channelId">Transport channel specific to transport.</param>
        /// <param name="options">Voice creation options.</param>
        /// <returns>Outgoing stream handler.</returns>
        public LocalVoiceVideo CreateLocalVoiceVideo(VoiceInfo voiceInfo, IVideoRecorder recorder, int channelId, VoiceCreateOptions options = default(VoiceCreateOptions))
        {
            options.Encoder = recorder.Encoder;
            var lv = (LocalVoiceVideo)createLocalVoice(channelId, (vId, chId) => new LocalVoiceVideo(this, vId, voiceInfo, channelId, options));
            if (recorder is IVideoRecorderPusher)
            {
                (recorder as IVideoRecorderPusher).VideoSink = lv;
            }
            return lv;
        }
#endif

        private byte voiceIDMin;
        private byte voiceIDMax;
        private byte voiceIdLast; // inited with voiceIDMax: the first id will be voiceIDMin

        private byte idInc(byte id)
        {
            return id == voiceIDMax ? voiceIDMin : (byte)(id + 1);
        }

        private byte getNewVoiceId()
        {
            var used = new bool[256];
            foreach (var v in localVoices)
            {
                used[v.Value.id] = true;
            }
            for (byte id = idInc(voiceIdLast); id != voiceIdLast; id = idInc(id))
            {
                if (!used[id])
                {
                    voiceIdLast = id;
                    return id;
                }
            }
            return 0;
        }

        void addVoice(byte newId, int channelId, LocalVoice v)
        {
            localVoices[newId] = v;

            List<LocalVoice> voiceList;
            if (!localVoicesPerChannel.TryGetValue(channelId, out voiceList))
            {
                voiceList = new List<LocalVoice>();
                localVoicesPerChannel[channelId] = voiceList;
            }
            voiceList.Add(v);

            if (this.transport.IsChannelJoined(channelId))
            {
                v.sendVoiceInfoAndConfigFrame();
            }
        }
        /// <summary>
        /// Removes local voice (outgoing data stream).
        /// <param name="voice">Handler of outgoing stream to be removed.</param>
        /// </summary>
        public void RemoveLocalVoice(LocalVoice voice)
        {
            this.localVoices.Remove(voice.id);

            this.localVoicesPerChannel[voice.channelId].Remove(voice);
            if (this.transport.IsChannelJoined(voice.channelId))
            {
                voice.onLeaveChannel();
            }

            voice.Dispose();
            this.logger.Log(LogLevel.Info, voice.LogPrefix + " removed");
        }

#region nonpublic

        private Dictionary<byte, LocalVoice> localVoices = new Dictionary<byte, LocalVoice>();
        private Dictionary<int, List<LocalVoice>> localVoicesPerChannel = new Dictionary<int, List<LocalVoice>>();
        // player id -> voice id -> voice
        private Dictionary<int, Dictionary<byte, RemoteVoice>> remoteVoices = new Dictionary<int, Dictionary<byte, RemoteVoice>>();

        private void clearRemoteVoices()
        {
            foreach (var playerVoices in remoteVoices)
            {
                foreach (var voice in playerVoices.Value)
                {
                    voice.Value.removeAndDispose();
                }
            }
            remoteVoices.Clear();
            this.logger.Log(LogLevel.Info, "[PV] Remote voices cleared");
        }

        private void clearRemoteVoicesInChannel(int channelId)
        {
            foreach (var playerVoices in remoteVoices)
            {
                List<byte> toRemove = new List<byte>();
                foreach (var voice in playerVoices.Value)
                {
                    if (voice.Value.channelId == channelId)
                    {
                        voice.Value.removeAndDispose();
                        toRemove.Add(voice.Key);
                    }
                }
                foreach (var id in toRemove)
                {
                    playerVoices.Value.Remove(id);
                }
            }
            this.logger.Log(LogLevel.Info, "[PV] Remote voices for channel " + this.channelStr(channelId) + " cleared");
        }

        private void clearRemoteVoicesInChannelForPlayer(int channelId, int playerId)
        {
            Dictionary<byte, RemoteVoice> playerVoices = null;
            if (remoteVoices.TryGetValue(playerId, out playerVoices))
            {
                List<byte> toRemove = new List<byte>();
                foreach (var v in playerVoices)
                {
                    if (v.Value.channelId == channelId)
                    {
                        v.Value.removeAndDispose();
                        toRemove.Add(v.Key);
                    }
                }
                foreach (var id in toRemove)
                {
                    playerVoices.Remove(id);
                }
            }
        }

        public void onJoinChannel(int channelId)
        {
            if (this.localVoicesPerChannel.TryGetValue(channelId, out List<LocalVoice> voiceList))
            {
                foreach (var v in voiceList)
                {
                    v.onJoinChannel();
                }
            }
        }

        public void onJoinAllChannels()
        {
            foreach (var v in localVoices)
            {
                v.Value.onJoinChannel();
            }
        }

        public void onLeaveChannel(int channel)
        {
            clearRemoteVoicesInChannel(channel);
        }

        public void onLeaveAllChannels()
        {
            clearRemoteVoices();
        }

        public void onPlayerJoin(int channelId, int playerId)
        {
            List<LocalVoice> voiceList;
            if (this.localVoicesPerChannel.TryGetValue(channelId, out voiceList))
            {
                foreach (var v in voiceList)
                {
                    v.onPlayerJoin(playerId);
                }
            }
        }

        // Joins all channels
        public void onPlayerJoin(int playerId)
        {
            foreach (var v in localVoices)
            {
                v.Value.onPlayerJoin(playerId);
            }
        }

        public void onPlayerLeave(int channelId, int playerId)
        {
            clearRemoteVoicesInChannelForPlayer(channelId, playerId);
        }

        // Leaves all channels
        public void onPlayerLeave(int playerId)
        {
            Dictionary<byte, RemoteVoice> playerVoices;
            if (remoteVoices.TryGetValue(playerId, out playerVoices))
            {
                List<byte> toRemove = new List<byte>();
                foreach (var v in playerVoices)
                {
                    v.Value.removeAndDispose();
                    toRemove.Add(v.Key);
                }
                foreach (var id in toRemove)
                {
                    playerVoices.Remove(id);
                }
            }
        }

        public void onVoiceInfo(int channelId, int playerId, byte voiceId, int eventBufferSize, VoiceInfo info)
        {
            Dictionary<byte, RemoteVoice> playerVoices = null;

            if (!remoteVoices.TryGetValue(playerId, out playerVoices))
            {
                playerVoices = new Dictionary<byte, RemoteVoice>();
                remoteVoices[playerId] = playerVoices;
            }

            if (!playerVoices.ContainsKey(voiceId))
            {
                var voiceStr = " p#" + this.playerStr(playerId) + " v#" + voiceId + " ch#" + channelStr(channelId);
                this.logger.Log(LogLevel.Info, "[PV] " + voiceStr + " Info received: " + info.ToString() + " evbs=" + eventBufferSize);

                var logPrefix = "[PV] Remote " + info.Codec + voiceStr;
                RemoteVoiceOptions options = new RemoteVoiceOptions(logger, logPrefix, info);
                if (this.OnRemoteVoiceInfoAction != null)
                {
                    this.OnRemoteVoiceInfoAction(channelId, playerId, voiceId, info, ref options);
                }
                var rv = new RemoteVoice(this, options, channelId, playerId, voiceId, info, eventBufferSize);
                playerVoices[voiceId] = rv;
                int delayFrames;
                if (remoteVoiceDelayFramesPerCodec.TryGetValue(info.Codec, out delayFrames))
                {
                    rv.DelayFrames = delayFrames;
                }
            }
            else
            {
                if (!this.SuppressInfoDuplicateWarning)
                {
                    this.logger.Log(LogLevel.Warning, "[PV] Info duplicate for voice #" + voiceId + " of player " + this.playerStr(playerId) + " at channel " + this.channelStr(channelId));
                }
            }
        }

        public void onVoiceRemove(int playerId, byte[] voiceIds)
        {
            Dictionary<byte, RemoteVoice> playerVoices = null;
            if (remoteVoices.TryGetValue(playerId, out playerVoices))
            {
                foreach (var voiceId in voiceIds)
                {
                    if (playerVoices.TryGetValue(voiceId, out RemoteVoice voice))
                    {
                        playerVoices.Remove(voiceId);
                        this.logger.Log(LogLevel.Info, "[PV] Remote voice #" + voiceId + " of player " + this.playerStr(playerId) + " at channel " + this.channelStr(voice.channelId) + " removed");
                        voice.removeAndDispose();
                    }
                    else
                    {
                        this.logger.Log(LogLevel.Warning, "[PV] Remote voice #" + voiceId + " of player " + this.playerStr(playerId) + " not found when trying to remove");
                    }
                }
            }
            else
            {
                this.logger.Log(LogLevel.Warning, "[PV] Remote voice list of player " + this.playerStr(playerId) + " not found when trying to remove voice(s)");
            }
        }

        Random rnd = new Random();
#if PV_DEBUG_ECHO_RTT_MEASURE
        private int prevRtt = 0;
#endif
        public void onFrame(int playerId, byte voiceId, ushort evNumber, ref FrameBuffer receivedBytes, bool isLocalPlayer)
        {
#if PV_DEBUG_ECHO_RTT_MEASURE
            if (isLocalPlayer)
            {
                // rtt measurement in debug echo mode
                LocalVoice voice;
                if (this.localVoices.TryGetValue(voiceId, out voice))
                {
                    int sendTime;
                    if (voice.eventTimestamps.TryGetValue(evNumber, out sendTime))
                    {
                        int rtt = Environment.TickCount - sendTime;
                        int rttvar = rtt - prevRtt;
                        prevRtt = rtt;
                        if (rttvar < 0) rttvar = -rttvar;
                        this.RoundTripTimeVariance = (rttvar + RoundTripTimeVariance * 19) / 20;
                        this.RoundTripTime = (rtt + RoundTripTime * 19) / 20;
                    }
                }
                //internal Dictionary<byte, DateTime> localEventTimestamps = new Dictionary<byte, DateTime>();
            }
#endif
            if (this.DebugLostPercent > 0 && rnd.Next(100) < this.DebugLostPercent)
            {
                this.logger.Log(LogLevel.Warning, "[PV] Debug Lost Sim: 1 packet dropped");
                return;
            }

            FramesReceived++;

            if (remoteVoices.TryGetValue(playerId, out var playerVoices))
            {

                if (playerVoices.TryGetValue(voiceId, out var voice))
                {
                    voice.receiveBytes(ref receivedBytes, evNumber);
                }
                else
                {
                    this.logger.Log(LogLevel.Warning, "[PV] Frame event for not inited voice #" + voiceId + " of player " + this.playerStr(playerId));
                }
            }
            else
            {
                this.logger.Log(LogLevel.Warning, "[PV] Frame event for voice #" + voiceId + " of not inited player " + this.playerStr(playerId));
            }
        }

        internal string channelStr(int channelId)
        {
            var str = this.transport.ChannelIdStr(channelId);
            if (str != null)
            {
                return channelId + "(" + str + ")";
            }
            else
            {
                return channelId.ToString();
            }
        }

        internal string playerStr(int playerId)
        {
            var str = this.transport.PlayerIdStr(playerId);
            if (str != null)
            {
                return playerId + "(" + str + ")";
            }
            else
            {
                return playerId.ToString();
            }
        }
        //public string ToStringFull()
        //{
        //    return string.Format("Photon.Voice.Client, local: {0}, remote: {1}",  localVoices.Count, remoteVoices.Count);
        //}

#endregion

        public void Dispose()
        {
            foreach (var v in this.localVoices)
            {
                v.Value.Dispose();
            }
            foreach (var playerVoices in remoteVoices)
            {
                foreach (var voice in playerVoices.Value)
                {
                    voice.Value.Dispose();
                }
            }
        }
    }
}