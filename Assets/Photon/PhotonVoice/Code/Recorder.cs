// ----------------------------------------------------------------------------
// <copyright file="Recorder.cs" company="Exit Games GmbH">
//   Photon Voice for Unity - Copyright (C) 2018 Exit Games GmbH
// </copyright>
// <summary>
// Component representing outgoing audio stream in scene.
// </summary>
// <author>developer@photonengine.com</author>
// ----------------------------------------------------------------------------

using System;
using POpusCodec.Enums;
using UnityEngine;
using UnityEngine.Serialization;
using System.Linq;

namespace Photon.Voice.Unity
{
    /// <summary>
    /// Component representing outgoing audio stream in scene.
    /// </summary>
    [AddComponentMenu("Photon Voice/Recorder")]
    [HelpURL("https://doc.photonengine.com/en-us/voice/v2/getting-started/recorder")]
    [DisallowMultipleComponent]
    public class Recorder : VoiceComponent
    {
        public const int MIN_OPUS_BITRATE = 6000;
        public const int MAX_OPUS_BITRATE = 510000;

        #region Private Fields

        [SerializeField]
        private bool voiceDetection;

        [SerializeField]
        private float voiceDetectionThreshold = 0.01f;

        [SerializeField]
        private int voiceDetectionDelayMs = 500;

        private object userData;

        private LocalVoice voice = LocalVoiceAudioDummy.Dummy;

        private IAudioDesc inputSource;

        private VoiceConnection voiceConnection;

        [SerializeField]
        [FormerlySerializedAs("audioGroup")]
        private byte interestGroup;

        [SerializeField]
        private bool useTargetPlayers; // to distinguish between null and empty targetPlayers array

        [SerializeField]
        private int[] targetPlayers;

        [SerializeField]
        private bool debugEchoMode;

        [SerializeField]
        private bool reliableMode;

        [SerializeField]
        private bool encrypt;

        [SerializeField]
        private bool transmitEnabled = true;

        [SerializeField]
        private SamplingRate samplingRate = SamplingRate.Sampling24000;

        [SerializeField]
        private OpusCodec.FrameDuration frameDuration = OpusCodec.FrameDuration.Frame20ms;

        [SerializeField, Range(MIN_OPUS_BITRATE, MAX_OPUS_BITRATE)]
        private int bitrate = 30000;

        [SerializeField]
        private InputSourceType sourceType;

        [SerializeField]
        private MicType microphoneType;

        [SerializeField]
        private AudioClip audioClip;

        [SerializeField]
        private bool loopAudioClip = true;

        [SerializeField]
        private bool recordingEnabled = true;

        private Func<IAudioDesc> inputFactory;

        [SerializeField]
        private IOS.AudioSessionParameters audioSessionParameters = IOS.AudioSessionParametersPresets.Game;

        // stores the preset for editor, the microphone initialization used only the field above
#if UNITY_EDITOR
        public enum EditorIosAudioSessionPreset
        {
            Custom,
            Game,
            VoIP,
        }
        [SerializeField]
#pragma warning disable CS0414 // The field is assigned but its value is never used (used in RecorderEditor)
        private EditorIosAudioSessionPreset editorAudioSessionPreset = EditorIosAudioSessionPreset.Game;
#pragma warning restore CS0414
#endif

        [SerializeField]
        private AndroidAudioInParameters androidMicrophoneSettings = AndroidAudioInParameters.Default;

        private bool isPausedOrInBackground;

        [SerializeField]
        private bool stopRecordingWhenPaused;

        [SerializeField]
        private bool useOnAudioFilterRead;

        [SerializeField]
        private bool useMicrophoneTypeFallback = true;

        [SerializeField]
        private bool recordWhenJoined = true;

        private DeviceInfo microphoneDevice = DeviceInfo.Default;

        // int instead of bool to use Interlocked.Exchange()
        private int microphoneDeviceChangePending;
        #endregion

        #region Properties

        internal void MicrophoneDeviceChangeDetected()
        {
            this.microphoneDeviceChangePending = 1;
        }

        /// <summary>If true, audio transmission is enabled.</summary>
        public bool TransmitEnabled
        {
            get { return this.transmitEnabled; }
            set
            {
                if (value != this.transmitEnabled)
                {
                    this.transmitEnabled = value;
                    if (this.voice != LocalVoiceAudioDummy.Dummy)
                    {
                        this.voice.TransmitEnabled = value;
                    }
                }
            }
        }

