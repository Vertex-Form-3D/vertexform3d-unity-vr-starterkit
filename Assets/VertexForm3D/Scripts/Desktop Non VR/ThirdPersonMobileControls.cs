using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// When <see cref="DesktopMobileControlSettings.UseMobileControls"/> is true, move/look come only from the mobile UI canvas.
/// This component adds optional two-finger pinch zoom (first- and third-person), mirroring mouse wheel on <see cref="XRRigController"/>.
/// In third person, pinch also updates <see cref="OrbitCamera"/> orbit distance like the wheel does. Add alongside <see cref="XRRigController"/>.
/// </summary>
/// <remarks>
/// Pinch cannot be expressed as a single Input System binding on <c>Scroll</c> the way
/// <c>&lt;Touchscreen&gt;/primaryTouch/delta</c> maps to look: the core package exposes per-touch
/// controls, not a synthesized “pinch” axis. Multi-touch pinch is therefore derived in code from
/// two touches (here via <see cref="EnhancedTouchSupport"/> / <see cref="Touch.activeTouches"/>).
/// XR Interaction Toolkit’s <c>TouchscreenGestureInputController</c> can expose pinch as a device
/// path in AR/XR setups, but that is not generally available for flat WebGL/mobile browsers.
/// </remarks>
[DefaultExecutionOrder(-40)]
public class ThirdPersonMobileControls : MonoBehaviour
{
    [SerializeField] private XRRigController rig;
    [SerializeField] private OrbitCamera orbitCamera;

    [Tooltip("Pinch zoom: scroll units per pixel of pinch radius change.")]
    [SerializeField]
    private float pinchZoomSensitivity = 0.015f;

    private float _lastPinchDistance;
    private bool _hadPinch;

    private void Awake()
    {
        if (rig == null)
            rig = GetComponent<XRRigController>();
        if (orbitCamera == null)
            orbitCamera = GetComponentInChildren<OrbitCamera>(true);
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void Update()
    {
        if (rig == null || !DesktopMobileControlSettings.UseMobileControls)
            return;

        if (rig.GetPlatformProperty())
            return;

        if (rig.isMultiplayer && !rig.IsLocalPlayer())
            return;

        if (Touch.activeTouches.Count >= 2)
            ProcessPinchZoom();
        else
            _hadPinch = false;
    }

    private void ProcessPinchZoom()
    {
        Touch t0 = Touch.activeTouches[0];
        Touch t1 = Touch.activeTouches[1];
        float dist = Vector2.Distance(t0.screenPosition, t1.screenPosition);

        if (!_hadPinch)
        {
            _lastPinchDistance = dist;
            _hadPinch = true;
            return;
        }

        float delta = dist - _lastPinchDistance;
        _lastPinchDistance = dist;
        float scroll = -delta * pinchZoomSensitivity;
        if (Mathf.Abs(scroll) <= 1e-5f || rig == null)
            return;

        rig.ApplyMobileScrollZoom(scroll);
        if (rig.isThirdPerson && orbitCamera != null)
            orbitCamera.ApplyExternalScroll(scroll);
    }
}
