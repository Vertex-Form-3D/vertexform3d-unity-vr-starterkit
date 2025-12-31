using UnityEngine;
using UnityEngine.XR.Hands;
using System.Collections.Generic;

public class RpmHandDriver : XRHandSkeletonDriver
    {
        [SerializeField] private Transform handBone;
        
        private static readonly Dictionary<XRHandJointID, string> JOINT_ID_BONE_PATH_MAP_LEFT = new()
        {
            { XRHandJointID.Palm, "LeftPalm" },
            { XRHandJointID.ThumbMetacarpal, "LeftHandThumb1" },
            { XRHandJointID.ThumbProximal, "LeftHandThumb1/LeftHandThumb2" },
            { XRHandJointID.ThumbDistal, "LeftHandThumb1/LeftHandThumb2/LeftHandThumb3" },
            { XRHandJointID.ThumbTip, "LeftHandThumb1/LeftHandThumb2/LeftHandThumb3/LeftHandThumb4" },
            { XRHandJointID.IndexMetacarpal, "LeftHandIndex0" },
            { XRHandJointID.IndexProximal, "LeftHandIndex0/LeftHandIndex1" },
            { XRHandJointID.IndexIntermediate, "LeftHandIndex0/LeftHandIndex1/LeftHandIndex2" },
            { XRHandJointID.IndexDistal, "LeftHandIndex0/LeftHandIndex1/LeftHandIndex2/LeftHandIndex3" },
            { XRHandJointID.IndexTip, "LeftHandIndex0/LeftHandIndex1/LeftHandIndex2/LeftHandIndex3/LeftHandIndex4" },
            { XRHandJointID.MiddleMetacarpal, "LeftHandMiddle0" },
            { XRHandJointID.MiddleProximal, "LeftHandMiddle0/LeftHandMiddle1" },
            { XRHandJointID.MiddleIntermediate, "LeftHandMiddle0/LeftHandMiddle1/LeftHandMiddle2" },
            { XRHandJointID.MiddleDistal, "LeftHandMiddle0/LeftHandMiddle1/LeftHandMiddle2/LeftHandMiddle3" },
            {
                XRHandJointID.MiddleTip,
                "LeftHandMiddle0/LeftHandMiddle1/LeftHandMiddle2/LeftHandMiddle3/LeftHandMiddle4"
            },
            { XRHandJointID.RingMetacarpal, "LeftHandRing0" },
            { XRHandJointID.RingProximal, "LeftHandRing0/LeftHandRing1" },
            { XRHandJointID.RingIntermediate, "LeftHandRing0/LeftHandRing1/LeftHandRing2" },
            { XRHandJointID.RingDistal, "LeftHandRing0/LeftHandRing1/LeftHandRing2/LeftHandRing3" },
            { XRHandJointID.RingTip, "LeftHandRing0/LeftHandRing1/LeftHandRing2/LeftHandRing3/LeftHandRing4" },
            { XRHandJointID.LittleMetacarpal, "LeftHandPinky0" },
            { XRHandJointID.LittleProximal, "LeftHandPinky0/LeftHandPinky1" },
            { XRHandJointID.LittleIntermediate, "LeftHandPinky0/LeftHandPinky1/LeftHandPinky2" },
            { XRHandJointID.LittleDistal, "LeftHandPinky0/LeftHandPinky1/LeftHandPinky2/LeftHandPinky3" },
            { XRHandJointID.LittleTip, "LeftHandPinky0/LeftHandPinky1/LeftHandPinky2/LeftHandPinky3/LeftHandPinky4" }
        };

        private static readonly Dictionary<XRHandJointID, string> JOINT_ID_BONE_PATH_MAP_RIGHT = new()
        {
            { XRHandJointID.Palm, "RightPalm" },
            { XRHandJointID.ThumbMetacarpal, "RightHandThumb1" },
            { XRHandJointID.ThumbProximal, "RightHandThumb1/RightHandThumb2" },
            { XRHandJointID.ThumbDistal, "RightHandThumb1/RightHandThumb2/RightHandThumb3" },
            { XRHandJointID.ThumbTip, "RightHandThumb1/RightHandThumb2/RightHandThumb3/RightHandThumb4" },
            { XRHandJointID.IndexMetacarpal, "RightHandIndex0" },
            { XRHandJointID.IndexProximal, "RightHandIndex0/RightHandIndex1" },
            { XRHandJointID.IndexIntermediate, "RightHandIndex0/RightHandIndex1/RightHandIndex2" },
            { XRHandJointID.IndexDistal, "RightHandIndex0/RightHandIndex1/RightHandIndex2/RightHandIndex3" },
            {
                XRHandJointID.IndexTip,
                "RightHandIndex0/RightHandIndex1/RightHandIndex2/RightHandIndex3/RightHandIndex4"
            },
            { XRHandJointID.MiddleMetacarpal, "RightHandMiddle0" },
            { XRHandJointID.MiddleProximal, "RightHandMiddle0/RightHandMiddle1" },
            { XRHandJointID.MiddleIntermediate, "RightHandMiddle0/RightHandMiddle1/RightHandMiddle2" },
            { XRHandJointID.MiddleDistal, "RightHandMiddle0/RightHandMiddle1/RightHandMiddle2/RightHandMiddle3" },
            {
                XRHandJointID.MiddleTip,
                "RightHandMiddle0/RightHandMiddle1/RightHandMiddle2/RightHandMiddle3/RightHandMiddle4"
            },
            { XRHandJointID.RingMetacarpal, "RightHandRing0" },
            { XRHandJointID.RingProximal, "RightHandRing0/RightHandRing1" },
            { XRHandJointID.RingIntermediate, "RightHandRing0/RightHandRing1/RightHandRing2" },
            { XRHandJointID.RingDistal, "RightHandRing0/RightHandRing1/RightHandRing2/RightHandRing3" },
            { XRHandJointID.RingTip, "RightHandRing0/RightHandRing1/RightHandRing2/RightHandRing3/RightHandRing4" },
            { XRHandJointID.LittleMetacarpal, "RightHandPinky0" },
            { XRHandJointID.LittleProximal, "RightHandPinky0/RightHandPinky1" },
            { XRHandJointID.LittleIntermediate, "RightHandPinky0/RightHandPinky1/RightHandPinky2" },
            { XRHandJointID.LittleDistal, "RightHandPinky0/RightHandPinky1/RightHandPinky2/RightHandPinky3" },
            {
                XRHandJointID.LittleTip,
                "RightHandPinky0/RightHandPinky1/RightHandPinky2/RightHandPinky3/RightHandPinky4"
            }
        };

        [ContextMenu("Set Bone References")]
        public void SetBoneReferences()
        {
            if (handBone == null)
            {
                Debug.LogError("Hand bone is not assigned.");
                return;
            }

            var handMap = handBone.name.Contains("LeftHand")
                ? JOINT_ID_BONE_PATH_MAP_LEFT
                : JOINT_ID_BONE_PATH_MAP_RIGHT;
            m_JointTransformReferences = new List<JointToTransformReference>();
            
            foreach (var kvp in handMap)
            {
                var joint = handBone.Find(kvp.Value);
                if (joint == null)
                {
                    Debug.LogWarning($"Joint transform not found: {kvp.Value}");
                    continue;
                }

                var jointRef = new JointToTransformReference
                {
                    xrHandJointID = kvp.Key,
                    jointTransform = joint
                };

                m_JointTransformReferences.Add(jointRef);
            }
        }
    }