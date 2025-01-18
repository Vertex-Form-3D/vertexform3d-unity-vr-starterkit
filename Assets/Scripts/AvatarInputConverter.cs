using Photon.Pun;
using UnityEngine;

namespace VertextFormCore
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

        public Transform XRHand_Left;
        public Transform XRHand_Right;

        public Vector3 headPositionOffset;
        public Vector3 handRotationOffset;
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
            if (XRHand_Right != null)
            {
                AvatarHand_Right.position = Vector3.Lerp(AvatarHand_Right.position, XRHand_Right.position, 0.5f);
                AvatarHand_Right.rotation = Quaternion.Lerp(AvatarHand_Right.rotation, XRHand_Right.rotation, 0.5f) * Quaternion.Euler(handRotationOffset);
            }

            if (XRHand_Left != null)
            {
                AvatarHand_Left.position = Vector3.Lerp(AvatarHand_Left.position, XRHand_Left.position, 0.5f);
                AvatarHand_Left.rotation = Quaternion.Lerp(AvatarHand_Left.rotation, XRHand_Left.rotation, 0.5f) * Quaternion.Euler(handRotationOffset);
            }
        }
    }
}