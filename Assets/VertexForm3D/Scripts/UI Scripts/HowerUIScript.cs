using UnityEngine;

[RequireComponent(typeof(UIEffect))]
public class HowerUIScript : MonoBehaviour
{
    public GameObject[] howerUI;
    UIEffect effect;
    void Start()
    {
        OnHowerExit();
        effect = GetComponent<UIEffect>();
        effect.onPointerEnterEvent.AddListener(OnHowerEnter);
        effect.onPointerExitEvent.AddListener(OnHowerExit);
    }

    void OnHowerEnter()
    {
        foreach (GameObject item in howerUI)
        {
            item.SetActive(true);
        }
    }

    void OnHowerExit()
    {
        foreach (GameObject item in howerUI)
        {
            item.SetActive(false);
        }
    }
}
