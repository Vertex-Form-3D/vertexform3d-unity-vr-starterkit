using UnityEngine;
using Photon.Voice.Unity;
using Photon.Voice.Fusion;
using System.Collections.Generic;

namespace VertexFormCore
{
    /// <summary>
    /// Comprehensive voice debugging utility to diagnose voice chat issues
    /// </summary>
    public class VoiceDebugger : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool autoDebugOnStart = true;
        [SerializeField] private float debugInterval = 5f;
        [SerializeField] private KeyCode manualDebugKey = KeyCode.F9;

        private FusionVoiceClient fusionVoiceClient;

        private void Start()
        {
            fusionVoiceClient = FindFirstObjectByType<FusionVoiceClient>();

            if (autoDebugOnStart)
            {
                Invoke(nameof(RunFullDiagnostics), 2f);
                InvokeRepeating(nameof(RunQuickCheck), debugInterval, debugInterval);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(manualDebugKey))
            {
                RunFullDiagnostics();
            }
        }

        /// <summary>
        /// Run a full diagnostic check of all voice components
        /// </summary>
        [ContextMenu("Run Full Voice Diagnostics")]
        public void RunFullDiagnostics()
        {
            Debug.Log("=== VOICE CHAT FULL DIAGNOSTICS ===");

            // 1. Check FusionVoiceClient
            CheckFusionVoiceClient();

            // 2. Check Primary Recorder
            CheckPrimaryRecorder();

            // 3. Check all PlayerNetworkSetup recorders
            CheckAllPlayerRecorders();

            // 4. Check all Speakers
            CheckAllSpeakers();

            // 5. Check VoiceNetworkObjects
            CheckVoiceNetworkObjects();

            // 6. Check Microphone
            CheckMicrophone();

            Debug.Log("=== END DIAGNOSTICS ===");
        }

        /// <summary>
        /// Quick periodic check
        /// </summary>
        private void RunQuickCheck()
        {
            if (fusionVoiceClient == null)
            {
                fusionVoiceClient = FindFirstObjectByType<FusionVoiceClient>();
            }

            if (fusionVoiceClient != null && fusionVoiceClient.Client != null)
            {
                var state = fusionVoiceClient.Client.State;
                if (state != Photon.Realtime.ClientState.Joined)
                {
                    Debug.LogWarning($"[VoiceDebugger] Voice not joined! Current state: {state}");
                }
            }
        }

        private void CheckFusionVoiceClient()
        {
            Debug.Log("--- Checking FusionVoiceClient ---");

            if (fusionVoiceClient == null)
            {
                fusionVoiceClient = FindFirstObjectByType<FusionVoiceClient>();
                if (fusionVoiceClient == null)
                {
                    Debug.LogError("[VoiceDebugger] ❌ FusionVoiceClient NOT FOUND!");
                    return;
                }
            }

            Debug.Log($"[VoiceDebugger] ✓ FusionVoiceClient found");
            Debug.Log($"[VoiceDebugger] AutoConnectAndJoin: {fusionVoiceClient.AutoConnectAndJoin}");
            Debug.Log($"[VoiceDebugger] UsePrimaryRecorder: {fusionVoiceClient.UsePrimaryRecorder}");

            if (fusionVoiceClient.Client != null)
            {
                Debug.Log($"[VoiceDebugger] Voice Client State: {fusionVoiceClient.Client.State}");
                Debug.Log($"[VoiceDebugger] Voice Client InRoom: {fusionVoiceClient.Client.InRoom}");
                Debug.Log($"[VoiceDebugger] Voice Room Name: {fusionVoiceClient.Client.CurrentRoom?.Name ?? "N/A"}");
                Debug.Log($"[VoiceDebugger] Voice Players Count: {fusionVoiceClient.Client.CurrentRoom?.PlayerCount ?? 0}");
            }
            else
            {
                Debug.LogError("[VoiceDebugger] ❌ Voice Client is NULL!");
            }
        }

