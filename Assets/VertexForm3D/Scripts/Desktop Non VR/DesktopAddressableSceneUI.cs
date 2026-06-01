using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VertexFormCore;
using UnityEngine.SceneManagement;
using System;

public class DesktopAddressableSceneUI : MonoBehaviour
{
    public static DesktopAddressableSceneUI Instance;
    public Canvas desktopCanvas;
    public GameObject journeysUI;
    public GameObject grabUI;
    public Image modeImage;
    public TMP_Text modeText;
    public Sprite firstPersonSprite;
    public Sprite thirdPersonSprite;
    public Button flyButton;

    public Button muteButton;
    public Sprite muteOnSprite;
    public Sprite muteOffSprite;

    public Sprite flyOnSprite;
    public Sprite flyOffSprite;

    private PlayerNetworkSetup pns;

    void Start()
    {
        desktopCanvas.gameObject.SetActive(false);
        if (Instance == null)
        {
            Instance = this;

        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void SetupDesktopAddressableSceneUI(PlayerNetworkSetup pns)
    {
        if (ProjectManager.instance.platforms.IsDesktopStylePlatform())
        {
            this.pns = pns;
            AssignModeEvent(pns);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ManageFlyUI()
    {
        if (flyButton == null) return;
        flyButton.interactable = true;
        if (pns != null && pns.playerUIManager.IsFlying())
        {
            Debug.Log("Fly Mode is enabled");
            flyButton.image.sprite = flyOnSprite;
        }
        else
        {
            Debug.Log("Fly Mode is disabled");
            flyButton.image.sprite = flyOffSprite;
        }
    }
    public void ManageMuteUI(bool isVoiceEnabled)
    {
        if (muteButton == null) return;

        if (isVoiceEnabled)
        {
            Debug.Log("Voice is enabled");
            muteButton.image.sprite = muteOnSprite;

        }
        else
        {
            Debug.Log("Voice is disabled");
            muteButton.image.sprite = muteOffSprite;
        }

    }

    void AssignModeEvent(PlayerNetworkSetup pns)
    {

        var xrRig = pns.GetComponent<XRRigController>();
        PersonMode mode = (PersonMode)PlayerPrefs.GetInt("VertexForm3D_PersonMode", 0);
        if (mode == PersonMode.Third)
        {
            xrRig.SwitchToThirdPerson();
        }
        else
        {
            xrRig.SwitchToFirstPerson();
        }
        var modeBtn = modeImage != null ? modeImage.GetComponent<Button>() : null;
        if (modeBtn != null)
        {
            modeBtn.onClick.AddListener(() =>
            {
                if (xrRig.isThirdPerson)
                    xrRig.SwitchToFirstPerson();
                else
                    xrRig.SwitchToThirdPerson();
            });
        }
        if (xrRig.isThirdPerson)
        {
            modeImage.sprite = thirdPersonSprite;
            modeText.text = "Third Person mode";
        }
        else
        {
            modeImage.sprite = firstPersonSprite;
            modeText.text = "First Person mode";
        }
        if (muteButton != null)
        {
            muteButton.onClick.AddListener(() =>
            {
                pns.playerUIManager.OnTapVoiceToggle(); Invoke(nameof(ManageMuteUI), .1f);
            });
        }
        pns.playerUIManager.onVoiceModeChanged += ManageMuteUI;
        ManageMuteUI(pns.playerUIManager.IsVoiceEnabled());
        if (flyButton != null)
            flyButton.onClick.AddListener(() => { pns.playerUIManager.OnTapFlyToggle(); Invoke(nameof(ManageFlyUI), .1f); });
        pns.playerUIManager.onFlyModeChanged += ManageFlyUI;
        ManageFlyUI();

        xrRig.onFPSModeStartEvent.AddListener(() =>
            {
                modeImage.sprite = firstPersonSprite;
                modeText.text = "First Person mode";
            });
        xrRig.onThirdPersonModeStartEvent.AddListener(() =>
        {
            modeImage.sprite = thirdPersonSprite;
            modeText.text = "Third Person mode";
        });

        // Body visibility is handled by AvatarHolder (respects showAvatarBodyInFirstPerson and keeps shadows).
        pns.avatarHolder?.ApplyBodyVisibilityForPersonMode(xrRig.isThirdPerson);

        desktopCanvas.gameObject.SetActive(true);

    }

    public void ShowGrabItem()
    {
        grabUI.SetActive(true);
    }
    public void HideGrabItem()
    {
        grabUI.SetActive(false);
    }
    public void OnTapMenuButton()
    {
        if (pns?.playerUIManager != null)
            pns.playerUIManager.HandleMenuUI();
    }

    public void OnTapSettingButton()
    {
        if (pns?.playerUIManager != null)
            pns.playerUIManager.HandleSettingUI();
    }
}
