using System.Collections;
using UnityEngine;
using Fusion;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Climbing;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Content.Interaction;
using Unity.XR.CoreUtils;
using Photon.Voice.Fusion;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using Photon.Voice.Unity;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;

namespace VertexFormCore
{
    public class PlayerNetworkSetup : NetworkBehaviour
    {
        [Networked] public bool isHandTracking { get; set; }
        public GameObject LocalXRRigGameobject;
        [SerializeField] private GameObject MainAvatarGameobject;

        [SerializeField] private GameObject[] AvatarHeadGameobjects;
        [SerializeField] private GameObject AvatarBodyGameobject;

        [SerializeField] private GameObject[] AvatarModelPrefabs;
        [SerializeField] private TextMeshProUGUI PlayerName_Text;
        [SerializeField] private GameObject cameraOffset;
        [SerializeField] private XROrigin xROrigin;
        public float maxHeight;
        public float normalHeight;
        public TeleportationProvider tp;
        public GravityProvider gp;
        public GameObject leftHand;
        public GameObject rightHand;
        public Renderer leftHandVisual;
        public Renderer rightHandVisual;
        public GameObject LeftController;
        public GameObject rightController;
        public GameObject leftControllerHand;
        public GameObject rightControllerHand;
        public ClimbProvider cp;
        [SerializeField] InputActionManager IAM;
        [SerializeField] XRInputModalityManager XRIMM;

        [Header("Notification")]
        public RectTransform notificationParent;
        // Individual voice components - better approach
        [Header("Voice Components")]
        [SerializeField] private VoiceNetworkObject voiceNetworkObject;
        [SerializeField] private Recorder playerRecorder;
        [SerializeField] private Speaker playerSpeaker;

        public AudioListener audioListener;
        public Camera cam;
        public PlayerUIManager playerUIManager;
        public LocomotionManager locomotionManager;

        [SerializeField] GameObject[] nonSyncableObjects;

        public CustomAvatarScriptable customAvatarScriptable;
        public AvatarHolder avatarHolder;
        public LoadRPMAvatar RPM_avatarLoader;
        public GameObject RPM_body;
        public Transform bodyTransform;
        public Transform headTransform;

        // Fusion networked properties
        [Networked] public bool isRPM { get; set; }
        [Networked] public NetworkString<_64> rpmID { get; set; }
        [Networked] public int AvatarSelectionNumber { get; set; }
        [Networked] public NetworkString<_16> PlayerName { get; set; }


        // Track if avatar has been initialized for remote players
        private bool avatarInitialized = false;

        void SetLayerRecursively(GameObject go, int layerNumber)
        {
            if (go == null) return;
            foreach (Transform trans in go.GetComponentsInChildren<Transform>(true))
            {
                trans.gameObject.layer = layerNumber;
            }
        }

        // Method to initialize avatar for remote players when AvatarSelectionNumber is available
        private void InitializeRemotePlayerAvatar()
        {
            if (!Object.HasInputAuthority && !avatarInitialized && AvatarSelectionNumber >= 0)
            {
                Debug.Log($"[PlayerNetworkSetup] Initializing remote player avatar with selection number: {AvatarSelectionNumber}");
                InitializeSelectedAvatarModel(AvatarSelectionNumber);
                avatarInitialized = true;
            }
        }