        /// <summary>If true, voice stream is sent encrypted.</summary>
        /// <remarks>Initialized with serialized field and can be updated by Editor at runtime.</remarks>
        public bool Encrypt
        {
            get => this.voice.Encrypt;
            set
            {
                this.voice.Encrypt = value;
                this.encrypt = value;
            }
        }

        /// <summary>If true, outgoing stream routed back to client via server same way as for remote client's streams.</summary>
        /// <remarks>Initialized with serialized field and can be updated by Editor at runtime.</remarks>
        public bool DebugEchoMode
        {
            get => this.voice.DebugEchoMode;
            set
            {
                this.voice.DebugEchoMode = value;
                this.debugEchoMode = value;
            }
        }

        /// <summary>If true, stream data sent in reliable mode.</summary>
        /// <remarks>Initialized with serialized field and can be updated by Editor at runtime.</remarks>
        public bool ReliableMode
        {
            get => this.voice.Reliable;
            set
            {
                this.voice.Reliable = value;
                this.reliableMode = value;
            }
        }

        /// <summary>If true, voice detection enabled.</summary>
        public bool VoiceDetection
        {
            get
            {
                return this.voiceDetection;
            }
            set
            {
                this.voiceDetection = value;
                if (this.VoiceDetector != null)
                {
                    this.VoiceDetector.On = value;
                }
            }
        }

        /// <summary>Voice detection threshold (0..1, where 1 is full amplitude).</summary>
        public float VoiceDetectionThreshold
        {
            get
            {
                return this.voiceDetectionThreshold;
            }
            set
            {
                if (this.voiceDetectionThreshold.Equals(value))
                {
                    return;
                }
                if (value < 0f || value > 1f)
                {
                    this.Logger.Log(LogLevel.Error, "Value out of range: VAD Threshold needs to be between [0..1], requested value: {0}", value);
                    return;
                }
                this.voiceDetectionThreshold = value;
                if (this.VoiceDetector != null)
                {
                    this.VoiceDetector.Threshold = this.voiceDetectionThreshold;
                }
            }
        }

        /// <summary>Keep detected state during this time after signal level dropped below threshold. Default is 500ms</summary>
        public int VoiceDetectionDelayMs
        {
            get
            {
                return this.voiceDetectionDelayMs;
            }
            set
            {
                if (this.voiceDetectionDelayMs == value)
                {
                    return;
                }
                this.voiceDetectionDelayMs = value;
                if (this.VoiceDetector != null)
                {
                    this.VoiceDetector.ActivityDelayMs = value;
                }
            }
        }

        /// <summary>Custom user object to be sent in the voice stream info event.</summary>
        public object UserData
        {
            get { return this.userData; }
            set
            {
                if (this.userData != value)
                {
                    this.userData = value;
                    this.Logger.Log(LogLevel.Info, "Recorder.UserData changed");
                    this.RestartRecording();
                }
            }
        }

        /// <summary>Set the method returning new Voice.IAudioDesc instance to be assigned to a new voice created with Source set to Factory</summary>
        public Func<IAudioDesc> InputFactory
        {
            get
            {
                return this.inputFactory;
            }
            set
            {
                if (this.inputFactory != value)
                {
                    this.inputFactory = value;
                    this.Logger.Log(LogLevel.Info, "Recorder.InputFactory changed");
                    if (this.SourceType == InputSourceType.Factory)
                    {
                        this.RestartRecording();
                    }
                }
            }
        }

        /// <summary>Returns voice activity detector for recorder's audio stream.</summary>
        public AudioUtil.IVoiceDetector VoiceDetector
        {
            get { return this.voiceAudio != null ? this.voiceAudio.VoiceDetector : null; }
        }

        /// <summary>Target interest group that will receive transmitted audio.</summary>
        /// <remarks>
        /// If InterestGroup != 0, recorder's audio data is sent only to clients listening to this group.
        /// Initialized with serialized field and can be updated by Editor at runtime.
        /// </remarks>
        public byte InterestGroup
        {
            get => this.voice.InterestGroup;
            set
            {
                this.voice.InterestGroup = value;
                this.interestGroup = value;
            }
        }

        /// <summary>Target players which will receive transmitted audio.</summary>
        /// <remarks>Initialized with serialized field and can be updated by Editor at runtime </remarks>
        public int[] TargetPlayers
        {
            get => this.voice.TargetPlayers;
            set
            {
                var x = value;
                this.voice.TargetPlayers = x;
                this.targetPlayers = x;
                this.useTargetPlayers = x != null;
            }
        }

