using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class UIEventTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public bool beginDrag ;
    public UnityEvent onBeginDrag = new UnityEvent();
    public UnityEvent onDrag = new UnityEvent();
    public UnityEvent onEndDrag = new UnityEvent();
    public UnityEvent onPointerEnter = new UnityEvent();
    public UnityEvent onPointerExit = new UnityEvent();
    public void OnBeginDrag(PointerEventData eventData)
    {
        if(onBeginDrag!=null)
        onBeginDrag?.Invoke();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if(onDrag != null)
        onDrag.Invoke();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if(onEndDrag != null)
        onEndDrag?.Invoke();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(onPointerEnter != null)
        onPointerEnter?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if(onPointerExit != null)
        onPointerExit?.Invoke();
    }
    void Start()
    {
        
    }
}
