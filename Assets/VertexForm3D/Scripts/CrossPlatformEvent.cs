using UnityEngine;
using UnityEngine.Events;
[DefaultExecutionOrder(-10000)]
public class CrossPlatformEvent : MonoBehaviour
{
    public UnityEvent onVREvent;
    public UnityEvent onDesktopEvent;
    void Awake()
    {
        if (ProjectManager.instance.platforms.platformChoice == platform.VR)
        {
            if (onVREvent != null)
            {
                onVREvent?.Invoke();
            }
        }
        else
        {
            if (onDesktopEvent != null)
            {
                onDesktopEvent?.Invoke();
            }
        }
    }

}
