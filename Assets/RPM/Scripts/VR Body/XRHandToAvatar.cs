using UnityEngine;
using UnityEngine.XR.Hands;
using System.Collections.Generic;
using Fusion;
using VertexFormCore;

public class XRHandToAvatar : NetworkBehaviour
{
    public XRHandSubsystem handSubsystem;
    public Transform[] avatarFingerBones;       // Assign RPM bones
    public Transform[] XRHandsBones;       // Assign RPM bones
    public XRHandJointID[] xrJoints;           // Match order with bones
    public bool isLeftHand = true;
    public NetworkObject networkObject;
    public PlayerNetworkSetup PNS;
    private void Start()
    {
        if (handSubsystem == null)
        {
            var subsystems = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            if (subsystems.Count > 0)
                handSubsystem = subsystems[0];
        }
        if (handSubsystem == null || !handSubsystem.running)
        {
            Debug.LogError("No running XRHandSubsystem found!");
        }
    }

    public void HandSync()
    {
        for (int i = 0; i < XRHandsBones.Length; i++)
        {
            if (XRHandsBones[i] != null && avatarFingerBones[i] != null)
            {
                avatarFingerBones[i].position = XRHandsBones[i].position;
                avatarFingerBones[i].rotation = XRHandsBones[i].rotation;
            }
        }
    }
    void LateUpdate()
    {
        if (networkObject != null)
        {
            if (PNS.isHandTracking)
            {
                HandSync();
            }
        }
        else
        {
            if (handSubsystem != null)
            {
                XRHand hand = isLeftHand ? handSubsystem.leftHand : handSubsystem.rightHand;
                if (hand.isTracked)
                {
                    HandSync();
                }
            }
        }


    }
}