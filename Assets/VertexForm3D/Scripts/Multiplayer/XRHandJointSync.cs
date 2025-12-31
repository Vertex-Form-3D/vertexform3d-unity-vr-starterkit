using UnityEngine;
using Fusion;
using UnityEngine.XR.Hands;
using System.Collections.Generic;
using System;

public class XRHandJointSync : NetworkBehaviour
{
    [Header("Hand Joint Settings")]
    public XRHandSkeletonDriver skeletonDriver;  // Reference to XRHandSkeletonDriver
    private Transform wristTransform;             // The wrist is the root joint
    private List<Transform> keyFingerJoints = new List<Transform>();          // Key joints like thumb tip, index tip, etc.

    // Fusion networked properties for hand joint data
    [Networked, Capacity(25)] public NetworkArray<Vector3> NetworkJointPositions { get; }
    [Networked, Capacity(25)] public NetworkArray<Quaternion> NetworkJointRotations { get; }

    void Awake()
    {
        if (skeletonDriver == null)
        {
            Debug.LogWarning("XRHandSkeletonDriver is not assigned in XRHandJointSync");
            return;
        }

        wristTransform = skeletonDriver.rootTransform;

        List<JointToTransformReference> jtrs = skeletonDriver.jointTransformReferences;

        foreach (JointToTransformReference jtr in jtrs)
        {
            keyFingerJoints.Add(jtr.jointTransform);
        }
    }

    public override void Spawned()
    {
        // Now we can safely check input authority after the object is spawned
        if (!Object.HasInputAuthority)
        {
            if (skeletonDriver != null)
            {
                skeletonDriver.enabled = false;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasInputAuthority)
        {
            // Send joint data to network
            for (int i = 0; i < keyFingerJoints.Count && i < NetworkJointPositions.Length; i++)
            {
                if (keyFingerJoints[i] != null)
                {
                    NetworkJointPositions.Set(i, keyFingerJoints[i].position);
                    NetworkJointRotations.Set(i, keyFingerJoints[i].rotation);
                }
            }
        }
    }

    void Update()
    {
        if (!Object.HasInputAuthority)
        {
            // Smooth movement for remote players
            for (int i = 0; i < keyFingerJoints.Count && i < NetworkJointPositions.Length; i++)
            {
                if (keyFingerJoints[i] != null)
                {
                    keyFingerJoints[i].position = Vector3.Lerp(keyFingerJoints[i].position, NetworkJointPositions[i], Time.deltaTime * 10);
                    keyFingerJoints[i].rotation = Quaternion.Lerp(keyFingerJoints[i].rotation, NetworkJointRotations[i], Time.deltaTime * 10);
                }
            }
        }
    }
}
