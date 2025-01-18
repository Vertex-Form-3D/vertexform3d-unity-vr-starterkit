using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NestedScrollRect : ScrollRect
{
    private bool _routeToParent = false;
    public bool _restScrollOnEnable = false;

    protected override void Start()
    {
        base.Start();
        this.onValueChanged.AddListener(HandleScroll);
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (_restScrollOnEnable)
        {
            RestScrollPosition();
        }
    }

    private void RestScrollPosition()
    {
        verticalNormalizedPosition = 0;
        horizontalNormalizedPosition = 0;
    }

    private void HandleScroll(Vector2 value)
    {
        if (_routeToParent)
        {
            // Pass the scroll event to the parent scroll view
            this.transform.parent.parent.GetComponentInParent<ScrollRect>().OnScroll(new PointerEventData(EventSystem.current));
        }
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        if (!horizontal && Math.Abs(eventData.delta.x) > Math.Abs(eventData.delta.y))
            _routeToParent = true;
        else if (!vertical && Math.Abs(eventData.delta.x) < Math.Abs(eventData.delta.y))
            _routeToParent = true;
        else
            _routeToParent = false;

        if (_routeToParent) {
            var parentScroll = this.transform.parent.parent.GetComponentInParent<ScrollRect>();
            parentScroll.OnBeginDrag(eventData); 
        }

        base.OnBeginDrag(eventData);
    }

    public override void OnDrag(PointerEventData eventData)
    {

        if (_routeToParent)
        {
            var parentScroll = this.transform.parent.parent.GetComponentInParent<ScrollRect>();
            if (parentScroll)
            {
                parentScroll.OnDrag(eventData);
            }
        }
        else
            base.OnDrag(eventData);
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        if (_routeToParent)
            this.transform.parent.parent.GetComponentInParent<ScrollRect>().OnEndDrag(eventData);
        else
            base.OnEndDrag(eventData);
    }
}