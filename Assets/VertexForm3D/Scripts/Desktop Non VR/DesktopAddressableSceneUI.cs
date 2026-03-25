using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VertexFormCore;
using UnityEngine.SceneManagement;
using System;

public class DesktopAddressableSceneUI : MonoBehaviour
{
    public Canvas desktopCanvas;
    public GameObject journeysUI;
    public Image modeImage;
    public TMP_Text modeText;
    public Sprite firstPersonSprite;
    public Sprite thirdPersonSprite;
    public TMP_Text[] flyText;
    public Button flyButton;

    void Start()
    {
        if (ProjectManager.instance.platforms.platformChoice == platform.Desktop)
        {
            StartCoroutine(IEAssignModeEvent());
            modeImage.GetComponent<Button>().onClick.AddListener(() =>
            {
                var xrRig = RoomManager.Instance?.localVRPlayer?.GetComponent<XRRigController>();
                if (xrRig == null) return;
                if (xrRig.isThirdPerson)
                    xrRig.SwitchToFirstPerson();
                else
                    xrRig.SwitchToThirdPerson();
            });
        }
        else
        {
            Destroy(gameObject);
        }
    }


    public void ManageFlyUI()
    {
        flyButton.interactable = true;
        var pns = RoomManager.Instance?.GetLocalPlayerSetup();
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

    IEnumerator IEAssignModeEvent()
    {
        while (RoomManager.Instance == null || RoomManager.Instance.GetLocalPlayerSetup() == null)
        {
            yield return null;
        }
        yield return new WaitForSeconds(.5f);
        var xrRig = RoomManager.Instance.localVRPlayer.GetComponent<XRRigController>();
        var pns = RoomManager.Instance.GetLocalPlayerSetup();
        desktopCanvas.gameObject.SetActive(true);
        if (xrRig == null || pns == null) yield break;
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
        var pns = RoomManager.Instance?.GetLocalPlayerSetup();
        if (pns != null) pns.playerUIManager.HandleMenuUI();
    }

    public void OnTapSettingButton()
    {
        var pns = RoomManager.Instance?.GetLocalPlayerSetup();
        if (pns != null) pns.playerUIManager.HandleSettingUI();
    }
}
