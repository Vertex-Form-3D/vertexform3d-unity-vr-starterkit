// -----------------------------------------------------------------------
// <copyright file="LoadBalancingTransport2.cs" company="Exit Games GmbH">
//   Photon Voice API Framework for Photon - Copyright (C) 2020 Exit Games GmbH
// </copyright>
// <summary>
//   Extends Photon Realtime API with audio streaming functionality.
// </summary>
// <author>developer@photonengine.com</author>
// ----------------------------------------------------------------------------


namespace Photon.Voice
{
    using System;
    using ExitGames.Client.Photon;
    using Realtime;


    /// <summary>
    /// Variant of LoadBalancingTransport. Aims to be non-alloc at the cost of breaking compatibility with older clients.
    /// </summary>
    public class LoadBalancingTransport2 : LoadBalancingTransport
    {
        public LoadBalancingTransport2(ILogger logger = null, ConnectionProtocol connectionProtocol = ConnectionProtocol.Udp, bool cppCompatibilityMode = false) : base(logger, connectionProtocol, cppCompatibilityMode)
        {
            this.LoadBalancingPeer.UseByteArraySlicePoolForEvents = true; // incoming byte[] events can be deserialized to a pooled ByteArraySlice
            this.LoadBalancingPeer.ReuseEventInstance = true;             // this won't store references to the event anyways
        }

        public override int GetPayloadFragmentSize(SendFrameParams par)
        {
            // rough estimate, TODO: improve and test
            int overhead = 3 * 2; // possible InterestGroup and Receivers: key, type, value
            if (par.TargetPlayers != null)
            {
                overhead += 3 + par.TargetPlayers.Length; // key, type, compressed ength and array
            }

            return 1118 - MAX_DATA_OFFSET - overhead; // <- protocol 18 theoretical encrypted; experimental encoded: 1119, non-encrypted: 1134
        }

        protected override byte FrameCode => VoiceEvent.FrameCode;

        const int MAX_DATA_OFFSET = 6;
        protected override object buildFrameMessage(byte voiceId, ushort evNumber, byte frNumber, ArraySegment<byte> data, FrameFlags flags)
        {
            // this uses a pooled slice, which is released within the send method (here RaiseEvent at the bottom)
            ByteArraySlice frameData = this.LoadBalancingPeer.ByteArraySlicePool.Acquire(data.Count + MAX_DATA_OFFSET);

            int pos = 1;
            frameData.Buffer[pos++] = voiceId;
            frameData.Buffer[pos++] = (byte)evNumber;
            frameData.Buffer[pos++] = (byte)flags;
            if (evNumber != frNumber) // save 1 byte if numbers match
            {
                frameData.Buffer[pos++] = (byte)frNumber;
                if (evNumber >> 8 != 0) // save 1 byte if evNumber < 256, also backward compatibility
                {
                    frameData.Buffer[pos++] = (byte)(evNumber >> 8);
                }
            }
            frameData.Buffer[0] = (byte)pos;

            Buffer.BlockCopy(data.Array, data.Offset, frameData.Buffer, pos, data.Count);
            frameData.Count = data.Count + pos; // need to set the count, as we manipulated the buffer directly
            return frameData;
        }

        protected override void onEventActionVoiceClient(EventData ev)
        {
            if (ev.Code == VoiceEvent.FrameCode)
            {
                // Payloads are arrays. If first array element is 0 than next is event subcode. Otherwise, the event is data frame with voiceId in 1st element.
                this.onVoiceFrameEvent(ev[(byte)ParameterCode.CustomEventContent], REMOTE_VOICE_CHANNEL, ev.Sender, this.LocalPlayer.ActorNumber);
            }
            else
            {
                base.onEventActionVoiceClient(ev);
            }
        }

        internal void onVoiceFrameEvent(object content0, int channelId, int playerId, int localPlayerId)
        {
            byte[] content;
            int contentLength;
            int sliceOffset = 0;
            ByteArraySlice slice = content0 as ByteArraySlice;
            if (slice != null)
            {
                content = slice.Buffer;
                contentLength = slice.Count;
                sliceOffset = slice.Offset;
            }
            else
            {
                content = content0 as byte[];
                contentLength = content.Length;
            }

            if (content == null || contentLength < 3)
            {
                this.logger.Log(LogLevel.Error, "[PV] onVoiceFrameEvent did not receive data (readable as byte[]) " + content0);
            }
            else
            {
                byte dataOffset = (byte)content[sliceOffset];
                byte voiceId = (byte)content[sliceOffset + 1];
                ushort evNumber = (byte)content[sliceOffset + 2];
                FrameFlags flags = 0;
                if (dataOffset > 3)
                {
                    flags = (FrameFlags)content[3];
                }
                byte frNumber = (byte)evNumber;
                if (dataOffset > 4)
                {
                    frNumber = content[4];
                }
                if (dataOffset > 5)
                {
                    evNumber += (ushort)(content[5] << 8);
                }
                FrameBuffer buffer;
                if (slice != null)
                {
                    buffer = new FrameBuffer(slice.Buffer, slice.Offset + dataOffset, contentLength - dataOffset, flags, frNumber, slice);
                }
                else
                {
                    buffer = new FrameBuffer(content, dataOffset, contentLength - dataOffset, flags, frNumber, null);
                }

                this.voiceClient.onFrame(playerId, voiceId, evNumber, ref buffer, playerId == localPlayerId);
                buffer.Release();
            }
        }
    }
}