        private void CheckPrimaryRecorder()
        {
            Debug.Log("--- Checking Primary Recorder ---");

            if (fusionVoiceClient == null || fusionVoiceClient.PrimaryRecorder == null)
            {
                Debug.LogWarning("[VoiceDebugger] ⚠ No Primary Recorder set on FusionVoiceClient");
                return;
            }

            Recorder recorder = fusionVoiceClient.PrimaryRecorder;
            Debug.Log($"[VoiceDebugger] ✓ Primary Recorder found: {recorder.gameObject.name}");
            Debug.Log($"[VoiceDebugger] TransmitEnabled: {recorder.TransmitEnabled}");
            Debug.Log($"[VoiceDebugger] RecordingEnabled: {recorder.RecordingEnabled}");
            Debug.Log($"[VoiceDebugger] IsCurrentlyTransmitting: {recorder.IsCurrentlyTransmitting}");
            Debug.Log($"[VoiceDebugger] IsRecording: {recorder.TransmitEnabled && recorder.RecordingEnabled}");
            Debug.Log($"[VoiceDebugger] InterestGroup: {recorder.InterestGroup}");
            Debug.Log($"[VoiceDebugger] Microphone Type: {recorder.MicrophoneType}");

            var micDevice = recorder.MicrophoneDevice;
            if (micDevice != null)
            {
                Debug.Log($"[VoiceDebugger] 🎤 MICROPHONE DEVICE: {micDevice.Name} (ID: {micDevice.IDInt})");
            }
            else
            {
                Debug.Log($"[VoiceDebugger] 🎤 MICROPHONE DEVICE: Default (First Available)");
            }

            if (!recorder.TransmitEnabled)
            {
                Debug.LogError("[VoiceDebugger] ❌ ISSUE: Recorder.TransmitEnabled is FALSE! Enable it to transmit voice.");
            }

            if (!recorder.RecordingEnabled)
            {
                Debug.LogError("[VoiceDebugger] ❌ ISSUE: Recorder.RecordingEnabled is FALSE! Enable it to record voice.");
            }

            CheckRecorderAudioSource(recorder);
        }

