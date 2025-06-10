using Photon.Pun;
using UnityEngine;

namespace VertexFormCore
{
    public class AvatarInputConverter : MonoBehaviour
    {
        //Avatar Transforms
        public Transform MainAvatarTransform;
        public Transform AvatarHead;
        public Transform AvatarBody;

        public Transform AvatarHand_Left;
        public Transform AvatarHand_Right;

        //XRRig Transforms
        public Transform XRHead;

        public Transform XRHandController_Left;
        public Transform XRHandController_Right;
        public Vector3 headPositionOffset;
        public Vector3 rightHandRotationOffset;
        public Vector3 leftHandRotationOffset;
        public PhotonView PhotonView;

        // Update is called once per frame
        void Update()
        {
            if (PhotonView != null)
            {
                if (!PhotonView.IsMine)
                {
                    return;
                }
            }
            //Head and Body synch
            MainAvatarTransform.position = Vector3.Lerp(MainAvatarTransform.position, XRHead.position + headPositionOffset, 0.5f);
            AvatarHead.rotation = Quaternion.Lerp(AvatarHead.rotation, XRHead.rotation, 0.5f);
            AvatarBody.rotation = Quaternion.Lerp(AvatarBody.rotation, Quaternion.Euler(new Vector3(0, AvatarHead.rotation.eulerAngles.y, 0)), 0.05f);

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