// -----------------------------------------------------------------------
// <copyright file="VoiceFramed.cs" company="Exit Games GmbH">
//   Photon Voice API Framework for Photon - Copyright (C) 2017 Exit Games GmbH
// </copyright>
// <summary>
//   Photon data streaming support.
// </summary>
// <author>developer@photonengine.com</author>
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
#if DUMP_TO_FILE
using System.IO;
#endif
using System.Threading;

namespace Photon.Voice
{
    /// <summary>Processor interface.</summary>
    public interface IProcessor<T> : IDisposable
    {
        /// <summary>Process a frame of data.</summary>
        /// <param name="buf">Buffer containing input data</param>
        /// <returns>Buffer containing output data or null if frame has been discarded (VAD)</returns>
        T[] Process(T[] buf);
    }

    /// <summary>
    /// Typed re-framing LocalVoice
    /// </summary>
    /// <remarks>
    /// Consumes data in array buffers of arbitrary length. Repacks them in frames of <see cref="VoiceInfo.FrameSize"/> length for further processing and encoding.
    /// </remarks>
    public class LocalVoiceFramed<T> : LocalVoice
    {
        Framer<T> framer;
#if DUMP_TO_FILE
        FileStream file;
        static int fileCnt = 0;
#endif
        // Process the frame by a range of processors.
        // Should return arrays exactly of info.FrameSize size or null to skip sending
        protected T[] processFrame(T[] buf, int p0, int p1)
        {
            for (int i = p0; i < p1; i++)
            {
                buf = processors[i].Process(buf);
                if (buf == null)
                {
                    break;
                }
            }
            return buf;
        }

        /// <summary>
        /// Adds processors after any built-in processors and everything added with AddPreProcessor.
        /// </summary>
        /// <param name="processors"></param>
        public void AddPostProcessor(params IProcessor<T>[] processors)
        {
            lock (disposeLock)
            {
                foreach (var p in processors)
                {
                    this.processors.Add(p);
                }
            }
        }

        int preProcessorsCnt;

        /// <summary>
        /// Adds processors before built-in processors and everything added with AddPostProcessor.
        /// </summary>
        /// <param name="processors"></param>
        public void AddPreProcessor(params IProcessor<T>[] processors)
        {
            lock (disposeLock)
            {
                foreach (var p in processors)
                {
                    this.processors.Insert(preProcessorsCnt++, p);
                }
            }
        }

        /// <summary>
        /// Adds processors before built-in processors and everything added with AddPostProcessor.
        /// </summary>
        /// <param name="processors"></param>
        public void RemoveProcessor(params IProcessor<T>[] processors)
        {
            lock (disposeLock)
            {
                foreach (var p in processors)
                {
                    var i = this.processors.IndexOf(p);
                    if (i >= 0)
                    {
                        if (i < preProcessorsCnt)
                        {
                            preProcessorsCnt--;
                        }
                        this.processors.Remove(p);
                    }
                }
            }
        }

        /// <summary>
        /// Clears all processors in pipeline including built-in resampling.
        /// User should add at least resampler processor after call.
        /// </summary>
        public void ClearProcessors()
        {
            lock (disposeLock)
            {
                this.processors.Clear();
                preProcessorsCnt = 0;
            }
        }

        // synchronized by disposeLock as it locks the entire processing pipeline anyways
        List<IProcessor<T>> processors = new List<IProcessor<T>>();

