using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Touch-friendly sprint (hold) and jump (press) for <see cref="XRRigController"/>.
/// </summary>
public class MobileRigActionPad : MonoBehaviour
{
    [SerializeField] private XRRigController rig;

    private void Awake()
    {
        if (rig == null)
            rig = GetComponentInParent<XRRigController>();
    }

    public void SetRig(XRRigController controller) => rig = controller;

    public void OnSprintPressed() => rig?.SetMobileSprintHeld(true);
    public void OnSprintReleased() => rig?.SetMobileSprintHeld(false);
    public void OnJumpPressed() => rig?.SetMobileJumpFromUi(true);
}

/// <summary>Hold-to-sprint on a UI graphic (works with touch).</summary>
[RequireComponent(typeof(Graphic))]
public class MobileSprintHoldZone : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, ICancelHandler
{
    [SerializeField] private MobileRigActionPad pad;

    private void Awake()
    {
        if (pad == null)
            pad = GetComponentInParent<MobileRigActionPad>();
    }

    public void OnPointerDown(PointerEventData eventData) => pad?.OnSprintPressed();
    public void OnPointerUp(PointerEventData eventData) => pad?.OnSprintReleased();
    public void OnCancel(BaseEventData eventData) => pad?.OnSprintReleased();
}
