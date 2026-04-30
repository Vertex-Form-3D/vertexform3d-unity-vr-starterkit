using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Look-zone touch input using direct Input.touches polling so multi-touch works
/// reliably (joystick held + look drag simultaneously).
/// Outputs per-frame screen-pixel delta via touchZoneOutputEvent.
/// </summary>
public class UIVirtualTouchZone : MonoBehaviour
{
    [System.Serializable]
    public class Event : UnityEvent<Vector2> { }

    [Header("Rect References")]
    public RectTransform containerRect;
    public RectTransform handleRect;

    [Header("Settings")]
    public bool clampToMagnitude;       // kept for inspector compatibility, not used
    public float magnitudeMultiplier = 1f;
    public bool invertXOutputValue;
    public bool invertYOutputValue;

    [Header("Output")]
    public Event touchZoneOutputEvent;

    private int _trackedFingerId = -1;

    void Start()
    {
        if (handleRect)
            handleRect.gameObject.SetActive(false);
    }

    void Update()
    {
        RectTransform zoneRect = containerRect != null ? containerRect : (RectTransform)transform;
        Camera cam = GetCanvasCamera();

        if (_trackedFingerId < 0)
        {
            // Try to claim a new touch that begins inside this zone
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.phase != TouchPhase.Began) continue;
                if (!RectTransformUtility.RectangleContainsScreenPoint(zoneRect, t.position, cam)) continue;

                _trackedFingerId = t.fingerId;
                if (handleRect)
                {
                    handleRect.gameObject.SetActive(true);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(zoneRect, t.position, cam, out Vector2 local);
                    handleRect.anchoredPosition = local;
                }
                break;
            }
            return;
        }

        // Find the tracked touch
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            if (t.fingerId != _trackedFingerId) continue;

            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                break; // fall through to release

            if (t.phase == TouchPhase.Moved)
            {
                Vector2 delta = t.deltaPosition;
                if (invertXOutputValue) delta.x = -delta.x;
                if (invertYOutputValue) delta.y = -delta.y;
                touchZoneOutputEvent.Invoke(delta * magnitudeMultiplier);
            }
            return;
        }

        // Touch ended or lost
        _trackedFingerId = -1;
        touchZoneOutputEvent.Invoke(Vector2.zero);
        if (handleRect)
            handleRect.gameObject.SetActive(false);
    }

    private Camera GetCanvasCamera()
    {
        Canvas c = GetComponentInParent<Canvas>();
        if (c == null) return null;
        return c.renderMode == RenderMode.ScreenSpaceOverlay ? null : c.worldCamera;
    }
}