        /// <summary>Returns true if audio stream broadcasts.</summary>
        public bool IsCurrentlyTransmitting
        {
            get { return this.RecordingEnabled && this.TransmitEnabled && this.voice.IsCurrentlyTransmitting; }
        }

        /// <summary>Level meter utility.</summary>
        public AudioUtil.ILevelMeter LevelMeter
        {
            get { return this.voiceAudio != null ? this.voiceAudio.LevelMeter : null; }
        }

        /// <summary>If true, voice detector calibration is in progress.</summary>
        public bool VoiceDetectorCalibrating { get { return this.voiceAudio != null && this.TransmitEnabled && this.voiceAudio.VoiceDetectorCalibrating; } }

        protected ILocalVoiceAudio voiceAudio { get { return this.voice as ILocalVoiceAudio; } }

        /// <summary>Audio data source.</summary>
        public InputSourceType SourceType
        {
            get { return this.sourceType; }
            set
            {
                if (this.sourceType != value)
                {
                    this.sourceType = value;
                    this.Logger.Log(LogLevel.Info, "Recorder.Source changed");
                    this.RestartRecording();
                }
            }
        }

        /// <summary>Which microphone API to use when the Source is set to Microphone.</summary>
        public MicType MicrophoneType
        {
            get
            {
                if (Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    return MicType.Photon; // force Photon type on WebGL
                }
                else
                {
                    return this.microphoneType;
                }
            }
            set
            {
                if (this.microphoneType != value)
                {
                    this.microphoneType = value;
                    this.Logger.Log(LogLevel.Info, "Recorder.MicrophoneType changed");
                    if (this.SourceType == InputSourceType.Microphone)
                    {
                        this.RestartRecording();
                    }
                }
            }
        }

        /// <summary>Source audio clip.</summary>
        public AudioClip AudioClip
        {
            get { return this.audioClip; }
            set
            {
                if (this.audioClip != value)
                {
                    this.audioClip = value;
                    this.Logger.Log(LogLevel.Info, "Recorder.AudioClip change");
                    if (this.SourceType == InputSourceType.AudioClip)
                    {
                        this.RestartRecording();
                    }
                }
            }
        }

        /// <summary>Loop playback for audio clip sources.</summary>
        public bool LoopAudioClip
        {
            get { return this.loopAudioClip; }
            set
            {
                if (this.loopAudioClip != value)
                {
                    this.loopAudioClip = value;
                    if (this.RecordingEnabled && this.SourceType == InputSourceType.AudioClip)
                    {
                        AudioClipWrapper wrapper = this.inputSource as AudioClipWrapper;
                        if (wrapper != null)
                        {
                            wrapper.Loop = value;
                        }
                        else
                        {
                            this.Logger.Log(LogLevel.Error, "Unexpected: Recorder inputSource is not of AudioClipWrapper type or is null.");
                        }
                    }
                }
            }
        }

        /// <summary>Outgoing audio stream sampling rate.</summary>
        public SamplingRate SamplingRate
        {
            get { return this.samplingRate; }
            set
            {
                if (this.samplingRate != value)
                {
                    this.samplingRate = value;
                    this.Logger.Log(LogLevel.Info, "Recorder.SamplingRate changed");
                    this.RestartRecording();
                }
            }
        }

        /// <summary>Outgoing audio stream encoder delay.</summary>
        public OpusCodec.FrameDuration FrameDuration
        {
            get { return this.frameDuration; }
            set
            {
                if (this.frameDuration != value)
                {
                    this.frameDuration = value;
                    this.Logger.Log(LogLevel.Info, "Recorder.FrameDuration changed");
                    this.RestartRecording();
                }
            }
        }

        /// <summary>Outgoing audio stream bitrate.</summary>
        public int Bitrate
        {
            get { return this.bitrate; }
            set
            {
                if (this.bitrate != value)
                {
                    if (value < MIN_OPUS_BITRATE || value > MAX_OPUS_BITRATE)
                    {
                        this.Logger.Log(LogLevel.Error, "Unsupported bitrate value {0}, valid range: {1}-{2}", value, MIN_OPUS_BITRATE, MAX_OPUS_BITRATE);
                    }
                    else
                    {
                        this.bitrate = value;
                        this.Logger.Log(LogLevel.Info, "Recorder.Bitrate changed");
                        this.RestartRecording();
                    }
                }
            }
        }

