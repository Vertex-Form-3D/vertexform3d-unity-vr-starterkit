using UnityEngine;

[RequireComponent(typeof(UIEffect))]
public class HowerUIScript : MonoBehaviour
{
    public GameObject[] howerUI;
    [Tooltip("When enabled, hover child objects stay hidden on mobile and pointer hover is ignored.")]
    public bool disableHoverOnMobile;
    UIEffect effect;

    bool MobileHoverDisabled =>
        disableHoverOnMobile && DesktopMobileControlSettings.UseMobileControls;

    void Start()
    {
        effect = GetComponent<UIEffect>();
        effect.onPointerEnterEvent.AddListener(OnHowerEnter);
        effect.onPointerExitEvent.AddListener(OnHowerExit);

        if (MobileHoverDisabled)
        {
            OnHowerExit();
            return;
        }

        if (DesktopMobileControlSettings.UseMobileMenuHoverUx)
        {
            OnHowerEnter();
            return;
        }

        OnHowerExit();
    }

    void OnHowerEnter()
    {
        if (MobileHoverDisabled)
            return;
        if (howerUI == null)
            return;
        foreach (GameObject item in howerUI)
        {
            if (item != null)
                item.SetActive(true);
        }
    }

    void OnHowerExit()
    {
        if (DesktopMobileControlSettings.UseMobileMenuHoverUx && !disableHoverOnMobile)
            return;
        if (howerUI == null)
            return;
        foreach (GameObject item in howerUI)
        {
            if (item != null)
                item.SetActive(false);
        }
    }
}
