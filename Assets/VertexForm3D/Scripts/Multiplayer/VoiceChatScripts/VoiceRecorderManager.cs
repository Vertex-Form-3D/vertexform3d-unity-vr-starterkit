using Photon.Voice.Unity;
using Photon.Voice.Fusion;
using UnityEngine;
using VertexFormCore;

public class VoiceRecorderManager : MonoBehaviour
{
    public Recorder recorder;
    [SerializeField] private FusionVoiceClient fusionVoiceClient;

    public static VoiceRecorderManager Instance;

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
        if (recorder != null)
        {
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
        if (recorder != null)
        {
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
}
