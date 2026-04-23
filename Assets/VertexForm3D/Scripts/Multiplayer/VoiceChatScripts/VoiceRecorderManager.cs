using Photon.Voice.Unity;
using Photon.Voice.Fusion;
using UnityEngine;
using VertexFormCore;

public class VoiceRecorderManager : MonoBehaviour
{
    public Recorder recorder;
    [SerializeField] private FusionVoiceClient fusionVoiceClient;
    [SerializeField] private float reconnectCooldownSeconds = 4f;

    public static VoiceRecorderManager Instance;
    private bool desiredTransmitEnabled = true;
    private float lastReconnectAttemptTime = -999f;
    private Photon.Realtime.ClientState? lastLoggedVoiceState;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void Start()
    {
        // Try to find FusionVoiceClient if not assigned
        if (fusionVoiceClient == null)
        {
            fusionVoiceClient = FindFirstObjectByType<FusionVoiceClient>();
            if (fusionVoiceClient == null)
            {
                Debug.LogError("[VoiceRecorderManager] FusionVoiceClient not found in scene!");
            }
        }

        // Start delayed initialization - PrimaryRecorder might not be ready immediately
        StartCoroutine(InitializeRecorderDelayed());

        // Respect project default on first load. Runtime UI toggles may override this later.
        desiredTransmitEnabled = GetDefaultTransmitFromSettings();

        // Start checking voice connection state
        InvokeRepeating(nameof(CheckVoiceConnectionState), 2f, 5f);
    }

    /// <summary>
    /// Initialize recorder with delay to ensure FusionVoiceClient is ready
    /// </summary>
    private System.Collections.IEnumerator InitializeRecorderDelayed()
    {
        float timeout = 10f;
        float elapsed = 0f;

        // Wait until recorder is assigned or timeout
        while (recorder == null && elapsed < timeout)
        {
            // Try to find FusionVoiceClient if still null
            if (fusionVoiceClient == null)
            {
                fusionVoiceClient = FindFirstObjectByType<FusionVoiceClient>();
            }

            // Try to get PrimaryRecorder
            if (fusionVoiceClient != null && fusionVoiceClient.PrimaryRecorder != null)
            {
                recorder = fusionVoiceClient.PrimaryRecorder;
                Debug.Log("[VoiceRecorderManager] Recorder initialized successfully");
                yield break;
            }

            elapsed += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }

        if (recorder == null)
        {
            Debug.LogWarning("[VoiceRecorderManager] PrimaryRecorder not found after timeout. Voice will be assigned per-player.");
        }
    }

    /// <summary>
    /// Periodically check and log voice connection state
    /// </summary>
    private void CheckVoiceConnectionState()
    {
        EnsureRecorderReference();

        bool joined = fusionVoiceClient != null &&
                      fusionVoiceClient.Client != null &&
                      fusionVoiceClient.Client.State == Photon.Realtime.ClientState.Joined;

        if (!joined)
        {
            if (fusionVoiceClient != null && fusionVoiceClient.Client != null)
            {
                var state = fusionVoiceClient.Client.State;
                if (IsVoiceClientTransitionState(state))
                {
                    if (lastLoggedVoiceState != state)
                    {
                        Debug.Log($"[VoiceRecorderManager] Voice is transitioning ({state}), skipping reconnect attempt.");
                        lastLoggedVoiceState = state;
                    }
                }
                else
                {
                    Debug.LogWarning("[VoiceRecorderManager] Voice disconnected or not joined. Attempting auto-reconnect...");
                    TryReconnectVoice();
                }
            }
            else
            {
                Debug.LogWarning("[VoiceRecorderManager] Voice client is unavailable. Attempting auto-reconnect...");
                TryReconnectVoice();
            }
        }
        else
        {
            lastLoggedVoiceState = Photon.Realtime.ClientState.Joined;
        }

        if (fusionVoiceClient != null && fusionVoiceClient.Client != null)
        {
            var clientState = fusionVoiceClient.Client.State;
            if (clientState != Photon.Realtime.ClientState.Joined)
            {
                Debug.LogWarning($"[VoiceRecorderManager] Voice client not in Joined state: {clientState}");
            }
        }

        if (recorder != null)
        {
            // Check if recorder is properly set up
            if (!recorder.RecordingEnabled)
            {
                Debug.LogWarning("[VoiceRecorderManager] Recorder recording is not enabled");
                recorder.RecordingEnabled = true;
            }

            // Keep transmit state consistent with desired runtime/default state.
            if (recorder.TransmitEnabled != desiredTransmitEnabled)
            {
                recorder.TransmitEnabled = desiredTransmitEnabled;
                Debug.Log($"[VoiceRecorderManager] Restored recorder transmit state to: {desiredTransmitEnabled}");
            }
        }
        else
        {
            Debug.LogWarning("[VoiceRecorderManager] Recorder is null");
        }
    }

