using UnityEngine;
using UnityEngine.Rendering;

namespace VertexFormCore
{
    public class AvatarHolder : MonoBehaviour
    {
        public Transform MainAvatarTransform;
        public Transform HeadTransform;
        public Transform ShadowHeadTransform;
        public Transform ShadowBodyTransform;
        public Transform BodyTransform;
        public Transform HandLeftTransform;
        public Transform HandRightTransform;

        public SkinnedMeshRenderer LeftHandVisual;
        public SkinnedMeshRenderer RightHandVisual;

        [SerializeField] private bool initLayer;

        private GameObject _visibleBody;
        private GameObject _shadowHead;
        private bool _personModeListenersRegistered;

        private void Start()
        {
            if (initLayer)
            {
                SetAvatarLayer();
            }

            TryRegisterPersonModeListeners();
        }

        private void OnDestroy()
        {
            UnregisterPersonModeListeners();
        }

        public void SetAvatar(GameObject head, GameObject body, bool setLayer = false)
        {
            if (_shadowHead != null)
            {
                Destroy(_shadowHead);
                _shadowHead = null;
            }

            _visibleBody = body;
            HeadTransform = head.transform;
            BodyTransform = body.transform;

            Transform headShadowParent = ShadowHeadTransform != null ? ShadowHeadTransform : head.transform.parent;
            _shadowHead = CreateShadowCopy(head);
            _shadowHead.transform.SetParent(headShadowParent, false);
            _shadowHead.transform.localPosition = Vector3.zero;
            _shadowHead.transform.localRotation = Quaternion.identity;

            if (setLayer)
            {
                SetAvatarLayer();
            }

            TryRegisterPersonModeListeners();
            SyncBodyVisibilityWithCurrentPersonMode();
        }

        public void SetAvatarLayer()
        {
            Debug.Log("-->setting avatar layer");
            if (HeadTransform != null)
                SetLayerRecursively(HeadTransform.gameObject, 7);
        }

        /// <summary>
        /// Desktop/Mobile first-person: hide the visible body mesh when configured, but keep shadow casting.
        /// </summary>
        public void ApplyBodyVisibilityForPersonMode(bool isThirdPerson)
        {
            if (_visibleBody == null && BodyTransform != null)
                _visibleBody = BodyTransform.gameObject;

            if (_visibleBody == null)
                return;

            _visibleBody.SetActive(true);

            bool showBody = isThirdPerson;
            if (!isThirdPerson)
            {
                var cfg = ProjectManager.instance != null ? ProjectManager.instance.uiLayoutConfig : null;
                showBody = cfg == null || cfg.showAvatarBodyInFirstPerson;
            }

            foreach (Renderer renderer in _visibleBody.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = showBody
                    ? ShadowCastingMode.On
                    : ShadowCastingMode.ShadowsOnly;
            }
        }

        private void TryRegisterPersonModeListeners()
        {
            if (_personModeListenersRegistered || !ShouldManageFirstPersonBodyVisibility())
                return;

            var xrRig = GetComponentInParent<XRRigController>() ?? FindAnyObjectByType<XRRigController>();
            if (xrRig == null)
                return;

            xrRig.onFPSModeStartEvent.AddListener(OnFirstPersonModeStarted);
            xrRig.onThirdPersonModeStartEvent.AddListener(OnThirdPersonModeStarted);
            _personModeListenersRegistered = true;
            SyncBodyVisibilityWithCurrentPersonMode();
        }

        private void UnregisterPersonModeListeners()
        {
            if (!_personModeListenersRegistered)
                return;

            var xrRig = GetComponentInParent<XRRigController>() ?? FindAnyObjectByType<XRRigController>();
            if (xrRig != null)
            {
                xrRig.onFPSModeStartEvent.RemoveListener(OnFirstPersonModeStarted);
                xrRig.onThirdPersonModeStartEvent.RemoveListener(OnThirdPersonModeStarted);
            }

            _personModeListenersRegistered = false;
        }

        private void OnFirstPersonModeStarted()
        {
            ApplyBodyVisibilityForPersonMode(false);
        }

        private void OnThirdPersonModeStarted()
        {
            ApplyBodyVisibilityForPersonMode(true);
        }

        private void SyncBodyVisibilityWithCurrentPersonMode()
        {
            var xrRig = GetComponentInParent<XRRigController>() ?? FindAnyObjectByType<XRRigController>();
            if (xrRig == null)
                return;

            ApplyBodyVisibilityForPersonMode(xrRig.isThirdPerson);
        }

        private bool ShouldManageFirstPersonBodyVisibility()
        {
            if (ProjectManager.instance == null || ProjectManager.instance.platforms == null)
                return false;

            if (!ProjectManager.instance.platforms.IsDesktopStylePlatform())
                return false;

            var playerSetup = GetComponentInParent<PlayerNetworkSetup>();
            if (playerSetup != null && playerSetup.Object != null)
                return playerSetup.Object.HasInputAuthority;

            return true;
        }

        private static GameObject CreateShadowCopy(GameObject source)
        {
            GameObject shadow = Instantiate(source);
            shadow.name = source.name + "_Shadow";
            foreach (Renderer renderer in shadow.GetComponentsInChildren<Renderer>(true))
                renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            SetLayerRecursively(shadow, 0);
            return shadow;
        }

        static void SetLayerRecursively(GameObject go, int layerNumber)
        {
            if (go == null) return;
            foreach (Transform trans in go.GetComponentsInChildren<Transform>(true))
                trans.gameObject.layer = layerNumber;
        }
    }
}
