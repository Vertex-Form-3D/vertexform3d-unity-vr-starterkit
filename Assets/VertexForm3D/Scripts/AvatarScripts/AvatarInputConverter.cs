using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VertexFormCore
{
    public class AvatarInputConverter : MonoBehaviour
    {
        //Avatar Transforms
        public PlayerNetworkSetup playerNetworkSetup;
        public Transform MainAvatarTransform;
        public Transform AvatarHead;
        public Transform AvatarBody;

        public Transform AvatarHand_Left;
        public Transform AvatarHand_Right;

        //XRRig Transforms
        public Transform XRHead;

        public Transform XRHandController_Left;
        public Transform XRHandController_Right;
        public Vector3 headPositionOffsetVR;
        public Vector3 headPositionOffsetDesktop;

        public Vector3 rightHandRotationOffset;
        public Vector3 leftHandRotationOffset;
        [SerializeField] private Vector3 baseLeftControllerPos = new Vector3(0.185f, 0.49f, -0.382f);
        [SerializeField] private Vector3 baseLeftControllerRot = new Vector3(0, 180, 38.62f);
        [SerializeField] private Vector3 baseRightControllerPos = new Vector3(-0.185f, 0.49f, -0.382f);
        [SerializeField] private Vector3 baseRightControllerRot = new Vector3(0, 180, -38.62f);
        public NetworkObject networkObject;

        [Header("Camera Offset (adjusts Y when sitting)")]
        [SerializeField] private Transform cameraOffset;
        private float _savedCameraOffsetY;
        private bool _wasSitting;

        void Start()
        {

        }
        void Update()
        {
            if (networkObject != null)
            {
                if (!networkObject.HasInputAuthority)
                {
                    return;
                }
            }

            bool isSitting = playerNetworkSetup != null && playerNetworkSetup.IsSitting;

            if (cameraOffset != null)
            {
                if (isSitting && !_wasSitting)
                    _savedCameraOffsetY = cameraOffset.localPosition.y;
                else if (!isSitting && _wasSitting)
                    cameraOffset.localPosition = new Vector3(cameraOffset.localPosition.x, _savedCameraOffsetY, cameraOffset.localPosition.z);
            }
            _wasSitting = isSitting;

            if (ProjectManager.instance.platformAndSettings.platformChoice == platform.VR)
            {
                Vector3 horizontalOffset = XRHead.TransformDirection(new Vector3(headPositionOffsetVR.x, 0, headPositionOffsetVR.z));
                Vector3 targetBodyPosition = new Vector3(XRHead.position.x + horizontalOffset.x, XRHead.position.y + headPositionOffsetVR.y, XRHead.position.z + horizontalOffset.z);
                if (playerNetworkSetup != null)
                {
                    if (isSitting)
                    {
                        AdjustCameraOffsetForSitting(headPositionOffsetVR.y);
                        MainAvatarTransform.position = Vector3.Lerp(MainAvatarTransform.position, new Vector3(XRHead.position.x, XRHead.position.y + headPositionOffsetVR.y, XRHead.position.z), 0.5f);
                    }
                    else
                    {
                        MainAvatarTransform.position = Vector3.Lerp(MainAvatarTransform.position, targetBodyPosition, 0.5f);
                        AvatarHead.rotation = Quaternion.Lerp(AvatarHead.rotation, XRHead.rotation, 0.5f);
                        AvatarBody.rotation = Quaternion.Lerp(AvatarBody.rotation, Quaternion.Euler(new Vector3(0, AvatarHead.rotation.eulerAngles.y, 0)), 0.05f);
                    }
                }
                else
                {
                    MainAvatarTransform.position = Vector3.Lerp(MainAvatarTransform.position, targetBodyPosition, 0.5f);
                    AvatarHead.rotation = Quaternion.Lerp(AvatarHead.rotation, XRHead.rotation, 0.5f);
                    AvatarBody.rotation = Quaternion.Lerp(AvatarBody.rotation, Quaternion.Euler(new Vector3(0, AvatarHead.rotation.eulerAngles.y, 0)), 0.05f);
                }

                //Hands synch
                if (XRHandController_Right != null)
                {
                    AvatarHand_Right.position = Vector3.Lerp(AvatarHand_Right.position, XRHandController_Right.position, 0.5f);
                    AvatarHand_Right.rotation = Quaternion.Lerp(AvatarHand_Right.rotation, XRHandController_Right.rotation, 0.5f) * Quaternion.Euler(rightHandRotationOffset);
                }

                if (XRHandController_Left != null)
                {
                    AvatarHand_Left.position = Vector3.Lerp(AvatarHand_Left.position, XRHandController_Left.position, 0.5f);
                    AvatarHand_Left.rotation = Quaternion.Lerp(AvatarHand_Left.rotation, XRHandController_Left.rotation, 0.5f) * Quaternion.Euler(leftHandRotationOffset);
                }
            }
            else if (ProjectManager.instance.platformAndSettings.platformChoice == platform.Desktop)
            {
                if (playerNetworkSetup != null)
                {
                    if (isSitting)
                    {
                        AdjustCameraOffsetForSitting(headPositionOffsetDesktop.y);
                        MainAvatarTransform.position = Vector3.Lerp(MainAvatarTransform.position, new Vector3(XRHead.position.x, XRHead.position.y + headPositionOffsetDesktop.y, XRHead.position.z), 0.5f);
                        AvatarHead.rotation = new Quaternion(0, 0, 0, 1);
                        AvatarBody.rotation = new Quaternion(0, 0, 0, 1);
                        if (XRHandController_Right != null)
                        {
                            AvatarHand_Right.localPosition = Vector3.Lerp(AvatarHand_Right.localPosition, baseRightControllerPos, 0.5f);
                            AvatarHand_Right.localRotation = Quaternion.Euler(baseRightControllerRot);
                        }

                        if (XRHandController_Left != null)
                        {
                            AvatarHand_Left.localPosition = Vector3.Lerp(AvatarHand_Left.localPosition, baseLeftControllerPos, 0.5f);
                            AvatarHand_Left.localRotation = Quaternion.Euler(baseLeftControllerRot);
                        }
                    }
                    else
                    {
                        MainAvatarTransform.position = Vector3.Lerp(MainAvatarTransform.position, XRHead.position + headPositionOffsetDesktop, 0.5f);
                        // Head rotates freely to match VR headset
                        AvatarHead.rotation = Quaternion.Lerp(AvatarHead.rotation, XRHead.rotation, 0.5f);
                        AvatarBody.rotation = Quaternion.Lerp(AvatarBody.rotation, Quaternion.Euler(new Vector3(0, AvatarHead.rotation.eulerAngles.y, 0)), 0.05f);
                        if (XRHandController_Right != null)
                        {
                            AvatarHand_Right.position = Vector3.Lerp(AvatarHand_Right.position, XRHandController_Right.position, 0.5f);
                            AvatarHand_Right.rotation = Quaternion.Lerp(AvatarHand_Right.rotation, XRHandController_Right.rotation, 0.5f) * Quaternion.Euler(rightHandRotationOffset);
                        }

                        if (XRHandController_Left != null)
                        {
                            AvatarHand_Left.position = Vector3.Lerp(AvatarHand_Left.position, XRHandController_Left.position, 0.5f);
                            AvatarHand_Left.rotation = Quaternion.Lerp(AvatarHand_Left.rotation, XRHandController_Left.rotation, 0.5f) * Quaternion.Euler(leftHandRotationOffset);
                        }
                    }
                }
                else
                {
                    MainAvatarTransform.position = Vector3.Lerp(MainAvatarTransform.position, XRHead.position + headPositionOffsetDesktop, 0.5f);
                    // Head rotates freely to match VR headset
                    AvatarHead.rotation = Quaternion.Lerp(AvatarHead.rotation, XRHead.rotation, 0.5f);
                    AvatarBody.rotation = Quaternion.Lerp(AvatarBody.rotation, Quaternion.Euler(new Vector3(0, AvatarHead.rotation.eulerAngles.y, 0)), 0.05f);
                    if (XRHandController_Right != null)
                    {
                        AvatarHand_Right.position = Vector3.Lerp(AvatarHand_Right.position, XRHandController_Right.position, 0.5f);
                        AvatarHand_Right.rotation = Quaternion.Lerp(AvatarHand_Right.rotation, XRHandController_Right.rotation, 0.5f) * Quaternion.Euler(rightHandRotationOffset);
                    }

                    if (XRHandController_Left != null)
                    {
                        AvatarHand_Left.position = Vector3.Lerp(AvatarHand_Left.position, XRHandController_Left.position, 0.5f);
                        AvatarHand_Left.rotation = Quaternion.Lerp(AvatarHand_Left.rotation, XRHandController_Left.rotation, 0.5f) * Quaternion.Euler(leftHandRotationOffset);
                    }
                }


                //Hands synch

            }


        }

        private void AdjustCameraOffsetForSitting(float headOffsetY)
        {
            if (cameraOffset == null || playerNetworkSetup == null) return;

            float targetY = playerNetworkSetup.transform.position.y;
            float currentTargetY = XRHead.position.y + headOffsetY;
            float yDelta = targetY - currentTargetY;

            Vector3 lp = cameraOffset.localPosition;
            cameraOffset.localPosition = new Vector3(lp.x, lp.y + yDelta, lp.z);
        }

        public void EnableControllerHands()
        {
            if (XRHandController_Right != null)
            {
                XRHandController_Right.gameObject.SetActive(true);
            }
            if (XRHandController_Left != null)
            {
                XRHandController_Left.gameObject.SetActive(true);
            }
        }

        public void DisableControllerHands()
        {
            if (XRHandController_Right != null)
            {
                XRHandController_Right.gameObject.SetActive(false);
            }
            if (XRHandController_Left != null)
            {
                XRHandController_Left.gameObject.SetActive(false);
            }
        }
    }
}