using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Use instead of <see cref="EventSystem.IsPointerOverGameObject"/> from Input System callbacks.
/// The built-in API reads last-frame UI state when called during event processing and logs warnings on WebGL/player builds.
/// </summary>
public static class DesktopPointerUIHelper
{
    public static bool IsPointerOverUIThisFrame()
    {
        if (EventSystem.current == null)
            return false;

        Vector2 pos;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            pos = Touchscreen.current.primaryTouch.position.ReadValue();
        else if (Mouse.current != null)
            pos = Mouse.current.position.ReadValue();
        else
            pos = (Vector2)Input.mousePosition;

        var data = new PointerEventData(EventSystem.current) { position = pos };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, results);
        return results.Count > 0;
    }
}
