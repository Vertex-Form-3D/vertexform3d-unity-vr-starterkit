using UnityEngine;
using Fusion;

namespace VertexFormCore
{
    public class MultiplayerVRSynchronization : NetworkBehaviour
    {
        //Main VRPlayer Transform Synch
        [Header("Main VRPlayer Transform Synch")]
        public Transform generalVRPlayerTransform;

        //Main Avatar Transform Synch
        [Header("Main Avatar Transform Synch")]
        public Transform mainAvatarTransform;

        //Head  Synch
        [Header("Avatar Head Transform Synch")]
        public Transform headTransform;
        public Transform shadowHeadTransform;

        //Body Synch
        [Header("Avatar Body Transform Synch")]
        public Transform bodyTransform;

        //Hands Synch
        [Header("Hands Transform Synch")]
        public Transform leftHandTransform;
        public Transform rightHandTransform;

        public AvatarInputConverter avatarInputConverter;

        // Fusion networked properties
        [Networked] public Vector3 NetworkPosition_GeneralVRPlayer { get; set; }
        [Networked] public Quaternion NetworkRotation_GeneralVRPlayer { get; set; }

        [Networked] public Vector3 NetworkPosition_MainAvatar { get; set; }
        [Networked] public Quaternion NetworkRotation_MainAvatar { get; set; }

        [Networked] public Quaternion NetworkRotation_Head { get; set; }
        [Networked] public Quaternion NetworkRotation_Body { get; set; }

        [Networked] public Vector3 NetworkPosition_LeftHand { get; set; }
        [Networked] public Quaternion NetworkRotation_LeftHand { get; set; }

        [Networked] public Vector3 NetworkPosition_RightHand { get; set; }
        [Networked] public Quaternion NetworkRotation_RightHand { get; set; }

        // Interpolation variables for smooth movement
        private float lerpRate = 10f;
        private bool firstTake = true;
        public override void Spawned()
        {
            firstTake = true;
            if (!Object.HasInputAuthority)
            {
                avatarInputConverter.enabled = false;
            }
        }
        public override void FixedUpdateNetwork()
        {
            if (Object.HasInputAuthority)
            {
                // Send current transform data to network
                NetworkPosition_GeneralVRPlayer = generalVRPlayerTransform.position;
                NetworkRotation_GeneralVRPlayer = generalVRPlayerTransform.rotation;

                NetworkPosition_MainAvatar = mainAvatarTransform.localPosition;
                NetworkRotation_MainAvatar = mainAvatarTransform.localRotation;

                NetworkRotation_Head = headTransform.localRotation;
                NetworkRotation_Body = bodyTransform.localRotation;

                NetworkPosition_LeftHand = leftHandTransform.localPosition;
                NetworkRotation_LeftHand = leftHandTransform.localRotation;

                NetworkPosition_RightHand = rightHandTransform.localPosition;
                NetworkRotation_RightHand = rightHandTransform.localRotation;
            }
        }

        void Update()
        {
            if (!Object.HasInputAuthority)
            {
                // Smoothly interpolate to networked positions for remote players
                if (firstTake)
                {
                    // Set initial positions immediately to avoid snapping
                    generalVRPlayerTransform.position = NetworkPosition_GeneralVRPlayer;
                    generalVRPlayerTransform.rotation = NetworkRotation_GeneralVRPlayer;

                    mainAvatarTransform.localPosition = NetworkPosition_MainAvatar;
                    mainAvatarTransform.localRotation = NetworkRotation_MainAvatar;

                    headTransform.localRotation = NetworkRotation_Head;
                    bodyTransform.localRotation = NetworkRotation_Body;

                    leftHandTransform.localPosition = NetworkPosition_LeftHand;
                    leftHandTransform.localRotation = NetworkRotation_LeftHand;

                    rightHandTransform.localPosition = NetworkPosition_RightHand;
                    rightHandTransform.localRotation = NetworkRotation_RightHand;

                    firstTake = false;
                }
                else
                {
                    // Smooth interpolation for ongoing updates
                    generalVRPlayerTransform.position = Vector3.Lerp(generalVRPlayerTransform.position, NetworkPosition_GeneralVRPlayer, lerpRate * Time.deltaTime);
                    generalVRPlayerTransform.rotation = Quaternion.Lerp(generalVRPlayerTransform.rotation, NetworkRotation_GeneralVRPlayer, lerpRate * Time.deltaTime);

                    mainAvatarTransform.localPosition = Vector3.Lerp(mainAvatarTransform.localPosition, NetworkPosition_MainAvatar, lerpRate * Time.deltaTime);
                    mainAvatarTransform.localRotation = Quaternion.Lerp(mainAvatarTransform.localRotation, NetworkRotation_MainAvatar, lerpRate * Time.deltaTime);

                    headTransform.localRotation = Quaternion.Lerp(headTransform.localRotation, NetworkRotation_Head, lerpRate * Time.deltaTime);
                    bodyTransform.localRotation = Quaternion.Lerp(bodyTransform.localRotation, NetworkRotation_Body, lerpRate * Time.deltaTime);

                    leftHandTransform.localPosition = Vector3.Lerp(leftHandTransform.localPosition, NetworkPosition_LeftHand, lerpRate * Time.deltaTime);
                    leftHandTransform.localRotation = Quaternion.Lerp(leftHandTransform.localRotation, NetworkRotation_LeftHand, lerpRate * Time.deltaTime);

                    rightHandTransform.localPosition = Vector3.Lerp(rightHandTransform.localPosition, NetworkPosition_RightHand, lerpRate * Time.deltaTime);
                    rightHandTransform.localRotation = Quaternion.Lerp(rightHandTransform.localRotation, NetworkRotation_RightHand, lerpRate * Time.deltaTime);
                }
            }
        }
    }
}