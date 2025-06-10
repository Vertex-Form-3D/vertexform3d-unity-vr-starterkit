using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;

namespace VertexFormCore
{
    public class PlayerUIManager : MonoBehaviour
    {
        [SerializeField] GameObject GoHome_Button;
        [SerializeField] GameObject menuUI;
        [SerializeField] GameObject settingUI;
        [SerializeField] InputData _inputData;
        [SerializeField] PlayerNetworkSetup networkSetup;

        [Header("Settings")]
        [SerializeField] private SettingButton voiceUISetting;
        [SerializeField] private SettingButton standUISetting;
        [SerializeField] private SettingButton sitUISetting;
        [SerializeField] private SettingButton GrabUISetting;
        [SerializeField] private SettingButton flyUISetting;
        [SerializeField] private SettingButton audioUISetting;
        [SerializeField] private NearFarInteractor[] nearFarInteractors;
        [SerializeField] private NearFarInteractor[] UIInteractors;
        [SerializeField] private NotificationHandler notificationHandler;
        public bool isMegaphone;
        static bool nearGrab = true;

        public float distanceFromCamera = 1.5f; // Distance from camera to place the canvas
                                                // Add a reference to the camera within your XR Rig
        public Transform xrCameraTransform;
        void Start()
        {
            GoHome_Button.GetComponent<Button>().onClick.AddListener(VirtualRoomManager.Instance.LeaveRoomAndLoadHomeScene);
            Sit();
        }

