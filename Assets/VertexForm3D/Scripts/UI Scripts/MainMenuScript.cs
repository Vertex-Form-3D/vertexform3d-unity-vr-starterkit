using UnityEngine;
using UnityEngine.UI;

public class MainMenuScript : MonoBehaviour
{
    public GameObject menuUI;
    public XRRigController xrRigController;
    public Image image;
    public Sprite thirdPersonSprite;
    public Sprite firstPersonSprite;
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
    public void UpdatePersonModeSprite()
    {
        if (xrRigController == null)
            return;
        if (xrRigController.isThirdPerson)
        {
            image.sprite = thirdPersonSprite;
        }
        else
        {
            image.sprite = firstPersonSprite;
        }
    }
}