        /// <summary>Gets or sets whether this Recorder is recording audio to be transmitted.</summary>
        public bool RecordingEnabled
        {
            get
            {
                return this.recordingEnabled;
            }
            set
            {
                if (this.recordingEnabled != value)
                {
                    this.recordingEnabled = value;
                    if (this.recordingEnabled)
                    {
                        this.RestartRecording();
                    }
                    else
                    {
                        this.StopRecording();
                    }
                }
            }
        }

        /// <summary> If true, stop recording when paused resume/restart when un-paused. </summary>
        public bool StopRecordingWhenPaused
        {
            get { return this.stopRecordingWhenPaused; }
            set { this.stopRecordingWhenPaused = value; }
        }

        /// <summary> If true, recording will make use of Unity's OnAudioFitlerRead callback from a muted local AudioSource. </summary>
        /// <remarks> If enabled, 3D sounds and voice positioning can be lost. </remarks>
        public bool UseOnAudioFilterRead
        {
            get
            {
                return this.useOnAudioFilterRead;
            }
            set
            {
                if (this.useOnAudioFilterRead != value)
                {
                    this.useOnAudioFilterRead = value;
                    this.Logger.Log(LogLevel.Info, "Recorder.UseOnAudioFilterRead changed");
                    if (this.SourceType == InputSourceType.Microphone && this.MicrophoneType == MicType.Unity)
                    {
                        this.RestartRecording();
                    }
                }
            }
        }

        /// <summary> If true, if recording fails to start with Unity microphone type, Photon microphone type is used -if available- as a fallback and vice versa. </summary>
        public bool UseMicrophoneTypeFallback
        {
            get
            {
                return this.useMicrophoneTypeFallback;
            }
            set
            {
                this.useMicrophoneTypeFallback = value;
            }
        }

        /// <summary> If true, recording starts when joining the room and stops when leaving the room. </summary>
        public bool RecordWhenJoined
        {
            get
            {
                return this.recordWhenJoined;
            }
            set
            {
                this.recordWhenJoined = value;
            }
        }

        public DeviceInfo MicrophoneDevice
        {
            get
            {
                return this.microphoneDevice;
            }
            set
            {
                if (this.microphoneDevice != value)
                {
                    this.microphoneDevice = value;
                    this.Logger.Log(LogLevel.Info, "Recorder.MicrophoneDevice changed");
                    if (this.SourceType == InputSourceType.Microphone)
                    {
                        this.RestartRecording();
                    }
                }
            }
        }

        public bool AndroidMicrophoneAGC {
			get {
				return this.androidMicrophoneSettings.EnableAGC;
			}
		}
        public bool AndroidMicrophoneAEC {
			get {
				return this.androidMicrophoneSettings.EnableAEC;
			}
		}
        public bool AndroidMicrophoneNS  {
			get {
				return this.androidMicrophoneSettings.EnableNS;
			}
		}

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the Recorder component to be able to transmit audio.
        /// Called by the VoiceConnection this Recorder belongs to.
        /// </summary>
        /// <param name="connection">The VoiceConnection to be used with this Recorder.</param>
        internal bool Init(VoiceConnection connection)
        {
            if (!this.isActiveAndEnabled)
            {
                this.Logger.Log(LogLevel.Warning, "Recorder is disabled.");
                return false;
            }
            if (this.voiceConnection != null)
            {
                this.Logger.Log(LogLevel.Warning, "Recorder already initialized.");
                return false;
            }
            this.voiceConnection = connection;
            this.RestartRecording(); // in case RecordingEnabled is true
            return true;
        }

        internal bool Deinit(VoiceConnection connection)
        {
            this.StopRecording();
            this.voiceConnection = null;
            return true;
        }

        // prevents multiple restarts per Update()
        // int instead of bool to use Interlocked.Exchange()
        int restartRecordingPending = 0;

        /// <summary>
        /// Restarts recording if <see cref="Recorder.RecordingEnabled"/> is true
        /// </summary>
        public bool RestartRecording()
        {
            if (this.RecordingEnabled)
            {
                restartRecordingPending = 1;
            }
            return this.RecordingEnabled;
        }

        /// <summary>Trigger voice detector calibration process.
        /// While calibrating, keep silence. Voice detector sets threshold basing on measured background noise level.
        /// </summary>
        /// <param name="durationMs">Duration of calibration in milliseconds.</param>
        /// <param name="detectionEndedCallback">Callback when VAD calibration ends.</param>
        public void VoiceDetectorCalibrate(int durationMs, Action<float> detectionEndedCallback = null)
        {
            if (this.voiceAudio != null)
            {
                if (!this.TransmitEnabled)
                {
                    this.Logger.Log(LogLevel.Warning, "Cannot start voice detection calibration when transmission is not enabled");
                    return;
                }
                this.voiceAudio.VoiceDetectorCalibrate(durationMs, newThreshold =>
                {
                    this.voiceDetectionThreshold = this.VoiceDetector.Threshold;
                    if (detectionEndedCallback != null)
                    {
                        detectionEndedCallback(this.voiceDetectionThreshold);
                    }
                });
            }
        }

