// To enable SoundTouch library tempo change for audio frame shrinking when catching up:
// Define PHOTON_VOICE_SOUND_TOUCH_ENABLE
// Set PlayDelayConfig.TempoChangeHQ to true
// Add SoundTouch library https://gitlab.com/soundtouch/soundtouch
//   Android: edit /Android-lib/jni/Android.mk:
//      add ../../SoundTouchDLL/SoundTouchDLL.cpp to sources list
//      add LOCAL_CFLAGS += -DDLL_EXPORTS
//   Windows: http://soundtouch.surina.net/download.html
// Add SoundTouch library C# wrapper https://gitlab.com/soundtouch/soundtouch/-/blob/master/source/csharp-example/SoundTouch.cs
// Replace "SoundTouch.dll" with "soundtouch" in SoundTouch.cs

using System;
using System.Collections.Concurrent;

namespace Photon.Voice
{
    public interface IAudioOut<T>
    {
        bool IsPlaying { get; }
        void Start(int frequency, int channels, int frameSamplesPerChannel);
        void Flush();
        void Stop();
        void Push(T[] frame);
        void Service();
        int Lag { get; } // ms
    }

    public class AudioOutDummy<T> : IAudioOut<T>
    {
        public bool IsPlaying => false;

        public int Lag => 0;

        public void Flush()
        {
        }

        public void Push(T[] frame)
        {
        }

        public void Service()
        {
        }

        public void Start(int frequency, int channels, int frameSamplesPerChannel)
        {
        }

        public void Stop()
        {
        }
    }

    public class AudioOutDelayControl
    {
        [Serializable]
        public struct PlayDelayConfig
        {
            static public PlayDelayConfig Default = new PlayDelayConfig()
            {
                Low = 200, // should work for fairly high rtt variance
                High = 200, // rely on automatic tolerance value
                Max = 1000, // drop frames if the delay is 1 sec. or higher
                SpeedUpPerc = 5,
#if PHOTON_VOICE_SOUND_TOUCH_ENABLE
                TempoChangeHQ = false,
#endif
            };
            public int Low; // ms: Audio player initilizes the delay with this value on Start and after flush and targets it during corrections
            public int High; // ms: Audio player tries to keep the delay below this value
            public int Max; // ms: Audio player guarantees that the delay never exceeds this value by dropping frames
            public int Delay // ms: simplified delay API
            {
                get => Low;
                set
                {
                    Low = value;
                    High = value; // rely on automatic tolerance value
                    Max = Default.Max;
                }
            }
            public int SpeedUpPerc; // playback speed-up to catch up the stream
        }
#if PHOTON_VOICE_SOUND_TOUCH_ENABLE
            public bool TempoChangeHQ;
#endif
    }

    // Consumes audio frames via Push(), optionally resizes and writes (OutWrite) them to the output to keep constant delay
    // between output playback position (OutPos) and input stream position (advanced with each write).
    // Assumes output is always playing.
    public abstract class AudioOutDelayControl<T> : AudioOutDelayControl, IAudioOut<T>
    {
        readonly protected int sizeofT = System.Runtime.InteropServices.Marshal.SizeOf(default(T));

        // methods to implement
        // returns playback position in samples, either absolute or in a ring buffer
        abstract public long OutPos { get; }
        abstract public void OutCreate(int frequency, int channels, int bufferSamples);
        abstract public void OutStart();
        // offsetSamples is an offset in the ring buffer
        abstract public void OutWrite(T[] data, int offsetSamples);

        const int TEMPO_UP_SKIP_GROUP = 6;

        private int frameSamples;
        private int frameSize;
        private int bufferSamples;
        private int bufferSamplesHalf;
        private int frequency;

        // stream positions in samples modulo bufferSamples
        private int writeSamplePos; // updated and read only in processFrame() (called from Service() or Push())
        private int clearSamplePos; // updated only in Service()

        private PlayDelayConfig playDelayConfig;
        private int channels;
        private bool started;
        private bool flushed = true;

        private int targetDelaySamples;
        private int upperTargetDelaySamples; // correct if the delay is higher: gradually move to the target using input frames resampling
        private int maxDelaySamples; // skip input samples if the delay is higher to always keep it below

        private const int NO_PUSH_TIMEOUT_MS = 120; // should be greater than Push() call interval, 60 ms max packet length + some jitter
        int lastPushTime = Environment.TickCount - NO_PUSH_TIMEOUT_MS;

        protected readonly ILogger logger;
        protected readonly string logPrefix;
        private readonly bool debugInfo;
        readonly bool processInService = false; // enqueue frame in Push() to process it in Service(), otherwise process directly in Push()

        private T[] zeroFrame;

        // Used in a hack allowing to distinguish between regular and zeroing OutWrite() calls in inherited classes
        public bool IsZeroFrame(T[] f)
        {
            return zeroFrame == f;
        }