        private void CheckAllPlayerRecorders()
        {
            Debug.Log("--- Checking All Player Recorders ---");

            var players = FindObjectsByType<PlayerNetworkSetup>(FindObjectsSortMode.None);
            Debug.Log($"[VoiceDebugger] Found {players.Length} PlayerNetworkSetup instances");

            foreach (var player in players)
            {
                if (player.Object != null && player.Object.HasInputAuthority)
                {
                    Debug.Log($"[VoiceDebugger] Checking LOCAL player: {player.PlayerName}");
                    Recorder recorder = player.GetComponent<Recorder>();

                    if (recorder == null)
                    {
                        recorder = player.GetComponentInChildren<Recorder>();
                    }

                    if (recorder != null)
                    {
                        Debug.Log($"[VoiceDebugger] ✓ Local player Recorder found");
                        Debug.Log($"[VoiceDebugger]   TransmitEnabled: {recorder.TransmitEnabled}");
                        Debug.Log($"[VoiceDebugger]   IsRecording: {recorder.TransmitEnabled && recorder.RecordingEnabled}");
                        Debug.Log($"[VoiceDebugger]   IsCurrentlyTransmitting: {recorder.IsCurrentlyTransmitting}");

                        var micDevice = recorder.MicrophoneDevice;
                        if (micDevice != null)
                        {
                            Debug.Log($"[VoiceDebugger]   🎤 Microphone: {micDevice.Name} (ID: {micDevice.IDInt})");
                        }
                        else
                        {
                            Debug.Log($"[VoiceDebugger]   🎤 Microphone: Default (First Available)");
                        }

                        if (!recorder.TransmitEnabled)
                        {
                            Debug.LogError($"[VoiceDebugger] ❌ LOCAL PLAYER RECORDER NOT TRANSMITTING!");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[VoiceDebugger] ⚠ No Recorder found for local player");
                    }
                }
            }
        }

        private void CheckAllSpeakers()
        {
            Debug.Log("--- Checking All Speakers ---");

            var speakers = FindObjectsByType<Speaker>(FindObjectsSortMode.None);
            Debug.Log($"[VoiceDebugger] Found {speakers.Length} Speaker instances");

            int activeSpeakers = 0;
            int playingSpeakers = 0;

            foreach (var speaker in speakers)
            {
                if (speaker.gameObject.activeInHierarchy)
                {
                    activeSpeakers++;

                    Debug.Log($"[VoiceDebugger] Speaker on: {speaker.gameObject.name}");
                    Debug.Log($"[VoiceDebugger]   IsPlaying: {speaker.IsPlaying}");
                    Debug.Log($"[VoiceDebugger]   IsLinked: {speaker.IsLinked}");

                    AudioSource audioSource = speaker.GetComponent<AudioSource>();
                    if (audioSource != null)
                    {
                        Debug.Log($"[VoiceDebugger]   AudioSource.volume: {audioSource.volume}");
                        Debug.Log($"[VoiceDebugger]   AudioSource.mute: {audioSource.mute}");
                        Debug.Log($"[VoiceDebugger]   AudioSource.spatialBlend: {audioSource.spatialBlend}");

                        if (audioSource.volume <= 0f)
                        {
                            Debug.LogError($"[VoiceDebugger] ❌ ISSUE: Speaker AudioSource volume is 0!");
                        }

                        if (audioSource.mute)
                        {
                            Debug.LogError($"[VoiceDebugger] ❌ ISSUE: Speaker AudioSource is MUTED!");
                        }
                    }
                    else
                    {
                        Debug.LogError($"[VoiceDebugger] ❌ ISSUE: Speaker has no AudioSource component!");
                    }

                    if (speaker.IsPlaying)
                    {
                        playingSpeakers++;
                    }
                }
            }

            Debug.Log($"[VoiceDebugger] Active Speakers: {activeSpeakers}, Playing: {playingSpeakers}");

            if (activeSpeakers == 0)
            {
                Debug.LogWarning("[VoiceDebugger] ⚠ NO ACTIVE SPEAKERS FOUND!");
            }
        }

        private void CheckVoiceNetworkObjects()
        {
            Debug.Log("--- Checking VoiceNetworkObjects ---");

            var voiceNetObjects = FindObjectsByType<VoiceNetworkObject>(FindObjectsSortMode.None);
            Debug.Log($"[VoiceDebugger] Found {voiceNetObjects.Length} VoiceNetworkObject instances");

            foreach (var vno in voiceNetObjects)
            {
                Debug.Log($"[VoiceDebugger] VoiceNetworkObject on: {vno.gameObject.name}");
                Debug.Log($"[VoiceDebugger]   RecorderInUse: {vno.RecorderInUse != null}");
                Debug.Log($"[VoiceDebugger]   SpeakerInUse: {vno.SpeakerInUse != null}");
                Debug.Log($"[VoiceDebugger]   IsRecording: {vno.IsRecording}");
                Debug.Log($"[VoiceDebugger]   IsSpeaking: {vno.IsSpeaking}");

                if (vno.RecorderInUse == null)
                {
                    Debug.LogWarning($"[VoiceDebugger] ⚠ VoiceNetworkObject has no RecorderInUse!");
                }

                if (vno.SpeakerInUse == null)
                {
                    Debug.LogWarning($"[VoiceDebugger] ⚠ VoiceNetworkObject has no SpeakerInUse!");
                }
            }
        }

        private void CheckMicrophone()
        {
            Debug.Log("--- Checking Microphone ---");
#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.Log("[VoiceDebugger] Microphone device enumeration not available on WebGL. Browser manages mic access via getUserMedia.");
#else
            string[] devices = Microphone.devices;
            Debug.Log($"[VoiceDebugger] Available Microphone Devices: {devices.Length}");

            string usedDevice = null;
            int usedDeviceIndex = -1;

            if (fusionVoiceClient != null && fusionVoiceClient.PrimaryRecorder != null)
            {
                var micDevice = fusionVoiceClient.PrimaryRecorder.MicrophoneDevice;
                if (micDevice != null && !string.IsNullOrEmpty(micDevice.Name))
                {
                    usedDevice = micDevice.Name;
                }
            }

            for (int i = 0; i < devices.Length; i++)
            {
                bool isUsed = (usedDevice != null && devices[i] == usedDevice) || (usedDevice == null && i == 0);
                string marker = isUsed ? " <- CURRENTLY USED" : "";

                if (isUsed)
                {
                    usedDeviceIndex = i;
                    Debug.Log($"[VoiceDebugger]   [{i}] {devices[i]}{marker}");
                }
                else
                {
                    Debug.Log($"[VoiceDebugger]   [{i}] {devices[i]}");
                }
            }

            if (devices.Length == 0)
            {
                Debug.LogError("[VoiceDebugger] NO MICROPHONE DEVICES FOUND!");
                Debug.LogError("[VoiceDebugger] Please check microphone permissions and connections!");
            }
            else if (usedDevice == null)
            {
                Debug.Log($"[VoiceDebugger] Using default microphone (index 0): {devices[0]}");
            }
            else
            {
                Debug.Log($"[VoiceDebugger] Recorder is using microphone: {usedDevice} (index: {usedDeviceIndex})");
            }

#if UNITY_ANDROID || UNITY_IOS
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                Debug.LogError("[VoiceDebugger] MICROPHONE PERMISSION NOT GRANTED!");
            }
#endif
#endif
        }

