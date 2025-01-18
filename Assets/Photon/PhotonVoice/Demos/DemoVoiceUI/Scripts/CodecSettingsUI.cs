namespace Photon.Voice.Unity.Demos.DemoVoiceUI
{
    using UnityEngine;
    using UnityEngine.UI;
    using System.Collections.Generic;
    using POpusCodec.Enums;

    public class CodecSettingsUI : MonoBehaviour
    {
        #pragma warning disable 649
        [SerializeField]
        private Dropdown frameDurationDropdown;
        [SerializeField]
        private Dropdown samplingRateDropdown;
        [SerializeField]
        private InputField bitrateInputField;
        [SerializeField]
        private Recorder recorder;
        #pragma warning restore 649

        private static readonly List<string> frameDurationOptions = new List<string>
        {
            "2.5ms", // EnumValueToString(OpusCodec.FrameDuration.Frame2dot5ms),
            "5ms",   // EnumValueToString(OpusCodec.FrameDuration.Frame5ms),
            "10ms",  // EnumValueToString(OpusCodec.FrameDuration.Frame10ms),
            "20ms",  // EnumValueToString( OpusCodec.FrameDuration.Frame20ms),
            "40ms",  // EnumValueToString(OpusCodec.FrameDuration.Frame40ms),
            "60ms"   // EnumValueToString(OpusCodec.FrameDuration.Frame60ms)
        };

        private static readonly List<string> samplingRateOptions = new List<string>
        {
            "8kHz",  // EnumValueToString(SamplingRate.Sampling08000),
            "12kHz", // EnumValueToString(SamplingRate.Sampling12000),
            "16kHz", // EnumValueToString(SamplingRate.Sampling16000),
            "24kHz", // EnumValueToString(SamplingRate.Sampling24000),
            "48kHz", // EnumValueToString(SamplingRate.Sampling48000)
        };

        private void Awake()
        {
            this.frameDurationDropdown.ClearOptions();
            this.frameDurationDropdown.AddOptions(frameDurationOptions);
            this.InitFrameDuration();
            this.frameDurationDropdown.SetSingleOnValueChangedCallback(this.OnFrameDurationChanged);

            this.samplingRateDropdown.ClearOptions();
            this.samplingRateDropdown.AddOptions(samplingRateOptions);
            this.InitSamplingRate();
            this.samplingRateDropdown.SetSingleOnValueChangedCallback(this.OnSamplingRateChanged);

            this.bitrateInputField.SetSingleOnValueChangedCallback(this.OnBitrateChanged);
            this.InitBitrate();
        }

        #region UNITY_EDITOR

        private void Update()
        {
            this.InitFrameDuration();
            this.InitSamplingRate();
            this.InitBitrate();
        }

        #endregion

        private void OnBitrateChanged(string newBitrateString)
        {
            int newBirate;
            if (int.TryParse(newBitrateString, out newBirate))
            {
                this.recorder.Bitrate = newBirate;
                if (this.recorder.RequiresRestart)
                {
                    this.recorder.RestartRecording();
                }
            }
        }

        private void OnFrameDurationChanged(int index)
        {
            OpusCodec.FrameDuration newFrameDuration = this.recorder.FrameDuration;
            switch (index)
            {
                case 0:
                    newFrameDuration = OpusCodec.FrameDuration.Frame2dot5ms;
                    break;
                case 1:
                    newFrameDuration = OpusCodec.FrameDuration.Frame5ms;
                    break;
                case 2:
                    newFrameDuration = OpusCodec.FrameDuration.Frame10ms;
                    break;
                case 3:
                    newFrameDuration = OpusCodec.FrameDuration.Frame20ms;
                    break;
                case 4:
                    newFrameDuration = OpusCodec.FrameDuration.Frame40ms;
                    break;
                case 5:
                    newFrameDuration = OpusCodec.FrameDuration.Frame60ms;
                    break;
            }
            this.recorder.FrameDuration = newFrameDuration;
            if (this.recorder.RequiresRestart)
            {
                this.recorder.RestartRecording();
            }
        }

        private void OnSamplingRateChanged(int index)
        {
            SamplingRate newSamplingRate = this.recorder.SamplingRate;
            switch (index)
            {
                case 0:
                    newSamplingRate = SamplingRate.Sampling08000;
                    break;
                case 1:
                    newSamplingRate = SamplingRate.Sampling12000;
                    break;
                case 2:
                    newSamplingRate = SamplingRate.Sampling16000;
                    break;
                case 3:
                    newSamplingRate = SamplingRate.Sampling24000;
                    break;
                case 4:
                    newSamplingRate = SamplingRate.Sampling48000;
                    break;
            }
            this.recorder.SamplingRate = newSamplingRate;
            if (this.recorder.RequiresRestart)
            {
                this.recorder.RestartRecording();
            }
        }

        private void InitFrameDuration()
        {
            int index = 0;
            switch (this.recorder.FrameDuration)
            {
                case OpusCodec.FrameDuration.Frame5ms:
                    index = 1;
                    break;
                case OpusCodec.FrameDuration.Frame10ms:
                    index = 2;
                    break;
                case OpusCodec.FrameDuration.Frame20ms:
                    index = 3;
                    break;
                case OpusCodec.FrameDuration.Frame40ms:
                    index = 4;
                    break;
                case OpusCodec.FrameDuration.Frame60ms:
                    index = 5;
                    break;
            }
            this.frameDurationDropdown.value = index;
        }

        private void InitSamplingRate()
        {
            int index = 0;
            switch (this.recorder.SamplingRate)
            {
                case SamplingRate.Sampling12000:
                    index = 1;
                    break;
                case SamplingRate.Sampling16000:
                    index = 2;
                    break;
                case SamplingRate.Sampling24000:
                    index = 3;
                    break;
                case SamplingRate.Sampling48000:
                    index = 4;
                    break;
            }
            this.samplingRateDropdown.value = index;
        }

        private void InitBitrate()
        {
            this.bitrateInputField.text = this.recorder.Bitrate.ToString();
        }

        //private static string EnumValueToString(object enumValue)
        //{
        //    return Enum.GetName(enumValue.GetType(), enumValue);
        //}
    }
}