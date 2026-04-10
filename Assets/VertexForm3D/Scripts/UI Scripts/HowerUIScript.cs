using UnityEngine;

[RequireComponent(typeof(UIEffect))]
public class HowerUIScript : MonoBehaviour
{
    public GameObject[] howerUI;
    UIEffect effect;

    void Start()
    {
        effect = GetComponent<UIEffect>();
        effect.onPointerEnterEvent.AddListener(OnHowerEnter);
        effect.onPointerExitEvent.AddListener(OnHowerExit);

        if (DesktopMobileControlSettings.UseMobileMenuHoverUx)
        {
            OnHowerEnter();
            return;
        }

        OnHowerExit();
    }

    void OnHowerEnter()
    {
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
        if (DesktopMobileControlSettings.UseMobileMenuHoverUx)
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
