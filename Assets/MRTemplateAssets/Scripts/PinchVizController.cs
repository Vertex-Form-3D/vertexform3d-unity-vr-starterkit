using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class PinchVizController : MonoBehaviour
{
    [SerializeField]
    SkinnedMeshRenderer m_Pointer;

    XRRayInteractor m_Interactor;

    // Start is called before the first frame update
    void Start()
    {
        m_Interactor = this.GetComponent<XRRayInteractor>();
    }

    // Update is called once per frame
    void Update()
    {
        var inputValue = Mathf.Max(m_Interactor.selectInput.ReadValue(), m_Interactor.uiPressInput.ReadValue());
        m_Pointer.SetBlendShapeWeight(0, inputValue * 100f);
    }
}
