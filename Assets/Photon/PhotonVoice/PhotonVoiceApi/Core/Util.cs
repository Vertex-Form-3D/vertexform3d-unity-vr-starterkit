using System;
using System.Linq;
using System.Threading;

namespace Photon.Voice
{
    /// <summary>Stores the config frame and prevents other frames decoding until the decoder is ready.</summary>
    public class DecoderConfigFrame : IDisposable
    {
        ILogger logger;
        IDecoder decoder;
        FrameBuffer configFrame;
        bool configFrameDecoded = false;

        public DecoderConfigFrame(ILogger logger, IDecoder decoder)
        {
            this.logger = logger;
            this.decoder = decoder;
        }

        /// <summary>Call it in Input().</summary>
        /// <param name="buf">Data frame.</param>
        /// <param name="decoderReady">True if the decoder is ready.</param>
        /// <returns>True if decoder is allowed to decode the current frame.</returns>
        public bool TryConfigure(ref FrameBuffer buf, bool decoderReady)
        {
            if (configFrameDecoded)
            {
                return true;
            }

            if (buf.IsConfig)
            {
                configFrame = buf;
                buf.Retain();
                logger.Log(LogLevel.Info, "[PV] [VD] storing config frame " + configFrame.Length);
            }

            if (!decoderReady)
            {
                return false;
            }

            if (configFrame.Array != null)
            {
                logger.Log(LogLevel.Info, "[PV] [VD] decoding config frame " + configFrame.Length);
                configFrameDecoded = true;
                decoder.Input(ref configFrame); // this calls TryConfigure recursively, make sure that configFrameDecoded is true
                configFrame.Release();
            }

            return buf.Array != configFrame.Array; // to avoid double decode if decoder is ready when config arrives
        }

        public void Dispose()
        {
            configFrame.Release();
        }
    }

    // Does not work until Start() gets called
    internal class SpacingProfile
    {
        short[] buf;
        bool[] info;
        int capacity;
        int ptr = 0;
        System.Diagnostics.Stopwatch watch;
        long watchLast;
        bool flushed;

        public SpacingProfile(int capacity)
        {
            this.capacity = capacity;
        }

        public void Start()
        {
            if (watch == null)
            {
                buf = new short[capacity];
                info = new bool[capacity];
                watch = System.Diagnostics.Stopwatch.StartNew();
            }
        }

        public void Update(bool lost, bool flush)
        {
            if (watch == null)
            {
                return;
            }

            if (flushed)
            {
                watchLast = watch.ElapsedMilliseconds;
            }
            var t = watch.ElapsedMilliseconds;
            buf[ptr] = (short)(t - watchLast);
            info[ptr] = lost;
            watchLast = t;
            ptr++;
            if (ptr == buf.Length)
            {
                ptr = 0;
            }
            flushed = flush;
        }

        public string Dump
        {
            get
            {
                if (watch == null)
                {
                    return "Error: Profiler not started.";
                }
                else
                {
                    var buf2 = buf.Select((v, i) => (info[i] ? "-" : "") + v.ToString()).ToArray();
                    return "max=" + Max + " " + string.Join(",", buf2, ptr, buf.Length - ptr) + ", " + string.Join(",", buf2, 0, ptr);
                }
            }
        }

        // do not call frequently
        public int Max { get { return buf.Select(v => Math.Abs(v)).Max(); } }
    }

    internal static class Util
    {
        static public void SetThreadName(Thread t, string name)
        {
            const int MAX = 25;
            if (name.Length > MAX)
            {
                name = name.Substring(0, MAX);
            }
            t.Name = name;
        }
    }

    // We need to decorate callbacks for Unity's IL2CPP with AOT.MonoPInvokeCallbackAttribute provided by Unity.
    // This is a replacement that still works like the original attribute but also allows compile code without Unity assemblies.
    public class MonoPInvokeCallbackAttribute : System.Attribute
    {
        private Type type;
        public MonoPInvokeCallbackAttribute(Type t) { type = t; }
    }
}