using System;
using System.Collections.Generic;

namespace Photon.Voice
{
    /// <summary>
    /// Generic Pool to re-use objects of a certain type (TType) that optionally match a certain property or set of properties (TInfo).
    /// </summary>
    /// <typeparam name="TType">Object type.</typeparam>
    /// <typeparam name="TInfo">Type of parameter used to check 2 objects identity (like integral length of array).</typeparam>
    public abstract class ObjectPool<TType, TInfo> : ObjectFactory<TType, TInfo>
    {
        protected int capacity;
        protected TInfo info;
        private TType[] freeObj = new TType[0];
        protected int pos;
        protected string name;
        private bool inited;
        abstract protected TType createObject(TInfo info);
        abstract protected void destroyObject(TType obj);
        abstract protected bool infosMatch(TInfo i0, TInfo i1);
        internal string LogPrefix { get { return "[ObjectPool] [" + name + "]"; } }

        /// <summary>Create a new ObjectPool instance with the given info structure.</summary>
        /// <param name="capacity">Capacity (size) of the object pool.</param>
        /// <param name="name">Name of the object pool.</param>
        /// <param name="info">Info about this Pool's objects.</param>
        public ObjectPool(int capacity, string name, TInfo info)
        {
            this.capacity = capacity;
            this.name = name;
            init(info);
        }

        /// <summary>(Re-)Initializes this ObjectPool.</summary>
        /// If there are objects available in this Pool, they will be destroyed.
        /// Allocates (Capacity) new Objects.
        /// <param name="info">Info about this Pool's objects.</param>
        private void init(TInfo info)
        {
            lock (this)
            {
                while (pos > 0)
                {
                    destroyObject(freeObj[--pos]);
                }
                this.info = info;
                this.freeObj = new TType[capacity];
                inited = true;
            }
        }

        /// <summary>The property (info) that objects in this Pool must match.</summary>
        public TInfo Info
        {
            get { return info; }
        }

        /// <summary>Acquire an existing object, or create a new one if none are available.</summary>
        /// <remarks>If it fails to get one from the pool, this will create from the info given in this pool's constructor.</remarks>
        public TType New()
        {
            lock (this)
            {
                if (pos > 0)
                {
                    return freeObj[--pos];
                }
                if (!inited)
                {
                    throw new Exception(LogPrefix + " not initialized");
                }
            }
            return createObject(this.info);
        }

        /// <summary>Acquire an existing object (if info matches), or create a new one from the passed info.</summary>
        /// <param name="info">Info structure to match, or create a new object with.</param>
        public TType New(TInfo info)
        {
            // TODO: this.info thread safety
            if (!infosMatch(this.info, info))
            {
                init(info);
            }
            return New();
        }

        /// <summary>Returns object to pool.</summary>
        /// <param name="obj">The object to return to the pool.</param>
        /// <param name="objInfo">The info structure about obj.</param>
        /// <remarks>obj is returned to the pool only if objInfo matches this pool's info. Else, it is destroyed.</remarks>
        virtual public bool Free(TType obj, TInfo objInfo)
        {
            // TODO: this.info thread safety
            if (infosMatch(this.info, objInfo))
            {
                lock (this)
                {
                    if (pos < freeObj.Length)
                    {
                        freeObj[pos++] = obj;
                        return true;
                    }
                }
            }

            // destroy if can't reuse
            //UnityEngine.Debug.Log(LogPrefix + " Free(Info) destroy");
            destroyObject(obj);
            // TODO: log warning
            return false;
        }

        /// <summary>Returns object to pool, or destroys it if the pool is full.</summary>
        /// <param name="obj">The object to return to the pool.</param>
        virtual public bool Free(TType obj)
        {
            lock (this)
            {
                if (pos < freeObj.Length)
                {
                    freeObj[pos++] = obj;
                    return true;
                }
            }

            // destroy if can't reuse
            //UnityEngine.Debug.Log(LogPrefix + " Free destroy " + pos);
            destroyObject(obj);
            // TODO: log warning
            return false;
        }

