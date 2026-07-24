using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace StarterAssets
{
    /// <summary>
    /// Bridges Starter Assets mobile UI (UIVirtualJoystick, UIVirtualTouchZone, buttons)
    /// to XRRigController + OrbitCamera (third person) or first-person look.
    /// Look input is polled directly via Enhanced Touch in Update() — no EventSystem dependency.
    /// </summary>
    public class UICanvasControllerInput : MonoBehaviour
    {
        [Header("Vertex Form rig")]
        [Tooltip("Local player desktop / XR rig (move, sprint, jump, look).")]
        public XRRigController xrRigController;

        [Tooltip("Optional; defaults to xrRigController.orbitCamera when null.")]
        public OrbitCamera orbitCamera;

        [Header("Look zone (direct touch)")]
        [Tooltip("RectTransform that defines the look-input area. If not assigned, the right half of the screen is used automatically.")]
        [SerializeField] private RectTransform lookZoneRect;

        [Tooltip("Sensitivity for look input. Screen-pixel delta per frame is multiplied by this value before being applied to the camera.")]
        [SerializeField] private float lookSensitivity = 0.3f;

        [Header("Desktop / mobile toggle")]
        [Tooltip("When true, disables this Canvas while DesktopMobileControlSettings.UseMobileControls is false.")]
        [SerializeField] private bool driveCanvasEnabledFromSettings = true;

        [Tooltip("If true, sets DesktopMobileControlSettings.UseMobileControls = true in Awake so the Canvas stays enabled and virtual controls work.")]
        [SerializeField] private bool requestMobileControlsOnAwake = true;

        private int _lookFingerId = -1;
        private Canvas _canvas;
        private bool _startupPlatformSyncAttempted;

        private void Awake()
        {
            if (orbitCamera == null && xrRigController != null)
                orbitCamera = xrRigController.orbitCamera;
            _canvas = GetComponent<Canvas>();
            if (requestMobileControlsOnAwake && !IsVrPlatformForThisRig())
                DesktopMobileControlSettings.SetUseMobileControls(true);
        }

        private void Start()
        {
            TrySyncMobileModeFromPlatformAsset();
            ApplyCanvasVisibility();
        }

        private void OnEnable()
        {
            DesktopMobileControlSettings.Changed += OnMobileSettingsChanged;
            EnhancedTouchSupport.Enable();
            ApplyCanvasVisibility();
        }

        private void OnDisable()
        {
            DesktopMobileControlSettings.Changed -= OnMobileSettingsChanged;
            _lookFingerId = -1;
        }

        private void LateUpdate()
        {
            if (!driveCanvasEnabledFromSettings || _canvas == null)
                return;
            TrySyncMobileModeFromPlatformAsset();
            bool wantEnabled = DesktopMobileControlSettings.UseFlatMobileControls && !IsVrPlatformForThisRig();
            if (_canvas.enabled != wantEnabled)
                ApplyCanvasVisibility();
        }

        private void Update()
        {
            if (!AllowVirtualInput || xrRigController == null)
                return;
            PollLookTouches();
        }

        private void PollLookTouches()
        {
            var touches = ETouch.activeTouches;

            // Claim a new look touch
            if (_lookFingerId < 0)
            {
                foreach (var t in touches)
                {
                    if (t.phase != UnityEngine.InputSystem.TouchPhase.Began) continue;
                    if (!IsInLookZone(t.screenPosition)) continue;
                    _lookFingerId = t.touchId;
                    break;
                }
                return;
            }

            // Process the tracked look touch
            foreach (var t in touches)
            {
                if (t.touchId != _lookFingerId) continue;

                if (t.phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                    t.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    _lookFingerId = -1;
                    return;
                }

                if (t.phase == UnityEngine.InputSystem.TouchPhase.Moved)
                    xrRigController.ApplyVirtualUiLook(t.delta * lookSensitivity);

                return;
            }

            // Touch lost between frames
            _lookFingerId = -1;
        }

        private bool IsInLookZone(Vector2 screenPos)
        {
            if (lookZoneRect != null)
            {
                Canvas c = lookZoneRect.GetComponentInParent<Canvas>();
                Camera cam = (c != null && c.renderMode != RenderMode.ScreenSpaceOverlay)
                    ? c.worldCamera : null;
                return RectTransformUtility.RectangleContainsScreenPoint(lookZoneRect, screenPos, cam);
            }
            // Default: right half of screen
            return screenPos.x > Screen.width * 0.5f;
        }

        private void OnMobileSettingsChanged(bool useMobile)
        {
            ApplyCanvasVisibility();
            _lookFingerId = -1;
            if (!useMobile && xrRigController != null)
            {
                xrRigController.moveInput = Vector2.zero;
                xrRigController.SetMobileSprintHeld(false);
            }
        }

        private void ApplyCanvasVisibility()
        {
            if (!driveCanvasEnabledFromSettings || _canvas == null)
                return;
            _canvas.enabled = DesktopMobileControlSettings.UseFlatMobileControls && !IsVrPlatformForThisRig();
        }

        private void TrySyncMobileModeFromPlatformAsset()
        {
            if (_startupPlatformSyncAttempted)
                return;

            if (ProjectManager.instance == null || ProjectManager.instance.platforms == null)
                return;

            Platforms pl = ProjectManager.instance.platforms;
            bool shouldUseMobileControls =
                pl.platformChoice == platform.Web &&
                (pl.webGpuBrowserKind == WebGpuBrowserKind.MobileBrowser ||
                 DesktopMobileControlSettings.UseMobileControls);

            DesktopMobileControlSettings.SetUseMobileControls(shouldUseMobileControls);
            _startupPlatformSyncAttempted = true;
        }

        private bool IsVrPlatformForThisRig()
        {
            if (xrRigController != null)
                return xrRigController.GetPlatformProperty();
            return ProjectManager.instance != null &&
                   ProjectManager.instance.platforms != null &&
                   ProjectManager.instance.platforms.IsVrStylePlatform();
        }

        private bool AllowVirtualInput =>
            DesktopMobileControlSettings.UseFlatMobileControls && !IsVrPlatformForThisRig();

        /// <summary>From UIVirtualJoystick → joystickOutputEvent</summary>
        public void VirtualMoveInput(Vector2 virtualMoveDirection)
        {
            if (!AllowVirtualInput || xrRigController == null)
                return;
            xrRigController.moveInput = virtualMoveDirection;
        }

        /// <summary>
        /// Legacy callback from UIVirtualTouchZone. Look is now handled by PollLookTouches() in Update().
        /// This remains wired in the inspector to avoid breaking serialized references.
        /// </summary>
        public void VirtualLookInput(Vector2 lookDelta) { }

        /// <summary>From UIVirtualButton jump.</summary>
        public void VirtualJumpInput(bool virtualJumpState)
        {
            if (!AllowVirtualInput || xrRigController == null)
                return;
            xrRigController.SetMobileJumpFromUi(virtualJumpState);
        }

        /// <summary>From UIVirtualButton sprint.</summary>
        public void VirtualSprintInput(bool virtualSprintState)
        {
            if (!AllowVirtualInput || xrRigController == null)
                return;
            xrRigController.SetMobileSprintHeld(virtualSprintState);
        }
    }
}
