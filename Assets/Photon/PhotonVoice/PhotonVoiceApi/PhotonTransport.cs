using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Photon.Voice
{
    // for convenience, it also calls VoiceClient payload handlers
    internal class PhotonTransportProtocol
    {
        enum EventSubcode : byte
        {
            VoiceInfo = 1,
            VoiceRemove = 2,
            Frame = 3,
        }

        enum EventParam : byte
        {
            VoiceId = 1,
            SamplingRate = 2,
            Channels = 3,
            FrameDurationUs = 4,
            Bitrate = 5,
            Width = 6,
            Height = 7,
            FPS = 8,
            KeyFrameInt = 9,
            UserData = 10,
            EventNumber = 11,
            Codec = 12,
            EventBufferSize = 13,
        }

        private VoiceClient voiceClient;
        private ILogger logger;

        public PhotonTransportProtocol(VoiceClient voiceClient, ILogger logger)
        {
            this.voiceClient = voiceClient;
            this.logger = logger;
        }

        internal object[] buildVoicesInfo(LocalVoice v)
        {
            object[] infos = new object[1];

            object[] content = new object[] { (byte)0, EventSubcode.VoiceInfo, infos };
            infos[0] = new Dictionary<byte, object>() {
                { (byte)EventParam.VoiceId, v.ID },
                { (byte)EventParam.Codec, v.Info.Codec },
                { (byte)EventParam.SamplingRate, v.Info.SamplingRate },
                { (byte)EventParam.Channels, v.Info.Channels },
                { (byte)EventParam.FrameDurationUs, v.Info.FrameDurationUs },
                { (byte)EventParam.Bitrate, v.Info.Bitrate },
                { (byte)EventParam.Width, v.Info.Width },
                { (byte)EventParam.Height, v.Info.Height },
                { (byte)EventParam.FPS, v.Info.FPS },
                { (byte)EventParam.KeyFrameInt, v.Info.KeyFrameInt },
                { (byte)EventParam.UserData, v.Info.UserData },
                { (byte)EventParam.EventNumber, (byte)0 }, // backward compatibility: old clients read it but use it only for logging
                { (byte)EventParam.EventBufferSize, v.EventBufferSize }
            };
            return content;
        }

        internal object[] buildVoiceRemoveMessage(LocalVoice v)
        {
            byte[] ids = new byte[] { v.ID };
            object[] content = new object[] { (byte)0, EventSubcode.VoiceRemove, ids };
            return content;
        }

        internal object[] buildFrameMessage(byte voiceId, ushort evNumber, byte frNumber, ArraySegment<byte> data, FrameFlags flags)
        {
            // ushort is not supported by serializer
            if (evNumber != frNumber) // save 1 byte if numbers match
            {
                if (evNumber >> 8 != 0) // save 1 byte if evNumber < 256, also backward compatibility
                {
                    return new object[] { voiceId, (byte)evNumber, data, (byte)flags, frNumber, (byte)(evNumber >> 8) };
                }
                return new object[] { voiceId, (byte)evNumber, data, (byte)flags, frNumber };
            }
            else
            {
                return new object[] { voiceId, (byte)evNumber, data, (byte)flags };
            }
        }

        // isLocalPlayer is required only for VoiceClient.RoundTripTime calculation
        internal void onVoiceEvent(object content0, int channelId, int playerId, bool isLocalPlayer)
        {
            object[] content = (object[])content0;
            if ((byte)content[0] == (byte)0)
            {
                switch ((byte)content[1])
                {
                    case (byte)EventSubcode.VoiceInfo:
                        this.onVoiceInfo(channelId, playerId, content[2]);
                        break;
                    case (byte)EventSubcode.VoiceRemove:
                        this.onVoiceRemove(channelId, playerId, content[2]);
                        break;
                    default:
                        logger.Log(LogLevel.Error, "[PV] Unknown sevent subcode " + content[1]);
                        break;
                }
            }
            else
            {
                byte voiceId = (byte)content[0];
                ushort evNumber = (byte)content[1];
                byte[] receivedBytes = (byte[])content[2];
                FrameFlags flags = 0;
                if (content.Length > 3)
                {
                    flags = (FrameFlags)content[3];
                }
                byte frNumber = (byte)evNumber;
                if (content.Length  > 4)
                {
                    frNumber = (byte)content[4];
                }
                if (content.Length > 5)
                {
                    evNumber += (ushort)((byte)content[5] << 8);
                }
                var buffer = new FrameBuffer(receivedBytes, flags, frNumber);
                this.voiceClient.onFrame( playerId, voiceId, evNumber, ref buffer, isLocalPlayer);
                buffer.Release();
            }
        }

        private void onVoiceInfo(int channelId, int playerId, object payload)
        {
            foreach (var el in (object[])payload)
            {
                var h = (Dictionary<byte, Object>)el;
                byte voiceId = (byte)h[(byte)EventParam.VoiceId];
                h.TryGetValue((byte)EventParam.EventBufferSize, out var eventBufferSize); // may be not present in the info from old client
                var info = createVoiceInfoFromEventPayload(h);
                voiceClient.onVoiceInfo(channelId, playerId, voiceId, eventBufferSize == null ? 0 : (int)eventBufferSize, info);
            }
        }

        private void onVoiceRemove(int channelId, int playerId, object payload)
        {
            var voiceIds = (byte[])payload;
            voiceClient.onVoiceRemove(playerId, voiceIds);
        }

        private VoiceInfo createVoiceInfoFromEventPayload(Dictionary<byte, object> h)
        {
            var i = new VoiceInfo();
            i.Codec = (Codec)h[(byte)EventParam.Codec];
            i.SamplingRate = (int)h[(byte)EventParam.SamplingRate];
            i.Channels = (int)h[(byte)EventParam.Channels];
            i.FrameDurationUs = (int)h[(byte)EventParam.FrameDurationUs];
            i.Bitrate = (int)h[(byte)EventParam.Bitrate];
            // check to keep compatibility with old clients
            if (h.ContainsKey((byte)EventParam.Width)) i.Width = (int)h[(byte)EventParam.Width];
            if (h.ContainsKey((byte)EventParam.Height)) i.Height = (int)h[(byte)EventParam.Height];
            if (h.ContainsKey((byte)EventParam.FPS)) i.FPS = (int)h[(byte)EventParam.FPS];
            if (h.ContainsKey((byte)EventParam.KeyFrameInt)) i.KeyFrameInt = (int)h[(byte)EventParam.KeyFrameInt];
            i.UserData = h[(byte)EventParam.UserData];

            return i;
        }
    }
}
