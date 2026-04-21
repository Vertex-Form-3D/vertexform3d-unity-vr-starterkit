using UnityEngine;

public class DesktopObjectDisable : MonoBehaviour
{
    void Awake()
    {
        if (ProjectManager.instance.platforms.IsVrStylePlatform())
        {
            gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(true);
        }
    }
}
