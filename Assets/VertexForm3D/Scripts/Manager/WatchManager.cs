using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class WatchManager : MonoBehaviour
{
    [SerializeField] private GameObject watch;
    [SerializeField] private XRInputModalityManager XRIMM;
    [SerializeField] private Transform leftControllerHand;
    [SerializeField] private Transform leftHand;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text secondsText;
    [SerializeField] private PhotonView PV;
    [SerializeField] private GameObject emojiPanel;
    private void Start()
    {
        if (PV != null)
        {
            if (PV.IsMine)
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
    }

    public void ManageEmojiPanel()
    {
        emojiPanel.SetActive(!emojiPanel.activeInHierarchy);
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
        watch.transform.SetParent(leftControllerHand);
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
        watch.transform.SetParent(leftHand);
        watch.transform.localPosition = Vector3.zero;
        watch.transform.localRotation = Quaternion.identity;
    }
}