        private void StartRecording()
        {
            this.StopRecording();
            if (this.voiceConnection == null)
            {
                this.Logger.Log(LogLevel.Info, "Recording can't be started if Recorder is not initialized.");
                return;
            }
            this.Logger.Log(LogLevel.Info, "Starting recording");
            if (this.inputSource != null)
            {
                this.inputSource.Dispose();
                this.inputSource = null;
            }
            this.voice.RemoveSelf();
            this.voice = this.CreateLocalVoiceAudioAndSource();
            if (this.voice == LocalVoiceAudioDummy.Dummy)
            {
                this.Logger.Log(LogLevel.Error, "Local input source setup and voice stream creation failed. No recording or transmission will be happening. See previous error log messages for more details.");
                if (this.inputSource != null)
                {
                    this.inputSource.Dispose();
                    this.inputSource = null;
                }
                return;
            }
            if (this.VoiceDetector != null)
            {
                this.VoiceDetector.Threshold = this.voiceDetectionThreshold;
                this.VoiceDetector.ActivityDelayMs = this.voiceDetectionDelayMs;
                this.VoiceDetector.On = this.voiceDetection;
            }
            this.SendPhotonVoiceCreatedMessage();
            this.voice.TransmitEnabled = this.TransmitEnabled;
        }

        private void StopRecording()
        {
            this.Logger.Log(LogLevel.Info, "Stopping recording");
            if (this.voice != LocalVoiceAudioDummy.Dummy)
            {
                this.voice.RemoveSelf();
                this.voice = LocalVoiceAudioDummy.Dummy;
                this.gameObject.SendMessage("PhotonVoiceRemoved", SendMessageOptions.DontRequireReceiver);
            }
            if (this.inputSource != null)
            {
                this.inputSource.Dispose();
                this.inputSource = null;
            }
        }

        //#if UNITY_EDITOR || (UNITY_IOS || UNITY_VISIONOS)
        /// <summary>
        /// Sets the AudioSessionParameters for iOS audio initialization when Photon MicrophoneType is used.
        /// </summary>
        /// <param name="asp">You can use custom value or one from presets, <see cref="IOS.AudioSessionParametersPresets"/></param>
        /// <returns>If a change has been made.</returns>
        public bool SetIosAudioSessionParameters(IOS.AudioSessionParameters asp)
        {
            return this.SetIosAudioSessionParameters(asp.Category, asp.Mode, asp.CategoryOptions);
        }
        /// <summary>
        /// Sets the AudioSessionParameters for iOS audio initialization when Photon MicrophoneType is used.
        /// </summary>
        /// <param name="category">Audio session category to be used.</param>
        /// <param name="mode">Audio session mode to be used.</param>
        /// <param name="options">Audio session category options to be used</param>
        /// <returns>If a change has been made.</returns>
        public bool SetIosAudioSessionParameters(IOS.AudioSessionCategory category, IOS.AudioSessionMode mode, IOS.AudioSessionCategoryOption[] options)
        {
            int opt = 0;
            if (options != null)
            {
                for (int i = 0; i < options.Length; i++)
                {
                    opt |= (int)options[i];
                }
            }
            if (this.audioSessionParameters.Category != category ||
                this.audioSessionParameters.Mode != mode ||
                this.audioSessionParameters.CategoryOptionsToInt() != opt)
            {
                this.audioSessionParameters.Category = category;
                this.audioSessionParameters.Mode = mode;
                this.audioSessionParameters.CategoryOptions = options;
                this.Logger.Log(LogLevel.Info, "Recorder.iOSAudioSessionParameters changed to {0}", this.audioSessionParameters);
                if (this.SourceType == InputSourceType.Microphone && this.MicrophoneType == MicType.Photon)
                {
                    this.RestartRecording();
                }
                return true;
            }
            return false;
        }
        //#endif

