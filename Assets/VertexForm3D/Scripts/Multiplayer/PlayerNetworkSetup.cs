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
using UnityEngine.InputSystem.XR;

namespace VertexFormCore
{
    public class PlayerNetworkSetup : NetworkBehaviour
    {
        [Networked] public bool isHandTracking { get; set; }
        public GameObject LocalXRRigGameobject;
        [SerializeField] private GameObject MainAvatarGameobject;

        [SerializeField] private TextMeshProUGUI PlayerName_Text;
        [SerializeField] private GameObject cameraOffset;
        [SerializeField] private XROrigin xROrigin;
        [Tooltip("Desktop/web only. In VR the standing height is calibrated from the headset instead — see standingEyeHeight.")]
        public float standingHeight;
        public float sittingHeight;

        [Header("VR eye heights")]
        [Tooltip("Eye height, in metres, that every VR player is placed at when standing, regardless " +
                 "of their real-world posture.")]
        public float standingEyeHeight = 1.85f;

        [Tooltip("Eye height, in metres, when seated. An average chair seat is ~0.45m and adult " +
                 "seated eye height above the seat is ~0.75m, so ~1.20m puts the avatar at a " +
                 "believable height for sitting in an ordinary chair.")]
        public float sittingEyeHeight = 1.35f;

        /// <summary>Below this the headset has not reported a real pose yet, so calibration is refused.</summary>
        const float MinValidTrackedHeadHeight = 0.2f;

        /// <summary>How long after a posture change to keep re-asserting the calibrated height.</summary>
        const float HeightCalibrationWindowSeconds = 8f;

        /// <summary>How far the eye height may drift before recalibrating, in metres.</summary>
        const float HeightCalibrationTolerance = 0.05f;

        bool _heightCalibrated;
        Coroutine _heightCalibrationCoroutine;
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
        public TrackedPoseDriver[] trackedPoseDrivers;
        [SerializeField] InputActionManager IAM;
        [SerializeField] XRInputModalityManager XRIMM;

        [Header("Notification")]
        public RectTransform notificationParentDesktop;
        public RectTransform notificationParentVR;


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

        public AvatarHolder avatarHolder;
        public Transform bodyTransform;
        public Transform headTransform;

        // Fusion networked properties
        [Networked] public int AvatarSelectionNumber { get; set; }
        [Networked] public NetworkString<_16> PlayerName { get; set; }
        [Networked] public platform Platform { get; set; }
        [Networked] public WebGpuBrowserKind WebGpuBrowserKind { get; set; }

        /// <summary>
        /// Megaphone state, replicated so late joiners get the current value. Applied in Render().
        /// Set through <see cref="MegaphoneHandler"/>, never written directly by remote clients.
        /// </summary>
        [Networked] public bool MegaphoneOn { get; set; }

        bool _megaphoneApplied;
        bool _lastAppliedMegaphone;
        Coroutine _remoteVoiceSetupCoroutine;

        public bool NetworkedIsVrStyle() => PlatformPresentation.IsVrStyle(Platform, WebGpuBrowserKind);

        public bool NetworkedIsDesktopStyle() => PlatformPresentation.IsDesktopStyle(Platform, WebGpuBrowserKind);

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
            AvatarInputConverter avatarInputConverter = LocalXRRigGameobject.GetComponent<AvatarInputConverter>();
            Debug.Log("-->on selected avatar " + avatarSelectionNumber + "for mine? " + Object.HasInputAuthority);

            GameObject body1 = Instantiate(ProjectManager.instance.uiLayoutConfig.avatarDatas[avatarSelectionNumber].body);
            body1.transform.SetParent(bodyTransform, false);
            GameObject head1 = Instantiate(ProjectManager.instance.uiLayoutConfig.avatarDatas[avatarSelectionNumber].head);
            head1.transform.SetParent(headTransform, false);
            body1.transform.localPosition = head1.transform.localPosition = Vector3.zero;
            avatarHolder.SetAvatar(head1, body1);

