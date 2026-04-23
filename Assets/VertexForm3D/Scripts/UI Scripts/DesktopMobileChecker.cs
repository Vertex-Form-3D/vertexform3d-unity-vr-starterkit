using UnityEngine;

public class DesktopMobileChecker : MonoBehaviour
{
    void Start()
    {
        if (ProjectManager.instance.platforms.IsDesktopStylePlatform() && !ProjectManager.instance.platforms.IsVrStylePlatform())
        {
            gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