        //#if UNITY_EDITOR || UNITY_ANDROID
        /// <summary>
        /// Sets the native Android audio input settings when the Photon microphone type is used.
        /// </summary>
        /// <param name="aec">Acoustic Echo Cancellation</param>
        /// <param name="agc">Automatic Gain Control</param>
        /// <param name="ns">Noise Suppression</param>
        /// <returns>If a change has been made.</returns>
        public bool SetAndroidNativeMicrophoneSettings(bool aec = false, bool agc = false, bool ns = false)
        {
            if (this.androidMicrophoneSettings.EnableAEC != aec ||
                this.androidMicrophoneSettings.EnableAGC != agc ||
                this.androidMicrophoneSettings.EnableNS != ns)
            {
                this.androidMicrophoneSettings.EnableAEC = aec;
                this.androidMicrophoneSettings.EnableAGC = agc;
                this.androidMicrophoneSettings.EnableNS = ns;

                this.Logger.Log(LogLevel.Info, "Recorder.nativeAndroidMicrophoneSettings changed to aec = {0}, agc = {1}, ns = {2}", aec, agc, ns);

                if (this.SourceType == InputSourceType.Microphone && this.MicrophoneType == MicType.Photon)
                {
                    this.RestartRecording();
                }

                return true;
            }
            else
            {
                return false;
            }
        }
        //#endif

        /// <summary> Resets audio session and parameters locally to fix broken recording due to system configuration modifications or audio interruptions or audio routing changes. </summary>
        /// <returns> If reset is done. </returns>
        public bool ResetLocalAudio()
        {
            if (this.inputSource != null && this.inputSource is IResettable)
            {
                this.Logger.Log(LogLevel.Info, "Resetting local audio.");
                (this.inputSource as IResettable).Reset();
                return true;
            }
            this.Logger.Log(LogLevel.Debug, "InputSource is null or not resettable.");
            return false;
        }

        #endregion

        #region Private Methods