        internal LocalVoiceFramed(VoiceClient voiceClient, byte id, VoiceInfo voiceInfo, int inSampleRate, int channelId, VoiceCreateOptions opt)
        : base(voiceClient, id, voiceInfo, channelId, opt)
        {
#if DUMP_TO_FILE
            file = File.Open("dump-" + fileCnt++ + ".raw", FileMode.Create);
#endif

            if (voiceInfo.FrameSize == 0)
            {
                throw new Exception(LogPrefix + ": non 0 frame size required for framed stream");
            }

            OptimalSourceFrameSize = voiceInfo.FrameSize;
            if (voiceInfo.SamplingRate != 0 && inSampleRate != voiceInfo.SamplingRate)
            {
                if (voiceInfo.SamplingRate <= 0 || inSampleRate / voiceInfo.SamplingRate > 10 || voiceInfo.SamplingRate / inSampleRate > 10)
                {
                    throw new Exception(LogPrefix + ": unsupported values for resamling ratio: " + voiceInfo.SamplingRate + "/" + inSampleRate);
                }
                const bool INTERPOLATE = true;
                this.framer = new FramerResampler<T>(voiceInfo.FrameSize, voiceInfo.Channels, voiceInfo.SamplingRate, inSampleRate, INTERPOLATE);
                OptimalSourceFrameSize = voiceInfo.FrameSize * inSampleRate / voiceInfo.SamplingRate;
                this.voiceClient.logger.Log(LogLevel.Warning, "[PV] Local voice #" + this.id + " audio source frequency " + inSampleRate + " and encoder sampling rate " + voiceInfo.SamplingRate + " do not match. Resampling will occur before encoding (FramerResampler" + (INTERPOLATE ? ", interp" : "") +  ").");
            }
            else // if no resampling required
            {
                this.framer = new Framer<T>(voiceInfo.FrameSize);
                this.voiceClient.logger.Log(LogLevel.Info, "[PV] Local voice #" + this.id + " audio source frequency and encoder sampling rate are the same " + voiceInfo.SamplingRate + ". No resampling required (Framer).");
            }

            this.bufferFactory = new ArrayPoolSet<T>(DATA_POOL_CAPACITY, Name, OptimalSourceFrameSize, 5);
        }

        bool dataEncodeThreadStarted;
        Queue<T[]> pushDataQueue = new Queue<T[]>();
        AutoResetEvent pushDataQueueReady = new AutoResetEvent(false);

        public int OptimalSourceFrameSize { get; private set; }

        /// <summary><see cref="PushData(T[])" and <see cref="PushDataAsync(T[])" callers should use this factory for optimal performance/>/>.</summary>
        public ObjectFactory<T[], int> BufferFactory { get { return bufferFactory; } }
        ObjectFactory<T[], int> bufferFactory;

        /// <summary>Wether this LocalVoiceFramed has capacity for more data buffers to be pushed asynchronously.</summary>
        public bool PushDataAsyncReady { get { lock (pushDataQueue) return pushDataQueue.Count < DATA_POOL_CAPACITY - 1; } } // 1 slot for buffer currently processed and not contained either by pool or queue