            //SetUpAvatarGameobject(avatarHolder.HeadTransform, avatarInputConverter.AvatarHead);
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
            // Set networked Platform (and PlayerName) immediately so other clients and local components (e.g. XRRigController) see the correct platform for THIS player, not the local ProjectManager.
            if (Object.HasInputAuthority)
            {
                PlayerName = ProjectManager.UserName;
                if (ProjectManager.instance != null && ProjectManager.instance.platforms != null)
                {
                    Platforms pl = ProjectManager.instance.platforms;
                    Platform = pl.platformChoice;
                    WebGpuBrowserKind = pl.webGpuBrowserKind;
                }
                else
                {
                    Platform = platform.Desktop;
                    WebGpuBrowserKind = WebGpuBrowserKind.None;
                }
            }
            Debug.Log("-->spawning player");
            StartCoroutine(InitializePlayer());
            Debug.Log("-->spawning player done");
            Debug.Log("transform.position: " + transform.position + "transform.rotation: " + transform.rotation);

        }

        // Self-correcting safety net: InitializePlayer() below makes a one-time decision about
        // whether to disable this player's AudioListener, based on Object.HasInputAuthority at
        // that moment. If Fusion hasn't finished the input-authority handshake yet when that check
        // runs, the LOCAL player's own listener can be disabled by mistake, which mutes everything
        // for that client (ambiance included) rather than just avoiding a duplicate listener for
        // remote players. Render() runs every frame with an authority value that is always
        // up to date, so it corrects that mistake automatically instead of leaving audio dead
        // for the rest of the session.
        public override void Render()
        {
            if (audioListener != null && audioListener.enabled != Object.HasInputAuthority)
            {
                audioListener.enabled = Object.HasInputAuthority;
                Debug.Log($"-->audio listener corrected to {(Object.HasInputAuthority ? "enabled (local player)" : "disabled (remote player)")}");
            }

            // Apply the networked megaphone state whenever it differs from what this client last
            // applied. Two bool comparisons per frame, and it means a client that joined after the
            // megaphone was switched on still picks up the correct value on its very first frame.
            if (!_megaphoneApplied || _lastAppliedMegaphone != MegaphoneOn)
            {
                _lastAppliedMegaphone = MegaphoneOn;
                _megaphoneApplied = true;
                ApplyMegaphoneState();
            }
        }