        private LocalVoice CreateLocalVoiceAudioAndSource()
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
#if !UNITY_2021_2_OR_NEWER // opus lib requires Emscripten 2.0.19
                this.Logger.Log(LogLevel.Error, "Recorder Opus encoder requies Unity 2021.2 or newer for WebGL");
                return new LocalVoiceAudioDummy();
#endif
            }
            int samplingRateInt = (int)this.samplingRate;
            switch (this.SourceType)
            {
                case InputSourceType.Microphone:
                {
                    bool fallbackMicrophone = false;
                    switch (this.MicrophoneType)
                    {
                        case MicType.Unity:
                        {
                            // if fallback, switch to default device from set by other type
                            DeviceInfo micDev = fallbackMicrophone ? DeviceInfo.Default : this.MicrophoneDevice;
                            this.Logger.Log(LogLevel.Info, "Setting recorder's source to Unity microphone device {0}", micDev);
                            // mic can ignore passed sampling rate and set its own
                            if (this.UseOnAudioFilterRead)
                            {
                                this.inputSource = new MicWrapperPusher(gameObject, micDev.IDString, samplingRateInt, this.Logger);
                            }
                            else
                            {
                                this.inputSource = new MicWrapper(micDev.IDString, samplingRateInt, this.Logger);
                            }
                            if (this.inputSource != null)
                            {
                                if (this.inputSource.Error != null)
                                {
                                    this.Logger.Log(LogLevel.Error, "Unity microphone input source creation failure: {0}", this.inputSource.Error);
                                }
                                else
                                {
                                    break;
                                }
                            }
                            if (this.UseMicrophoneTypeFallback && !fallbackMicrophone)
                            {
                                fallbackMicrophone = true;
                                this.Logger.Log(LogLevel.Error, "Unity microphone failed. Falling back to Photon microphone");
                                goto case MicType.Photon;
                            }
                        }
                        break;
                        case MicType.Photon:
                        {
                            object otherParams = null;
                            // if fallback, switch to default device from set by other type
                            DeviceInfo micDev = fallbackMicrophone ? DeviceInfo.Default : this.MicrophoneDevice;
                            this.Logger.Log(LogLevel.Info, "Setting recorder's source to Photon microphone device={0}", micDev);

                            // TODO: only iOS and Android need specific processing
                            // Per platform Logging left to save something from previous file version
                            switch (Application.platform)
                            {
                                case RuntimePlatform.WindowsPlayer:
                                case RuntimePlatform.WindowsEditor:
                                    this.Logger.Log(LogLevel.Info, "Setting recorder's source to WindowsAudioInPusher");
                                    break;
                                case RuntimePlatform.WSAPlayerARM:
                                case RuntimePlatform.WSAPlayerX64:
                                case RuntimePlatform.WSAPlayerX86:
                                    this.Logger.Log(LogLevel.Info, "Setting recorder's source to UWP.AudioInPusher");
                                    break;
                                case RuntimePlatform.IPhonePlayer:
#if UNITY_VISIONOS
                                case RuntimePlatform.VisionOS:
#endif
                                    otherParams = audioSessionParameters;
                                    this.Logger.Log(LogLevel.Info, "Setting recorder's source to IOS.AudioInPusher with session {0}", audioSessionParameters);
                                    break;
                                case RuntimePlatform.OSXPlayer:
                                case RuntimePlatform.OSXEditor:
                                    this.Logger.Log(LogLevel.Info, "Setting recorder's source to MacOS.AudioInPusher");
                                    break;
                                case RuntimePlatform.Switch:
                                    this.Logger.Log(LogLevel.Info, "Setting recorder's source to Switch.AudioInPusher");
                                    break;
                                case RuntimePlatform.Android:
                                    otherParams = androidMicrophoneSettings;
                                    this.Logger.Log(LogLevel.Info, "Setting recorder's source to UnityAndroidAudioInAEC");
                                    break;
                                case RuntimePlatform.WebGLPlayer:
#if UNITY_2021_2_OR_NEWER // requires ES6
                                    this.Logger.Log(LogLevel.Info, "Setting recorder's source to Unity.WebAudioMicIn");
                                    break;
#else
                                    this.Logger.Log(LogLevel.Error, "Microphone cature requies Unity 2021.2 or newer for WebGL");
                                    goto default;
#endif
                                default:
                                    this.Logger.Log(LogLevel.Error, "Photon microphone type is not supported for the current platform {0}", Application.platform);
                                    break;
                            }
                            this.inputSource = Platform.CreateDefaultAudioSource(this.Logger, micDev, samplingRateInt, 1, otherParams);

                            if (this.inputSource != null)
                            {
                                if (this.inputSource.Error != null)
                                {
                                    this.Logger.Log(LogLevel.Error, "Photon microphone input source creation failure: {0}", this.inputSource.Error);
                                }
                                else
                                {
                                    break;
                                }
                            }
                            if (this.UseMicrophoneTypeFallback && !fallbackMicrophone)
                            {
                                fallbackMicrophone = true;
                                this.Logger.Log(LogLevel.Error, "Photon microphone failed. Falling back to Unity microphone");
                                goto case MicType.Unity;
                            }
                            break;
                        }
                        default:
                            this.Logger.Log(LogLevel.Error, "unknown MicrophoneType value {0}", this.MicrophoneType);
                            return LocalVoiceAudioDummy.Dummy;
                    }
                }
                break;
                case InputSourceType.AudioClip:
                {
                    if (ReferenceEquals(null, this.AudioClip))
                    {
                        this.Logger.Log(LogLevel.Error, "AudioClip property must be set for AudioClip audio source");
                        return LocalVoiceAudioDummy.Dummy;
                    }
                    AudioClipWrapper audioClipWrapper = new AudioClipWrapper(this.AudioClip); // never fails, no need to check Error
                    audioClipWrapper.Loop = this.LoopAudioClip;
                    this.inputSource = audioClipWrapper;
                }
                break;
                case InputSourceType.Factory:
                {
                    if (this.InputFactory == null)
                    {
                        // this.Logger.Log(LogLevel.Error, "Recorder.InputFactory must be specified if Recorder.Source set to Factory");
                        // return LocalVoiceAudioDummy.Dummy;
                        this.Logger.Log(LogLevel.Warning, "Recorder.Source is Factory but Recorder.InputFactory is not set. Setting it to ToneAudioReader.");
                        this.InputFactory = () => new AudioUtil.ToneAudioPusher<float>();
                    }
                    this.inputSource = this.InputFactory();
                    if (this.inputSource.Error != null)
                    {
                        this.Logger.Log(LogLevel.Error, "InputFactory creation failure: {0}.", this.inputSource.Error);
                    }
                }
                break;
                default:
                    this.Logger.Log(LogLevel.Error, "unknown Source value {0}", this.SourceType);
                    return LocalVoiceAudioDummy.Dummy;
            }
            if (this.inputSource == null || this.inputSource.Error != null)
            {
                return LocalVoiceAudioDummy.Dummy;
            }
            if (this.inputSource.Channels == 0)
            {
                this.Logger.Log(LogLevel.Error, "inputSource.Channels is zero");
                return LocalVoiceAudioDummy.Dummy;
            }

