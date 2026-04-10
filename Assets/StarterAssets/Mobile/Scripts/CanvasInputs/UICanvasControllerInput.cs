using UnityEngine;

namespace StarterAssets
{
    /// <summary>
    /// Bridges Starter Assets mobile UI (<see cref="UIVirtualJoystick"/>, <see cref="UIVirtualTouchZone"/>, buttons)
    /// to <see cref="XRRigController"/> + <see cref="OrbitCamera"/> (third person) or first-person look.
    /// Wire the virtual controls' UnityEvents to these methods in the Inspector.
    /// </summary>
    /// <remarks>
    /// Move and look are only driven by this canvas when <see cref="DesktopMobileControlSettings.UseMobileControls"/> is true
    /// and the Vertex Form platform for this rig is not <see cref="platform.VR"/>.
    /// Optional <see cref="ThirdPersonMobileControls"/> on the rig adds two-finger pinch zoom (first- and third-person; third-person also updates orbit distance).
    /// </remarks>
    public class UICanvasControllerInput : MonoBehaviour
    {
        [Header("Vertex Form rig")]
        [Tooltip("Local player desktop / XR rig (move, sprint, jump, look).")]
        public XRRigController xrRigController;

        [Tooltip("Optional; defaults to xrRigController.orbitCamera when null.")]
        public OrbitCamera orbitCamera;

        [Header("Look scaling")]
        [Tooltip("UIVirtualTouchZone sends offset-from-press; we convert successive samples to per-frame deltas, then scale toward screen-pixel units used by OrbitCamera / FPS look.")]
        [SerializeField]
        private float lookDeltaToPixelsScale = 800f;

        [Header("Desktop / mobile toggle")]
        [Tooltip("When true, disables this Canvas while DesktopMobileControlSettings.UseMobileControls is false.")]
        [SerializeField]
        private bool driveCanvasEnabledFromSettings = true;

        [Tooltip("If true, sets DesktopMobileControlSettings.UseMobileControls = true in Awake so the Canvas stays enabled and virtual controls work. Turn off if you only toggle mobile mode from WebGL JS.")]
        [SerializeField]
        private bool requestMobileControlsOnAwake = true;

        private Vector2 _lastLookSample;
        private Canvas _canvas;

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
            ApplyCanvasVisibility();
        }

        private void OnEnable()
        {
            DesktopMobileControlSettings.Changed += OnMobileSettingsChanged;
            ApplyCanvasVisibility();
        }

        private void OnDisable()
        {
            DesktopMobileControlSettings.Changed -= OnMobileSettingsChanged;
        }

        private void OnMobileSettingsChanged(bool useMobile)
        {
            ApplyCanvasVisibility();
            _lastLookSample = Vector2.zero;
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
            _canvas.enabled = DesktopMobileControlSettings.UseMobileControls && !IsVrPlatformForThisRig();
        }

        /// <summary>Uses <see cref="XRRigController.GetPlatformProperty"/> when a rig is assigned; otherwise <see cref="ProjectManager"/>.</summary>
        private bool IsVrPlatformForThisRig()
        {
            if (xrRigController != null)
                return xrRigController.GetPlatformProperty();
            return ProjectManager.instance != null && ProjectManager.instance.platforms.platformChoice == platform.VR;
        }

        private bool AllowVirtualInput =>
            DesktopMobileControlSettings.UseMobileControls && !IsVrPlatformForThisRig();

        /// <summary>From UIVirtualJoystick → joystickOutputEvent</summary>
        public void VirtualMoveInput(Vector2 virtualMoveDirection)
        {
            if (!AllowVirtualInput || xrRigController == null)
                return;
            xrRigController.moveInput = virtualMoveDirection;
        }

        /// <summary>From UIVirtualTouchZone → touchZoneOutputEvent</summary>
        public void VirtualLookInput(Vector2 virtualLookFromTouchZone)
        {
            if (!AllowVirtualInput || xrRigController == null)
                return;

            if (DesktopMobileControlSettings.SuppressLookWhileMultiTouch)
            {
                _lastLookSample = Vector2.zero;
                return;
            }

            if (virtualLookFromTouchZone.sqrMagnitude < 1e-8f)
            {
                _lastLookSample = Vector2.zero;
                return;
            }

            Vector2 delta = virtualLookFromTouchZone - _lastLookSample;
            _lastLookSample = virtualLookFromTouchZone;

            Vector2 deltaPixels = delta * lookDeltaToPixelsScale;
            xrRigController.ApplyVirtualUiLook(deltaPixels);
        }

        /// <summary>From UIVirtualButton jump (bool while held, or use click for one-shot).</summary>
        public void VirtualJumpInput(bool virtualJumpState)
        {
            if (!AllowVirtualInput || xrRigController == null)
                return;
            xrRigController.SetMobileJumpFromUi(virtualJumpState);
        }

        /// <summary>From UIVirtualButton sprint (bool while held).</summary>
        public void VirtualSprintInput(bool virtualSprintState)
        {
            if (!AllowVirtualInput || xrRigController == null)
                return;
            xrRigController.SetMobileSprintHeld(virtualSprintState);
        }
    }
}