        private T[] resampledFrame;

#if PHOTON_VOICE_SOUND_TOUCH_ENABLE
        soundtouch.SoundTouch st;
#endif
        AudioUtil.TempoUp<T> tempoUp;
        bool tempoChangeHQ; // true if library is available

        public AudioOutDelayControl(bool processInService, PlayDelayConfig playDelayConfig, ILogger logger, string logPrefix, bool debugInfo)
        {
            this.processInService = processInService;
            this.playDelayConfig = playDelayConfig;
            this.logger = logger;
            this.logPrefix = logPrefix;
            this.debugInfo = debugInfo;
        }

        public int Lag
        {
            get
            {
                if (this.started)
                {
                    int d = this.writeSamplePos - (int)(OutPos % this.bufferSamples);
                    // detect ring buffer wrapping and convert to ms
                    return (d > bufferSamplesHalf ? d - bufferSamples : d < -bufferSamplesHalf ? d + bufferSamples : d) *1000 / frequency;
                }
                else
                {
                    return 0;
                }
            }
        }

        public bool IsFlushed
        {
            get { return !started || this.flushed; }
        }

        public bool IsPlaying
        {
            get { return !IsFlushed && (Environment.TickCount - lastPushTime < NO_PUSH_TIMEOUT_MS); }
        }

        public void Start(int frequency, int channels, int frameSamples)
        {
            //frequency = (int)(frequency * 1.2); // underrun test
            //frequency = (int)(frequency / 1.2); // overrun test

            this.frequency = frequency;
            this.channels = channels;

            // adjust play delay tolerances so that they do not switch the state until playSamplePos is updated in Service()
            // assuming Service() is called at least 30 times per sec. plus some threshold
            int playSamplePosUpdateInterval = frequency / 20;

            this.targetDelaySamples = playDelayConfig.Low * frequency / 1000;
            // add a tolerance to make sure that we have something to play when the delay set to 0
            if (this.targetDelaySamples < playSamplePosUpdateInterval)
            {
                this.targetDelaySamples = playSamplePosUpdateInterval;
            }

            // make sure that the tolerance is not less than the user set
            this.upperTargetDelaySamples = this.targetDelaySamples + (playDelayConfig.High - playDelayConfig.Low) * frequency / 1000;

            // add some tolerance in case it set to 0 by user
            if (this.upperTargetDelaySamples < targetDelaySamples + playSamplePosUpdateInterval)
            {
                this.upperTargetDelaySamples = targetDelaySamples + playSamplePosUpdateInterval;
            }

            // make sure that the tolerance is not less than the user set
            // if playDelayConfig.High <= playDelayConfig.Max, the catching-up is disabled
            this.maxDelaySamples = this.upperTargetDelaySamples + (playDelayConfig.Max - playDelayConfig.High) * frequency / 1000;

            this.bufferSamples = 4 * this.maxDelaySamples; // make sure we have enough space to detect wrapped positions order in the buffer
            if (this.bufferSamples < 2 * frequency)
            {
                this.bufferSamples = 2 * frequency; // at least 2 sec.
            }
            this.bufferSamplesHalf = bufferSamples / 2;
            this.frameSamples = frameSamples;
            this.frameSize = frameSamples * channels;

            this.writeSamplePos = this.targetDelaySamples;

            if (this.framePool == null || this.framePool.Info != this.frameSize)
            {
                this.framePool = new ArrayPool<T>(FRAME_POOL_CAPACITY, "AudioOutDelayControl", this.frameSize);
            }

            this.zeroFrame = new T[this.frameSize];
            this.resampledFrame = new T[this.frameSize];

#if PHOTON_VOICE_SOUND_TOUCH_ENABLE
            if (this.playDelayConfig.TempoChangeHQ)
            {
                try
                {
                    st = new soundtouch.SoundTouch();
                    st.Channels = (uint)channels;
                    st.SampleRate = (uint)frequency;
                    tempoChangeHQ = true;
                }
                catch (DllNotFoundException e)
                {
                    logger.Log(LogLevel.Error, "{0} SoundTouch library not found, disabling HQ tempo mode: {1}", this.logPrefix, e);
                    tempoChangeHQ = false;
                }
            }
#else
            tempoChangeHQ = false;
#endif
            if (!tempoChangeHQ)
            {
                tempoUp = new AudioUtil.TempoUp<T>();
            }

            OutCreate(frequency, channels, bufferSamples);
            OutStart();
            this.started = true;
            this.logger.Log(LogLevel.Info, "{0} Start: {1} bs={2} ch={3} f={4} tds={5} utds={6} mds={7} speed={8} tempo={9}", this.logPrefix, sizeofT == 2 ? "short" : "float", bufferSamples, channels, frequency, targetDelaySamples, upperTargetDelaySamples, maxDelaySamples, playDelayConfig.SpeedUpPerc, tempoChangeHQ ? "HQ" : "LQ");
        }