        private void CheckRecorderAudioSource(Recorder recorder)
        {
            AudioSource audioSource = recorder.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                Debug.Log($"[VoiceDebugger] Recorder has AudioSource:");
                Debug.Log($"[VoiceDebugger]   volume: {audioSource.volume}");
                Debug.Log($"[VoiceDebugger]   mute: {audioSource.mute}");
            }
        }

        /// <summary>
        /// Force enable voice transmission
        /// </summary>
        [ContextMenu("Force Enable Voice Transmission")]
        public void ForceEnableVoice()
        {
            Debug.Log("[VoiceDebugger] Force enabling voice transmission...");

            // Enable primary recorder
            if (fusionVoiceClient != null && fusionVoiceClient.PrimaryRecorder != null)
            {
                fusionVoiceClient.PrimaryRecorder.TransmitEnabled = true;
                fusionVoiceClient.PrimaryRecorder.RecordingEnabled = true;
                Debug.Log("[VoiceDebugger] Primary Recorder enabled");
            }

            // Enable all player recorders
            var players = FindObjectsByType<PlayerNetworkSetup>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                if (player.Object != null && player.Object.HasInputAuthority)
                {
                    Recorder recorder = player.GetComponentInChildren<Recorder>();
                    if (recorder != null)
                    {
                        recorder.TransmitEnabled = true;
                        recorder.RecordingEnabled = true;
                        Debug.Log($"[VoiceDebugger] Enabled recorder for local player: {player.PlayerName}");
                    }
                }
            }

            // Unmute all speakers
            var speakers = FindObjectsByType<Speaker>(FindObjectsSortMode.None);
            foreach (var speaker in speakers)
            {
                AudioSource audioSource = speaker.GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    audioSource.mute = false;
                    if (audioSource.volume < 0.5f)
                    {
                        audioSource.volume = 1f;
                    }
                    Debug.Log($"[VoiceDebugger] Unmuted speaker on: {speaker.gameObject.name}");
                }
            }

            Debug.Log("[VoiceDebugger] Voice transmission force-enabled!");
        }

        /// <summary>
        /// Switch to a specific microphone device by index
        /// </summary>
        public void SetMicrophoneDevice(int deviceIndex)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.Log("[VoiceDebugger] Microphone device selection not supported on WebGL.");
#else
            string[] devices = Microphone.devices;
            if (deviceIndex < 0 || deviceIndex >= devices.Length)
            {
                Debug.LogError($"[VoiceDebugger] Invalid device index {deviceIndex}. Available devices: 0-{devices.Length - 1}");
                return;
            }

            string deviceName = devices[deviceIndex];
            Debug.Log($"[VoiceDebugger] Switching to microphone device [{deviceIndex}]: {deviceName}");

            Photon.Voice.DeviceInfo deviceInfo = new Photon.Voice.DeviceInfo(deviceIndex, deviceName);

            if (fusionVoiceClient != null && fusionVoiceClient.PrimaryRecorder != null)
            {
                fusionVoiceClient.PrimaryRecorder.MicrophoneDevice = deviceInfo;
                Debug.Log($"[VoiceDebugger] Primary Recorder microphone changed to: {deviceName}");
            }

            var players = FindObjectsByType<PlayerNetworkSetup>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                if (player.Object != null && player.Object.HasInputAuthority)
                {
                    Recorder recorder = player.GetComponentInChildren<Recorder>();
                    if (recorder != null)
                    {
                        recorder.MicrophoneDevice = deviceInfo;
                        Debug.Log($"[VoiceDebugger] Local player recorder microphone changed to: {deviceName}");
                    }
                }
            }
#endif
        }

        /// <summary>
        /// Switch to Oculus Virtual Audio Device (VR headset microphone)
        /// </summary>
        [ContextMenu("Switch to Oculus Microphone")]
        public void SwitchToOculusMicrophone()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.Log("[VoiceDebugger] Oculus microphone not available on WebGL.");
#else
            string[] devices = Microphone.devices;
            for (int i = 0; i < devices.Length; i++)
            {
                if (devices[i].Contains("Oculus") || devices[i].Contains("Virtual Audio"))
                {
                    Debug.Log($"[VoiceDebugger] Found Oculus microphone at index {i}: {devices[i]}");
                    SetMicrophoneDevice(i);
                    return;
                }
            }
            Debug.LogWarning("[VoiceDebugger] Oculus Virtual Audio Device not found!");
#endif
        }
    }
}