        /// <summary>Asynchronously push data into this stream.</summary>
        // Accepts array of arbitrary size. Automatically splits or aggregates input to buffers of length <see cref="FrameSize"></see>.
        // Expects buf content to be preserved until PushData is called from a worker thread. Releases buffer to <see cref="BufferFactory"></see> then.
        public void PushDataAsync(T[] buf)
        {
            if (disposed) return;

            if (!threadingEnabled)
            {
                PushData(buf);
                this.bufferFactory.Free(buf, buf.Length);

                return;
            }

            if (!dataEncodeThreadStarted)
            {
                voiceClient.logger.Log(LogLevel.Info, LogPrefix + ": Starting data encode thread");
#if NETFX_CORE
                Windows.System.Threading.ThreadPool.RunAsync((x) =>
                {
                    PushDataAsyncThread();
                });
#else
                var t = new Thread(PushDataAsyncThread);
                t.Start();
                Util.SetThreadName(t, "[PV] Enc" + shortName);
#endif
                dataEncodeThreadStarted = true;
            }

            // Caller should check this asap in general case if packet production is expensive.
            // This is not the case For lightweight audio stream. Also overflow does not happen for audio stream normally.
            // Make sure that queue is not too large even if caller missed the check.
            if (this.PushDataAsyncReady)
            {
                lock (pushDataQueue)
                {
                    pushDataQueue.Enqueue(buf);
                }
                pushDataQueueReady.Set();
            }
            else
            {
                this.bufferFactory.Free(buf, buf.Length);
                if (framesSkipped == framesSkippedNextLog)
                {
                    voiceClient.logger.Log(LogLevel.Warning, LogPrefix + ": PushData queue overflow. Frames skipped: " + (framesSkipped + 1));
                    framesSkippedNextLog = framesSkipped + 10;
                }
                framesSkipped++;
            }
        }
        int framesSkippedNextLog;
        int framesSkipped;
        bool exitThread = false;
        private void PushDataAsyncThread()
        {

#if PROFILE
            UnityEngine.Profiling.Profiler.BeginThreadProfiling("PhotonVoice", LogPrefix);
#endif

            try
            {
                while (!exitThread)
                {
                    pushDataQueueReady.WaitOne(); // Wait until data is pushed to the queue or Dispose signals.

#if PROFILE
                    UnityEngine.Profiling.Profiler.BeginSample("Encoder");
#endif

                    while (true) // Dequeue and process while the queue is not empty
                    {
                        if (exitThread) break; // early exit to save few resources

                        T[] b = null;
                        lock (pushDataQueue)
                        {
                            if (pushDataQueue.Count > 0)
                            {
                                b = pushDataQueue.Dequeue();
                            }
                        }
                        if (b != null)
                        {
                            PushData(b);
                            this.bufferFactory.Free(b, b.Length);
                        }
                        else
                        {
                            break;
                        }
                    }

#if PROFILE
                    UnityEngine.Profiling.Profiler.EndSample();
#endif

                }
            }
            catch (Exception e)
            {
                voiceClient.logger.Log(LogLevel.Error, LogPrefix + ": Exception in encode thread: " + e);
                throw e;
            }
            finally
            {
                Dispose();
                this.bufferFactory.Dispose();

#if NETFX_CORE
                pushDataQueueReady.Dispose();
#else
                pushDataQueueReady.Close();
#endif

                voiceClient.logger.Log(LogLevel.Info, LogPrefix + ": Exiting data encode thread");

#if PROFILE
                UnityEngine.Profiling.Profiler.EndThreadProfiling();
#endif

            }
        }


        // counter for detection of first frame for which process() returned null
        int processNullFramesCnt = 0;
        /// <summary>Synchronously push data into this stream.</summary>
        // Accepts array of arbitrary size. Automatically splits or aggregates input to buffers of length <see cref="FrameSize"></see>.
        public void PushData(T[] buf)
        {
            if (this.TransmitEnabled)
            {
                if (this.encoder is IEncoderDirect<T[]>)
                {
                    lock (disposeLock)
                    {
                        if (!disposed)
                        {
                            var preProcessed = processFrame(buf, 0, preProcessorsCnt);
                            if (preProcessed != null)
                            {
                                foreach (var framed in framer.Frame(preProcessed))
                                {
                                    var processed = processFrame(framed, preProcessorsCnt, processors.Count);
                                    if (processed != null)
                                    {
                                        processNullFramesCnt = 0;
                                        ((IEncoderDirect<T[]>)this.encoder).Input(processed);
                                    }
                                    else
                                    {
                                        processNullFramesCnt++;
                                        if (processNullFramesCnt == 1)
                                        {
                                            this.encoder.EndOfStream();
                                        }
                                    }
                                }
                            }
                            else
                            {
                                processNullFramesCnt++;
                                if (processNullFramesCnt == 1)
                                {
                                    this.encoder.EndOfStream();
                                }
                            }
                        }
                    }
                }
                else
                {
                    throw new Exception(LogPrefix + ": PushData(T[]) called on encoder of unsupported type " + (this.encoder == null ? "null" : this.encoder.GetType().ToString()));
                }
            }
        }

        /// <summary>
        /// Releases resources used by the <see cref="LocalVoiceFramed{T}"/> instance.
        /// Buffers used for asynchronous push will be disposed in encoder thread's 'finally'.
        /// </summary>
        public override void Dispose()
        {
#if DUMP_TO_FILE
            file.Close();
#endif
            exitThread = true;
            lock (disposeLock)
            {
                if (!disposed)
                {
                    foreach (var p in processors)
                    {
                        p.Dispose();
                    }
                    base.Dispose();
                    pushDataQueueReady.Set(); // let worker exit
                }
            }
        }
    }
}