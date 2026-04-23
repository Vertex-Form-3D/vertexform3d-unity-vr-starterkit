using UnityEngine;

public class MainMenuScript : MonoBehaviour
{
    public GameObject menuUI;
    public XRRigController xrRigController;
    public void toggleMenu()
    {
        menuUI.SetActive(!menuUI.activeInHierarchy);
        UpdateInputLockFromOpenPanels();
    }
    public void closeMenu()
    {
        menuUI.SetActive(false);
        UpdateInputLockFromOpenPanels();
    }

    private void UpdateInputLockFromOpenPanels()
    {
        if (xrRigController == null)
            return;

        xrRigController.SetUiInputLocked(menuUI.activeInHierarchy);
    }
}
