// ----------------------------------------------------------------------------
// <copyright file="VoiceDemoUI.cs" company="Exit Games GmbH">
// Photon Voice Demo for PUN - Copyright (C) Exit Games GmbH
// </copyright>
// <summary>
// UI manager class for the PUN Voice Demo
// </summary>
// <author>developer@photonengine.com</author>
// ----------------------------------------------------------------------------

using System;
using Photon.Voice.Unity;
using Photon.Voice.PUN;

#pragma warning disable 0649 // Field is never assigned to, and will always have its default value

namespace ExitGames.Demos.DemoPunVoice
{
    using Photon.Pun;
    using UnityEngine;
    using UnityEngine.UI;
    using Client.Photon;

#if !UNITY_EDITOR && UNITY_PS4
    using Sony.NP;
#elif !UNITY_EDITOR && UNITY_SHARLIN
    using System.Runtime.InteropServices;
#endif

    public class VoiceDemoUI : MonoBehaviour
    {
#if !UNITY_EDITOR && UNITY_SHARLIN
    [DllImport("PhotonVoiceLocalUserIDPlugin")]
    private static extern int egpvgetLocalUserID(); // returns the user ID of the user at index 0 in the list of local users
#endif
        [SerializeField]
        private Text punState;
        [SerializeField]
        private Text voiceState;
        
        private PhotonVoiceNetwork punVoiceNetwork;

        private Canvas canvas;

        [SerializeField]
        private Button punSwitch;
        private Text punSwitchText;
        [SerializeField]
        private Button voiceSwitch;
        private Text voiceSwitchText;
        [SerializeField]
        private Button calibrateButton;
        private Text calibrateText;

        [SerializeField]
        private Text voiceDebugText;

        public Recorder recorder;

        [SerializeField]
        private GameObject inGameSettings;

        [SerializeField]
        private GameObject globalSettings;

        [SerializeField]
        private Text devicesInfoText;

        private GameObject debugGO;

        private bool debugMode;

        private float volumeBeforeMute;

        private DebugLevel previousDebugLevel;

        public bool DebugMode
        {
            get
            {
                return this.debugMode;
            }
            set
            {
                this.debugMode = value;
                this.debugGO.SetActive(this.debugMode);
                this.voiceDebugText.text = String.Empty;
                if (this.debugMode)
                {
                    this.previousDebugLevel = this.punVoiceNetwork.Client.LoadBalancingPeer.DebugOut;
                    this.punVoiceNetwork.Client.LoadBalancingPeer.DebugOut = DebugLevel.ALL;
                }
                else
                {
                    this.punVoiceNetwork.Client.LoadBalancingPeer.DebugOut = this.previousDebugLevel;
                }
                if (DebugToggled != null)
                {
                    DebugToggled(this.debugMode);
                }
            }
        }

        public delegate void OnDebugToggle(bool debugMode);

        public static event OnDebugToggle DebugToggled;

        [SerializeField]
        private int calibrationMilliSeconds = 2000;

        private void Awake()
        {
            this.punVoiceNetwork = PhotonVoiceNetwork.Instance;
        }

        private void OnEnable()
        {
            ChangePOV.CameraChanged += this.OnCameraChanged;
            BetterToggle.ToggleValueChanged += this.BetterToggle_ToggleValueChanged;
            CharacterInstantiation.CharacterInstantiated += this.CharacterInstantiation_CharacterInstantiated;
            this.punVoiceNetwork.Client.StateChanged += this.VoiceClientStateChanged;
            PhotonNetwork.NetworkingClient.StateChanged += this.PunClientStateChanged;
        }

        private void OnDisable()
        {
            ChangePOV.CameraChanged -= this.OnCameraChanged;
            BetterToggle.ToggleValueChanged -= this.BetterToggle_ToggleValueChanged;
            CharacterInstantiation.CharacterInstantiated -= this.CharacterInstantiation_CharacterInstantiated;
            this.punVoiceNetwork.Client.StateChanged -= this.VoiceClientStateChanged;
            PhotonNetwork.NetworkingClient.StateChanged -= this.PunClientStateChanged;
        }

        private void CharacterInstantiation_CharacterInstantiated(GameObject character)
        {
            if (this.recorder) // probably using a global recorder
            {
                return;
            }
            PhotonVoiceView photonVoiceView = character.GetComponent<PhotonVoiceView>();
            if (photonVoiceView.IsRecorder)
            {
                this.recorder = photonVoiceView.RecorderInUse;
            }
        }

