using UnityEngine;

namespace VertexFormCore
{
    public class AvatarHolder : MonoBehaviour
    {

        public Transform MainAvatarTransform;
        public Transform HeadTransform;
        public Transform ShadowHeadTransform;
        public Transform BodyTransform;
        public Transform HandLeftTransform;
        public Transform HandRightTransform;
        [SerializeField] private bool initLayer;
        private void Start()
        {
            if (initLayer)
            {
                SetAvatarLayer();
            }
        }

        public void SetAvatar(GameObject head, GameObject body, bool setLayer = false)
        {
            GameObject shadowHead1 = Instantiate(head);
            shadowHead1.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            foreach (MeshRenderer meshRenderer in shadowHead1.GetComponentsInChildren<MeshRenderer>())
            {
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
            SetLayerRecursively(shadowHead1, 0);
            shadowHead1.transform.SetParent(ShadowHeadTransform, false);
            shadowHead1.transform.localPosition = Vector3.zero;
            HeadTransform = head.transform;
            //ShadowHeadTransform = head.transform;
            BodyTransform = body.transform;
            if (setLayer)
            {
                SetAvatarLayer();
            }
        }
        public void SetAvatarLayer()
        {
            Debug.Log("-->setting avatar layer");
            //Setting the layer of avatar head to AvatarLocalHead layer so that it does not block the view of the local VR Player
            SetLayerRecursively(HeadTransform.gameObject, 7);

            //Setting the layer of avatar body to AvatarLocalBody layer so that it does not block the view of the local VR Player
            // SetLayerRecursively(BodyTransform.gameObject, 6);
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