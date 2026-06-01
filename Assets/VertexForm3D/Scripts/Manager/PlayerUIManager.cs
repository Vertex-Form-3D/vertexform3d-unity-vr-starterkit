using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Fusion;
#if UNITY_WEBGL && !UNITY_EDITOR
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace VertexFormCore
{
    public class PlayerUIManager : MonoBehaviour
    {
        [SerializeField] GameObject GoHome_Button_Desktop;
        [SerializeField] GameObject GoHome_Button_VR;
        [SerializeField] GameObject menuUI;
        [SerializeField] GameObject desktopMenuUI;
        [SerializeField] GameObject settingUI;

        [SerializeField] GameObject emojiPanelDesktop;
        [SerializeField] GameObject emojiPanelVR;
        [SerializeField] InputData _inputData;
        [SerializeField] PlayerNetworkSetup networkSetup;
        [SerializeField] NetworkRunner networkRunner;
        [SerializeField] GameObject selfieStickPrefab;
        [SerializeField] Button selfieButton;
        [SerializeField] XRRigController xrRigController;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI dateText;

        [Header("Setting Buttons (Single Toggle Each)")]
        [SerializeField] private SettingButton voiceUISetting; // Toggles Mute/Unmute
        [SerializeField] private SettingButton postureUISetting; // Toggles Sit/Stand
        [SerializeField] private SettingButton grabUISetting; // Toggles Near/Distance Grab
        [SerializeField] private SettingButton flyUISetting; // Toggles Fly On/Off
        [SerializeField] private SettingButton audioUISetting; // Toggles Megaphone On/Off
        [SerializeField] private SettingButton emojiUISetting; // Toggles Megaphone On/Off

        [Header("Voice - Microphone Selection")]
        [SerializeField] private TMP_Dropdown microphoneDropdown;

        [SerializeField] private NearFarInteractor[] nearFarInteractors;
        [SerializeField] private NearFarInteractor[] UIInteractors;
        [SerializeField] private NotificationHandler notificationHandler;

        public event Action onFlyModeChanged;
        public event Action<bool> onVoiceModeChanged;
        public float distanceFromCamera = 1.5f;
        public Transform xrCameraTransform;
        public NetworkObject networkObject;

        private NetworkObject spawnedSelfieStick;

        [Header("WebGL WebXR (browser)")]
        [Tooltip("WebGL WebXR player only. If B/Y map to the wrong controller, toggle to swap left/right fallback order.")]
        [SerializeField] private bool webXrSwapMenuSettingsControllerOrder;

        // State tracking booleans
        private bool isStanding = true; // true = Standing, false = Sitting
        private bool isVoiceEnabled = true; // true = Unmuted, false = Muted
        private bool isNearGrab = true; // true = Near Grab, false = Distance Grab
        private bool isFlying = false; // true = Flying, false = Grounded
        private bool isMegaphone = false; // true = Megaphone On, false = Off
        public bool canFlyGlobally = false; // Set by scene/project
        public bool IsFlying() { return isFlying; } // Set by scene/project
        public bool IsVoiceEnabled() { return isVoiceEnabled; } // Set by scene/project

        /// <summary>Runtime mic toggle (settings UI). True = unmuted / should transmit when voice is wired.</summary>
        public bool IsLocalVoiceUnmuted => isVoiceEnabled;

        void Start()
        {
            if (xrRigController == null)
                xrRigController = GetComponentInParent<XRRigController>();

            // Go home: VirtualRoomManager may not exist in addressable / WebGL flows until a bootstrap scene loads it.
            void LeaveHomeSafe()
            {
                if (VirtualRoomManager.Instance != null)
                    VirtualRoomManager.Instance.LeaveRoomAndLoadHomeScene();
            }
            if (GoHome_Button_Desktop != null)
            {
                var b = GoHome_Button_Desktop.GetComponent<Button>();
                if (b != null) b.onClick.AddListener(LeaveHomeSafe);
            }
            if (GoHome_Button_VR != null)
            {
                var b = GoHome_Button_VR.GetComponent<Button>();
                if (b != null) b.onClick.AddListener(LeaveHomeSafe);
            }
            if (selfieButton != null)
                selfieButton.onClick.AddListener(OnTapSelfieStick);

            WireSettingButton(postureUISetting, OnTapPostureToggle);
            WireSettingButton(voiceUISetting, OnTapVoiceToggle);
            WireSettingButton(grabUISetting, OnTapGrabToggle);
            WireSettingButton(flyUISetting, OnTapFlyToggle);
            WireSettingButton(audioUISetting, OnTapMegaphoneToggle);
            WireSettingButton(emojiUISetting, ManageEmojiPanel);

            SetupMicrophoneDropdown();

            if (networkObject != null && networkObject.HasInputAuthority)
            {
                InitializeAllSettings();
            }

            UpdateInputLockFromOpenPanels();
        }

        private static void WireSettingButton(SettingButton setting, UnityEngine.Events.UnityAction handler)
        {
            if (setting == null || setting.button == null || handler == null) return;
            setting.button.onClick.RemoveAllListeners();
            setting.button.onClick.AddListener(handler);
        }

        /// <summary>
        /// Populates the microphone dropdown with available devices and wires selection to Fusion Voice.
        /// </summary>
        private void SetupMicrophoneDropdown()
        {
            if (microphoneDropdown == null) return;
            if (VoiceRecorderManager.Instance == null) return;

            microphoneDropdown.onValueChanged.RemoveAllListeners();

            string[] devices = VoiceRecorderManager.Instance.GetMicrophoneDevices();
            microphoneDropdown.ClearOptions();
            if (devices != null && devices.Length > 0)
            {
                microphoneDropdown.AddOptions(new System.Collections.Generic.List<string>(devices));
                int currentIndex = VoiceRecorderManager.Instance.GetCurrentMicrophoneDeviceIndex();
                currentIndex = Mathf.Clamp(currentIndex, 0, devices.Length - 1);
                microphoneDropdown.SetValueWithoutNotify(currentIndex);
                microphoneDropdown.RefreshShownValue();
            }
            else
            {
                microphoneDropdown.AddOptions(new System.Collections.Generic.List<string> { "No microphone found" });
                microphoneDropdown.interactable = false;
            }

            microphoneDropdown.onValueChanged.AddListener(OnMicrophoneDropdownChanged);
        }

        private void OnMicrophoneDropdownChanged(int index)
        {
            if (VoiceRecorderManager.Instance == null) return;
            string[] devices = VoiceRecorderManager.Instance.GetMicrophoneDevices();
            if (devices == null || index < 0 || index >= devices.Length) return;
            VoiceRecorderManager.Instance.SetMicrophoneDevice(index);
        }

        /// <summary>
        /// World-space VR menu/settings/emoji path when the asset is VR <b>or</b> immersive XR is running
        /// (WebGL WebXR may use <see cref="platform.WebGPU"/> with a flat browser kind until a session starts).
        /// </summary>
        private bool UseHeadMountedMenuPath()
        {
            if (DesktopMobileControlSettings.IsImmersiveXrOrHeadMountedPresentationActive)
                return true;
            return ProjectManager.instance != null &&
                   ProjectManager.instance.platforms != null &&
                   ProjectManager.instance.platforms.IsVrStylePlatform();
        }

        /// <summary>Prefer <see cref="InputDevices.GetDeviceAtXRNode"/> (fresh each frame); fall back to inspector / <see cref="InputData.Instance"/>.</summary>
        private UnityEngine.XR.InputDevice ResolveHandController(bool rightHand)
        {
            var node = rightHand ? XRNode.RightHand : XRNode.LeftHand;
            var fromNode = InputDevices.GetDeviceAtXRNode(node);
            if (fromNode.isValid)
                return fromNode;

            var data = _inputData != null ? _inputData : InputData.Instance;
            if (data == null)
                return default(UnityEngine.XR.InputDevice);
            return rightHand ? data._rightController : data._leftController;
        }

        /// <summary>
        /// Opens menu/settings from controller: Quest-style <b>B</b> (right) / <b>Y</b> (left) map to <see cref="UnityEngine.XR.CommonUsages.secondaryButton"/>;
        /// some WebXR runtimes only expose <see cref="UnityEngine.XR.CommonUsages.menuButton"/> or Input System names like <c>buttonEast</c>.
        /// </summary>
        private bool ReadSecondaryButtonHeld(bool rightHand)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // de-panther WebXR: A/B/X/Y come through WebXRManager.OnControllerUpdate as buttonA/buttonB (see WebXRControllerData).
            if (TryReadDePantherWebXrFaceButtonBHeld(rightHand))
                return true;
#endif
            UnityEngine.XR.InputDevice dev = ResolveHandController(rightHand);
            if (dev.isValid)
            {
                if (dev.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bool secondary) && secondary)
                    return true;
                if (dev.TryGetFeatureValue(UnityEngine.XR.CommonUsages.menuButton, out bool menu) && menu)
                    return true;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            if (TryWebGlControllerMenuButtonsInputSystem(rightHand, webXrSwapMenuSettingsControllerOrder, out bool fromIS) && fromIS)
                return true;
#endif
            return false;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private static Type _dePantherWebXrFaceType;
        private static MethodInfo _dePantherRightB;
        private static MethodInfo _dePantherLeftB;
        private static bool _dePantherWebXrFaceProbeFailed;

        /// <summary>
        /// Calls <c>VertexForm.WebXRBridge.WebXRFaceButtonInput</c> via reflection so <see cref="PlayerUIManager"/> can stay in the default assembly
        /// while the bridge references the non-auto-referenced <c>WebXR</c> package assembly.
        /// </summary>
        private static bool TryReadDePantherWebXrFaceButtonBHeld(bool rightHand)
        {
            if (_dePantherWebXrFaceProbeFailed)
                return false;

            try
            {
                if (_dePantherWebXrFaceType == null)
                {
                    _dePantherWebXrFaceType = Type.GetType("VertexForm.WebXRBridge.WebXRFaceButtonInput, VertexForm.WebXRBridge");
                    if (_dePantherWebXrFaceType == null)
                    {
                        _dePantherWebXrFaceProbeFailed = true;
                        return false;
                    }

                    _dePantherRightB = _dePantherWebXrFaceType.GetMethod("IsRightButtonBHeld", BindingFlags.Public | BindingFlags.Static);
                    _dePantherLeftB = _dePantherWebXrFaceType.GetMethod("IsLeftButtonBHeld", BindingFlags.Public | BindingFlags.Static);
                    if (_dePantherRightB == null || _dePantherLeftB == null)
                    {
                        _dePantherWebXrFaceProbeFailed = true;
                        return false;
                    }
                }

                var m = rightHand ? _dePantherRightB : _dePantherLeftB;
                return m.Invoke(null, null) is bool pressed && pressed;
            }
            catch
            {
                _dePantherWebXrFaceProbeFailed = true;
                return false;
            }
        }
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
        /// <summary>WebXR device layouts differ; try several <see cref="ButtonControl"/> names on matched hand devices.</summary>
        private static readonly string[] WebGlInputSystemMenuButtonCandidates =
        {
            "secondaryButton",
            "menuButton",
            "buttonEast",
            "buttonWest",
            "buttonNorth",
            "buttonSouth",
        };

        private static readonly List<UnityEngine.InputSystem.InputDevice> WebGlXrControllerScratch = new List<UnityEngine.InputSystem.InputDevice>(8);

        private static bool TryWebGlControllerMenuButtonsInputSystem(bool rightHand, bool swapMenuSettingsControllerOrder, out bool pressed)
        {
            pressed = false;
            foreach (var dev in InputSystem.devices)
            {
                if (dev == null || !dev.added || !dev.enabled)
                    continue;
                if (WebGlInputDeviceExcludedFromHandControllerMenuRouting(dev))
                    continue;
                if (!WebGlInputDeviceMatchesControllerHand(dev, rightHand))
                    continue;
                if (TryWebGlMenuButtonsOnInputSystemDevice(dev, out pressed))
                    return true;
            }

            // WebXR often reports invalid legacy XR <c>UnityEngine.XR.InputDevice</c> at XR nodes while Input System has
            // controllers without LeftHand/RightHand usages or "left"/"right" in the device name.
            // Fall back: pick among XR-like devices ordered by deviceId (commonly left then right).
            BuildWebGlSortedXrControllerScratchList();
            if (WebGlXrControllerScratch.Count == 0)
                return false;

            UnityEngine.InputSystem.InputDevice fallback = null;
            if (WebGlXrControllerScratch.Count >= 2)
            {
                int idxLeft = swapMenuSettingsControllerOrder ? 1 : 0;
                int idxRight = swapMenuSettingsControllerOrder ? 0 : 1;
                fallback = rightHand ? WebGlXrControllerScratch[idxRight] : WebGlXrControllerScratch[idxLeft];
            }
            else if (WebGlXrControllerScratch.Count == 1 && rightHand)
                fallback = WebGlXrControllerScratch[0];

            if (fallback == null)
                return false;

            return TryWebGlMenuButtonsOnInputSystemDevice(fallback, out pressed);
        }

        /// <summary>XR-like controllers for WebGL fallback; sorted by <c>deviceId</c>.</summary>
        private static void BuildWebGlSortedXrControllerScratchList()
        {
            WebGlXrControllerScratch.Clear();
            foreach (var dev in InputSystem.devices)
            {
                if (dev == null || !dev.added || !dev.enabled)
                    continue;
                if (WebGlInputDeviceExcludedFromHandControllerMenuRouting(dev))
                    continue;
                if (!WebGlInputDeviceLooksLikeXrController(dev))
                    continue;
                WebGlXrControllerScratch.Add(dev);
            }

            if (WebGlXrControllerScratch.Count == 0)
            {
                foreach (var dev in InputSystem.devices)
                {
                    if (dev == null || !dev.added || !dev.enabled)
                        continue;
                    if (WebGlInputDeviceExcludedFromHandControllerMenuRouting(dev))
                        continue;
                    if (dev.TryGetChildControl<ButtonControl>("secondaryButton") == null)
                        continue;
                    WebGlXrControllerScratch.Add(dev);
                }
            }

            WebGlXrControllerScratch.Sort(static (a, b) => a.deviceId.CompareTo(b.deviceId));
        }

        private static bool WebGlButtonIsActive(ButtonControl btn)
        {
            return btn != null && (btn.isPressed || btn.wasPressedThisFrame);
        }

        private static bool WebGlAxisPressedLikeButton(AxisControl axis, float threshold = 0.65f)
        {
            return axis != null && axis.ReadValue() >= threshold;
        }

        /// <summary>Some WebXR layouts nest controls; some expose face buttons only as axes or one-frame presses.</summary>
        private static bool TryWebGlMenuButtonsOnInputSystemDevice(UnityEngine.InputSystem.InputDevice dev, out bool pressed)
        {
            pressed = false;
            foreach (var controlName in WebGlInputSystemMenuButtonCandidates)
            {
                var btn = dev.TryGetChildControl<ButtonControl>(controlName);
                if (WebGlButtonIsActive(btn))
                {
                    pressed = true;
                    return true;
                }

                var axisAsBtn = dev.TryGetChildControl<AxisControl>(controlName);
                if (WebGlAxisPressedLikeButton(axisAsBtn))
                {
                    pressed = true;
                    return true;
                }
            }

            foreach (var prefix in WebGlInputSystemMenuButtonPathPrefixes)
            {
                foreach (var controlName in WebGlInputSystemMenuButtonCandidates)
                {
                    string combined = prefix + "/" + controlName;
                    var btn = dev.TryGetChildControl<ButtonControl>(combined);
                    if (WebGlButtonIsActive(btn))
                    {
                        pressed = true;
                        return true;
                    }

                    var axisAsBtn = dev.TryGetChildControl<AxisControl>(combined);
                    if (WebGlAxisPressedLikeButton(axisAsBtn))
                    {
                        pressed = true;
                        return true;
                    }
                }
            }

            foreach (var c in dev.allControls)
            {
                if (c is not ButtonControl bt || !WebGlButtonIsActive(bt))
                    continue;
                if (!WebGlInputControlLooksLikeMenuFaceButton(c))
                    continue;
                pressed = true;
                return true;
            }

            return false;
        }

        /// <summary>Extra path segments before <c>secondaryButton</c> etc. (nested layouts).</summary>
        private static readonly string[] WebGlInputSystemMenuButtonPathPrefixes =
        {
            "xrController",
            "XRController",
            "leftHand",
            "rightHand",
            "LeftHand",
            "RightHand",
        };

        private static bool WebGlInputControlLooksLikeMenuFaceButton(UnityEngine.InputSystem.InputControl c)
        {
            string path = (c.path ?? string.Empty).ToLowerInvariant();
            if (path.Contains("thumbstick") || path.Contains("joystick") || path.Contains("stick"))
                return false;
            if (path.Contains("trigger") && !path.Contains("secondary"))
                return false;

            string name = (c.name ?? string.Empty).ToLowerInvariant();
            if (name.Contains("touch") && name.IndexOf("secondary", StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            switch (name)
            {
                case "secondarybutton":
                case "secondary":
                case "menubutton":
                case "menu":
                case "buttoneast":
                case "buttonwest":
                case "buttonnorth":
                case "buttonsouth":
                case "start":
                case "select":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// WebXR registers a <b>tracked display</b> (HMD) as an Input System device; it matched our old
        /// "layout contains WebXR" rule and broke left/right ordering. Exclude HMD/display/non-hand XR here.
        /// </summary>
        private static bool WebGlInputDeviceExcludedFromHandControllerMenuRouting(UnityEngine.InputSystem.InputDevice dev)
        {
            string layout = (dev.layout ?? string.Empty).ToLowerInvariant();
            string path = (dev.path ?? string.Empty).ToLowerInvariant();

            if (layout.Contains("keyboard") || layout.Contains("mouse") || layout.Contains("pen"))
                return true;

            if (path.Contains("{head}") || path.Contains("centereye") || path.Contains("/hmd") ||
                path.Contains("headmounted"))
                return true;

            bool mentionsController = layout.Contains("controller");

            if (layout.Contains("trackeddisplay") ||
                (layout.Contains("tracked") && layout.Contains("display") && !mentionsController))
                return true;

            if ((layout.Contains("webxr") || layout.Contains("openxr")) &&
                layout.Contains("display") &&
                !mentionsController)
                return true;

            if ((layout.Contains("hmd") || layout.Contains("headset")) && !mentionsController)
                return true;

            return false;
        }

        private static bool WebGlInputDeviceLooksLikeXrController(UnityEngine.InputSystem.InputDevice dev)
        {
            string layout = dev.layout ?? string.Empty;
            if (layout.IndexOf("XR", StringComparison.OrdinalIgnoreCase) >= 0 &&
                layout.IndexOf("Controller", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (layout.IndexOf("WebXR", StringComparison.OrdinalIgnoreCase) >= 0 &&
                layout.IndexOf("Controller", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (layout.IndexOf("OpenXR", StringComparison.OrdinalIgnoreCase) >= 0 &&
                layout.IndexOf("Controller", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return dev.TryGetChildControl<AxisControl>("trigger") != null &&
                   dev.TryGetChildControl<ButtonControl>("secondaryButton") != null;
        }

        private static bool WebGlInputDeviceMatchesControllerHand(UnityEngine.InputSystem.InputDevice dev, bool rightHand)
        {
            foreach (var u in dev.usages)
            {
                if (rightHand && u == UnityEngine.InputSystem.CommonUsages.RightHand)
                    return true;
                if (!rightHand && u == UnityEngine.InputSystem.CommonUsages.LeftHand)
                    return true;
            }

            string path = dev.path ?? string.Empty;
            if (path.IndexOf("{RightHand}", StringComparison.OrdinalIgnoreCase) >= 0)
                return rightHand;
            if (path.IndexOf("{LeftHand}", StringComparison.OrdinalIgnoreCase) >= 0)
                return !rightHand;

            string n = dev.name ?? string.Empty;
            if (rightHand)
                return n.IndexOf("right", StringComparison.OrdinalIgnoreCase) >= 0;
            return n.IndexOf("left", StringComparison.OrdinalIgnoreCase) >= 0;
        }
#endif

        private void PollXrMenuAndSettingsButtons()
        {
            bool rightHeld = ReadSecondaryButtonHeld(true);
            if (rightHeld && !rightPrimaryButtonPressed)
            {
                rightPrimaryButtonPressed = true;
                HandleMenuUI();
            }
            else if (!rightHeld)
                rightPrimaryButtonPressed = false;

            bool leftHeld = ReadSecondaryButtonHeld(false);
            if (leftHeld && !leftPrimaryButtonPressed)
            {
                leftPrimaryButtonPressed = true;
                HandleSettingUI();
            }
            else if (!leftHeld)
                leftPrimaryButtonPressed = false;
        }

        public void ManageEmojiPanel()
        {
            CloseSettingsUIIfOpen();

            if (!UseHeadMountedMenuPath())
            {
                if (emojiPanelDesktop == null) return;
                emojiPanelDesktop.SetActive(!emojiPanelDesktop.activeInHierarchy);
                if (emojiPanelDesktop.transform.childCount > 0)
                    emojiPanelDesktop.transform.GetChild(0).localScale = Vector3.one * 0.5f;
                if (desktopMenuUI != null) desktopMenuUI.SetActive(false);
            }
            else
            {
                MoveCanvasToCamera(emojiPanelVR);
                emojiPanelVR.SetActive(!emojiPanelVR.activeInHierarchy);
            }

            UpdateInputLockFromOpenPanels();
        }
        public void InitializeAllSettings()
        {
            isStanding = ProjectManager.instance.settingsUI.defaultSettings.standDefault == toggle.on;

            // Posture
            if (isStanding)
                Stand();
            else
                Sit();

            // Voice
            if (ProjectManager.instance.settingsUI.defaultSettings.micType == micType.mute)
                MuteVoice();
            else
                UnmuteVoice();

            // Grab Mode
            isNearGrab = ProjectManager.instance.settingsUI.defaultSettings.grabMode == grabMode.near;
            ApplyGrabMode();

            // Megaphone
            isMegaphone = ProjectManager.instance.settingsUI.defaultSettings.megaphone == toggle.on;
            ApplyMegaphoneMode();

            // Fly Mode
            canFlyGlobally = SceneLoader.Instance != null && SceneLoader.Instance.isFlyModeEnabled;
            bool defaultFlyOn = ProjectManager.instance.settingsUI.defaultSettings.flyMode == toggle.on;
            isFlying = canFlyGlobally && defaultFlyOn;
            ApplyFlyMode();

            InvokeRepeating(nameof(UpdateClock), 0f, 1f);
        }

        private void UpdateClock()
        {
            System.DateTime now = System.DateTime.Now;

            if (timeText != null)
                timeText.text = now.ToString("HH:mm:ss");

            if (dateText != null)
            {
                // Custom format: "12 December, 2025"
                string monthName = now.ToString("MMMM"); // Full month name, e.g., "December"
                string formattedDate = $"{now.Day} {monthName}, {now.Year}";
                dateText.text = formattedDate;
            }
        }

        public void OnTapSelfieStick()
        {
            if (networkRunner == null)
                networkRunner = RoomManager.Instance.Runner;

            if (spawnedSelfieStick != null)
            {
                networkRunner.Despawn(spawnedSelfieStick);
                spawnedSelfieStick = null;
            }
            else
            {
                HandleSettingUI();
                Vector3 pos = xrCameraTransform.position + xrCameraTransform.forward * 0.5f;
                pos.y -= 0.4f;
                Debug.Log("Spawning Selfie Stick at: " + pos + "  " + (xrCameraTransform.forward * 0.5f));
                spawnedSelfieStick = networkRunner.Spawn(selfieStickPrefab, pos, Quaternion.identity, networkRunner.LocalPlayer);
            }
        }

        bool rightPrimaryButtonPressed;
        bool leftPrimaryButtonPressed;

        private void Update()
        {
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.N)) HandleMenuUI();
            if (Input.GetKeyDown(KeyCode.M)) HandleSettingUI();
#endif

            PollXrMenuAndSettingsButtons();
        }

        // ==================== TOGGLE HANDLERS ====================
        public void OnTapPostureToggle()
        {
            if (isStanding) Sit();
            else Stand();
            CloseSettingsUIIfOpen();
        }

        public void OnTapVoiceToggle()
        {
            if (isVoiceEnabled) MuteVoice();
            else UnmuteVoice();
            onVoiceModeChanged?.Invoke(isVoiceEnabled);
            CloseSettingsUIIfOpen();
        }

        public void OnTapGrabToggle()
        {
            isNearGrab = !isNearGrab;
            ApplyGrabMode();
            CloseSettingsUIIfOpen();
        }

        public void OnTapFlyToggle()
        {
            if (!canFlyGlobally)
            {
                notificationHandler?.ShowMessage("Fly Mode is disabled in this World", "#FF0000");
                return;
            }
            isFlying = !isFlying;
            ApplyFlyMode();
            CloseSettingsUIIfOpen();
        }

        public void OnTapMegaphoneToggle()
        {
            isMegaphone = !isMegaphone;
            ApplyMegaphoneMode();
            CloseSettingsUIIfOpen();
        }

        // ==================== APPLY FUNCTIONS ====================
        private void Sit()
        {
            if (networkSetup == null) return;
            if (networkSetup.IsSitting)
            {
                return;
            }
            networkSetup.SetSittingHeight(false);
            isStanding = false;
            postureUISetting?.Disable(); // Sets disableSprite + disableText
        }

        private void Stand()
        {
            if (networkSetup == null) return;
            if (networkSetup.IsSitting)
            {
                return;
            }
            networkSetup.SetStandingHeight(false);
            isStanding = true;
            postureUISetting?.Enable(); // Sets enableSprite + enableText
        }

        private void MuteVoice()
        {
            VoiceRecorderManager.Instance?.DisableRecorder();
            isVoiceEnabled = false;
            voiceUISetting?.Disable();
        }

        private void UnmuteVoice()
        {
            VoiceRecorderManager.Instance?.EnableRecorder();
            isVoiceEnabled = true;
            voiceUISetting?.Enable();
        }

        private void ApplyGrabMode()
        {
            if (nearFarInteractors == null) return;
            bool enableFar = !isNearGrab;
            foreach (var interactor in nearFarInteractors)
            {
                if (interactor != null) interactor.enableFarCasting = enableFar;
            }

            if (isNearGrab)
            {
                grabUISetting?.Disable(); // Near Grab is active → show as "disabled" style (convention in your UI)
                HandleUIInteractor(true);
            }
            else
            {
                grabUISetting?.Enable(); // Distance Grab active
                HandleUIInteractor(false);
            }
        }

        private void ApplyFlyMode()
        {
            if (networkSetup == null) return;
            var flying = networkSetup.GetComponent<FlyingModeScript>();
            if (isFlying)
            {
                if (flying != null) flying.enabled = true;
                if (networkSetup.gp != null) networkSetup.gp.useGravity = false;
                flyUISetting?.Enable();
            }
            else
            {
                if (flying != null) flying.enabled = false;
                if (networkSetup.gp != null) networkSetup.gp.useGravity = true;
                flyUISetting?.Disable();
            }
            onFlyModeChanged?.Invoke();
        }

        private void ApplyMegaphoneMode()
        {
            if (networkSetup == null) return;
            networkSetup.MegaphoneHandler(isMegaphone);
            if (isMegaphone)
                audioUISetting?.Enable();
            else
                audioUISetting?.Disable();
        }

        void HandleUIInteractor(bool active)
        {
            if (UIInteractors == null) return;
            foreach (var interactor in UIInteractors)
            {
                if (interactor != null) interactor.gameObject.SetActive(active);
            }
        }

        // ==================== UI Positioning ====================
        public void HandleMenuUI()
        {
            if (!UseHeadMountedMenuPath())
            {

                Debug.Log("Menu Clicked");
                if (desktopMenuUI != null)
                    desktopMenuUI.SetActive(!desktopMenuUI.activeInHierarchy);
                if (menuUI != null) menuUI.SetActive(false);
                if (settingUI != null) settingUI.SetActive(false);
            }
            else
            {

                distanceFromCamera = 3;
                if (menuUI == null)
                {
                    return;
                }
                if (menuUI.activeInHierarchy)
                {
                    menuUI.SetActive(false);
                }
                else
                {
                    MoveCanvasToCamera(menuUI);
                    menuUI.SetActive(true);
                    if (settingUI != null) settingUI.SetActive(false);
                }
            }

            UpdateInputLockFromOpenPanels();
        }

        public void HandleSettingUI()
        {
            if (!UseHeadMountedMenuPath())
            {
                if (settingUI != null)
                {
                    Debug.Log("Setting UI Clicked");
                    var settingsCanvas = settingUI.GetComponentInChildren<Canvas>();
                    if (settingsCanvas != null)
                        settingsCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    var settingsCanvasScaler = settingUI.GetComponentInChildren<CanvasScaler>();
                    if (settingsCanvasScaler != null)
                        settingsCanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    settingsCanvasScaler.referenceResolution = new Vector2(1920, 1080);
                    settingsCanvasScaler.matchWidthOrHeight = 1f;
                    settingUI.SetActive(!settingUI.activeInHierarchy);
                }
                if (desktopMenuUI != null) desktopMenuUI.SetActive(false);
                if (settingUI != null && settingUI.activeInHierarchy)
                    SetupMicrophoneDropdown();
            }
            else
            {

                if (settingUI != null)
                {
                    var settingsCanvas = settingUI.GetComponentInChildren<Canvas>();
                    if (settingsCanvas != null)
                        settingsCanvas.renderMode = RenderMode.WorldSpace;
                }
                distanceFromCamera = 2;
                if (settingUI == null)
                {
                    return;
                }
                if (settingUI.activeInHierarchy)
                {
                    settingUI.SetActive(false);
                }
                else
                {
                    MoveCanvasToCamera(settingUI);
                    settingUI.SetActive(true);
                    if (menuUI != null) menuUI.SetActive(false);
                    SetupMicrophoneDropdown(); // Refresh device list when opening settings
                }
            }

            UpdateInputLockFromOpenPanels();
        }

        private bool IsAnyBlockingPanelOpen()
        {
            return (desktopMenuUI != null && desktopMenuUI.activeInHierarchy) ||
                   (menuUI != null && menuUI.activeInHierarchy) ||
                   (settingUI != null && settingUI.activeInHierarchy) ||
                   (emojiPanelDesktop != null && emojiPanelDesktop.activeInHierarchy) ||
                   (emojiPanelVR != null && emojiPanelVR.activeInHierarchy);
        }

        private void CloseSettingsUIIfOpen()
        {
            if (settingUI != null && settingUI.activeInHierarchy)
                settingUI.SetActive(false);
            UpdateInputLockFromOpenPanels();
        }

        private void UpdateInputLockFromOpenPanels()
        {
            if (xrRigController == null)
                return;

            xrRigController.SetUiInputLocked(IsAnyBlockingPanelOpen());
        }

        void MoveCanvasToCamera(GameObject UIObject)
        {
            if (xrCameraTransform == null || UIObject == null)
                return;
            if (UseHeadMountedMenuPath())
            {
                UIObject.transform.position = xrCameraTransform.position + xrCameraTransform.forward * distanceFromCamera;
                Vector3 flatForward = xrCameraTransform.forward;
                flatForward.y = 0;
                flatForward.Normalize();
                UIObject.transform.forward = -flatForward;
                UIObject.transform.rotation = Quaternion.Euler(0, UIObject.transform.eulerAngles.y + 180, 0);
            }

        }
    }

    [Serializable]
    public class SettingButton
    {
        public Button button;
        public Image icon;
        public TextMeshProUGUI UIText;

        public Sprite enableSprite;
        public Sprite disableSprite;

        public string enableText = "On";        // Text when in "enabled/active" state
        public string disableText = "Off";      // Text when in "disabled/inactive" state

        public void SetText(string str)
        {
            if (UIText) UIText.text = str;
        }

        public void Enable()
        {
            if (icon && enableSprite) icon.sprite = enableSprite;
            if (UIText) UIText.text = enableText;
        }

        public void Disable()
        {
            if (icon && disableSprite) icon.sprite = disableSprite;
            if (UIText) UIText.text = disableText;
        }
    }
}