        private void InitToggles(Toggle[] toggles)
        {
            if (toggles == null) { return; }
            for (int i = 0; i < toggles.Length; i++)
            {
                Toggle toggle = toggles[i];
                switch (toggle.name)
                {
                    case "Mute":
                        toggle.isOn = AudioListener.volume <= 0.001f;
                        break;

                    case "VoiceDetection":
                        toggle.isOn = this.recorder != null && this.recorder.VoiceDetection;
                        break;

                    case "DebugVoice":
                        toggle.isOn = this.DebugMode;
                        break;

                    case "Transmit":
                        toggle.isOn = this.recorder != null && this.recorder.TransmitEnabled;
                        break;

                    case "DebugEcho":
                        toggle.isOn = this.recorder != null && this.recorder.DebugEchoMode;
                        break;

                    case "AutoConnectAndJoin":
                        toggle.isOn = this.punVoiceNetwork.AutoConnectAndJoin;
                        break;

                    case "AutoLeaveAndDisconnect":
                        toggle.isOn = this.punVoiceNetwork.AutoLeaveAndDisconnect;
                        break;
                }
            }
        }

        private void BetterToggle_ToggleValueChanged(Toggle toggle)
        {
            switch (toggle.name)
            {
                case "Mute":
                    //AudioListener.pause = toggle.isOn;
                    if (toggle.isOn)
                    {
                        this.volumeBeforeMute = AudioListener.volume;
                        AudioListener.volume = 0f;
                    }
                    else
                    {
                        AudioListener.volume = this.volumeBeforeMute;
                        this.volumeBeforeMute = 0f;
                    }
                    break;
                case "Transmit":
                    if (this.recorder)
                    {
                        this.recorder.TransmitEnabled = toggle.isOn;
                    }
                    break;
                case "VoiceDetection":
                    if (this.recorder)
                    {
                        this.recorder.VoiceDetection = toggle.isOn;
                    }
                    break;
                case "DebugEcho":
                    if (this.recorder)
                    {
                        this.recorder.DebugEchoMode = toggle.isOn;
                    }
                    break;
                case "DebugVoice":
                    this.DebugMode = toggle.isOn;
                    break;
                case "AutoConnectAndJoin":
                    this.punVoiceNetwork.AutoConnectAndJoin = toggle.isOn;
                    break;
                case "AutoLeaveAndDisconnect":
                    this.punVoiceNetwork.AutoLeaveAndDisconnect = toggle.isOn;
                    break;
            }
        }

        private void OnCameraChanged(Camera newCamera)
        {
            this.canvas.worldCamera = newCamera;
        }

        private void Start()
        {
            this.canvas = this.GetComponentInChildren<Canvas>();
            if (this.punSwitch != null)
            {
                this.punSwitchText = this.punSwitch.GetComponentInChildren<Text>();
                this.punSwitch.onClick.AddListener(this.PunSwitchOnClick);
            }
            if (this.voiceSwitch != null)
            {
                this.voiceSwitchText = this.voiceSwitch.GetComponentInChildren<Text>();
                this.voiceSwitch.onClick.AddListener(this.VoiceSwitchOnClick);
            }
            if (this.calibrateButton != null)
            {
                this.calibrateButton.onClick.AddListener(this.CalibrateButtonOnClick);
                this.calibrateText = this.calibrateButton.GetComponentInChildren<Text>();
            }
            if (this.punState != null)
            {
                this.debugGO = this.punState.transform.parent.gameObject;
            }
            this.volumeBeforeMute = AudioListener.volume;
            this.previousDebugLevel = this.punVoiceNetwork.Client.LoadBalancingPeer.DebugOut;
            if (this.globalSettings != null)
            {
                this.globalSettings.SetActive(true);
                this.InitToggles(this.globalSettings.GetComponentsInChildren<Toggle>());
            }
            if (this.devicesInfoText != null)
            {
                if (UnityMicrophone.devices == null || UnityMicrophone.devices.Length == 0)
                {
                    this.devicesInfoText.enabled = true;
                    this.devicesInfoText.color = Color.red;
                    this.devicesInfoText.text = "No microphone device detected!";
                }
                else if (UnityMicrophone.devices.Length == 1)
                {
                    this.devicesInfoText.text = string.Format("Mic.: {0}", UnityMicrophone.devices[0]);
                }
                else
                {
                    this.devicesInfoText.text = string.Format("Multi.Mic.Devices:\n0. {0} (active)\n", UnityMicrophone.devices[0]);
                    for (int i = 1; i < UnityMicrophone.devices.Length; i++)
                    {
                        this.devicesInfoText.text = string.Concat(this.devicesInfoText.text, string.Format("{0}. {1}\n", i, UnityMicrophone.devices[i]));
                    }
                }
            }

#if !UNITY_EDITOR && UNITY_PS4
            UserProfiles.LocalUsers localUsers = new UserProfiles.LocalUsers();
            UserProfiles.GetLocalUsers(localUsers);
            int userID = localUsers.LocalUsersIds[0].UserId.Id;

            punVoiceNetwork.PlayStationUserID = userID;
#elif !UNITY_EDITOR && UNITY_SHARLIN
            punVoiceNetwork.PlayStationUserID = egpvgetLocalUserID();
#endif
        }

