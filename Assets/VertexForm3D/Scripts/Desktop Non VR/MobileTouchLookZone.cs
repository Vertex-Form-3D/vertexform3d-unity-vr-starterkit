using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Invisible (or tinted) full-screen region that accumulates pointer drag delta for camera look.
/// </summary>
[RequireComponent(typeof(Image))]
public class MobileTouchLookZone : MonoBehaviour, IDragHandler
{
    private Vector2 _accumulated;

    public Vector2 ConsumeAccumulatedDelta()
    {
        Vector2 v = _accumulated;
        _accumulated = Vector2.zero;
        return v;
    }

    public void OnDrag(PointerEventData eventData)
    {
        _accumulated += eventData.delta;
    }

    private void Reset()
    {
        var img = GetComponent<Image>();
        img.raycastTarget = true;
        img.color = new Color(1f, 1f, 1f, 0.02f);
    }
}