        ConcurrentQueue<T[]> frameQueue = new ConcurrentQueue<T[]>();
        public const int FRAME_POOL_CAPACITY = 50;
        ArrayPool<T> framePool;
        bool catchingUp = false;

        void processFrame(T[] frame)
        {
            int playSamplePos = (int)(OutPos % this.bufferSamples);
            int d = this.writeSamplePos - playSamplePos;
            // detect ring buffer wrapping
            int lagSamples = d > bufferSamplesHalf ? d - bufferSamples : d < -bufferSamplesHalf ? d + bufferSamples : d;

            if (!this.flushed)
            {
                if (lagSamples > maxDelaySamples)
                {
                    if (this.debugInfo)
                    {
                        logger.Log(LogLevel.Debug, "{0} overrun {1} {2} {3} {4} {5}", this.logPrefix, upperTargetDelaySamples, lagSamples, playSamplePos, this.writeSamplePos, playSamplePos + targetDelaySamples);
                    }
                    this.writeSamplePos = (playSamplePos + maxDelaySamples) % this.bufferSamples;
                    lagSamples = maxDelaySamples;
                }
                else if (lagSamples < 0)
                {
                    if (this.debugInfo)
                    {
                        logger.Log(LogLevel.Debug, "{0} underrun {1} {2} {3} {4} {5}", this.logPrefix, upperTargetDelaySamples, lagSamples, playSamplePos, this.writeSamplePos, playSamplePos + targetDelaySamples);
                    }
                    this.writeSamplePos = (playSamplePos + targetDelaySamples) % this.bufferSamples;
                    lagSamples = targetDelaySamples;
                }
            }

            if (frame == null) // flush signalled
            {
                this.flushed = true;
                if (this.debugInfo)
                {
                    logger.Log(LogLevel.Debug, "{0} stream flush pause {1} {2} {3} {4} {5}", this.logPrefix, upperTargetDelaySamples, lagSamples, playSamplePos, this.writeSamplePos, playSamplePos + targetDelaySamples);
                }
                if (catchingUp)
                {
#if PHOTON_VOICE_SOUND_TOUCH_ENABLE
                    if (tempoChangeHQ)
                    {
                        st.Flush();
                        writeTempoHQ();
                    }
#endif
                    catchingUp = false;
                    if (this.debugInfo)
                    {
                        logger.Log(LogLevel.Debug, "{0} stream sync reset {1} {2} {3} {4} {5}", this.logPrefix, upperTargetDelaySamples, lagSamples, playSamplePos, this.writeSamplePos, playSamplePos + targetDelaySamples);
                    }
                }

return;
            }
            else
            {
                if (this.flushed)
                {
                    this.writeSamplePos = (playSamplePos + targetDelaySamples) % this.bufferSamples;
                    lagSamples = targetDelaySamples;
                    this.flushed = false;
                    if (this.debugInfo)
                    {
                        logger.Log(LogLevel.Debug, "{0} stream unpause {1} {2} {3} {4} {5}", this.logPrefix, upperTargetDelaySamples, lagSamples, playSamplePos, this.writeSamplePos, playSamplePos + targetDelaySamples);
                    }
                }
            }

            // starting catching up
            if (lagSamples > upperTargetDelaySamples && !catchingUp)
            {
                if (!tempoChangeHQ)
                {
                    tempoUp.Begin(channels, playDelayConfig.SpeedUpPerc, TEMPO_UP_SKIP_GROUP);
                }
#if PHOTON_VOICE_SOUND_TOUCH_ENABLE
                else
                {
                    st.Clear();
                    var tempo = (float)(100 + playDelayConfig.SpeedUpPerc) / 100;
                    st.Tempo = tempo;
                }
#endif
                catchingUp = true;
                if (this.debugInfo)
                {
                    logger.Log(LogLevel.Debug, "{0} stream sync started {1} {2} {3} {4} {5}", this.logPrefix, upperTargetDelaySamples, lagSamples, playSamplePos, this.writeSamplePos, playSamplePos + targetDelaySamples);
                }
            }

            // finishing catching up
            bool frameIsWritten = false; // first frame after switching from catching up requires special processing to flush TempoUp (the end of skipping wave removed if required)
            if (lagSamples <= targetDelaySamples && catchingUp)
            {
                if (!tempoChangeHQ)
                {
                    int skipSamples = tempoUp.End(frame);
                    int resampledLenSamples = frame.Length / channels - skipSamples;
                    Buffer.BlockCopy(frame, skipSamples * channels * sizeofT, resampledFrame, 0, resampledLenSamples * channels * sizeofT);
                    writeResampled(resampledFrame, resampledLenSamples);
                    frameIsWritten = true;
                }
#if PHOTON_VOICE_SOUND_TOUCH_ENABLE
                else
                {
                    st.Flush();
                    writeTempoHQ();
                    st.Clear();
                }
#endif
                catchingUp = false;
                if (this.debugInfo)
                {
                    logger.Log(LogLevel.Debug, "{0} stream sync finished {1} {2} {3} {4} {5}", this.logPrefix, upperTargetDelaySamples, lagSamples, playSamplePos, this.writeSamplePos, playSamplePos + targetDelaySamples);
                }
            }

            if (frameIsWritten)
            {

return;
            }

            if (catchingUp)
            {
                if (!tempoChangeHQ)
                {
                    int resampledLenSamples = tempoUp.Process(frame, resampledFrame);
                    writeResampled(resampledFrame, resampledLenSamples);
                }
#if PHOTON_VOICE_SOUND_TOUCH_ENABLE
                else
                {
                    if (sizeofT == 2)
                    {
                        st.PutSamplesI16(frame as short[], (uint)(frame.Length / channels));
                    }
                    else
                    {
                        st.PutSamples(frame as float[], (uint)(frame.Length / channels));
                    }
                    lagSamples -= writeTempoHQ();
                }
#endif
            }
            else
            {
                OutWrite(frame, this.writeSamplePos);
                this.writeSamplePos = (this.writeSamplePos + frame.Length / this.channels) % this.bufferSamples;
            }
        }

