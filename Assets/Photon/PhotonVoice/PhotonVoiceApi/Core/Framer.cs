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

namespace Photon.Voice
{
    /// <summary>Utility class to re-frame packets.</summary>
    public class Framer<T>
    {
        protected T[] frame;

        /// <summary>Create new Framer instance.</summary>
        public Framer(int frameSize)
        {
            this.frame = new T[frameSize];
            var x = new T[1];
            if (x[0] is byte)
                this.sizeofT = sizeof(byte);
            else if (x[0] is short)
                this.sizeofT = sizeof(short);
            else if (x[0] is float)
                this.sizeofT = sizeof(float);
            else
                throw new Exception("Input data type is not supported: " + x[0].GetType());
        }
        protected int sizeofT;
        protected int framePos = 0;

        /// <summary>Append arbitrary-sized buffer and return available full frames.</summary>
        /// <param name="buf">Array of samples to add.</param>
        /// <returns>Enumerator of full frames (might be none).</returns>
        public virtual IEnumerable<T[]> Frame(T[] buf)
        {
            // quick return in trivial case
            if (frame.Length == buf.Length && framePos == 0)
            {
                yield return buf;
            }
            else
            {
                var bufPos = 0;

                while (frame.Length - framePos <= buf.Length - bufPos)
                {
                    var l = frame.Length - framePos;
                    Buffer.BlockCopy(buf, bufPos * sizeofT, frame, framePos * sizeofT, l * sizeofT);
                    //Console.WriteLine("=== Y {0} {1} -> {2} {3} ", bufPos, bufPos + l, sourceFramePos, sourceFramePos + l);
                    bufPos += l;
                    framePos = 0;

                    yield return this.frame;
                }
                if (bufPos != buf.Length)
                {
                    var l = buf.Length - bufPos;
                    Buffer.BlockCopy(buf, bufPos * sizeofT, frame, framePos * sizeofT, l * sizeofT);
                    //Console.WriteLine("=== L {0} {1} -> {2} {3} ", bufPos, bufPos + l, sourceFramePos, sourceFramePos + l);
                    framePos += l;
                }
            }
        }
    }

    public class FramerResampler<T> : Framer<T>
    {
        protected bool TisFloat;
        protected bool interpolate;
        protected int channels;
        protected int resampleNum;
        protected int resampleDen;
        protected float resampleRatioInv;
        protected int delta;
        T inSampleCh1;
        T inSampleCh2;
        float inSampleCh1Interp;
        float inSampleCh2Interp;
        float inSampleCh1InterpChange; // duff between current and previous samples values
        float inSampleCh2InterpChange;
        public FramerResampler(int frameSize, int channels, int resampleNum, int resampleDen, bool interpolate)
            : base(frameSize / channels * channels)
        {
            this.TisFloat = default(T) is float;
            this.channels = channels;
            this.resampleNum = resampleNum;
            this.resampleDen = resampleDen;
            this.interpolate = interpolate;
            this.resampleRatioInv = (float)resampleDen / resampleNum;
        }

