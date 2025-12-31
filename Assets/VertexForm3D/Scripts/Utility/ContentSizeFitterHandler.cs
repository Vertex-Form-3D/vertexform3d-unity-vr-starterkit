using UnityEngine;
using UnityEngine.UI;

public class ContentSizeFitterHandler : MonoBehaviour
{
    private void OnTransformChildrenChanged()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }

    private void OnEnable()
    {
        Invoke(nameof(RebuildLayout), .2f);
    }
    void RebuildLayout()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }
}