        bool rightPrimaryButtonPressed;
        bool leftPrimaryButtonPressed;
        private void Update()
        {
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.N))
            {
                HandleMenuUI();
            }
            if (Input.GetKeyDown(KeyCode.M))
            {
                HandleSettingUI();
            }
#endif
            if (_inputData._rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool rightPrimaryButton))
            {
                if (rightPrimaryButton)
                {
                    if (!rightPrimaryButtonPressed)
                    {
                        rightPrimaryButtonPressed = true;
                        HandleMenuUI();
                    }
                }
                else
                {
                    rightPrimaryButtonPressed = false;
                }
            }
            if (_inputData._leftController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool leftprimaryButton))
            {
                if (leftprimaryButton)
                {
                    if (!leftPrimaryButtonPressed)
                    {
                        leftPrimaryButtonPressed = true;
                        HandleSettingUI();
                    }
                }
                else
                {
                    leftPrimaryButtonPressed = false;
                }
            }
        }

        public void InitializeSetting()
        {
            if (VoiceRecorderManager.Instance.GetRecorderStatus())
            {
                voiceUISetting.Enable();
                voiceUISetting.SetText("Mute");
            }
            else
            {
                voiceUISetting.SetText("Unmute");
                voiceUISetting.Disable();
            }
            if (isMegaphone)
            {
                audioUISetting.Enable();
                audioUISetting.SetText("MegaPhone On");
            }
            else
            {
                audioUISetting.Disable();
                audioUISetting.SetText("MegaPhone Off");
            }

            InitializeFlyMode();
            InitializeGrabMode();
        }
        public bool canFly;
        public void InitializeFlyMode()
        {
            canFly = SceneLoader.Instance.isFlyModeEnabled;
            if (SceneLoader.Instance.isFlyModeEnabled)
            {
                networkSetup.GetComponent<FlyingModeScript>().enabled = true;
                networkSetup.gp.useGravity = false;
                flyUISetting.SetText("Fly Mode On");
                flyUISetting.Enable();
            }
            else
            {
                networkSetup.GetComponent<FlyingModeScript>().enabled = false;
                networkSetup.gp.useGravity = true;
                flyUISetting.SetText("Fly Mode Off");
                flyUISetting.Disable();
            }
        }
        public void OnTapFlyMode()
        {
            if (!SceneLoader.Instance.isFlyModeEnabled)
            {
                notificationHandler.ShowMessage("Fly Mode is disabled in this World", "#FF0000");
                return;
            }
            if (canFly)
            {
                canFly = false;
                networkSetup.GetComponent<FlyingModeScript>().enabled = false;
                networkSetup.gp.useGravity = true;
                flyUISetting.SetText("Fly Mode Off");
                flyUISetting.Disable();
            }
            else
            {
                networkSetup.GetComponent<FlyingModeScript>().enabled = true;
                networkSetup.gp.useGravity = false;
                canFly = true;
                flyUISetting.SetText("Fly Mode On");
                flyUISetting.Enable();
            }
        }
        public void OnTapSit()
        {
            Sit();
        }

        public void OnTapStand()
        {
            Stand();
        }

        public void OnTapVoiceButton()
        {
            if (VoiceRecorderManager.Instance.GetRecorderStatus())
            {
                VoiceRecorderManager.Instance.DisableRecorder();
                voiceUISetting.SetText("Unmute");
                voiceUISetting.Disable();
            }
            else
            {
                voiceUISetting.SetText("Mute");
                voiceUISetting.Enable();
                VoiceRecorderManager.Instance.EnableRecorder();
            }
        }
        public void Sit()
        {
            Debug.Log("Sit height Called");
            networkSetup.CallSetSittingHeightRPC();
        }

        public void ChangeAudioMode()
        {
            if (isMegaphone)
            {
                audioUISetting.Disable();
                audioUISetting.SetText("MegaPhone Off");
                isMegaphone = false;
            }
            else
            {
                audioUISetting.Enable();
                audioUISetting.SetText("MegaPhone On");
                isMegaphone = true;
            }
            networkSetup.MegaphoneHandler(isMegaphone);
        }

        public void SetAudioMode()
        {
            foreach (PlayerNetworkSetup pns in SpawnManager.Instance.allPlayers)
            {
                if (isMegaphone)
                {
                    pns.voiceView.SpeakerInUse.GetComponent<AudioSource>().spatialBlend = 0;
                }
                else
                {
                    pns.voiceView.SpeakerInUse.GetComponent<AudioSource>().spatialBlend = 1;
                }
            }
        }

        public void InitializeGrabMode()
        {
            if (nearGrab)
            {
                foreach (NearFarInteractor interactor in nearFarInteractors)
                {
                    interactor.enableFarCasting = false;
                }
                GrabUISetting.Disable();
                GrabUISetting.SetText("Near Grab");
                HandleUIInteractor(true);
            }
            else
            {
                foreach (NearFarInteractor interactor in nearFarInteractors)
                {
                    interactor.enableFarCasting = true;
                }
                GrabUISetting.Enable();
                GrabUISetting.SetText("Distance Grab");
                HandleUIInteractor(false);
            }
        }

        public void ChangeGrabMode()
        {
            if (nearGrab)
            {
                foreach (NearFarInteractor interactor in nearFarInteractors)
                {
                    interactor.enableFarCasting = true;
                }
                nearGrab = false;
                GrabUISetting.Enable();
                GrabUISetting.SetText("Distance Grab");
                HandleUIInteractor(false);
            }
            else
            {
                foreach (NearFarInteractor interactor in nearFarInteractors)
                {
                    interactor.enableFarCasting = false;
                }
                GrabUISetting.Disable();
                GrabUISetting.SetText("Near Grab");
                HandleUIInteractor(true);
                nearGrab = true;
            }
        }

        void HandleUIInteractor(bool active)
        {
            foreach (NearFarInteractor interactor in UIInteractors)
            {
                interactor.gameObject.SetActive(active);
            }
        }
        public void Stand()
        {
            Debug.Log("Stand height Called");
            networkSetup.CallSetStandingHeightRPC();
        }
        public void HandleMenuUI()
        {
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
                settingUI.SetActive(false);
            }
        }

        public void HandleSettingUI()
        {
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
                menuUI.SetActive(false);
            }
        }

        void MoveCanvasToCamera(GameObject UIObject)
        {
            // Set the canvas position to the camera position plus forward vector times the desired distance
            UIObject.transform.position = xrCameraTransform.position + xrCameraTransform.forward * distanceFromCamera;

            // Make the canvas face the camera horizontally by setting its forward direction to the inverse of the camera's (on the XZ plane)
            Vector3 cameraForwardOnGround = xrCameraTransform.forward;
            cameraForwardOnGround.y = 0; // This ensures the UI canvas will only rotate around the Y axis
            cameraForwardOnGround.Normalize();

            UIObject.transform.forward = -cameraForwardOnGround;
            // Level the canvas to the ground (maintain zero rotation along X and Z axes)
            UIObject.transform.rotation = Quaternion.Euler(
                0,  // zero X rotation
                UIObject.transform.rotation.eulerAngles.y + 180,  // maintain Y rotation
                0   // zero Z rotation
            );
        }
    }
}

[Serializable]
public class SettingButton
{
    //public Image[] images;
    public Image icon;
    public TextMeshProUGUI UIText;
    public Sprite enableSprite;
    public Sprite disableSprite;

    public void SetText(string str)
    {
        UIText.text = str;
    }
    public void Enable()
    {
        icon.sprite = enableSprite;
    }
    public void Disable()
    {
        icon.sprite = disableSprite;
    }
}