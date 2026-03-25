using UnityEngine;
using UnityEngine.UI;
using Photon.Voice.Fusion;
using Fusion;
using DG.Tweening;

namespace VertexFormCore
{
    public class HighlightVoice : NetworkBehaviour
    {
        [SerializeField]
        private Image micImage;

        [SerializeField]
        private Image speakerImage;

        [SerializeField]
        private PlayerNetworkSetup playerNetworkSetup;

        [SerializeField]
        private NetworkObject networkObject;

        [Header("LipSync Settings")]

        public float amplitude;
        [SerializeField]
        private SkinnedMeshRenderer headMesh; // Head mesh with viseme blendshapes

        [SerializeField]
        private string visemeBlendShape = "viseme_O"; // Blendshape for mouth open

        [SerializeField]
        private float amplitudeThreshold = 0.01f; // Min amplitude to trigger

        [SerializeField]
        private float maxBlendWeight = 100f; // Max blendshape weight

        [SerializeField]
        private float smoothing = 0.1f; // Smoothing for viseme transitions

        public AudioSource audioSource; // For local mic or remote playback
        private float currentWeight;
        private bool isLocalPlayer;
        Transform localPlayerCam;
        private void Awake()
        {
            if (micImage != null)
                micImage.enabled = false;
            if (speakerImage != null)
                speakerImage.enabled = false;

            // Try to get PlayerNetworkSetup if not assigned
            if (playerNetworkSetup == null)
            {
                playerNetworkSetup = GetComponent<PlayerNetworkSetup>();
            }
        }

        public override void Spawned()
        {
            isLocalPlayer = Object.HasInputAuthority;
            if (isLocalPlayer)
            {
                // Local: Link Recorder to AudioSource
                if (playerNetworkSetup != null && playerNetworkSetup.GetPlayerRecorder() != null)
                {
                    /*var recorder = playerNetworkSetup.GetPlayerRecorder();
                    recorder.TransmitToAudioSource(audioSource); // Assumes method exists or plugin setup*/
                    Debug.Log("Local lip-sync initialized with mic.");
                }
                else
                {
                    Debug.LogError("Recorder not found for local player!");
                }
            }
            else
            {
                // Remote: Wait for Speaker
                StartCoroutine(WaitForRemoteSpeaker());
            }

            // Start voice and lip-sync checks
            InvokeRepeating(nameof(CheckVoiceAndLipSync), 0.1f, 0.1f);
        }

        private System.Collections.IEnumerator WaitForRemoteSpeaker()
        {
            float timeout = 10f; // 10 second timeout
            float elapsed = 0f;

            // First wait for speaker to exist
            while (playerNetworkSetup.GetPlayerSpeaker() == null && elapsed < timeout)
            {
                elapsed += 0.5f;
                yield return new WaitForSeconds(0.5f);
            }

            if (playerNetworkSetup.GetPlayerSpeaker() == null)
            {
                Debug.LogError($"[HighlightVoice] Timeout waiting for Speaker on remote player {playerNetworkSetup.PlayerName}");
                yield break;
            }

            // Get the audio source
            audioSource = playerNetworkSetup.GetPlayerSpeaker().GetComponent<AudioSource>();

            if (audioSource != null)
            {
                Debug.Log($"[HighlightVoice] Remote lip-sync linked to voice stream for player {playerNetworkSetup.PlayerName}");
            }
            else
            {
                Debug.LogError($"[HighlightVoice] AudioSource not found on Speaker for remote player {playerNetworkSetup.PlayerName}");
            }
        }

        private void CheckVoiceAndLipSync()
        {
            if (playerNetworkSetup == null) return;

            // Mic UI (local player)
            if (micImage != null && playerNetworkSetup.GetPlayerRecorder() != null)
            {
                var recorder = playerNetworkSetup.GetPlayerRecorder();
                if (recorder != null)
                {
                    micImage.enabled = recorder.RecordingEnabled && recorder.TransmitEnabled;
                }
            }

            // Speaker UI (remote players)
            if (speakerImage != null && playerNetworkSetup.GetPlayerSpeaker() != null)
            {
                var speaker = playerNetworkSetup.GetPlayerSpeaker();
                if (speaker != null)
                {
                    speakerImage.enabled = speaker.IsPlaying;
                }
            }

            // LookAt for remote players
            if (networkObject != null && !isLocalPlayer && RoomManager.Instance != null)
            {
                var localPlayerSetup = RoomManager.Instance.GetLocalPlayerSetup();
                if (localPlayerSetup != null && localPlayerCam == null)
                    localPlayerCam = localPlayerSetup.cam.transform;

                if (localPlayerCam != null)
                {
                    transform.LookAt(localPlayerCam);
                }
            }

            // Lip-sync
            if (headMesh != null && audioSource != null && audioSource.isPlaying)
            {
                amplitude = GetAudioAmplitude(audioSource);
                float targetWeight = amplitude > amplitudeThreshold ? maxBlendWeight * Mathf.Clamp01(amplitude) : 0f;
                currentWeight = Mathf.Lerp(currentWeight, targetWeight, smoothing);

                if (headMesh.sharedMesh != null)
                {
                    int blendShapeIndex = headMesh.sharedMesh.GetBlendShapeIndex(visemeBlendShape);
                    if (blendShapeIndex >= 0)
                    {
                        headMesh.SetBlendShapeWeight(blendShapeIndex, currentWeight);
                    }
                }
            }
        }

        private float GetAudioAmplitude(AudioSource source)
        {
            float[] samples = new float[512];
            source.GetOutputData(samples, 0);
            float sum = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                sum += Mathf.Abs(samples[i]);
            }
            return sum / samples.Length; // Average amplitude
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_SetMuted(bool muted)
        {
            if (isLocalPlayer && playerNetworkSetup.GetPlayerRecorder() != null)
            {
                playerNetworkSetup.GetPlayerRecorder().TransmitEnabled = !muted;
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (audioSource != null)
            {
                Destroy(audioSource);
            }
            CancelInvoke(nameof(CheckVoiceAndLipSync));
            base.Despawned(runner, hasState);
        }
    }
}