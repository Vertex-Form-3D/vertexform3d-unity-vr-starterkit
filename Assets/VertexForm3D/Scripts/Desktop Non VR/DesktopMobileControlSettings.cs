using System;
using UnityEngine;

/// <summary>
/// Global switch between desktop input (mouse / keyboard / gamepad via Input System) and mobile input
/// (Starter Assets UI canvas for move/look; optional <see cref="ThirdPersonMobileControls"/> for two-finger pinch zoom).
/// Menu hover UX uses <see cref="UseMobileMenuHoverUx"/> (mobile flag and not VR — see <see cref="IsVrPlatform"/>).
/// Set from WebGL page JS (via <see cref="WebGLMobileControlBridge"/>), a menu, or dev tools.
/// </summary>
public static class DesktopMobileControlSettings
{
    private static bool _useMobileControls;

    /// <summary>False = desktop controls; true = mobile / touch UI path.</summary>
    public static bool UseMobileControls => _useMobileControls;

    /// <summary>True when <see cref="ProjectManager"/> reports <c>platform.VR</c> (Quest, etc.).</summary>
    public static bool IsVrPlatform =>
        ProjectManager.instance != null &&
        ProjectManager.instance.platforms != null &&
        ProjectManager.instance.platforms.platformChoice == platform.VR;

    /// <summary>
    /// Touch-style menu hover (keep hover roots visible for raycasts). Same as mobile controls but never in VR.
    /// </summary>
    public static bool UseMobileMenuHoverUx => _useMobileControls && !IsVrPlatform;

    /// <summary>
    /// Mobile + at least two touches: treat as pinch (zoom), not look. Blocks touch axis / virtual look so
    /// <see cref="Touchscreen"/> primary-touch delta does not orbit or FPS-rotate while pinching.
    /// </summary>
    public static bool SuppressLookWhileMultiTouch =>
        _useMobileControls && Input.touchCount >= 2;

    /// <summary>Fired when <see cref="SetUseMobileControls"/> changes the value.</summary>
    public static event Action<bool> Changed;

    /// <summary>Call from WebGL plugin, <c>SendMessage</c>, or your own loader.</summary>
    public static void SetUseMobileControls(bool useMobile)
    {
        if (_useMobileControls == useMobile)
            return;
        _useMobileControls = useMobile;
        Changed?.Invoke(useMobile);
        Debug.Log($"[DesktopMobileControlSettings] UseMobileControls = {useMobile}");
    }
}
