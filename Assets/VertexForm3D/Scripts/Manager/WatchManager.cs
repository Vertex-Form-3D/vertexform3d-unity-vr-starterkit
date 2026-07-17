using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using VertexFormCore;

public class WatchManager : MonoBehaviour
{
    [SerializeField] private GameObject watch;
    [SerializeField] private XRInputModalityManager XRIMM;
    public bool isRPM;
    [SerializeField] private Transform RPMLeftHand;
    [SerializeField] private Transform leftControllerHand;
    [SerializeField] private Transform leftHand;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text secondsText;
    [SerializeField] private NetworkObject networkObject;
    [SerializeField] private GameObject emojiPanel;
    public Button emojiButton;

    private void Start()
    {
        isRPM = PlayerPrefs.GetString(MultiplayerVRConstants.IS_RPM) == "true" ? true : false;
        if (networkObject != null)
        {
            if (networkObject.HasInputAuthority)
            {
                XRIMM.trackedHandModeStarted.AddListener(OnTrackedHandModeStarted);
                XRIMM.trackedHandModeEnded.AddListener(OnTrackedHandModeEnded);
                XRIMM.motionControllerModeStarted.AddListener(OnMotionControllerModeStarted);
                XRIMM.motionControllerModeEnded.AddListener(OnMotionControllerModeEnded);
                InvokeRepeating(nameof(ShowTime), 1, 1);
                if (XRIMM.leftController.activeInHierarchy || XRIMM.rightController.activeInHierarchy)
                {
                    OnMotionControllerModeStarted();
                }
                else
                {
                    OnTrackedHandModeStarted();
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }
        emojiButton?.onClick.AddListener(ManageEmojiPanel);
    }

    public void ManageEmojiPanel()
    {
        emojiPanel.SetActive(!emojiPanel.activeInHierarchy);
        var playerSetup = GetComponentInParent<PlayerNetworkSetup>();
        playerSetup?.playerUIManager?.RefreshInputLockFromOpenPanels();
    }

    public void ShowTime()
    {
        System.DateTime now = System.DateTime.Now;

        // Format hours and minutes
        string hoursAndMinutes = now.ToString("HH:mm");
        timeText.text = hoursAndMinutes;
        // Get the seconds
        string seconds = now.Second.ToString("D2"); // D2 ensures two digits, e.g., 01, 02
        secondsText.text = seconds;
        // Print to the console
    }

    private void OnMotionControllerModeStarted()
    {
        if (isRPM)
        {
            watch.transform.SetParent(RPMLeftHand);
        }
        else
        {
            watch.transform.SetParent(leftControllerHand);
        }
        watch.transform.localPosition = Vector3.zero;
        watch.transform.localRotation = Quaternion.identity;
    }

    private void OnMotionControllerModeEnded()
    {

    }

    private void OnTrackedHandModeEnded()
    {

    }

    private void OnTrackedHandModeStarted()
    {
        if (isRPM)
        {
            watch.transform.SetParent(RPMLeftHand);
        }
        else
        {
            watch.transform.SetParent(leftHand);
        }
        watch.transform.localPosition = Vector3.zero;
        watch.transform.localRotation = Quaternion.identity;
    }
}
