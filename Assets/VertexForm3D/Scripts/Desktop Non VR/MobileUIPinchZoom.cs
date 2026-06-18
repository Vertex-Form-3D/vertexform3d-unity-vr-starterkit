using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Mobile two-finger pinch zoom for a UI panel (Login screen, info card, etc.). Scales
/// <see cref="target"/>'s <c>localScale</c> between <see cref="minScale"/> and <see cref="maxScale"/>,
/// keeping the pinch midpoint anchored under the user's fingers. Double-tap with one finger resets to 1.
/// </summary>
/// <remarks>
/// Lives next to <see cref="ThirdPersonMobileControls"/> — that script handles world-camera pinch;
/// this one handles UI pinch. They never run simultaneously on the same two touches because the
/// 3D pinch is wired to the rig and only applies world-space zoom, while this one mutates a UI scale.
/// If you put a pinchable UI <em>on top of</em> a pinchable 3D scene, gate them with a raycast / canvas
/// blocker so a pinch over the UI does not also drive the camera. Gated by
/// <see cref="DesktopMobileControlSettings.UseFlatMobileControls"/> so it is inert in VR / desktop builds.
/// </remarks>
[DefaultExecutionOrder(-30)]
public class MobileUIPinchZoom : MonoBehaviour
{
    [Tooltip("RectTransform to scale. If null, uses this GameObject's RectTransform.")]
    [SerializeField] private RectTransform target;

