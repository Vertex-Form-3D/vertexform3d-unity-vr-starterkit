using UnityEngine;

namespace VertextFormCore
{
    public class AvatarHolder : MonoBehaviour
    {

        public Transform MainAvatarTransform;
        public Transform HeadTransform;
        public Transform BodyTransform;
        public Transform HandLeftTransform;
        public Transform HandRightTransform;
        [SerializeField] private bool initLayer;
        private void Start()
        {
            if (initLayer) { 
            SetAvatarLayer();
            }
        }
        public void SetAvatarLayer()
        {
            //Setting the layer of avatar head to AvatarLocalHead layer so that it does not block the view of the local VR Player
            SetLayerRecursively(HeadTransform.gameObject, 7);

            //Setting the layer of avatar body to AvatarLocalBody layer so that it does not block the view of the local VR Player
            SetLayerRecursively(BodyTransform.gameObject, 6);
        }
        void SetLayerRecursively(GameObject go, int layerNumber)
        {
            if (go == null) return;
            foreach (Transform trans in go.GetComponentsInChildren<Transform>(true))
            {
                trans.gameObject.layer = layerNumber;
            }
        }
    }
}