using UnityEngine;

public class DesktopObjectDisable : MonoBehaviour
{
    void Awake()
    {
        if (ProjectManager.instance.platforms.platformChoice == platform.VR)
        {
            gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(true);
        }
    }
}
