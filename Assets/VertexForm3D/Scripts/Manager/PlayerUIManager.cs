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

        [Header("VR Panel Placement")]
        [Tooltip("How far below eye level a panel may follow the user's gaze, in degrees. Looking further down than this still opens the panel at this angle, so it cannot be pushed into the floor.")]
        [SerializeField] private float maxPanelDownAngle = 15f;
        [Tooltip("How far above eye level a panel may follow the user's gaze, in degrees.")]
        [SerializeField] private float maxPanelUpAngle = 30f;
        [Tooltip("Minimum gap kept between the bottom edge of a panel and the floor beneath the user.")]
        [SerializeField] private float panelFloorClearance = 0.3f;

        [Tooltip("Hard lower limit for the centre of a world-space panel, measured up from the floor under " +
                 "the user. This is the guarantee that a panel never sinks: it does not depend on what the " +
                 "panel is built from, so new art or a nested model cannot defeat it. Raise it if a panel " +
                 "still reads as too low; the whole panel moves up together.")]
        [SerializeField] private float minPanelCenterHeight = 1.3f;
        [Tooltip("How far below the head to look for the floor.")]
        [SerializeField] private float floorProbeDistance = 5f;
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
        /// (WebGL WebXR may use <see cref="platform.Web"/> with a flat browser kind until a session starts).
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

        /// <summary>
        /// Closes emoji panels and refreshes movement lock. Call after selecting an emoji,
        /// or when a panel is deactivated via prefab SetActive (which skips ManageEmojiPanel).
        /// </summary>
        public void CloseEmojiPanels()
        {
            if (emojiPanelDesktop != null && emojiPanelDesktop.activeSelf)
                emojiPanelDesktop.SetActive(false);
            if (emojiPanelVR != null && emojiPanelVR.activeSelf)
                emojiPanelVR.SetActive(false);
            UpdateInputLockFromOpenPanels();
        }

        /// <summary>
        /// Re-evaluates open blocking panels and updates XR rig input lock.
        /// Safe to call after external SetActive close buttons.
        /// </summary>
        public void RefreshInputLockFromOpenPanels()
        {
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
            PollXrMenuAndSettingsButtons();
        }

        // ==================== TOGGLE HANDLERS ====================
        public void OnTapPostureToggle()
        {
            if (isStanding) Sit();
            else Stand();
            //  CloseSettingsUIIfOpen();
        }

        public void OnTapVoiceToggle()
        {
            if (isVoiceEnabled) MuteVoice();
            else UnmuteVoice();
            onVoiceModeChanged?.Invoke(isVoiceEnabled);
        }

        public void OnTapGrabToggle()
        {
            isNearGrab = !isNearGrab;
            ApplyGrabMode();
            //  CloseSettingsUIIfOpen();
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
            //CloseSettingsUIIfOpen();
        }

        public void OnTapMegaphoneToggle()
        {
            isMegaphone = !isMegaphone;
            ApplyMegaphoneMode();
            //CloseSettingsUIIfOpen();
        }

        // ==================== APPLY FUNCTIONS ====================
        private void Sit()
        {
            if (!TryResolveNetworkSetup("Sit")) return;

            networkSetup.SetSittingHeight(false);
            isStanding = false;
            postureUISetting?.Disable(); // Sets disableSprite + disableText
        }

        private void Stand()
        {
            if (!TryResolveNetworkSetup("Stand")) return;

            // Standing up from a seat is the whole point of this button, so it must NOT bail out
            // when IsSitting is true. The old guard did exactly that, which meant pressing Stand
            // while seated on a SitSpot silently did nothing — it only worked once something else
            // cleared IsSitting, which reads to the player as the button being delayed or ignored.
            networkSetup.LeaveCurrentSeatIfAny();
            networkSetup.SetStandingHeight(false);
            isStanding = true;
            postureUISetting?.Enable(); // Sets enableSprite + enableText
        }

        /// <summary>
        /// Resolves networkSetup on demand instead of silently doing nothing when it is null.
        ///
        /// The posture buttons can be pressed in the first seconds after arriving, before the local
        /// player has finished spawning and this reference has been assigned. The old code returned
        /// without a word, so the press was simply lost and the player had to press again a few
        /// seconds later — which is what "clicking Standing is delayed" actually was.
        /// </summary>
        private bool TryResolveNetworkSetup(string action)
        {
            if (networkSetup != null) return true;

            networkSetup = RoomManager.Instance != null ? RoomManager.Instance.GetLocalPlayerSetup() : null;

            if (networkSetup == null)
            {
                Debug.LogWarning($"[PlayerUIManager] {action} pressed before the local player finished spawning — ignored.");
                return false;
            }

            return true;
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

        private void LateUpdate()
        {
            // Prefab close buttons often call GameObject.SetActive(false) directly and skip
            // ManageEmojiPanel, leaving movement locked until mute/unmute refreshes the lock.
            if (xrRigController == null || !xrRigController.IsUiInputLocked)
                return;
            if (!IsAnyBlockingPanelOpen())
                xrRigController.SetUiInputLocked(false);
        }

        void MoveCanvasToCamera(GameObject UIObject)
        {
            if (xrCameraTransform == null || UIObject == null)
                return;
            if (UseHeadMountedMenuPath())
            {
                Vector3 head = xrCameraTransform.position;

                // Horizontal facing. Looking straight up or down leaves almost nothing of forward once the
                // vertical part is removed, so fall back to the head's up vector, which at that moment
                // points the way the face is turned.
                Vector3 flatForward = xrCameraTransform.forward;
                flatForward.y = 0f;
                if (flatForward.sqrMagnitude < 0.0001f)
                {
                    flatForward = xrCameraTransform.forward.y < 0f ? xrCameraTransform.up : -xrCameraTransform.up;
                    flatForward.y = 0f;
                }
                if (flatForward.sqrMagnitude < 0.0001f)
                    flatForward = Vector3.forward;
                flatForward.Normalize();

                // Still opens where the user is looking, but the vertical part of the gaze is limited.
                // Positioning along the raw camera forward is what put panels in the floor: looking 45
                // degrees down with the Main Map at 3 m placed its centre over 2 m below the eyes.
                float pitch = Mathf.Asin(Mathf.Clamp(xrCameraTransform.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
                pitch = Mathf.Clamp(pitch, -maxPanelDownAngle, maxPanelUpAngle);
                Vector3 direction = flatForward * Mathf.Cos(pitch * Mathf.Deg2Rad) +
                                    Vector3.up * Mathf.Sin(pitch * Mathf.Deg2Rad);

                // Upright and facing the user, as before.
                UIObject.transform.rotation = Quaternion.LookRotation(flatForward, Vector3.up);
                UIObject.transform.position = head + direction * distanceFromCamera;

                KeepPanelAboveFloor(UIObject, head);
            }

        }

        /// <summary>
        /// Hard guarantee on top of the angle limit: lifts the panel if its bottom edge would still sit
        /// below the floor under the user. Covers a small room, a user sitting low, or a distance large
        /// enough that even the limited downward angle reaches the ground.
        /// </summary>
        void KeepPanelAboveFloor(GameObject UIObject, Vector3 head)
        {
            if (!TryFindFloorBelow(head, out float floorY))
            {
                // No collider under the user (a floor mesh without a collider, for instance). The player's
                // own root sits at their feet, so use that rather than skipping the check entirely.
                if (xrCameraTransform == null)
                    return;
                floorY = xrCameraTransform.root.position.y;
            }

            // Only ever lifts, never lowers. Both rules below propose a lift and the larger one wins, so
            // adding a rule can never drag a panel down into something the other rule was protecting it from.
            float lift = 0f;

            // Primary guarantee. Works off the panel's own transform, which is the one thing that is always
            // there, so it holds regardless of what the panel is made of. The measurement below has now been
            // wrong twice in the same direction — first it missed the Main Map's tab bar because it only read
            // the root canvas, then it missed the laptop model because it only read UI graphics. This rule
            // does not care.
            float minimumCentre = floorY + minPanelCenterHeight;
            if (UIObject.transform.position.y < minimumCentre)
                lift = minimumCentre - UIObject.transform.position.y;

            // Secondary. When the panel's extents can be measured, keep the lowest visible point clear of the
            // floor too — that catches a panel taller than minPanelCenterHeight allows for.
            if (TryGetLowestVisibleY(UIObject, out float bottom))
            {
                float liftForBottomEdge = (floorY + panelFloorClearance) - bottom;
                if (liftForBottomEdge > lift)
                    lift = liftForBottomEdge;
            }

            if (lift > 0f)
                UIObject.transform.position += Vector3.up * lift;
        }

        /// <summary>
        /// Lowest point of anything that will actually be drawn in the panel. Measures every graphic rather
        /// than the root canvas, because parts of a panel can sit outside the canvas rectangle — the Main Map's
        /// bottom tab bar does — and those were the parts still ending up under the floor.
        ///
        /// It also measures mesh renderers, because a panel is not only UI. The Main Map is framed by a 3D
        /// laptop model (MainMapVisual Element, 8 mesh renderers), and its body and keyboard hang well below
        /// the canvas that shows the map. Measuring graphics alone lifted the screen clear of the floor and
        /// left the laptop underneath it sunk into the ground.
        ///
        /// Counts anything that will be visible once the panel is switched on, since this runs just before that.
        /// </summary>
        bool TryGetLowestVisibleY(GameObject UIObject, out float lowest)
        {
            lowest = float.MaxValue;
            bool found = false;
            var corners = new Vector3[4];
            Transform root = UIObject.transform;

            foreach (var graphic in UIObject.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
            {
                if (graphic == null || !graphic.enabled || !WillBeActive(graphic.transform, root))
                    continue;

                graphic.rectTransform.GetWorldCorners(corners);
                for (int i = 0; i < 4; i++)
                {
                    if (corners[i].y < lowest)
                    {
                        lowest = corners[i].y;
                        found = true;
                    }
                }
            }

            foreach (var renderer in UIObject.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !renderer.enabled || !WillBeActive(renderer.transform, root))
                    continue;

                // Effects have no meaningful resting extent and would drag the measurement around.
                if (renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer)
                    continue;

                if (TryGetRendererLowestY(renderer, out float rendererLowest) && rendererLowest < lowest)
                {
                    lowest = rendererLowest;
                    found = true;
                }
            }

            return found;
        }

        /// <summary>
        /// Lowest world-space Y of a renderer's geometry.
        ///
        /// Built from the mesh rather than read off <see cref="Renderer.bounds"/> on purpose: that property
        /// is only filled in once the object has been drawn, and this runs immediately before the panel is
        /// switched on for the first time. A panel that has never been visible would otherwise measure as
        /// nothing and be left where it was.
        /// </summary>
        static bool TryGetRendererLowestY(Renderer renderer, out float lowest)
        {
            lowest = float.MaxValue;

            Mesh mesh = null;
            if (renderer is SkinnedMeshRenderer skinned)
                mesh = skinned.sharedMesh;
            else if (renderer.TryGetComponent(out MeshFilter filter))
                mesh = filter.sharedMesh;

            if (mesh == null)
            {
                // Sprite renderers and anything else without a mesh filter. Bounds are the only option here,
                // so skip it when they are still empty rather than reporting a false floor at the origin.
                Bounds worldBounds = renderer.bounds;
                if (worldBounds.size.sqrMagnitude <= 0f)
                    return false;

                lowest = worldBounds.min.y;
                return true;
            }

            Bounds local = mesh.bounds;
            Vector3 centre = local.center;
            Vector3 extents = local.extents;
            Matrix4x4 toWorld = renderer.transform.localToWorldMatrix;

            // All eight corners: the panel is rotated to face the user, so the lowest corner in world space
            // is not necessarily the one that is lowest in the mesh's own space.
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3(
                    centre.x + ((i & 1) == 0 ? -extents.x : extents.x),
                    centre.y + ((i & 2) == 0 ? -extents.y : extents.y),
                    centre.z + ((i & 4) == 0 ? -extents.z : extents.z));

                float y = toWorld.MultiplyPoint3x4(corner).y;
                if (y < lowest)
                    lowest = y;
            }

            return true;
        }

        /// <summary>True if every object from this one up to the panel root is switched on.</summary>
        static bool WillBeActive(Transform t, Transform root)
        {
            for (Transform current = t; current != null; current = current.parent)
            {
                if (!current.gameObject.activeSelf)
                    return false;
                if (current == root)
                    return true;
            }
            return true;
        }

        /// <summary>
        /// Floor height under the head. Ignores triggers and anything belonging to the local player's own
        /// rig — the rig carries colliders of its own (its character controller, hands) that a straight
        /// ray down from the head would otherwise hit first.
        /// </summary>
        bool TryFindFloorBelow(Vector3 head, out float floorY)
        {
            floorY = 0f;
            Transform ownRoot = xrCameraTransform != null ? xrCameraTransform.root : null;

            RaycastHit[] hits = Physics.RaycastAll(head, Vector3.down, floorProbeDistance, ~0, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            bool found = false;
            for (int i = 0; i < hits.Length; i++)
            {
                if (ownRoot != null && hits[i].collider.transform.IsChildOf(ownRoot))
                    continue;
                if (hits[i].distance < nearest)
                {
                    nearest = hits[i].distance;
                    floorY = hits[i].point.y;
                    found = true;
                }
            }
            return found;
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