        /// <summary>Free resources assoicated with this ObjectPool</summary>
        public void Dispose()
        {
            lock (this)
            {
                while (pos > 0)
                {
                    destroyObject(freeObj[--pos]);
                }
                freeObj = new TType[0];
            }
        }
    }

    /// <summary>
    /// Pool of Arrays with components of type T, with ObjectPool info being the array's size.
    /// </summary>
    /// <typeparam name="T">Array element type.</typeparam>
    public class ArrayPool<T> : ObjectPool<T[], int>
    {
        public ArrayPool(int capacity, string name, int info) : base(capacity, name, info) { }
        protected override T[] createObject(int info)
        {
            //UnityEngine.Debug.Log(LogPrefix + " Create " + info);
            return new T[info];
        }

        protected override void destroyObject(T[] obj)
        {
            //UnityEngine.Debug.Log(LogPrefix + " Dispose " + pos + " " + obj.GetHashCode());
        }

        protected override bool infosMatch(int i0, int i1)
        {
            return i0 == i1;
        }
    }

    /// <summary>
    /// ArrayPool set of limited size.
    /// </summary>
    /// <typeparam name="T">Array element type.</typeparam>
    public class ArrayPoolSet<T> : ObjectFactory<T[], int>
    {
        Dictionary<int, ArrayPool<T>> pools;
        int capacity;
        string name;
        int defaultInfo;
        int setSize;

        public ArrayPoolSet(int capacity, string name, int defaultInfo, int setSize)
        {
            this.capacity = capacity;
            this.name = name;
            this.defaultInfo = defaultInfo;
            this.setSize = setSize;
            this.pools = new Dictionary<int, ArrayPool<T>>(setSize);
        }

        public T[] New()
        {
            return New(defaultInfo);
        }

        public bool Free(T[] obj)
        {
            return Free(obj, defaultInfo);
        }

        public T[] New(int info)
        {
            ArrayPool<T> pool;
            lock (pools)
            {
                if (!pools.TryGetValue(info, out pool))
                {
                    if (pools.Count < setSize)
                    {
                        pool = new ArrayPool<T>(capacity, name + " [" + info + "]", info);
                        pools[info] = pool;
                    }
                }
            }

            return pool != null ? pool.New() : new T[info];
        }

        public bool Free(T[] obj, int info)
        {
            ArrayPool<T> pool;
            lock (pools)
            {
                pools.TryGetValue(info, out pool);
            }

            return pool != null && pool.Free(obj, info);
        }

        public void Dispose()
        {
            lock (pools)
            {
                foreach (var p in pools)
                {
                    p.Value.Dispose();
                }
            }
        }
    }

    /// <summary>
    /// Set of ArrayPool's of N^2-sized arrays used to produce an ArraySegment with the requested size.
    /// </summary>
    /// <typeparam name="T">Array element type.</typeparam>
    public class ArraySegmentPool<T> : ObjectFactory<ArraySegment<T>, int>
    {
        const int SLOT_0_SIZE_LOG2 = 6; // 1 << 6 = 64
        const int MAX_SIZE_LOG2 = 16; // 1 << 16 = 256*256
        // counts leading 0's
        static uint nlz(uint x)
        {
            uint y;
            uint n = 32;
            y = x >> 16; if (y != 0) { n = n - 16; x = y; }
            y = x >> 8; if (y != 0) { n = n - 8; x = y; }
            y = x >> 4; if (y != 0) { n = n - 4; x = y; }
            y = x >> 2; if (y != 0) { n = n - 2; x = y; }
            y = x >> 1; if (y != 0) return n - 2;
            return n - x;
        }

        static int slot(uint x)
        {
            x--; // 100000 -> 011111 to put round sizes to the correct slot
            int leftmost1 = 32 - (int)nlz(x);
            return Math.Max(0, leftmost1 - SLOT_0_SIZE_LOG2 /* to start with 64 */ );
        }

        ArrayPool<T>[] pools = new ArrayPool<T>[MAX_SIZE_LOG2 - SLOT_0_SIZE_LOG2 + 1];
        int capacity;
        string name;
        int defaultInfo;

