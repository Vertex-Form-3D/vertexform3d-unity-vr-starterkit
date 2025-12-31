using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
public class RadialScrollView : MonoBehaviour, IDragHandler, IScrollHandler
{
    [Header("Layout Settings")]
    [SerializeField] private float radius = 200f;
    [SerializeField] private float minAngle = 0f;
    [SerializeField] private float maxAngle = 360f;

    [Header("Scroll Settings")]
    [SerializeField] private float scrollSensitivity = 0.5f;
    [SerializeField] private bool invertScroll = false;
    [SerializeField] private bool infiniteScroll = true;

    // === NEW: Scroll enable/disable ===
    [Header("Control")]
    [SerializeField] private bool canScroll = false; // Set this to false to disable scrolling

    private RectTransform rectTransform;
    private List<RectTransform> childItems = new List<RectTransform>();
    private int childCount = 0;
    private float currentAngle = 0f;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        RefreshLayout();
    }

    private void OnEnable()
    {
        radius = 0;
        DOTween.To(() => radius, x => radius = x, 180, 0.2f).SetEase(Ease.InOutBounce);
        RefreshLayout();
    }

    private void Update()
    {
        RefreshLayout();
    }

    public void RefreshLayout()
    {
        RefreshChildren();
        LayoutChildren();
    }

    private void RefreshChildren()
    {
        childItems.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            RectTransform childRect = child as RectTransform;
            if (childRect != null && child.gameObject.activeInHierarchy)
            {
                childItems.Add(childRect);
            }
        }
        childCount = childItems.Count;
    }

    private void LayoutChildren()
    {
        if (childCount == 0) return;

        float totalAngleSpan = maxAngle - minAngle;
        float angleStep = totalAngleSpan / childCount;
        float startAngle = minAngle + currentAngle;

        for (int i = 0; i < childCount; i++)
        {
            float angle = startAngle + i * angleStep;
            float rad = angle * Mathf.Deg2Rad;
            Vector2 position = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;

            RectTransform child = childItems[i];
            child.anchoredPosition = position;

            // Optional: Rotate child to face outward (tangential)
            child.localRotation = Quaternion.Euler(0, 0, angle + 90f);
        }
    }

    // Called when dragging (mouse or touch)
    public void OnDrag(PointerEventData eventData)
    {
        if (!canScroll) return; // ← NEW: Block input if scrolling is disabled

        float delta = eventData.delta.x + eventData.delta.y * 0.3f;
        ApplyScrollDelta(delta);
    }

    // Called on mouse wheel scroll
    public void OnScroll(PointerEventData eventData)
    {
        if (!canScroll) return; // ← NEW: Block input if scrolling is disabled

        float delta = eventData.scrollDelta.y * 15f;
        ApplyScrollDelta(delta);
    }

    private void ApplyScrollDelta(float delta)
    {
        float direction = invertScroll ? -1f : 1f;
        float angleChange = delta * scrollSensitivity * direction;
        currentAngle += angleChange;

        if (infiniteScroll)
        {
            currentAngle = Mathf.Repeat(currentAngle, 360f);
        }
        else
        {
            float totalSpan = maxAngle - minAngle;
            float maxOffset = Mathf.Max(0, totalSpan - 360f);
            currentAngle = Mathf.Clamp(currentAngle, -maxOffset, 0f);
        }

        LayoutChildren();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        RefreshLayout();
    }
#endif

    // Optional: Public methods to enable/disable scrolling from other scripts
    public void SetScrollEnabled(bool enabled)
    {
        canScroll = enabled;
    }

    public bool IsScrollEnabled()
    {
        return canScroll;
    }
}