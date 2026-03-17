using UnityEngine;

public class DesktopObjectDisable : MonoBehaviour
{
    void Awake()
    {
        if (ProjectManager.instance.platformAndSettings.platformChoice == platform.VR)
        {
            gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(true);
        }
    }
}