        private IEnumerator InitializePlayer()
        {
            var xrRig = GetComponent<XRRigController>() ?? GetComponentInParent<XRRigController>() ?? GetComponentInChildren<XRRigController>();
            if (xrRig != null)
            {
                xrRig.orbitCamera.SetTargetOffsetToDefault();
            }
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
                // PlayerName and Platform already set in Spawned() for immediate sync; ensure consistency here
                if (ProjectManager.instance != null && ProjectManager.instance.platforms != null)
                {
                    Platforms pl = ProjectManager.instance.platforms;
                    Platform = pl.platformChoice;
                    WebGpuBrowserKind = pl.webGpuBrowserKind;
                }

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
                MainAvatarGameobject.SetActive(true);
                {
                    InitializeSelectedAvatarModel(avatarSelectionNumber);
                }
                DesktopAddressableSceneUI.Instance.SetupDesktopAddressableSceneUI(this);


                Debug.Log("-->avatar initialized");
                // foreach (GameObject head in AvatarHeadGameobjects)
                // {
                //     SetLayerRecursively(head, 6);
                // }
                // SetLayerRecursively(AvatarBodyGameobject, 7);
                HandAndControllerSync();
            }
            else
            {
                cam.enabled = false;
                //The player is remote
                IAM.actionAssets.Clear();
                XRIMM.enabled = false;
                XRIMM.leftHand = XRIMM.rightHand = null;
                foreach (TrackedPoseDriver poseDriver in trackedPoseDrivers)
                {
                    if (poseDriver != null)
                    {
                        poseDriver.enabled = false;
                    }
                }
                for (int i = 0; i < nonSyncableObjects.Length; i++)
                {
                    if (nonSyncableObjects[i].gameObject != null)
                    {
                        GameObject g = nonSyncableObjects[i].gameObject;
                        //g.SetActive(false);
                        Destroy(g);
                    }
                }

                // foreach (GameObject head in AvatarHeadGameobjects)
                // {
                //     SetLayerRecursively(head, 0);
                // }
                yield return new WaitForSeconds(0.5f); // Small delay to ensure networked properties are synced

                // Setup voice components for remote player to ensure we can hear them
                SetupRemotePlayerVoiceComponents();

                InitializeRemotePlayerAvatar();


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
            SetStandingHeight(true);
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
            leftHand.SetActive(true);
            rightHand.SetActive(true);
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

        /// <summary>True while the local player is seated (desktop: movement is skipped). Cleared when standing.</summary>
        public bool IsSitting { get; private set; }
        public bool IsSittingHeightFixed { get; private set; }

        /// <summary>Current seat (set when sitting). Used so movement input can trigger leave.</summary>
        private SitSpot _currentSitSpot;

        public void SetCurrentSitSpot(SitSpot spot) { _currentSitSpot = spot; }
        public void ClearCurrentSitSpot() { _currentSitSpot = null; }

        /// <summary>Call when local player wants to stand (e.g. pressed move keys while sitting).</summary>
        public void LeaveCurrentSeatIfAny()
        {
            if (_currentSitSpot != null)
            {
                _currentSitSpot.HandleLeave();
                _currentSitSpot = null;
            }
        }

        public void SittingOnObject(Transform sittingPosition)
        {
            Debug.Log("SittingOnObject: " + sittingPosition.position + " " + sittingPosition.rotation + " " + Object.HasInputAuthority);
            if (!Object.HasInputAuthority || sittingPosition == null) return;

            IsSitting = true;
            transform.position = sittingPosition.position;
            transform.rotation = sittingPosition.rotation;

            // Sync desktop camera/orbit to face the seat forward (so we look in sitting direction)
            var xrRig = GetComponent<XRRigController>() ?? GetComponentInParent<XRRigController>() ?? GetComponentInChildren<XRRigController>();
            if (xrRig != null)
            {
                xrRig.SetLookRotation(sittingPosition.rotation);
                xrRig.orbitCamera.targetOffset = new Vector3(0, 0.8f, 0);
            }

            Debug.Log($"[PlayerNetworkSetup] Player moved to sitting position: {sittingPosition.position}");
        }
        public void LeavingSeat()
        {
            IsSitting = false;
        }


        public void SetSittingHeight(bool calledFromSitSpot)
        {
            if (calledFromSitSpot)
            {
                IsSitting = true;
            }
            IsSittingHeightFixed = true;

            if (NetworkedIsVrStyle())
                StartEyeHeightCalibration();
            else
                cameraOffset.transform.localPosition = Vector3.up * sittingHeight;
        }

        public void SetStandingHeight(bool calledFromSitSpot)
        {
            if (calledFromSitSpot)
            {
                IsSitting = false;
            }
            IsSittingHeightFixed = false;

            if (NetworkedIsVrStyle())
                StartEyeHeightCalibration();
            else
                cameraOffset.transform.localPosition = Vector3.up * standingHeight;

            var xrRig = GetComponent<XRRigController>() ?? GetComponentInParent<XRRigController>() ?? GetComponentInChildren<XRRigController>();
            if (xrRig != null)
            {
                Debug.Log("Resetting orbit camera target offset");

                xrRig.orbitCamera.ResetTargetOffset();

            }
        }

        /// <summary>
        /// Puts the VR player's eyes at <see cref="standingEyeHeight"/> regardless of what their body
        /// is doing in the real world — sitting in a chair, lying down, standing on a box.
        ///
        /// The headset reports its real height above the physical floor, so writing a fixed offset
        /// (the old behaviour) gave a different in-world height for every player and every posture:
        /// someone seated arrived crouching. Instead the offset is SOLVED for, so the camera lands on
        /// the target height right now:
        ///
        ///     offset = targetEyeHeight - currentTrackedHeadHeight
        ///
        /// Real head movement afterwards still works normally, because it is relative to this new
        /// baseline — stand up out of the chair and you genuinely rise. Only the starting point is
        /// normalised.
        /// </summary>
        /// <summary>Eye height the calibration aims for, following the current posture.</summary>
        float TargetEyeHeight => IsSittingHeightFixed ? sittingEyeHeight : standingEyeHeight;

        void CalibrateVrEyeHeight()
        {
            if (cameraOffset == null || xROrigin == null)
            {
                Debug.LogWarning("[PlayerNetworkSetup] Cannot calibrate standing height — cameraOffset or xROrigin is not assigned.");
                return;
            }

            // Camera position in XR Origin space already includes the current offset, so subtract it
            // back out to recover the headset's own tracked height.
            float currentOffsetY = cameraOffset.transform.localPosition.y;
            float trackedHeadHeight = xROrigin.CameraInOriginSpacePos.y - currentOffsetY;

            // Before the headset reports a real pose this reads as ~0. Calibrating then would bake in
            // a bogus offset — which is a version of the bug being fixed — so refuse and let the
            // retry loop try again next frame.
            if (trackedHeadHeight < MinValidTrackedHeadHeight)
                return;

            float target = TargetEyeHeight;
            float newOffsetY = target - trackedHeadHeight;
            Vector3 local = cameraOffset.transform.localPosition;
            cameraOffset.transform.localPosition = new Vector3(local.x, newOffsetY, local.z);

            if (!_heightCalibrated)
            {
                Debug.Log($"[PlayerNetworkSetup] Calibrated {(IsSittingHeightFixed ? "sitting" : "standing")} height: " +
                          $"tracked head {trackedHeadHeight:F2}m, target {target:F2}m, offset {newOffsetY:F2}m.");
            }

            _heightCalibrated = true;
        }

        /// <summary>
        /// Re-runs the standing-height calibration. Wire this to a recenter gesture (both thumbsticks
        /// clicked, say) or a menu button so a player can reset themselves after changing posture.
        /// </summary>
        public void RecenterStandingHeight()
        {
            if (!NetworkedIsVrStyle())
                return;

            _heightCalibrated = false;
            StartEyeHeightCalibration();
        }

        void StartEyeHeightCalibration()
        {
            if (_heightCalibrationCoroutine != null)
                StopCoroutine(_heightCalibrationCoroutine);

            _heightCalibrationCoroutine = StartCoroutine(KeepEyeHeightCalibrated());
        }

        /// <summary>
        /// Keeps calibrating until it sticks, then stops.
        ///
        /// A single call at spawn is not reliable for two reasons. The headset may not report a valid
        /// pose for the first few frames. And XROrigin re-asserts its own value for the camera offset
        /// when the tracking origin mode resolves — which happens asynchronously, and again after
        /// SceneLoader stops and restarts the XR subsystems during a scene transition. That
        /// re-assertion landing after the spawn-time call is what wipes the height today. Rather than
        /// guess the ordering, this re-applies over a short window and verifies the result held.
        /// </summary>
        IEnumerator KeepEyeHeightCalibrated()
        {
            float elapsed = 0f;

            while (elapsed < HeightCalibrationWindowSeconds)
            {
                float target = TargetEyeHeight;
                float actual = xROrigin != null ? xROrigin.CameraInOriginSpacePos.y : target;

                if (!_heightCalibrated || Mathf.Abs(actual - target) > HeightCalibrationTolerance)
                    CalibrateVrEyeHeight();

                elapsed += Time.deltaTime;
                yield return null;
            }

            _heightCalibrationCoroutine = null;
        }
        public void ResetPosition()
        {
            Debug.Log("Reset Position");
            transform.localPosition = Vector3.zero;
        }
        /// <summary>
        /// Turns this player's megaphone on or off.
        ///
        /// Writes networked STATE rather than firing an RPC. The previous implementation sent
        /// RPC_MegaPhoneHandle to RpcTargets.All, and a Fusion RPC only reaches clients connected at
        /// that instant — it is never replayed. So anyone who joined after the megaphone was switched
        /// on never learned about it, kept this player at 3D positional audio, and could not hear
        /// them from across the room. Whether it worked came down purely to join order, which is why
        /// it appeared to work for some people and not others. As networked state, Fusion replicates
        /// the current value to late joiners automatically.
        /// </summary>
        public void MegaphoneHandler(bool active)
        {
            if (Object.HasInputAuthority)
            {
                MegaphoneOn = active;
                Debug.Log($"[PlayerNetworkSetup] Megaphone set to {active} for player {PlayerName}");
            }
        }

        /// <summary>Applies the current networked megaphone state to this player's audio.</summary>
        void ApplyMegaphoneState()
        {
            SetSpatialBlend(MegaphoneOn ? 0f : 1f);
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
                playerRecorder.TransmitEnabled = false;
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
        /// Sets up a remote player's voice and keeps checking until their audio is actually arriving.
        ///
        /// The previous version ran once, roughly half a second after spawn. If the Speaker object
        /// existed it configured the AudioSource, logged success and stopped — but a Speaker existing
        /// is not the same as it being LINKED to that player's voice stream. Photon links the stream
        /// asynchronously, and on a client that was still streaming in the world it could easily be
        /// late. The result was one specific remote player being inaudible on one specific client
        /// while everyone else came through fine, with a log line claiming setup had succeeded.
        ///
        /// This version polls until speaker.IsLinked is true, so the log tells you whether voice is
        /// genuinely working rather than whether an object reference was non-null.
        /// </summary>
        private void SetupRemotePlayerVoiceComponents()
        {
            if (_remoteVoiceSetupCoroutine != null)
                StopCoroutine(_remoteVoiceSetupCoroutine);

            _remoteVoiceSetupCoroutine = StartCoroutine(SetupRemotePlayerVoiceRoutine());
        }

        private IEnumerator SetupRemotePlayerVoiceRoutine()
        {
            const float timeout = 30f;   // generous: a client may still be downloading the world
            const float interval = 0.5f;

            float elapsed = 0f;
            bool configured = false;

            Debug.Log($"[PlayerNetworkSetup] Setting up remote player voice components for {PlayerName}");

            while (elapsed < timeout)
            {
                if (playerSpeaker == null)
                    playerSpeaker = GetPlayerSpeaker();

                if (playerSpeaker != null)
                {
                    if (!configured)
                    {
                        AudioSource audioSource = playerSpeaker.GetComponent<AudioSource>();
                        if (audioSource != null)
                        {
                            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
                            audioSource.minDistance = 1f;
                            audioSource.maxDistance = 50f;

                            // Deliberately NOT spatialBlend = 1f. Hardcoding 3D here overwrote the
                            // megaphone every time this ran — including from the old retry path — so
                            // a player who switched their megaphone on went quiet again the moment
                            // their speaker finished linking. Derive it from the networked state.
                            ApplyMegaphoneState();

                            configured = true;
                            Debug.Log($"[PlayerNetworkSetup] Remote player speaker audio source configured for {PlayerName}");
                        }
                        else
                        {
                            Debug.LogWarning($"[PlayerNetworkSetup] AudioSource not found on remote player speaker for {PlayerName}");
                        }
                    }

                    if (playerSpeaker.IsLinked)
                    {
                        Debug.Log($"[PlayerNetworkSetup] Remote voice READY for {PlayerName} after {elapsed:F1}s (speaker linked).");
                        _remoteVoiceSetupCoroutine = null;
                        yield break;
                    }
                }

                elapsed += interval;
                yield return new WaitForSeconds(interval);
            }

            // Timed out. Say exactly which half failed — a missing speaker and an unlinked speaker
            // are different problems, and "random voice issues" is what you get without this.
            string state = playerSpeaker == null ? "speaker MISSING" : "speaker present but NOT LINKED";
            Debug.LogError($"[PlayerNetworkSetup] Remote voice NOT ready for {PlayerName} after {timeout}s — {state}. " +
                           $"This player will be inaudible on this client.");

            _remoteVoiceSetupCoroutine = null;
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