            VoiceInfo voiceInfo = VoiceInfo.CreateAudioOpus(this.samplingRate, this.inputSource.Channels, this.frameDuration, this.Bitrate, this.UserData);
            AudioSampleType audioSampleType = AudioSampleType.Source;

            WebRtcAudioDsp dsp = this.GetComponent<WebRtcAudioDsp>();
            if (null != dsp)
            {
                dsp.AdjustVoiceInfo(ref voiceInfo, ref audioSampleType);
            }

            VoiceCreateOptions opt = new VoiceCreateOptions()
            {
                InterestGroup = this.interestGroup,
                TargetPlayers = this.useTargetPlayers ? this.targetPlayers : null,
                DebugEchoMode = this.debugEchoMode,
                Encrypt = this.encrypt,
                Reliable = this.reliableMode,
            };
            return this.voiceConnection.VoiceClient.CreateLocalVoiceAudioFromSource(voiceInfo, this.inputSource, audioSampleType, VoiceConnection.ChannelAudio, opt);
        }

        protected virtual void SendPhotonVoiceCreatedMessage()
        {
            this.gameObject.SendMessage("PhotonVoiceCreated", new Unity.PhotonVoiceCreatedParams { Voice = this.voice, AudioDesc = this.inputSource }, SendMessageOptions.DontRequireReceiver);
        }

        protected void Update()
        {
            if (this.voiceConnection == null)
            {
                return;
            }

            if (System.Threading.Interlocked.Exchange(ref this.microphoneDeviceChangePending, 0) != 0)
            {
                // can trigger restart handled below
                this.HandleDeviceChange();
            }
            // restarting recording
            if (System.Threading.Interlocked.Exchange(ref this.restartRecordingPending, 0) != 0)
            {
                if (this.RecordingEnabled)
                {
                    this.Logger.Log(LogLevel.Info, "Restarting recording");
                    this.StartRecording();
                }
            }
        }

        private void OnDestroy()
        {
            if (this.voiceConnection == null)
            {
                return;
            }

            this.Logger.Log(LogLevel.Info, "Recorder is about to be destroyed, removing local voice.");
            this.StopRecording();
            this.voiceConnection.RemoveRecorder(this);
        }

        private void HandleDeviceChange()
        {
            if (this.RecordingEnabled)
            {
                if (this.SourceType == InputSourceType.Microphone)
                {
                    if (this.ResetLocalAudio())
                    {
                        this.Logger.Log(LogLevel.Info, "Local audio reset as a result of audio config/device change.");
                    }
                    else
                    {
                        this.Logger.Log(LogLevel.Info, "Restarting Recording as a result of audio config/device change.");
                        this.RestartRecording();
                    }
                }
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (this.voiceConnection == null)
            {
                return;
            }

            this.Logger.Log(LogLevel.Debug, "OnApplicationPause({0})", paused);
            this.HandleApplicationPause(paused);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (this.voiceConnection == null)
            {
                return;
            }

            this.Logger.Log(LogLevel.Debug, "OnApplicationFocus({0})", focused);
            this.HandleApplicationPause(!focused);
        }

        private void HandleApplicationPause(bool paused)
        {
            this.Logger.Log(LogLevel.Info, "App paused?= {0}, isPausedOrInBackground = {1}, StopRecordingWhenPaused = {2}, RecordingEnabled = {3}", paused, this.isPausedOrInBackground, this.StopRecordingWhenPaused, this.RecordingEnabled);
            if (this.isPausedOrInBackground == paused) // OnApplicationFocus and OnApplicationPause both called
            {
                return;
            }
            if (paused)
            {
                this.isPausedOrInBackground = true;
                if (this.StopRecordingWhenPaused && this.RecordingEnabled)
                {
                    this.Logger.Log(LogLevel.Info, "Stopping recording as application went to background or paused");
                    this.StopRecording();
                }
            }
            else
            {
                if (!this.StopRecordingWhenPaused)
                {
                    if (this.ResetLocalAudio())
                    {
                        this.Logger.Log(LogLevel.Info, "Local audio reset as application is back from background or unpaused");
                    }
                }
                else if (this.RecordingEnabled)
                {
                    this.Logger.Log(LogLevel.Info, "Starting recording as application is back from background or unpaused");
                    this.RestartRecording();
                }
                this.isPausedOrInBackground = false;
            }
        }

#endregion

        public enum InputSourceType
        {
            Microphone,
            AudioClip,
            Factory
        }

        public enum MicType
        {
            Unity,
            Photon
        }
    }
}