    private void OnDestroy()
    {
        CancelInvoke(nameof(CheckVoiceConnectionState));
    }



    public void EnableRecorder()
    {
        desiredTransmitEnabled = true;
        EnsureRecorderReference();
        if (recorder != null)
        {
            recorder.RecordingEnabled = true;
            recorder.TransmitEnabled = true;
            Debug.Log("[VoiceRecorderManager] Recorder enabled");
        }
        else
        {
            Debug.LogError("[VoiceRecorderManager] Cannot enable recorder - recorder is null");
        }
    }

    public void DisableRecorder()
    {
        desiredTransmitEnabled = false;
        EnsureRecorderReference();
        if (recorder != null)
        {
            recorder.RecordingEnabled = true;
            recorder.TransmitEnabled = false;
            Debug.Log("[VoiceRecorderManager] Recorder disabled");
        }
        else
        {
            Debug.LogError("[VoiceRecorderManager] Cannot disable recorder - recorder is null");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("[VoiceRecorderManager] Space key pressed");
            if (recorder != null)
            {
                Debug.Log("[VoiceRecorderManager] Recorder status: " + recorder.TransmitEnabled);
            }
            else
            {
                Debug.LogWarning("[VoiceRecorderManager] Recorder is null");
            }
        }
    }

    /// <summary>
    /// Alternative method to control voice through the local player directly
    /// </summary>
    public void SetLocalPlayerVoiceMuted(bool muted)
    {
        PlayerNetworkSetup[] players = FindObjectsByType<PlayerNetworkSetup>(FindObjectsSortMode.None);
        foreach (PlayerNetworkSetup player in players)
        {
            if (player.Object != null && player.Object.HasInputAuthority)
            {
                player.SetVoiceMuted(muted);
                return;
            }
        }
        Debug.LogWarning("[VoiceRecorderManager] Local player not found for voice control");
    }

    /// <summary>
    /// Returns the list of available microphone device names (same order as Unity Microphone.devices).
    /// </summary>
    public string[] GetMicrophoneDevices()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log("[VoiceRecorderManager] Microphone device enumeration not available on WebGL. Browser manages mic access.");
        return new string[] { "Default (Browser)" };
#else
        return Microphone.devices;
#endif
    }

    /// <summary>
    /// Returns the current microphone device index, or 0 if default/unknown.
    /// </summary>
    public int GetCurrentMicrophoneDeviceIndex()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return 0;
#else
        string[] devices = Microphone.devices;
        if (devices == null || devices.Length == 0) return 0;
        if (recorder == null) return 0;
        var dev = recorder.MicrophoneDevice;
        if (dev.IsDefault || dev.IDInt < 0) return 0;
        int index = Mathf.Clamp(dev.IDInt, 0, devices.Length - 1);
        return index;
#endif
    }

    /// <summary>
    /// Switch the voice recorder to use the microphone at the given device index.
    /// Index should match Unity Microphone.devices order.
    /// </summary>
    public void SetMicrophoneDevice(int deviceIndex)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log("[VoiceRecorderManager] Microphone device selection not supported on WebGL.");
        return;
