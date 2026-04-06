using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Fusion;

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
        public float distanceFromCamera = 1.5f;
        public Transform xrCameraTransform;
        public NetworkObject networkObject;

        private NetworkObject spawnedSelfieStick;

        // State tracking booleans
        private bool isStanding = true; // true = Standing, false = Sitting
        private bool isVoiceEnabled = true; // true = Unmuted, false = Muted
        private bool isNearGrab = true; // true = Near Grab, false = Distance Grab
        private bool isFlying = false; // true = Flying, false = Grounded
        private bool isMegaphone = false; // true = Megaphone On, false = Off
        public bool canFlyGlobally = false; // Set by scene/project
        public bool IsFlying() { return isFlying; } // Set by scene/project

        void Start()
        {
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

        public void ManageEmojiPanel()
        {
            if (ProjectManager.instance.platforms.platformChoice == platform.Desktop)
            {
                if (emojiPanelDesktop == null) return;
                emojiPanelDesktop.SetActive(!emojiPanelDesktop.activeInHierarchy);
                if (emojiPanelDesktop.transform.childCount > 0)
                    emojiPanelDesktop.transform.GetChild(0).localScale = Vector3.one * 0.5f;
                if (desktopMenuUI != null) desktopMenuUI.SetActive(false);
            }
            else
            {
                if (!emojiPanelVR.activeInHierarchy)
                {
                    HandleSettingUI();
                }
                MoveCanvasToCamera(emojiPanelVR);
                emojiPanelVR.SetActive(!emojiPanelVR.activeInHierarchy);
            }

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
#if UNITY_EDITOR || UNITY_WEBGL
            if (Input.GetKeyDown(KeyCode.N)) HandleMenuUI();
            if (Input.GetKeyDown(KeyCode.M)) HandleSettingUI();
#endif

#if !UNITY_WEBGL
            if (_inputData._rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool rightBtn))
            {
                if (rightBtn && !rightPrimaryButtonPressed)
                {
                    rightPrimaryButtonPressed = true;
                    HandleMenuUI();
                }
                else if (!rightBtn) rightPrimaryButtonPressed = false;
            }

            if (_inputData._leftController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool leftBtn))
            {
                if (leftBtn && !leftPrimaryButtonPressed)
                {
                    leftPrimaryButtonPressed = true;
                    HandleSettingUI();
                }
                else if (!leftBtn) leftPrimaryButtonPressed = false;
            }
#endif
        }

        // ==================== TOGGLE HANDLERS ====================
        public void OnTapPostureToggle()
        {
            if (isStanding) Sit();
            else Stand();
        }

        public void OnTapVoiceToggle()
        {
            if (isVoiceEnabled) MuteVoice();
            else UnmuteVoice();
        }

        public void OnTapGrabToggle()
        {
            isNearGrab = !isNearGrab;
            ApplyGrabMode();
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
        }

        public void OnTapMegaphoneToggle()
        {
            isMegaphone = !isMegaphone;
            ApplyMegaphoneMode();
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
            if (ProjectManager.instance.platforms.platformChoice == platform.Desktop)
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

        }

        public void HandleSettingUI()
        {
            if (ProjectManager.instance.platforms.platformChoice == platform.Desktop)
            {
                if (settingUI != null)
                {
                    Debug.Log("Setting UI Clicked");
                    var settingsCanvas = settingUI.GetComponentInChildren<Canvas>();
                    if (settingsCanvas != null)
                        settingsCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
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

        }

        void MoveCanvasToCamera(GameObject UIObject)
        {
            if (ProjectManager.instance.platforms.platformChoice == platform.VR)
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