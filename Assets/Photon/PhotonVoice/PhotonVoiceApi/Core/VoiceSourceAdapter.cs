// -----------------------------------------------------------------------
// <copyright file="VoiceSourceAdapter.cs" company="Exit Games GmbH">
//   Photon Voice API Framework for Photon - Copyright (C) 2017 Exit Games GmbH
// </copyright>
// <summary>
//   Photon data streaming support.
// </summary>
// <author>developer@photonengine.com</author>
// ----------------------------------------------------------------------------

using System;

namespace Photon.Voice
{
    /// <summary>
    /// Adapter base reading data from <see cref="IDataReader{T}.Read"></see> and pushing it to <see cref="LocalVoice"></see>.
    /// </summary>
    /// <remarks>
    /// Use this with a LocalVoice of same T type.
    /// </remarks>
    public abstract class BufferReaderPushAdapterBase<T> : IServiceable
    {
        protected IDataReader<T> reader;

        /// <summary>Do the actual data read/push.</summary>
        /// <param name="localVoice">LocalVoice instance to push data to.</param>
        public abstract void Service(LocalVoice localVoice);

        /// <summary>Create a new BufferReaderPushAdapterBase instance</summary>
        /// <param name="reader">DataReader to read from.</param>
        public BufferReaderPushAdapterBase(IDataReader<T> reader)
        {
            this.reader = reader;
        }

        /// <summary>Release resources associated with this instance.</summary>
        public void Dispose()
        {
            this.reader.Dispose();
        }
    }

    /// <summary>
    /// <see cref="BufferReaderPushAdapter{T}"></see> implementation using asynchronous <see cref="LocalVoiceFramed{T}.PushDataAsync"></see>.
    /// </summary>
    /// <remarks>
    /// Acquires a buffer from pool before each Read, releases buffer after last Read (brings Acquire/Release overhead).
    /// Expects localVoice to be a <see cref="LocalVoiceFramed{T}"></see> of same T.
    /// </remarks>
    public class BufferReaderPushAdapterAsyncPool<T> : BufferReaderPushAdapterBase<T>
    {
        /// <summary>Create a new BufferReaderPushAdapter instance</summary>
        /// <param name="reader">DataReader to read from.</param>
        public BufferReaderPushAdapterAsyncPool(IDataReader<T> reader) : base(reader) { }

        /// <summary>Do the actual data read/push.</summary>
        /// <param name="localVoice">LocalVoice instance to push data to. Must be a <see cref="LocalVoiceFramed{T}"></see> of same T.</param>
        public override void Service(LocalVoice localVoice)
        {
            var v = (LocalVoiceFramed<T>)localVoice;
            T[] buf = v.BufferFactory.New();
            while (this.reader.Read(buf))
            {
                v.PushDataAsync(buf);
                buf = v.BufferFactory.New();
            }
            // release unused buffer
            v.BufferFactory.Free(buf, buf.Length);
        }
    }

    /// <summary>
    /// <see cref="BufferReaderPushAdapter{T}"></see> implementation using asynchronous <see cref="LocalVoiceFramed{T}.PushDataAsync"></see>, converting float samples to short.
    /// </summary>
    /// <remarks>
    /// This adapter works exactly like <see cref="BufferReaderPushAdapterAsyncPool{T}"></see>, but it converts float samples to short.
    /// Acquires a buffer from pool before each Read, releases buffer after last Read.
    ///
    /// Expects localVoice to be a <see cref="LocalVoiceFramed{T}"></see> of same T.
    /// </remarks>
    public class BufferReaderPushAdapterAsyncPoolFloatToShort : BufferReaderPushAdapterBase<float>
    {
        float[] buffer;

        /// <summary>Create a new BufferReaderPushAdapter instance</summary>
        /// <param name="reader">DataReader to read from.</param>
        public BufferReaderPushAdapterAsyncPoolFloatToShort(IDataReader<float> reader) : base(reader)
        {
            buffer = new float[0];
        }

        /// <summary>Do the actual data read/push.</summary>
        /// <param name="localVoice">LocalVoice instance to push data to. Must be a <see cref="LocalVoiceFramed{T}"></see> of same T.</param>
        public override void Service(LocalVoice localVoice)
        {
            var v = ((LocalVoiceFramed<short>)localVoice);
            short[] buf = v.BufferFactory.New();

            if (buffer.Length != buf.Length)
            {
                buffer = new float[buf.Length];
            }

            while (this.reader.Read(buffer))
            {
                AudioUtil.Convert(buffer, buf, buf.Length);
                v.PushDataAsync(buf);
                buf = v.BufferFactory.New();
            }
            // release unused buffer
            v.BufferFactory.Free(buf, buf.Length);
        }
    }

    /// <summary>
    /// <see cref="BufferReaderPushAdapter{T}"></see> implementation using asynchronous <see cref="LocalVoiceFramed{T}.PushDataAsync"></see>, converting short samples to float.
    /// </summary>
    /// This adapter works exactly like <see cref="BufferReaderPushAdapterAsyncPool{T}"></see>, but it converts short samples to float.
    /// Acquires a buffer from pool before each Read, releases buffer after last Read.
    ///
    /// Expects localVoice to be a <see cref="LocalVoiceFramed{T}"></see> of same T.
    public class BufferReaderPushAdapterAsyncPoolShortToFloat : BufferReaderPushAdapterBase<short>
    {
        short[] buffer = new short[0];

        /// <summary>Create a new BufferReaderPushAdapter instance</summary>
        /// <param name="reader">DataReader to read from.</param>
        public BufferReaderPushAdapterAsyncPoolShortToFloat(IDataReader<short> reader) : base(reader)
        {
        }

        /// <summary>Do the actual data read/push.</summary>
        /// <param name="localVoice">LocalVoice instance to push data to. Must be a <see cref="LocalVoiceFramed{T}"></see> of same T.</param>
        public override void Service(LocalVoice localVoice)
        {
            var v = ((LocalVoiceFramed<float>)localVoice);
            float[] buf = v.BufferFactory.New();

            if (buffer.Length != buf.Length)
            {
                buffer = new short[buf.Length];
            }

            while (this.reader.Read(buffer))
            {
                AudioUtil.Convert(buffer, buf, buf.Length);
                v.PushDataAsync(buf);
                buf = v.BufferFactory.New();
            }
            // release unused buffer
            v.BufferFactory.Free(buf, buf.Length);
        }
    }
}