        // should be called in Update thread
        public void Service()
        {
            if (this.started)
            {
                if (processInService)
                {
                    while (frameQueue.TryDequeue(out T[] frame))
                    {
                        processFrame(frame);
                        if (frame == null)
                        {
                            break;  // flush signalled
                        }
                        framePool.Free(frame, frame.Length);
                    }
                }

                int playSamplePos = (int)(OutPos % this.bufferSamples);
                if (this.clearSamplePos > playSamplePos)
                {
                    this.clearSamplePos -= this.bufferSamples;
                }
                // clear played back buffer segment
                for (; this.clearSamplePos + this.frameSamples < playSamplePos; this.clearSamplePos += this.frameSamples)
                {
                    int o = (int)(this.clearSamplePos % this.bufferSamples);
                    if (o < 0) o += this.bufferSamples;
                    OutWrite(this.zeroFrame, o);
                }
            }
        }

#if PHOTON_VOICE_SOUND_TOUCH_ENABLE
        int writeTempoHQ()
        {
            int resampledLenSamples;
            if (sizeofT == 2)
            {
                resampledLenSamples = (int)st.ReceiveSamplesI16(resampledFrame as short[], (uint)(resampledFrame.Length / channels));
            }
            else
            {
                resampledLenSamples = (int)st.ReceiveSamples(resampledFrame as float[], (uint)(resampledFrame.Length / channels));
            }
            return writeResampled(resampledFrame, resampledLenSamples);
        }
#endif

        int writeResampled(T[] f, int resampledLenSamples)
        {
            // zero not used part of the buffer because OutWrite() writes entire frame
            // if this frame is the last, grabage may be played back
            int tailSize = (f.Length - resampledLenSamples * channels) * sizeofT;
            if (tailSize > 0) // it may be 0 what BlockCopy does not like
            {
                Buffer.BlockCopy(this.zeroFrame, 0, f, resampledLenSamples * channels * sizeofT, tailSize);
            }

            OutWrite(f, this.writeSamplePos);
            this.writeSamplePos = (this.writeSamplePos + resampledLenSamples) % this.bufferSamples;
            return resampledLenSamples;
        }

        // may be called on any thread
        public void Push(T[] frame)
        {
            if (!this.started)
            {
                return;
            }

            if (frame.Length == 0)
            {
                return;
            }

            if (frame.Length != this.frameSize)
            {
                logger.Log(LogLevel.Error, "{0} audio frames are not of size: {1} != {2}", this.logPrefix, frame.Length, this.frameSize);
                return;
            }

            if (processInService)
            {
                T[] b = framePool.New();
                Buffer.BlockCopy(frame, 0, b, 0, frame.Length * sizeofT);
                this.frameQueue.Enqueue(b);
            }
            else
            {
                processFrame(frame);
            }

            lastPushTime = Environment.TickCount;
        }

        public void Flush()
        {
            if (processInService)
            {
                this.frameQueue.Enqueue(null);
            }
            else
            {
                processFrame(null);
            }
        }

        virtual public void Stop()
        {
#if PHOTON_VOICE_SOUND_TOUCH_ENABLE
            if (st != null)
            {
                st.Dispose();
                st = null;
            }
#endif
            this.started = false;
        }
    }
}