using System;
using System.Collections.Generic;

namespace Photon.Voice
{
    // Keeps buffer size within given bounds (discards or repeats samples) even if numbers of pushed and read samples per second are different
    public class AudioSyncBuffer<T> : AudioOutDelayControl<T>
    {
        T[] buffer;
        int readPosSamples;
        int sampleRate;
        int channels;
        int bufferSamples;
        bool started;

        public AudioSyncBuffer(PlayDelayConfig playDelayConfig, ILogger logger, string logPrefix, bool debugInfo)
            : base(false, playDelayConfig, logger, "[PV] [Unity] AudioSyncBuffer" + (logPrefix == "" ? "" : " " + logPrefix), debugInfo)
        {
        }

        override public long OutPos => readPosSamples;

        override public void OutCreate(int frequency, int channels, int bufferSamples)
        {
            sampleRate = frequency;
            this.channels = channels;
            this.bufferSamples = bufferSamples;
            buffer = new T[channels * bufferSamples];
        }

        override public void OutStart()
        {
            started = true;
        }

        override public void OutWrite(T[] data, int offsetSamples)
        {
            int offset = offsetSamples * channels;
            int rem = buffer.Length - offset;
            if (data.Length > rem)
            {
                Array.Copy(data, 0, buffer, offset, rem);
                Array.Copy(data, rem, buffer, 0, data.Length - rem);
            }
            else
            {
                Array.Copy(data, 0, buffer, offset, data.Length);
            }
        }

        override public void Stop()
        {
            started = false;
        }

        public void Read(T[] outBuf, int outChannels, int outSampleRate)
        {
            if (started)
            {
                int inSamples = outBuf.Length / outChannels * sampleRate / outSampleRate;
                int offset = readPosSamples * channels;
                int rem = buffer.Length - offset;
                if (this.sampleRate == outSampleRate && this.channels == outChannels)
                {
                    if (outBuf.Length > rem)
                    {
                        Array.Copy(buffer, offset, outBuf, 0, rem);
                        Array.Copy(buffer, 0, outBuf, rem, outBuf.Length - rem);
                    }
                    else
                    {
                        Array.Copy(buffer, offset, outBuf, 0, outBuf.Length);
                    }
                }
                else
                {
                    int inLen = inSamples * channels;
                    if (inLen > rem)
                    {
                        int outRem = rem * outSampleRate / sampleRate * outChannels / channels;
                        AudioUtil.Resample(buffer, offset, rem, channels, outBuf, 0, outRem, outChannels);
                        AudioUtil.Resample(buffer, 0, inLen - rem, channels, outBuf, outRem, outBuf.Length - outRem, outChannels);
                    }
                    else
                    {
                        AudioUtil.Resample(buffer, offset, inLen, channels, outBuf, 0, outBuf.Length, outChannels);
                    }
                }
               readPosSamples = (readPosSamples + inSamples) % bufferSamples;
            }
        }
    }
}