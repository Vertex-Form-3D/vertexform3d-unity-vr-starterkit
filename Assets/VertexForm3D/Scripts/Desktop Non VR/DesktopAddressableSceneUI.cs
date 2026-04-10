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
    public Image modeImage;
    public TMP_Text modeText;
    public Sprite firstPersonSprite;
    public Sprite thirdPersonSprite;
    public TMP_Text[] flyText;
    public Button flyButton;

    private PlayerNetworkSetup pns;

    void Start()
    {
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
        if (ProjectManager.instance.platforms.platformChoice == platform.Desktop)
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
            foreach (TMP_Text txt in flyText)
            {
                txt.color = Color.green;
                flyButton.image.color = Color.green;
            }
            flyText[1].text = "- Fly is on";
        }
        else
        {
            Debug.Log("Fly Mode is disabled");
            foreach (TMP_Text txt in flyText)
            {
                txt.color = Color.red;
            }
            flyButton.image.color = Color.red;
            flyText[1].text = "- Fly is off";
        }
    }

    void AssignModeEvent(PlayerNetworkSetup pns)
    {

        var xrRig = pns.GetComponent<XRRigController>();
        desktopCanvas.gameObject.SetActive(true);
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

    }

    public void ShowGrabItem()
    {
        journeysUI.SetActive(true);
    }
    public void HideGrabItem()
    {
        journeysUI.SetActive(false);
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
