using UnityEngine;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(-10000)]
public class EventSystemHandler : MonoBehaviour
{
    public EventSystem VREventSystem;
    public EventSystem DesktopEventSystem;

    void Awake()
    {
        // Disable both first so nothing is "current", then enable only the correct one.
        DesktopEventSystem.gameObject.SetActive(false);
        VREventSystem.gameObject.SetActive(false);

        if (ProjectManager.instance.platforms.platformChoice == platform.VR)
        {
            VREventSystem.gameObject.SetActive(true);
            EventSystem.current = VREventSystem;
        }
        else
        {
            DesktopEventSystem.gameObject.SetActive(true);
            EventSystem.current = DesktopEventSystem;
        }
    }
}
