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
        [SerializeField] GameObject GoHome_Button;
        [SerializeField] GameObject menuUI;
        [SerializeField] GameObject settingUI;
        [SerializeField] GameObject emojiPanel;
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

        [SerializeField] private NearFarInteractor[] nearFarInteractors;
        [SerializeField] private NearFarInteractor[] UIInteractors;
        [SerializeField] private NotificationHandler notificationHandler;

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
        private bool canFlyGlobally = false; // Set by scene/project

        void Start()
        {
            // Assign button events programmatically
            GoHome_Button.GetComponent<Button>().onClick.AddListener(VirtualRoomManager.Instance.LeaveRoomAndLoadHomeScene);
            selfieButton.onClick.AddListener(OnTapSelfieStick);

            postureUISetting.button.onClick.RemoveAllListeners();
            postureUISetting.button.onClick.AddListener(OnTapPostureToggle);

            voiceUISetting.button.onClick.RemoveAllListeners();
            voiceUISetting.button.onClick.AddListener(OnTapVoiceToggle);

            grabUISetting.button.onClick.RemoveAllListeners();
            grabUISetting.button.onClick.AddListener(OnTapGrabToggle);

            flyUISetting.button.onClick.RemoveAllListeners();
            flyUISetting.button.onClick.AddListener(OnTapFlyToggle);

            audioUISetting.button.onClick.RemoveAllListeners();
            audioUISetting.button.onClick.AddListener(OnTapMegaphoneToggle);

            emojiUISetting.button.onClick.RemoveAllListeners();
            emojiUISetting.button.onClick.AddListener(ManageEmojiPanel);

            if (networkObject.HasInputAuthority)
            {
                InitializeAllSettings();
            }
        }

        public void ManageEmojiPanel()
        {
            if (!emojiPanel.activeInHierarchy)
            {
                HandleSettingUI();
            }
            MoveCanvasToCamera(emojiPanel);
            emojiPanel.SetActive(!emojiPanel.activeInHierarchy);
        }
        public void InitializeAllSettings()
        {
            isStanding = ProjectManager.instance.projectDataSO.projectData.defaultSetting.standDefault == toggle.on;

            // Posture
            if (isStanding)
                Stand();
            else
                Sit();

            // Voice
            if (ProjectManager.instance.projectDataSO.projectData.defaultSetting.micType == micType.mute)
                MuteVoice();
            else
                UnmuteVoice();

            // Grab Mode
            isNearGrab = ProjectManager.instance.projectDataSO.projectData.defaultSetting.grabMode == grabMode.near;
            ApplyGrabMode();

            // Megaphone
            isMegaphone = ProjectManager.instance.projectDataSO.projectData.defaultSetting.megaphone == toggle.on;
            ApplyMegaphoneMode();

            // Fly Mode
            canFlyGlobally = SceneLoader.Instance.isFlyModeEnabled;
            bool defaultFlyOn = ProjectManager.instance.projectDataSO.projectData.defaultSetting.flyMode == toggle.on;
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
                Debug.Log("Spawning Selfie Stick at: " + pos+"  "+ (xrCameraTransform.forward * 0.5f) );
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
                notificationHandler.ShowMessage("Fly Mode is disabled in this World", "#FF0000");
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
            networkSetup.CallSetSittingHeightRPC();
            isStanding = false;
            postureUISetting.Disable(); // Sets disableSprite + disableText
        }

        private void Stand()
        {
            networkSetup.CallSetStandingHeightRPC();
            isStanding = true;
            postureUISetting.Enable(); // Sets enableSprite + enableText
        }

        private void MuteVoice()
        {
            VoiceRecorderManager.Instance.DisableRecorder();
            isVoiceEnabled = false;
            voiceUISetting.Disable();
        }

        private void UnmuteVoice()
        {
            VoiceRecorderManager.Instance.EnableRecorder();
            isVoiceEnabled = true;
            voiceUISetting.Enable();
        }

        private void ApplyGrabMode()
        {
            bool enableFar = !isNearGrab;
            foreach (var interactor in nearFarInteractors)
                interactor.enableFarCasting = enableFar;

            if (isNearGrab)
            {
                grabUISetting.Disable(); // Near Grab is active → show as "disabled" style (convention in your UI)
                HandleUIInteractor(true);
            }
            else
            {
                grabUISetting.Enable(); // Distance Grab active
                HandleUIInteractor(false);
            }
        }

        private void ApplyFlyMode()
        {
            if (isFlying)
            {
                networkSetup.GetComponent<FlyingModeScript>().enabled = true;
                networkSetup.gp.useGravity = false;
                flyUISetting.Enable();
            }
            else
            {
                networkSetup.GetComponent<FlyingModeScript>().enabled = false;
                networkSetup.gp.useGravity = true;
                flyUISetting.Disable();
            }
        }

        private void ApplyMegaphoneMode()
        {
            networkSetup.MegaphoneHandler(isMegaphone);
            if (isMegaphone)
                audioUISetting.Enable();
            else
                audioUISetting.Disable();
        }

        void HandleUIInteractor(bool active)
        {
            foreach (var interactor in UIInteractors)
                interactor.gameObject.SetActive(active);
        }

        // ==================== UI Positioning ====================
        public void HandleMenuUI()
        {
            if (menuUI == null) return;
            menuUI.SetActive(!menuUI.activeInHierarchy);
            if (menuUI.activeInHierarchy)
            {
                MoveCanvasToCamera(menuUI);
                settingUI.SetActive(false);
            }
        }

        public void HandleSettingUI()
        {
            if (settingUI == null) return;
            settingUI.SetActive(!settingUI.activeInHierarchy);
            if (settingUI.activeInHierarchy)
            {
                MoveCanvasToCamera(settingUI);
                menuUI.SetActive(false);
            }
        }

        void MoveCanvasToCamera(GameObject UIObject)
        {
            UIObject.transform.position = xrCameraTransform.position + xrCameraTransform.forward * distanceFromCamera;
            Vector3 flatForward = xrCameraTransform.forward;
            flatForward.y = 0;
            flatForward.Normalize();
            UIObject.transform.forward = -flatForward;
            UIObject.transform.rotation = Quaternion.Euler(0, UIObject.transform.eulerAngles.y + 180, 0);
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