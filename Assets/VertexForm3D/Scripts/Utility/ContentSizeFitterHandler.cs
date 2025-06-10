using UnityEngine;
using UnityEngine.UI;

public class ContentSizeFitterHandler : MonoBehaviour
{
    private void OnTransformChildrenChanged()
    {
        Debug.Log("OnTransformChildrenChanged called");
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
