//#define STATS
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Photon.Voice
{
    // Encapsulates byte array slice, FrameFlags and dispose Action
    public struct FrameBuffer
    {
        readonly byte[] array;
        readonly int offset;
        readonly int count;
        readonly IDisposable disposer;
        bool disposed;
        int refCnt; // See Retain()

        GCHandle gcHandle;
        IntPtr ptr;
        bool pinned;
#if STATS
        static internal int statDisposerCreated;
        static internal int statDisposerDisposed;
        static internal int statPinned;
        static internal int statUnpinned;
#else
        static internal int statDisposerCreated = Int32.MaxValue;
        static internal int statDisposerDisposed = Int32.MaxValue;
        static internal int statPinned = Int32.MaxValue;
        static internal int statUnpinned = Int32.MaxValue;
#endif
        public FrameBuffer(byte[] array, int offset, int count, FrameFlags flags, byte frameNum, IDisposable disposer)
        {
            this.array = array;
            this.offset = offset;
            this.count = count;
            this.Flags = flags;
            this.FrameNum = frameNum;
            this.disposer = disposer;
            this.disposed = false;
            this.refCnt = 1;

            this.gcHandle = new GCHandle();
            this.ptr = IntPtr.Zero;
            this.pinned = false;
#if STATS
            if (disposer != null)
            {
                Interlocked.Increment(ref statDisposerCreated);
            }
#endif
        }

        public FrameBuffer(byte[] array, FrameFlags flags, byte frameNum)
        {
            this.array = array;
            this.offset = 0;
            this.count = array == null ? 0 : array.Length;
            this.Flags = flags;
            this.FrameNum = frameNum;
            this.disposer = null;
            this.disposed = false;
            this.refCnt = 1;

            this.gcHandle = new GCHandle();
            this.ptr = IntPtr.Zero;
            this.pinned = false;
#if STATS
            if (disposer != null) // false
            {
                Interlocked.Increment(ref statDisposerCreated);
            }
#endif
        }

        // creates a copy with modified offset, count and flags
        public FrameBuffer(FrameBuffer from, int offset, int count, FrameFlags flags, byte frameNum)
        {
            this = from;
            this.offset = offset;
            this.count = count;
            this.Flags = flags;
            this.FrameNum = frameNum;
        }

        // Pins underlying buffer and returns the pointer to it with offset.
        // Unpins in Dispose().
        public IntPtr Ptr
        {
            get
            {
                if (!pinned && array != null)
                {
                    gcHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
                    ptr = IntPtr.Add(gcHandle.AddrOfPinnedObject(), offset);
                    pinned = true;
#if STATS
                    Interlocked.Increment(ref statPinned);
#endif
                }
                return ptr;
            }
        }

        // Use Retain() to prevent the owner from disposing the buffer when it calls Release(). Since FrameBuffer is a struct, ref counter
        // is shared only between parameters in stack passed as ref, so ref counter is rather a flag, not a counter.
        // To preserve the buffer for future use:
        // void foo(ref FrameBuffer f1) {
        //   this.f2 = f1; // don't call on copy!
        //   f1.Retain()
        // }
        // in other thread or in subsequent call or when disposing the owner:
        //   this.f2.release();
        public void Retain()
        {
            refCnt++;
        }

        // Call on each owned FrameBuffer after processing:
        // var f1 = new FrameBuffer()
        // foo(f1)
        // f1.Release()
        public void Release()
        {
            refCnt--;
            if (refCnt <= 0)
            {
                Dispose();
            }
        }

        private void Dispose()
        {
            // not allocated if was created with FrameBuffer()
            if (pinned)
            {
                gcHandle.Free();
                pinned = false;
#if STATS
                Interlocked.Increment(ref statUnpinned);
#endif
            }
            if (disposer != null && !disposed)
            {
                disposer.Dispose();
                disposed = true;
#if STATS
                Interlocked.Increment(ref statDisposerDisposed);
#endif
            }
        }

        public byte[] Array { get { return array; } }
        public int Length { get { return count; } }
        public int Offset { get { return offset; } }
        public FrameFlags Flags { get; }
        public byte FrameNum { get; }
        public bool IsFEC => (Flags & FrameFlags.FEC) != 0;
        public bool IsConfig => (Flags & FrameFlags.Config) != 0;
        public bool IsKeyframe => (Flags & FrameFlags.KeyFrame) != 0;

        public override string ToString()
        {
            if (array == null)
            {
                return "null";
            }
            else
            {
                var frag = Flags & FrameFlags.MaskFrag;
                var part = Flags & FrameFlags.MaskPart;
                return "#" + FrameNum + " " + part + " " + frag + (frag == FrameFlags.FragNotEnd ? " c#" + array[offset + count - 1] : "") + (IsFEC ? " FEC" : "") + " r#" + refCnt;
            }
        }
    }
}