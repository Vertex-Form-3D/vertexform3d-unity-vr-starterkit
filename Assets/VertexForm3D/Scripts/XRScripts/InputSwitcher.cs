using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class InputSwitcher : MonoBehaviour
{
    [SerializeField] XRInputModalityManager XRIMM;
    public List<GameObject> handControllerModels;
    public List<GameObject> hand;
    void Start()
    {
        HandAndControllerSync();
        Init();
    }
    private void HandAndControllerSync()
    {
        XRIMM.trackedHandModeStarted.AddListener(OnTrackedHandModeStarted);
        XRIMM.trackedHandModeEnded.AddListener(OnTrackedHandModeEnded);
        XRIMM.motionControllerModeStarted.AddListener(OnMotionControllerModeStarted);
        XRIMM.motionControllerModeEnded.AddListener(OnMotionControllerModeEnded);
    }

    public void Init()
    {
        if (XRIMM.leftController.activeInHierarchy || XRIMM.rightController.activeInHierarchy)
        {
            OnMotionControllerModeStarted();
        }
        else
        {
            OnTrackedHandModeStarted();
        }
    }


    private void OnMotionControllerModeStarted()
    {
        foreach (GameObject handController in handControllerModels)
        {
            handController.SetActive(true);
        }
        foreach (GameObject hand in hand)
        {
            hand.SetActive(false);
        }
    }
    private void OnMotionControllerModeEnded()
    {

    }

    private void OnTrackedHandModeEnded()
    {

    }

    private void OnTrackedHandModeStarted()
    {
        foreach (GameObject handController in handControllerModels)
        {
            handController.SetActive(false);
        }
        foreach (GameObject hand in hand)
        {
            hand.SetActive(true);
        }
    }

}