        public override IEnumerable<T[]> Frame(T[] bufT)
        {
            int bufPos = 0;
            int bufLen = bufT.Length / channels * channels;
            if (!interpolate)
            {
                switch (channels)
                {
                    case 1:
                        while (bufPos < bufLen)
                        {
                            if (delta <= 0)
                            {
                                inSampleCh1 = bufT[bufPos++];
                                delta += resampleNum;
                            }
                            while (delta > 0)
                            {
                                this.frame[framePos++] = inSampleCh1;
                                if (framePos == this.frame.Length)
                                {
                                    yield return this.frame;
                                    framePos = 0;
                                }
                                delta -= resampleDen;
                            }
                        }
                        break;
                    case 2:
                        while (bufPos < bufLen)
                        {
                            if (delta <= 0)
                            {
                                inSampleCh1 = bufT[bufPos++];
                                inSampleCh2 = bufT[bufPos++];
                                delta += resampleNum;
                            }
                            while (delta > 0)
                            {
                                this.frame[framePos++] = inSampleCh1;
                                this.frame[framePos++] = inSampleCh2;
                                if (framePos == this.frame.Length)
                                {
                                    yield return this.frame;
                                    framePos = 0;
                                }
                                delta -= resampleDen;
                            }
                        }
                        break;
                }
            }
            else
            {
                // float and short code differ only in type casts for arrays and values stored to frame
                if (TisFloat)
                {
                    var buf = bufT as float[];
                    var frame = this.frame as float[];
                    // interpolation factor
                    float deltaK = (float)delta / resampleNum; // update regularly from delta to avoid float precision loss
                    switch (channels)
                    {
                        case 1:
                            while (bufPos < bufLen)
                            {
                                if (delta <= 0)
                                {
                                    var x = buf[bufPos++];
                                    inSampleCh1InterpChange = inSampleCh1Interp - x;
                                    inSampleCh1Interp = x;
                                    delta += resampleNum;
                                    deltaK += 1.0f;
                                }
                                while (delta > 0)
                                {
                                    frame[framePos++] = inSampleCh1Interp + inSampleCh1InterpChange * deltaK;
                                    if (framePos == frame.Length)
                                    {
                                        yield return this.frame;
                                        framePos = 0;
                                    }
                                    delta -= resampleDen;
                                    deltaK -= resampleRatioInv;
                                }
                            }
                            break;
                        case 2:
                            while (bufPos < bufLen)
                            {
                                if (delta <= 0)
                                {
                                    var x1 = buf[bufPos++];
                                    var x2 = buf[bufPos++];
                                    inSampleCh1InterpChange = inSampleCh1Interp - x1;
                                    inSampleCh2InterpChange = inSampleCh2Interp - x2;
                                    inSampleCh1Interp = x1;
                                    inSampleCh2Interp = x2;
                                    delta += resampleNum;
                                    deltaK += 1.0f;
                                }
                                while (delta > 0)
                                {
                                    frame[framePos++] = inSampleCh1Interp + inSampleCh1InterpChange * deltaK;
                                    frame[framePos++] = inSampleCh2Interp + inSampleCh2InterpChange * deltaK;
                                    if (framePos == frame.Length)
                                    {
                                        yield return this.frame;
                                        framePos = 0;
                                    }
                                    delta -= resampleDen;
                                    deltaK -= resampleRatioInv;
                                }
                            }
                            break;
                    }
                }
                else
                {
                    var buf = bufT as short[];
                    var frame = this.frame as short[];
                    // interpolation factor
                    float deltaK = (float)delta / resampleNum; // update regularly from delta to avoid float precision loss
                    switch (channels)
                    {
                        case 1:
                            while (bufPos < bufLen)
                            {
                                if (delta <= 0)
                                {
                                    var x = buf[bufPos++];
                                    inSampleCh1InterpChange = inSampleCh1Interp - x;
                                    inSampleCh1Interp = x;
                                    delta += resampleNum;
                                    deltaK += 1.0f;
                                }
                                while (delta > 0)
                                {
                                    frame[framePos++] = (short)(inSampleCh1Interp + inSampleCh1InterpChange * deltaK);
                                    if (framePos == frame.Length)
                                    {
                                        yield return this.frame;
                                        framePos = 0;
                                    }
                                    delta -= resampleDen;
                                    deltaK -= resampleRatioInv;
                                }
                            }
                            break;
                        case 2:
                            while (bufPos < bufLen)
                            {
                                if (delta <= 0)
                                {
                                    var x1 = buf[bufPos++];
                                    var x2 = buf[bufPos++];
                                    inSampleCh1InterpChange = inSampleCh1Interp - x1;
                                    inSampleCh2InterpChange = inSampleCh2Interp - x2;
                                    inSampleCh1Interp = x1;
                                    inSampleCh2Interp = x2;
                                    delta += resampleNum;
                                    deltaK += 1.0f;
                                }
                                while (delta > 0)
                                {
                                    frame[framePos++] = (short)(inSampleCh1Interp + inSampleCh1InterpChange * deltaK);
                                    frame[framePos++] = (short)(inSampleCh2Interp + inSampleCh2InterpChange * deltaK);
                                    if (framePos == frame.Length)
                                    {
                                        yield return this.frame;
                                        framePos = 0;
                                    }
                                    delta -= resampleDen;
                                    deltaK -= resampleRatioInv;
                                }
                            }
                            break;
                    }
                }
            }
        }
    }
}