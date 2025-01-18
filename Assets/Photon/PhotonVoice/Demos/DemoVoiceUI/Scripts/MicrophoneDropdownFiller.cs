#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
#define PHOTON_MICROPHONE_WSA
#endif

#if PHOTON_MICROPHONE_WSA || UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
#define PHOTON_MICROPHONE_ENUMERATOR
#endif

#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_ANDROID || UNITY_IOS || UNITY_WSA || UNITY_EDITOR_OSX || UNITY_EDITOR_WIN
#define PHOTON_MICROPHONE_SUPPORTED
#endif

namespace Photon.Voice.Unity.Demos.DemoVoiceUI
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Serialization;
    using UnityEngine.UI;


    public struct MicRef
    {
        public Recorder.MicType MicType;
        public string Name;
        public int PhotonId;
        public string PhotonIdString;

        public MicRef(string name, int id)
        {
            this.MicType = Recorder.MicType.Photon;
            this.Name = name;
            this.PhotonId = id;
            this.PhotonIdString = string.Empty;
        }

        public MicRef(string name, string id)
        {
            this.MicType = Recorder.MicType.Photon;
            this.Name = name;
            this.PhotonId = -1;
            this.PhotonIdString = id;
        }

        public MicRef(string name)
        {
            this.MicType = Recorder.MicType.Unity;
            this.Name = name;
            this.PhotonId = -1;
            this.PhotonIdString = string.Empty;
        }

        public override string ToString()
        {
            return string.Format("Mic reference: {0}", this.Name);
        }
    }

    public class MicrophoneDropdownFiller : MonoBehaviour
    {
        private List<MicRef> micOptions;

        #pragma warning disable 649
        [SerializeField]
        private Dropdown micDropdown;

        [SerializeField]
        private Recorder recorder;

        [SerializeField]
        [FormerlySerializedAs("RefreshButton")]
        private GameObject refreshButton;

        [SerializeField]
        [FormerlySerializedAs("ToggleButton")]
        private GameObject toggleButton;
        #pragma warning restore 649

        private Toggle photonToggle;

        private void Awake()
        {
            this.photonToggle = this.toggleButton.GetComponentInChildren<Toggle>();
            this.RefreshMicrophones();
        }

        private void SetupMicDropdown()
        {
            this.micDropdown.ClearOptions();

            this.micOptions = new List<MicRef>();
            List<string> micOptionsStrings = new List<string>();

            for(int i=0; i < Microphone.devices.Length; i++)
            {
                string x = Microphone.devices[i];
                this.micOptions.Add(new MicRef(x));
                micOptionsStrings.Add(string.Format("[Unity] {0}", x));
            }

            #if PHOTON_MICROPHONE_ENUMERATOR
            if (this.recorder.MicrophonesEnumerator.IsSupported)
            {
                int i = 0;
                foreach (DeviceInfo deviceInfo in this.recorder.MicrophonesEnumerator.Devices)
                {
                    string n = deviceInfo.Name;
                    #if PHOTON_MICROPHONE_WSA
                    this.micOptions.Add(new MicRef(n, deviceInfo.IDString));
                    #else
                    this.micOptions.Add(new MicRef(n, deviceInfo.IDInt));
                    #endif
                    micOptionsStrings.Add(string.Format("[Photon] {0}", n));
                    i++;
                }
            }
            #endif

            this.micDropdown.AddOptions(micOptionsStrings);
            this.micDropdown.onValueChanged.RemoveAllListeners();
            this.micDropdown.onValueChanged.AddListener(delegate { this.MicDropdownValueChanged(this.micOptions[this.micDropdown.value]); });
        }

        private void MicDropdownValueChanged(MicRef mic)
        {
            this.recorder.MicrophoneType = mic.MicType;

            switch (mic.MicType)
            {
                case Recorder.MicType.Unity:
                    this.recorder.UnityMicrophoneDevice = mic.Name;
                    break;
                case Recorder.MicType.Photon:
                    #if PHOTON_MICROPHONE_WSA
                    this.recorder.PhotonMicrophoneDeviceIdString = mic.PhotonIdString;
                    #else
                    this.recorder.PhotonMicrophoneDeviceId = mic.PhotonId;
                    #endif
                    break;
            }

            if (this.recorder.RequiresRestart)
            {
                this.recorder.RestartRecording();
            }
        }

        private void SetCurrentValue()
        {
            if (this.micOptions == null)
            {
                Debug.LogWarning("micOptions list is null");
                return;
            }
            #if PHOTON_MICROPHONE_ENUMERATOR
            bool photonMicEnumAvailable = this.recorder.MicrophonesEnumerator.IsSupported;
            #else
            bool photonMicEnumAvailable = false;
            #endif
            this.photonToggle.onValueChanged.RemoveAllListeners();
            this.photonToggle.isOn = this.recorder.MicrophoneType == Recorder.MicType.Photon;
            if (!photonMicEnumAvailable)
            {
                this.photonToggle.onValueChanged.AddListener(this.PhotonMicToggled);
            }
            this.micDropdown.gameObject.SetActive(photonMicEnumAvailable || this.recorder.MicrophoneType == Recorder.MicType.Unity);
            #if PHOTON_MICROPHONE_SUPPORTED
            this.toggleButton.SetActive(!photonMicEnumAvailable);
            #else
            this.toggleButton.SetActive(false);
            #endif
            this.refreshButton.SetActive(photonMicEnumAvailable || this.recorder.MicrophoneType == Recorder.MicType.Unity);
            for (int valueIndex = 0; valueIndex < this.micOptions.Count; valueIndex++)
            {
                MicRef val = this.micOptions[valueIndex];
                if (this.recorder.MicrophoneType == val.MicType)
                {
                    if (this.recorder.MicrophoneType == Recorder.MicType.Unity &&
                        Recorder.CompareUnityMicNames(val.Name, this.recorder.UnityMicrophoneDevice))
                    {
                        this.micDropdown.value = valueIndex;
                        return;
                    }
                    #if PHOTON_MICROPHONE_WSA
                    if (this.recorder.MicrophoneType == Recorder.MicType.Photon &&
                        string.Equals(val.PhotonIdString, this.recorder.PhotonMicrophoneDeviceIdString))
                    {
                        this.micDropdown.value = valueIndex;
                        return;
                    }                             
                    #else
                    if (this.recorder.MicrophoneType == Recorder.MicType.Photon &&
                        val.PhotonId == this.recorder.PhotonMicrophoneDeviceId)
                    {
                        this.micDropdown.value = valueIndex;
                        return;
                    }                    
                    #endif
                }
            }
            for (int valueIndex = 0; valueIndex < this.micOptions.Count; valueIndex++)
            {
                MicRef val = this.micOptions[valueIndex];
                if (this.recorder.MicrophoneType == val.MicType)
                {
                    if (this.recorder.MicrophoneType == Recorder.MicType.Unity)
                    {
                        this.micDropdown.value = valueIndex;
                        this.recorder.UnityMicrophoneDevice = val.Name;
                        break;
                    }
                    if (this.recorder.MicrophoneType == Recorder.MicType.Photon)
                    {
                        this.micDropdown.value = valueIndex;
                        #if PHOTON_MICROPHONE_WSA
                        this.recorder.PhotonMicrophoneDeviceIdString = val.PhotonIdString;
                        #else
                        this.recorder.PhotonMicrophoneDeviceId = val.PhotonId;
                        #endif
                        break;
                    }
                }
            }
            if (this.recorder.RequiresRestart)
            {
                this.recorder.RestartRecording();
            }
        }

        public void PhotonMicToggled(bool on)
        {
            this.micDropdown.gameObject.SetActive(!on);
            this.refreshButton.SetActive(!on);
            if (on)
            {
                this.recorder.MicrophoneType = Recorder.MicType.Photon;
            }
            else
            {
                this.recorder.MicrophoneType = Recorder.MicType.Unity;
            }
            
            if (this.recorder.RequiresRestart)
            {
                this.recorder.RestartRecording();
            }
        }

        public void RefreshMicrophones()
        {
            #if PHOTON_MICROPHONE_ENUMERATOR
            //Debug.Log("Refresh Mics");
            this.recorder.MicrophonesEnumerator.Refresh();
            #endif
            this.SetupMicDropdown();
            this.SetCurrentValue();
        }

        // sync. UI in case a change happens from the Unity Editor Inspector
        private void PhotonVoiceCreated()
        {
            this.RefreshMicrophones();
        }
    }
}