        private void PunSwitchOnClick()
        {
            if (PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.Joined)
            {

                PhotonNetwork.Disconnect();
            }
            else if (PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.Disconnected ||
                PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.PeerCreated)
            {
                PhotonNetwork.ConnectUsingSettings();
            }
        }

        private void VoiceSwitchOnClick()
        {
            if (this.punVoiceNetwork.ClientState == Photon.Realtime.ClientState.Joined)
            {
                this.punVoiceNetwork.Disconnect();
            }
            else if (this.punVoiceNetwork.ClientState == Photon.Realtime.ClientState.PeerCreated
                     || this.punVoiceNetwork.ClientState == Photon.Realtime.ClientState.Disconnected)
            {
                this.punVoiceNetwork.ConnectAndJoinRoom();
            }
        }

        private void CalibrateButtonOnClick()
        {
            if (this.recorder && !this.recorder.VoiceDetectorCalibrating)
            {
                this.recorder.VoiceDetectorCalibrate(this.calibrationMilliSeconds);
            }
        }

        private void Update()
        {
            // editor only two-ways binding for toggles
#if UNITY_EDITOR
            this.InitToggles(this.globalSettings.GetComponentsInChildren<Toggle>());
#endif
            if (this.recorder != null && this.recorder.LevelMeter != null)
            {
                this.voiceDebugText.text = string.Format("Amp: avg. {0:0.000000}, peak {1:0.000000}", this.recorder.LevelMeter.CurrentAvgAmp, this.recorder.LevelMeter.CurrentPeakAmp);
            }
        }

        private void PunClientStateChanged(Photon.Realtime.ClientState fromState, Photon.Realtime.ClientState toState)
        {
            this.punState.text = string.Format("PUN: {0}", toState);
            switch (toState)
            {
                case Photon.Realtime.ClientState.PeerCreated:
                case Photon.Realtime.ClientState.Disconnected:
                    this.punSwitch.interactable = true;
                    this.punSwitchText.text = "PUN Connect";
                    break;
                case Photon.Realtime.ClientState.Joined:
                    this.punSwitch.interactable = true;
                    this.punSwitchText.text = "PUN Disconnect";
                    break;
                default:
                    this.punSwitch.interactable = false;
                    this.punSwitchText.text = "PUN busy";
                    break;
            }
            this.UpdateUiBasedOnVoiceState(this.punVoiceNetwork.ClientState);
        }

        private void VoiceClientStateChanged(Photon.Realtime.ClientState fromState, Photon.Realtime.ClientState toState)
        {
            this.UpdateUiBasedOnVoiceState(toState);
        }

        private void UpdateUiBasedOnVoiceState(Photon.Realtime.ClientState voiceClientState)
        {
            this.voiceState.text = string.Format("PhotonVoice: {0}", voiceClientState);
            switch (voiceClientState)
            {
                case Photon.Realtime.ClientState.Joined:
                    this.voiceSwitch.interactable = true;
                    this.inGameSettings.SetActive(true);
                    this.voiceSwitchText.text = "Voice Disconnect";
                    this.InitToggles(this.inGameSettings.GetComponentsInChildren<Toggle>());
                    if (this.recorder != null)
                    {
                        this.calibrateButton.interactable = !this.recorder.VoiceDetectorCalibrating;
                        this.calibrateText.text = this.recorder.VoiceDetectorCalibrating ? "Calibrating" : string.Format("Calibrate ({0}s)", this.calibrationMilliSeconds / 1000);
                    }
                    else
                    {
                        this.calibrateButton.interactable = false;
                        this.calibrateText.text = "Unavailable";
                    }
                    break;
                case Photon.Realtime.ClientState.PeerCreated:
                case Photon.Realtime.ClientState.Disconnected:
                    if (PhotonNetwork.InRoom)
                    {
                        this.voiceSwitch.interactable = true;
                        this.voiceSwitchText.text = "Voice Connect";
                        this.voiceDebugText.text = String.Empty;
                    }
                    else
                    {
                        this.voiceSwitch.interactable = false;
                        this.voiceSwitchText.text = "Voice N/A";
                        this.voiceDebugText.text = String.Empty;
                    }
                    this.calibrateButton.interactable = false;
                    this.voiceSwitchText.text = "Voice Connect";
                    this.calibrateText.text = "Unavailable";
                    this.inGameSettings.SetActive(false);
                    break;
                default:
                    this.voiceSwitch.interactable = false;
                    this.voiceSwitchText.text = "Voice busy";
                    break;
            }
        }
    }



}