#else
        string[] devices = Microphone.devices;
        if (devices == null || devices.Length == 0)
        {
            Debug.LogWarning("[VoiceRecorderManager] No microphone devices found.");
            return;
        }
        if (deviceIndex < 0 || deviceIndex >= devices.Length)
        {
            Debug.LogWarning($"[VoiceRecorderManager] Invalid device index {deviceIndex}. Using 0.");
            deviceIndex = 0;
        }
        string deviceName = devices[deviceIndex];
        var deviceInfo = new Photon.Voice.DeviceInfo(deviceIndex, deviceName);

        if (recorder != null)
        {
            recorder.MicrophoneDevice = deviceInfo;
            Debug.Log($"[VoiceRecorderManager] Microphone set to: {deviceName}");
        }

        PlayerNetworkSetup[] players = FindObjectsByType<PlayerNetworkSetup>(FindObjectsSortMode.None);
        foreach (PlayerNetworkSetup player in players)
        {
            if (player.Object != null && player.Object.HasInputAuthority)
            {
                Recorder playerRecorder = player.GetComponentInChildren<Recorder>();
                if (playerRecorder != null && playerRecorder != recorder)
                {
                    playerRecorder.MicrophoneDevice = deviceInfo;
                }
                break;
            }
        }
#endif
    }

    private void EnsureRecorderReference()
    {
        if (recorder != null)
            return;

        if (fusionVoiceClient == null)
            fusionVoiceClient = FindFirstObjectByType<FusionVoiceClient>();

        if (fusionVoiceClient != null && fusionVoiceClient.PrimaryRecorder != null)
        {
            recorder = fusionVoiceClient.PrimaryRecorder;
            Debug.Log("[VoiceRecorderManager] Recorder reference recovered from FusionVoiceClient.PrimaryRecorder.");
        }
    }

    private bool GetDefaultTransmitFromSettings()
    {
        if (ProjectManager.instance == null ||
            ProjectManager.instance.settingsUI == null ||
            ProjectManager.instance.settingsUI.defaultSettings == null)
        {
            return true;
        }

        return ProjectManager.instance.settingsUI.defaultSettings.micType == micType.unmute;
    }

    private void TryReconnectVoice()
    {
        if (Time.unscaledTime - lastReconnectAttemptTime < reconnectCooldownSeconds)
            return;

        if (RoomManager.Instance != null)
        {
            var runner = RoomManager.Instance.Runner;
            if (runner == null || !runner.IsRunning || runner.SessionInfo == null)
            {
                Debug.Log("[VoiceRecorderManager] Reconnect deferred: Fusion runner not joined yet.");
                return;
            }

            lastReconnectAttemptTime = Time.unscaledTime;
            RoomManager.Instance.JoinVoiceLobby();
            return;
        }

        lastReconnectAttemptTime = Time.unscaledTime;

        if (fusionVoiceClient == null)
            fusionVoiceClient = FindFirstObjectByType<FusionVoiceClient>();

        if (fusionVoiceClient == null)
        {
            Debug.LogWarning("[VoiceRecorderManager] Cannot reconnect voice: FusionVoiceClient not found.");
            return;
        }

        if (fusionVoiceClient.AutoConnectAndJoin)
            fusionVoiceClient.AutoConnectAndJoin = false;

        if (fusionVoiceClient.Client != null)
        {
            var state = fusionVoiceClient.Client.State;
            if (state == Photon.Realtime.ClientState.Joined)
                return;

            if (IsVoiceClientTransitionState(state))
            {
                if (lastLoggedVoiceState != state)
                {
                    Debug.Log($"[VoiceRecorderManager] Reconnect deferred while voice is in state: {state}");
                    lastLoggedVoiceState = state;
                }
                return;
            }
        }

        bool reconnectRequested = fusionVoiceClient.ConnectAndJoinRoom();
        if (!reconnectRequested)
        {
            Debug.LogWarning("[VoiceRecorderManager] Voice reconnect request was not accepted.");
        }
    }

    private static bool IsVoiceClientTransitionState(Photon.Realtime.ClientState state)
    {
        return state != Photon.Realtime.ClientState.Disconnected &&
               state != Photon.Realtime.ClientState.PeerCreated &&
               state != Photon.Realtime.ClientState.Joined;
    }
}
