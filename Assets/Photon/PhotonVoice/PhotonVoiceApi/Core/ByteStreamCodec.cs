using System;
using UnityEngine;

namespace Photon.Voice
{
    // Streams out given buffers without processing
    public class ByteStreamEncoder : IEncoderDirect<byte[]>
    {
        public string Error => "";

        // Set by VoiceClient
        public Action<ArraySegment<byte>, FrameFlags> Output { private get; set; }

        // You can leave this method empty and call Output in some other way.
        public void Input(byte[] buf)
        {
            Output(new ArraySegment<byte>(buf), (FrameFlags)0);
        }

        // Or instead of sending data to Output, return a non-empty buffer here.
        // Called once per service()
        public ArraySegment<byte> DequeueOutput(out FrameFlags flags)
        {
            flags = 0;
            return EmptyBuffer;
        }
        private static readonly ArraySegment<byte> EmptyBuffer = new ArraySegment<byte>(Array.Empty<byte>());

        public void EndOfStream()
        {
        }

        public I GetPlatformAPI<I>() where I : class
        {
            return null;
        }

        public void Dispose()
        {
        }
    }

    // Passes incoming buffers to output without processing
    public class ByteStreamDecoder : IDecoder
    {
        public string Error => "";

        public delegate void OutputDelegate(ref FrameBuffer buf);

        OutputDelegate output;
        Action onMissingFrame;
        public ByteStreamDecoder(OutputDelegate output, Action onMissingFrame)
        {
            this.output = output;
            this.onMissingFrame = onMissingFrame;
        }

        public void Input(ref FrameBuffer buf)
        {
            if (buf.Array == null)
            {
                onMissingFrame?.Invoke();
                return;
            }

            // Normally Input() is called in a worker thread.
            // Use buf.Retain() / Release() if you need buf to be valid after return from Input().
            output?.Invoke(ref buf);
        }

        public void Open(VoiceInfo info)
        {
        }

        public void Dispose()
        {
        }
    }
}