        public ArraySegmentPool(int capacity, string name, int defaultInfo)
        {
            this.capacity = capacity;
            this.name = name;
            this.defaultInfo = defaultInfo;
        }

        public ArraySegment<T> New()
        {
            return New(defaultInfo);
        }

        public ArraySegment<T> New(int info)
        {
            if (info == 0)
            {
                return new ArraySegment<T>(Array.Empty<T>());
            }

            int s = slot((uint)info);
            if (s < pools.Length)
            {
                ArrayPool<T> pool;
                lock (pools)
                {
                    pool = pools[s];
                    if (pool == null)
                    {
                        pool = new ArrayPool<T>(capacity, name + " [" + s + "]", (1 << SLOT_0_SIZE_LOG2) << (s));
                        pools[s] = pool;
                    }
                }

                return new ArraySegment<T>(pool.New(), 0, info);
            }
            else
            {
                throw new ArgumentException("ArraySegmentPool New size is too large: " + info);
            }
        }

        public bool Free(ArraySegment<T> obj, int info)
        {
            return Free(obj);
        }

        public bool Free(ArraySegment<T> obj)
        {
            if (obj.Count == 0)
            {
                return false;
            }

            int s = slot((uint)obj.Array.Length);
            if (s < pools.Length)
            {
                var pool = pools[s];
                return pool != null && pool.Free(obj.Array);
            }
            return false;
        }

        public void Dispose()
        {
            foreach (var p in pools)
            {
                if (p != null)
                {
                    p.Dispose();
                }
            }
        }

        static public void test()
        {
            ObjectFactory<ArraySegment<T>, int> bufferFactory = new ArraySegmentPool<T>(100, " =======", 12345);
            Queue<ArraySegment<T>> test = new Queue<ArraySegment<T>>();

            for (int i = 0; i < 5; i++)
            {
                new System.Threading.Thread(() =>
                {
                    int cnt = 0;
                    while (true)
                    {
                        lock (test)
                            if (test.Count < 100)
                            {
                                var s = cnt++;
                                var b = bufferFactory.New(s);
                                if (s > 0)
                                {
                                    b.Array[b.Offset + s - 1] = default(T);
                                }
                                test.Enqueue(b);
                            }
                    }
                }).Start();

                new System.Threading.Thread(() =>
                {
                    while (true)
                    {
                        lock (test)
                            if (test.Count > 10)
                                bufferFactory.Free(test.Dequeue());
                    }
                }).Start();
            }
        }

    }

    public class ImageBufferNativePool<T> : ObjectPool<T, ImageBufferInfo> where T : ImageBufferNative
    {
        public delegate T Factory(ImageBufferNativePool<T> pool, ImageBufferInfo info);
        Factory factory;
        public ImageBufferNativePool(int capacity, Factory factory, string name, ImageBufferInfo info) : base(capacity, name, info)
        {
            this.factory = factory;
        }
        protected override T createObject(ImageBufferInfo info)
        {
            //UnityEngine.Debug.Log(LogPrefix + " Create " + pos);
            return factory(this, info);
        }

        protected override void destroyObject(T obj)
        {
            //UnityEngine.Debug.Log(LogPrefix + " Dispose " + pos + " " + obj.GetHashCode());
            obj.Dispose();
        }

        // only height and stride compared, other parameters do not affect native buffers and can be simple overwritten
        protected override bool infosMatch(ImageBufferInfo i0, ImageBufferInfo i1)
        {
            if (i0.Height != i1.Height)
            {
                return false;
            }
            var s0 = i0.Stride;
            var s1 = i1.Stride;
            if (s0.Length != s1.Length)
            {
                return false;
            }
            switch (i0.Stride.Length)
            {
                // most common case are 1 and 3 planes
                case 1:
                    return s0[0] == s1[0];
                case 2:
                    return s0[0] == s1[0] && s0[1] == s1[1];
                case 3:
                    return s0[0] == s1[0] && s0[1] == s1[1] && s0[2] == s1[2];
                default:
                    for (int i = 0; i < s0.Length; i++)
                    {
                        if (s0[i] != s1[i])
                        {
                            return false;
                        }
                    }
                    return true;
            }
        }
    }
}
