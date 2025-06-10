using UnityEngine;
using Photon.Pun;
using UnityEngine.XR.Hands;
using System.Collections.Generic;
using System;

public class XRHandJointSync : MonoBehaviourPun, IPunObservable
{
    [Header("Hand Joint Settings")]
    public XRHandSkeletonDriver skeletonDriver;  // Reference to XRHandSkeletonDriver
    private Transform wristTransform;             // The wrist is the root joint
    private List<Transform> keyFingerJoints=new List<Transform>();          // Key joints like thumb tip, index tip, etc.

    // Network data storage
    private Vector3[] networkJointPositions;
    private Quaternion[] networkJointRotations;

    void Awake()
    {
        PhotonNetwork.SendRate = 20;
        wristTransform = skeletonDriver.rootTransform;
        
        List<JointToTransformReference> jtrs= skeletonDriver.jointTransformReferences;

        foreach (JointToTransformReference jtr in jtrs)
        {
            keyFingerJoints.Add(jtr.jointTransform);
        }
        if (!photonView.IsMine)
        {
            // Initialize network data arrays to match joint count
            networkJointPositions = new Vector3[keyFingerJoints.Count];
            networkJointRotations = new Quaternion[keyFingerJoints.Count];
            skeletonDriver.enabled = false;
        }
    }

    void Update()
    {
        if (!photonView.IsMine)
        {            
            // Smooth movement for remote players
            for (int i = 0; i < keyFingerJoints.Count; i++)
            {
                keyFingerJoints[i].position = Vector3.Lerp(keyFingerJoints[i].position, networkJointPositions[i], Time.deltaTime * 10);
                keyFingerJoints[i].rotation = Quaternion.Lerp(keyFingerJoints[i].rotation, networkJointRotations[i], Time.deltaTime * 10);
            }
        }
    }

    // Photon PUN2 Synchronization Method
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)  // Send data if this is the local player
        {
            for (int i = 0; i < keyFingerJoints.Count; i++)
            {
                stream.SendNext(keyFingerJoints[i].position);
                stream.SendNext(keyFingerJoints[i].rotation);
            }
        }
        else  // Receive data from remote players
        {
            for (int i = 0; i < keyFingerJoints.Count; i++)
            {
                networkJointPositions[i] = (Vector3)stream.ReceiveNext();
                networkJointRotations[i] = (Quaternion)stream.ReceiveNext();
            }
        }
    }
}