    [Tooltip("Canvas the target lives under. If null, found automatically. Used to translate pinch midpoints from screen space to local UI space.")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Minimum zoom as a multiplier of the original scale (1 = original size). Works regardless of whether the canvas starts at localScale 1 or 0.001.")]
    [SerializeField, Min(0.1f)] private float minScale = 1f;

    [Tooltip("Maximum zoom as a multiplier of the original scale (3 = 3× original).")]
    [SerializeField, Min(0.1f)] private float maxScale = 3f;

    [Tooltip("Additive zoom per pixel of pinch-distance change. 0.005 means a 100 px pinch adds 0.5 to the zoom multiplier. Lower = slower.")]
    [SerializeField, Min(0.0001f)] private float zoomSensitivity = 0.005f;

    [Tooltip("If true, double-tap with a single finger resets the panel scale and position.")]
    [SerializeField] private bool doubleTapToReset = true;

    [Tooltip("Maximum gap (seconds) between taps to count as a double tap.")]
    [SerializeField, Min(0.05f)] private float doubleTapInterval = 0.35f;

    [Tooltip("If true, the script only runs while DesktopMobileControlSettings.UseFlatMobileControls is true (flat WebGL / mobile / WebGPU mobile-shell). Turn off if you want pinch on touch monitors regardless.")]
    [SerializeField] private bool requireFlatMobile = true;

    [Tooltip("If true, this pinch only activates on a WebGPU MobileBrowser runtime. Desktop browser / native desktop UIs that share this canvas under ScreenSpaceOverlay will not receive pinch.")]
    [SerializeField] private bool mobileBrowserOnly = true;

    private float _lastPinchDistance;
    private bool _hadPinch;
    private Vector3 _initialScale;
    private Vector2 _initialAnchoredPos;
    private float _zoomMultiplier = 1f;
    private float _lastTapTime = -10f;

    private void Awake()
    {
        if (target == null)
            target = GetComponent<RectTransform>();
        if (target != null)
        {
            _initialScale = target.localScale;
            _initialAnchoredPos = target.anchoredPosition;
        }
        if (canvas == null && target != null)
            canvas = target.GetComponentInParent<Canvas>();
    }

    private void OnEnable()
    {
        // EnhancedTouchSupport.Enable() is reference-counted, so coexisting with ThirdPersonMobileControls is safe.
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
        _hadPinch = false;
    }

    private void Update()
    {
        if (target == null)
            return;
        if (requireFlatMobile && !DesktopMobileControlSettings.UseFlatMobileControls)
        {
            _hadPinch = false;
            return;
        }
        if (mobileBrowserOnly && !IsMobileBrowserRuntime())
        {
            // Desktop browser / native desktop runs the same ScreenSpaceOverlay login canvas — skip pinch there.
            _hadPinch = false;
            return;
        }

        var touches = Touch.activeTouches;

        // Two-finger pinch: scale around the pinch midpoint so the content under the fingers stays under the fingers.
        if (touches.Count >= 2)
        {
            Vector2 p0 = touches[0].screenPosition;
            Vector2 p1 = touches[1].screenPosition;
            float distance = Vector2.Distance(p0, p1);
            Vector2 midpoint = (p0 + p1) * 0.5f;

            if (!_hadPinch || _lastPinchDistance <= 0.001f)
            {
                _lastPinchDistance = distance;
                _hadPinch = true;
                return;
            }

            // Additive zoom: change the multiplier by pinch pixel-delta, not by ratio. Multiplicative ratios
            // compound per frame and feel runaway on a 60 Hz device. With additive, a slow pinch is a slow zoom
            // regardless of frame rate, because the *total* multiplier delta over a gesture equals
            // (total pinch pixel change) × zoomSensitivity, no matter how many frames it spanned.
            float pixelDelta = distance - _lastPinchDistance;
            float nextMultiplier = Mathf.Clamp(_zoomMultiplier + pixelDelta * zoomSensitivity, minScale, maxScale);
            float multiplierChange = nextMultiplier - _zoomMultiplier;
            if (!Mathf.Approximately(multiplierChange, 0f) && _zoomMultiplier > 0.0001f)
            {
                float appliedRatio = nextMultiplier / _zoomMultiplier;
                _zoomMultiplier = nextMultiplier;
                ZoomAroundScreenPoint(midpoint, appliedRatio);
            }

            _lastPinchDistance = distance;
            return;
        }

        _hadPinch = false;
        _lastPinchDistance = 0f;

        // Single-finger double tap → reset.
        if (doubleTapToReset && touches.Count == 1)
        {
            var t = touches[0];
            if (t.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                if (Time.unscaledTime - _lastTapTime <= doubleTapInterval)
                {
                    ResetZoom();
                    _lastTapTime = -10f;
                }
                else
                {
                    _lastTapTime = Time.unscaledTime;
                }
            }
        }
    }

    private static bool IsMobileBrowserRuntime()
    {
        if (ProjectManager.instance == null || ProjectManager.instance.platforms == null)
            return false;
        return ProjectManager.instance.platforms.webGpuBrowserKind == WebGpuBrowserKind.MobileBrowser;
    }

    /// <summary>Scale the target by <paramref name="ratio"/> while keeping <paramref name="screenPoint"/> visually anchored.</summary>
    private void ZoomAroundScreenPoint(Vector2 screenPoint, float ratio)
    {
        RectTransform parentRect = target.parent as RectTransform;
        if (parentRect == null)
        {
            target.localScale *= ratio;
            return;
        }

        Camera uiCamera = ResolveUiCamera();

        // Pivot point in the parent's local space — keep this point stationary while scaling.
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, uiCamera, out Vector2 pivotLocal))
        {
            target.localScale *= ratio;
            return;
        }

        Vector2 anchoredBefore = target.anchoredPosition;
        Vector2 offset = anchoredBefore - pivotLocal;
        target.localScale *= ratio;
        target.anchoredPosition = pivotLocal + offset * ratio;
    }

    private Camera ResolveUiCamera()
    {
        if (canvas == null) return null;
        switch (canvas.renderMode)
        {
            case RenderMode.ScreenSpaceOverlay:
                return null;
            case RenderMode.ScreenSpaceCamera:
            case RenderMode.WorldSpace:
                return canvas.worldCamera;
            default:
                return null;
        }
    }

    /// <summary>Restore the original scale and anchored position. Safe to call from a UI button.</summary>
    public void ResetZoom()
    {
        if (target == null)
            return;
        target.localScale = _initialScale;
        target.anchoredPosition = _initialAnchoredPos;
        _zoomMultiplier = 1f;
        _hadPinch = false;
        _lastPinchDistance = 0f;
    }

    /// <summary>Set zoom directly as a multiplier of the original scale (e.g. wire a UI slider with range Min Scale..Max Scale).</summary>
    public void SetZoom(float multiplier)
    {
        if (target == null)
            return;
        float clamped = Mathf.Clamp(multiplier, minScale, maxScale);
        target.localScale = _initialScale * clamped;
        _zoomMultiplier = clamped;
    }
}
