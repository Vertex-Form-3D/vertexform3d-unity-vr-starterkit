using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(-99999)]
public class EventSystemHandler : MonoBehaviour
{
    public static EventSystemHandler Instance { get; private set; }

    public EventSystem VREventSystem;
    public EventSystem DesktopEventSystem;

    void Awake()
    {
        Instance = this;
        RemoveForeignEventSystems();
        ActivatePlatformEventSystem();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Destroys every <see cref="EventSystem"/> in loaded scenes except the managed VR and Desktop ones.
    /// Call after an addressable (or other additive) scene finishes loading.
    /// </summary>
    public void RemoveForeignEventSystems()
    {
        var allowed = new HashSet<EventSystem>();
        if (VREventSystem != null)
            allowed.Add(VREventSystem);
        if (DesktopEventSystem != null)
            allowed.Add(DesktopEventSystem);

        var allEventSystems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allEventSystems.Length; i++)
        {
            var eventSystem = allEventSystems[i];
            if (eventSystem == null || allowed.Contains(eventSystem))
                continue;

            Destroy(eventSystem.gameObject);
        }

        ActivatePlatformEventSystem();
    }

    void ActivatePlatformEventSystem()
    {
        if (VREventSystem != null)
            VREventSystem.gameObject.SetActive(false);
        if (DesktopEventSystem != null)
            DesktopEventSystem.gameObject.SetActive(false);

        EventSystem active = null;
        if (ProjectManager.instance.platforms.IsVrStylePlatform())
        {
            if (VREventSystem != null)
            {
                VREventSystem.gameObject.SetActive(true);
                active = VREventSystem;
            }
        }
        else
        {
            if (DesktopEventSystem != null)
            {
                DesktopEventSystem.gameObject.SetActive(true);
                active = DesktopEventSystem;
            }
        }

        if (active != null)
            EventSystem.current = active;
    }
}
