using Oculus.Interaction;
using Photon.Voice.PUN;
using System;
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Management;

namespace VertextFormCore
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
        [SerializeField] private SettingButton audioUISetting;
        [SerializeField] private NearFarInteractor[] nearFarInteractors;
        [SerializeField] private NearFarInteractor[] UIInteractors;
        public bool isMegaphone;


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
            foreach (NearFarInteractor interactor in nearFarInteractors)
            {
                if (interactor.enableFarCasting)
                {
                    GrabUISetting.Enable();
                    GrabUISetting.SetText("Distance Grab");
                }
                else
                {
                    GrabUISetting.Disable();
                    GrabUISetting.SetText("Near Grab");
                }
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

        public void ChangeGrabMode()
        {
            foreach (NearFarInteractor interactor in nearFarInteractors)
            {
                if (interactor.enableFarCasting)
                {
                    interactor.enableFarCasting = false;
                    GrabUISetting.Disable();
                    GrabUISetting.SetText("Near Grab");
                    HandleUIInteractor(true);
                }
                else
                {
                    GrabUISetting.Enable();
                    GrabUISetting.SetText("Distance Grab");
                    interactor.enableFarCasting = true;
                    HandleUIInteractor(false);
                }
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
                UIObject.transform.rotation.eulerAngles.y,  // maintain Y rotation
                0   // zero Z rotation
            );
        }
    }
}

[Serializable]
public class SettingButton
{
    //public Image[] images;
    public SpriteRenderer[] spriteRenderers;
    public TextMeshProUGUI UIText;
    public Sprite[] enableSprite;
    public Sprite[] disableSprite;

    public void SetText(string str)
    {
        UIText.text = str;
    }
    public void Enable()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            spriteRenderers[i].sprite = enableSprite[i];
        }
    }
    public void Disable()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            spriteRenderers[i].sprite = disableSprite[i];
        }
    }
}