        public void InitializeSelectedAvatarModel(int avatarSelectionNumber)
        {
            Debug.Log("-->on selected avatar " + avatarSelectionNumber + "for mine? " + Object.HasInputAuthority);
            AvatarInputConverter avatarInputConverter = LocalXRRigGameobject.GetComponent<AvatarInputConverter>();

            GameObject body1 = Instantiate(customAvatarScriptable.avatarDatas[avatarSelectionNumber].body);
            body1.transform.SetParent(bodyTransform, false);
            GameObject head1 = Instantiate(customAvatarScriptable.avatarDatas[avatarSelectionNumber].head);
            head1.transform.SetParent(headTransform, false);
            body1.transform.localPosition = head1.transform.localPosition = Vector3.zero;
            avatarHolder.SetAvatar(head1, body1);

            SetUpAvatarGameobject(avatarHolder.HeadTransform, avatarInputConverter.AvatarHead);
            SetUpAvatarGameobject(avatarHolder.BodyTransform, avatarInputConverter.AvatarBody);
            SetUpAvatarGameobject(avatarHolder.HandLeftTransform, avatarInputConverter.AvatarHand_Left);
            SetUpAvatarGameobject(avatarHolder.HandRightTransform, avatarInputConverter.AvatarHand_Right);

            if (!Object.HasInputAuthority)
            {
                if (avatarInputConverter.AvatarHand_Left.GetComponentInChildren<AnimateHand>())
                {
                    Debug.Log("-->destroying left hand");
                    Destroy(avatarInputConverter.AvatarHand_Left.GetComponentInChildren<AnimateHand>());
                }
                if (avatarInputConverter.AvatarHand_Right.GetComponentInChildren<AnimateHand>())
                {
                    Debug.Log("-->destroying right hand");
                    Destroy(avatarInputConverter.AvatarHand_Right.GetComponentInChildren<AnimateHand>());
                }
            }
            else
            {
                Debug.Log("-->on selected avatar " + avatarSelectionNumber + "for mine? " + Object.HasInputAuthority);
                avatarHolder.SetAvatarLayer();
            }
        }

        void SetUpAvatarGameobject(Transform avatarModelTransform, Transform mainAvatarTransform)
        {
            avatarModelTransform.SetParent(mainAvatarTransform);
            avatarModelTransform.localPosition = Vector3.zero;
            avatarModelTransform.localRotation = Quaternion.identity;
        }

        public override void Spawned()
        {
            // Reset avatar initialization flag
            avatarInitialized = false;
            Debug.Log("-->spawning player");
            StartCoroutine(InitializePlayer());
            Debug.Log("-->spawning player done");
            Debug.Log("transform.position: " + transform.position + "transform.rotation: " + transform.rotation);

        }

        private IEnumerator InitializePlayer()
        {
            Debug.Log("-->initializing player");
            // Wait for network runner to be ready
            while (Runner == null || !Runner.IsClient)
            {
                Debug.Log("-->waiting for network runner to be ready");
                yield return new WaitForSeconds(0.1f);
            }

            // Disable AudioListener for remote players (don't destroy as it may be needed by voice components)
            if (audioListener != null && !Object.HasInputAuthority)
            {
                audioListener.enabled = false;
                Debug.Log("-->audio listener disabled for remote player");
            }
            // Set player name from stored PlayerPrefs or generate one
            string playerName = PlayerPrefs.GetString("PlayerName", "Player" + Random.Range(1000, 9999));
            if (Object.HasInputAuthority)
            {
                Debug.Log("Setting player name to: " + playerName);
                //PlayerName = playerName;
                PlayerName = ProjectManager.UserName;
            }
            Debug.Log("-->player name set");
            gameObject.name = $"player {PlayerName}";
            Debug.Log("-->game object name set");
            if (!RoomManager.Instance.allPlayers.Contains(this))
            {
                RoomManager.Instance.allPlayers.Add(this);
                Debug.Log("-->player added to spawn manager");
            }

            if (Object.HasInputAuthority)
            {
                Debug.Log("-->player is local");
                //The player is local
                LocalXRRigGameobject.SetActive(true);
                SetupIndividualVoiceComponents(); // Call this here
                playerUIManager.InitializeAllSettings();
                //Getting the avatar selection data
                int avatarSelectionNumber = PlayerPrefs.GetInt(MultiplayerVRConstants.AVATAR_SELECTION_NUMBER);
                AvatarSelectionNumber = avatarSelectionNumber;
                isRPM = PlayerPrefs.GetString(MultiplayerVRConstants.IS_RPM) == "true" ? true : false;
                MainAvatarGameobject.SetActive(!isRPM);
                RPM_body.SetActive(isRPM);
                if (isRPM)
                {
                    rpmID = PlayerPrefs.GetString(MultiplayerVRConstants.RPM_AVATAR_ID);
                    Debug.Log("Loading RPM Avatar from URL: " + rpmID);
                    RPM_avatarLoader.LoadAvatar(rpmID.ToString());
                }
                else
                {
                    InitializeSelectedAvatarModel(avatarSelectionNumber);
                }

                Debug.Log("-->avatar initialized");
                foreach (GameObject head in AvatarHeadGameobjects)
                {
                    SetLayerRecursively(head, 6);
                }
                SetLayerRecursively(AvatarBodyGameobject, 7);

                // Add AudioListener to local player if not already present
                if (MainAvatarGameobject.GetComponent<AudioListener>() == null)
                {
                    MainAvatarGameobject.AddComponent<AudioListener>();
                    Debug.Log("-->AudioListener added to local player");
                }
                HandAndControllerSync();
            }
            else
            {
                cam.enabled = false;
                //The player is remote
                IAM.actionAssets.Clear();
                XRIMM.enabled = false;
                XRIMM.leftHand = XRIMM.rightHand = null;
                for (int i = 0; i < nonSyncableObjects.Length; i++)
                {
                    if (nonSyncableObjects[i].gameObject != null)
                    {
                        GameObject g = nonSyncableObjects[i].gameObject;
                        //g.SetActive(false);
                        Destroy(g);
                    }
                }
                MainAvatarGameobject.SetActive(!isRPM);
                RPM_body.SetActive(isRPM);
                foreach (GameObject head in AvatarHeadGameobjects)
                {
                    SetLayerRecursively(head, 0);
                }
                yield return new WaitForSeconds(0.5f); // Small delay to ensure networked properties are synced

                // Setup voice components for remote player to ensure we can hear them
                SetupRemotePlayerVoiceComponents();
                if (isRPM)
                {
                    RPM_avatarLoader.LoadAvatar(rpmID.ToString());
                }
                else
                {
                    InitializeRemotePlayerAvatar();
                }

                Debug.Log("-->remote player avatar initialized");
            }

            if (PlayerName_Text != null)
            {
                Debug.Log("-->setting player name text");
                PlayerName_Text.text = PlayerName.ToString();
                float yRot = Object.HasInputAuthority == true ? 0 : 180;
                PlayerName_Text.transform.localRotation = Quaternion.Euler(Vector3.up * yRot);
                Debug.Log("-->player name text set");
            }
        }

