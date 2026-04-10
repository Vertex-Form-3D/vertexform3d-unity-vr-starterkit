using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Screen-space virtual stick: drag within the background to produce a normalized (-1..1) vector.
/// </summary>
[RequireComponent(typeof(Image))]
public class MobileVirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [SerializeField] private RectTransform handle;
    [Tooltip("Radius in local units the handle may move from center.")]
    [SerializeField] private float handleRange = 72f;

    private RectTransform _background;
    private Vector2 _value;
    private int _activePointerId = int.MinValue;

    public Vector2 Value => _value;
    public bool IsHeld => _activePointerId != int.MinValue;

    private void Awake()
    {
        _background = transform as RectTransform;
        if (handle == null && transform.childCount > 0)
            handle = transform.GetChild(0) as RectTransform;
    }

    private void Reset()
    {
        var img = GetComponent<Image>();
        img.raycastTarget = true;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_activePointerId != int.MinValue)
            return;
        _activePointerId = eventData.pointerId;
        UpdateHandle(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != _activePointerId)
            return;
        UpdateHandle(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != _activePointerId)
            return;
        _activePointerId = int.MinValue;
        _value = Vector2.zero;
        if (handle != null)
            handle.anchoredPosition = Vector2.zero;
    }

    private void UpdateHandle(PointerEventData eventData)
    {
        if (_background == null)
            return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _background, eventData.position, eventData.pressEventCamera, out Vector2 local))
            return;

        float r = Mathf.Max(handleRange, 1f);
        Vector2 clamped = Vector2.ClampMagnitude(local, r);
        _value = clamped / r;
        if (handle != null)
            handle.anchoredPosition = clamped;
    }
}