        private void HandAndControllerSync()
        {
            XRIMM.trackedHandModeStarted.AddListener(OnTrackedHandModeStarted);
            XRIMM.trackedHandModeEnded.AddListener(OnTrackedHandModeEnded);
            XRIMM.motionControllerModeStarted.AddListener(OnMotionControllerModeStarted);
            XRIMM.motionControllerModeEnded.AddListener(OnMotionControllerModeEnded);
            if (XRIMM.leftController.activeInHierarchy || XRIMM.rightController.activeInHierarchy)
            {
                OnMotionControllerModeStarted();
            }
            else
            {
                OnTrackedHandModeStarted();
            }
        }


        private void OnMotionControllerModeStarted()
        {
            isHandTracking = false;
            RPC_EnableHandController();
        }

        private void OnMotionControllerModeEnded()
        {

        }

        private void OnTrackedHandModeEnded()
        {

        }

        private void OnTrackedHandModeStarted()
        {
            isHandTracking = true;
            RPC_EnableHand();
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
        public void RPC_EnableHandController()
        {
            leftHand.SetActive(false);
            rightHand.SetActive(false);
            rightControllerHand.SetActive(true);
            leftControllerHand.SetActive(true);
            LeftController.SetActive(true);
            rightController.SetActive(true);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
        public void RPC_EnableHand()
        {
            Debug.Log("isRPM: " + isRPM + !isRPM);
            leftHand.SetActive(true);
            rightHand.SetActive(true);
            leftHandVisual.enabled = rightHandVisual.enabled = (!isRPM);
            LeftController.SetActive(false);
            rightController.SetActive(false);
            rightControllerHand.SetActive(false);
            leftControllerHand.SetActive(false);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (RoomManager.Instance != null && RoomManager.Instance.allPlayers.Contains(this))
            {
                RoomManager.Instance.allPlayers.Remove(this);
            }
        }

        public void CallSetStandingHeightRPC()
        {
            if (Object.HasInputAuthority)
            {
                RPC_SetStandingHeight();
            }
        }

        //[Rpc(RpcSources.InputAuthority, RpcTargets.All)]
        public void RPC_SetStandingHeight()
        {
            cameraOffset.transform.localPosition = Vector3.up * maxHeight;
#if UNITY_EDITOR
            if (IsDeviceSimulatorActive())
            {
                xROrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.NotSpecified;
                cameraOffset.transform.localPosition = Vector3.up * 1.6f;
            }
#endif
        }

        public void CallSetSittingHeightRPC()
        {
            if (Object.HasInputAuthority)
            {
                RPC_SetSittingHeight();
            }
        }

        //[Rpc(RpcSources.InputAuthority, RpcTargets.All)]
        public void RPC_SetSittingHeight()
        {
            cameraOffset.transform.localPosition = Vector3.up * normalHeight;
#if UNITY_EDITOR
            if (IsDeviceSimulatorActive())
            {
                xROrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.NotSpecified;
                cameraOffset.transform.localPosition = Vector3.up * 1f;
            }
#endif
        }

        public void ResetPosition()
        {
            Debug.Log("Reset Position");
            transform.localPosition = Vector3.zero;
        }

        private bool IsDeviceSimulatorActive()
        {
            var ds = FindAnyObjectByType<XRDeviceSimulator>();
            if (ds != null)
            {
                return true;
            }
            return false;
        }

        public void MegaphoneHandler(bool active)
        {
            if (Object.HasInputAuthority)
            {
                RPC_MegaPhoneHandle(active, Object.Id);
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
        public void RPC_MegaPhoneHandle(bool on, NetworkId objectId)
        {
            // Only apply to the specific player that requested it
            if (Object != null && Object.Id == objectId)
            {
                Debug.Log($"[PlayerNetworkSetup] RPC_MegaPhoneHandle called for player {PlayerName} - Megaphone: {on}");
                SetMegaphoneMode(on);
            }
        }

        private void OnApplicationPause(bool pause)
        {
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.tag == "Respawn")
            {
                ResetPosition();
            }
        }

        public void HandleMasterClient()
        {
            // In Fusion, master client handling is done differently
            // This functionality might be handled by the RoomManager or SpawnManager
            if (Object.HasInputAuthority)
            {
                Debug.Log("Handle master client - functionality moved to RoomManager");
            }
        }

        /// <summary>
        /// Setup individual voice components for this player (LOCAL PLAYER)
        /// </summary>
        private void SetupIndividualVoiceComponents()
        {
            if (playerRecorder != null)
            {
                VoiceRecorderManager.Instance.recorder = playerRecorder;

                // Ensure recorder is properly configured
                playerRecorder.TransmitEnabled = true;
                playerRecorder.RecordingEnabled = true;

                Debug.Log($"[PlayerNetworkSetup] Recorder setup complete - TransmitEnabled: {playerRecorder.TransmitEnabled}, RecordingEnabled: {playerRecorder.RecordingEnabled}");
            }
            else
            {
                Debug.LogError("[PlayerNetworkSetup] PlayerRecorder is null! Voice will not work.");
            }

            if (playerSpeaker != null)
            {
                Debug.Log($"[PlayerNetworkSetup] Speaker found and ready");
            }
            else
            {
                Debug.LogWarning("[PlayerNetworkSetup] PlayerSpeaker is null - this may be normal for local player");
            }


            Debug.Log($"[PlayerNetworkSetup] Voice components setup - Recorder: {playerRecorder != null}, Speaker: {playerSpeaker != null}, VoiceNetworkObject: {voiceNetworkObject != null}");
        }

        /// <summary>
        /// Setup voice components for remote players to ensure their audio is heard
        /// </summary>
        private void SetupRemotePlayerVoiceComponents()
        {
            Debug.Log($"[PlayerNetworkSetup] Setting up remote player voice components for {PlayerName}");

            // For remote players, we primarily need the Speaker component
            if (playerSpeaker != null)
            {
                AudioSource audioSource = playerSpeaker.GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    // Ensure audio source is properly configured for 3D spatial audio
                    audioSource.spatialBlend = 1f; // Default to 3D spatial audio
                    audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
                    audioSource.minDistance = 1f;
                    audioSource.maxDistance = 50f;
                    Debug.Log($"[PlayerNetworkSetup] Remote player speaker audio source configured for {PlayerName}");
                }
                else
                {
                    Debug.LogWarning($"[PlayerNetworkSetup] AudioSource not found on remote player speaker for {PlayerName}");
                }
            }
            else if (voiceNetworkObject != null)
            {
                // Fallback: try to get speaker from VoiceNetworkObject
                Debug.Log($"[PlayerNetworkSetup] Waiting for VoiceNetworkObject to initialize speaker for {PlayerName}");
                StartCoroutine(WaitForRemotePlayerSpeaker());
            }
            else
            {
                Debug.LogError($"[PlayerNetworkSetup] No voice components found for remote player {PlayerName}!");
            }
        }

        /// <summary>
        /// Coroutine to wait for remote player speaker to be initialized
        /// </summary>
        private IEnumerator WaitForRemotePlayerSpeaker()
        {
            float timeout = 10f;
            float elapsed = 0f;

            while (playerSpeaker == null && elapsed < timeout)
            {
                playerSpeaker = GetPlayerSpeaker();
                if (playerSpeaker != null)
                {
                    Debug.Log($"[PlayerNetworkSetup] Remote player speaker found for {PlayerName}");
                    SetupRemotePlayerVoiceComponents(); // Call setup again now that speaker exists
                    yield break;
                }
                elapsed += 0.5f;
                yield return new WaitForSeconds(0.5f);
            }

            if (playerSpeaker == null)
            {
                Debug.LogError($"[PlayerNetworkSetup] Timeout: Could not find speaker for remote player {PlayerName}");
            }
        }
        /// <summary>
        /// Get the individual player recorder for muting
        /// </summary>
        public Recorder GetPlayerRecorder()
        {
            return playerRecorder ?? voiceNetworkObject?.RecorderInUse;
        }

        /// <summary>
        /// Get the individual player speaker for spatial audio control
        /// </summary>
        public Speaker GetPlayerSpeaker()
        {
            return playerSpeaker ?? voiceNetworkObject?.SpeakerInUse; // Fallback to legacy speaker
        }

        /// <summary>
        /// Mute/unmute this player's voice
        /// </summary>
        public void SetVoiceMuted(bool muted)
        {
            Recorder recorder = GetPlayerRecorder();
            if (recorder != null)
            {
                recorder.TransmitEnabled = !muted;
                Debug.Log($"[PlayerNetworkSetup] Player voice {(muted ? "muted" : "unmuted")}");
            }
        }

        /// <summary>
        /// Control spatial audio blend for this player's voice
        /// </summary>
        public void SetSpatialBlend(float spatialBlend)
        {
            Speaker playerSpeaker = GetPlayerSpeaker();
            if (playerSpeaker != null)
            {
                AudioSource audioSource = playerSpeaker.GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    audioSource.spatialBlend = Mathf.Clamp01(spatialBlend);
                    Debug.Log($"[PlayerNetworkSetup] Spatial blend set to {audioSource.spatialBlend} for player {PlayerName}");
                }
                else
                {
                    Debug.LogWarning($"[PlayerNetworkSetup] AudioSource not found on Speaker for player {PlayerName}");
                }
            }
            else
            {
                // For local player, the speaker might not exist yet, so we need to try recorder's audio source
                if (Object.HasInputAuthority && playerRecorder != null)
                {
                    AudioSource recorderAudioSource = playerRecorder.GetComponent<AudioSource>();
                    if (recorderAudioSource != null)
                    {
                        recorderAudioSource.spatialBlend = Mathf.Clamp01(spatialBlend);
                        Debug.Log($"[PlayerNetworkSetup] Spatial blend set to {recorderAudioSource.spatialBlend} for local player {PlayerName} via Recorder");
                    }
                }
                else
                {
                    Debug.LogWarning($"[PlayerNetworkSetup] Speaker not found for player {PlayerName}. Spatial blend not set.");
                }
            }
        }

        /// <summary>
        /// Enable/disable megaphone mode (2D audio vs 3D spatial audio)
        /// </summary>
        public void SetMegaphoneMode(bool enabled)
        {
            // Megaphone ON = 0f spatial blend (2D audio, everyone hears at same volume)
            // Megaphone OFF = 1f spatial blend (3D audio, volume based on distance)
            SetSpatialBlend(enabled ? 0f : 1f);
            Debug.Log($"[PlayerNetworkSetup] Megaphone mode {(enabled ? "enabled" : "disabled")} for player {PlayerName}");
        }
    }
}
