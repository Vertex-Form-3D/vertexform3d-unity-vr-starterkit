using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Global switch between desktop input (mouse / keyboard / gamepad via Input System) and mobile input
/// (Starter Assets UI canvas for move/look; optional <see cref="ThirdPersonMobileControls"/> for two-finger pinch zoom).
/// Menu hover UX uses <see cref="UseMobileMenuHoverUx"/> (mobile flag and not VR — see <see cref="IsVrPlatform"/>).
/// Set from WebGL page JS (via <see cref="WebGLMobileControlBridge"/>), a menu, or dev tools.
/// WebGL + WebXR: the page may still flag “mobile” from UA; when an <see cref="XRDisplaySubsystem"/> is running,
/// <see cref="UseFlatMobileControls"/> becomes false so controller / headset input is not blocked.
/// </summary>
public static class DesktopMobileControlSettings
{
    private static bool _useMobileControls;
    private static readonly List<XRDisplaySubsystem> XrDisplayScratch = new List<XRDisplaySubsystem>();
    private static int _xrRunningCachedFrame = -1;
    private static bool _xrRunningCached;

    /// <summary>False = desktop controls; true = mobile / touch UI path.</summary>
    public static bool UseMobileControls => _useMobileControls;

    /// <summary>
    /// True while an XR display subsystem is running (immersive WebXR, Quest, PCVR, etc.).
    /// Cached per frame; safe to read from input callbacks.
    /// </summary>
    public static bool IsImmersiveXrPresentationActive
    {
        get
        {
            int f = Time.frameCount;
            if (f == _xrRunningCachedFrame)
                return _xrRunningCached;
            _xrRunningCachedFrame = f;
            XrDisplayScratch.Clear();
            SubsystemManager.GetSubsystems(XrDisplayScratch);
            _xrRunningCached = false;
            foreach (var s in XrDisplayScratch)
            {
                if (s.running)
                {
                    _xrRunningCached = true;
                    break;
                }
            }
            return _xrRunningCached;
        }
    }

    /// <summary>
    /// <see cref="IsImmersiveXrPresentationActive"/> or a tracked / present HMD (WebXR Export sometimes
    /// omits a running <see cref="XRDisplaySubsystem"/> while <see cref="InputDevices"/> still drives the headset).
    /// </summary>
    public static bool IsImmersiveXrOrHeadMountedPresentationActive
    {
        get
        {
            if (IsImmersiveXrPresentationActive)
                return true;

            var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (!head.isValid)
                return false;

            return head.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) && tracked;
        }
    }

    /// <summary>
    /// Use touch UI + touch-only input routing (e.g. block gamepad move in <see cref="XRRigController"/>).
    /// False while immersive XR is active on a VR-style target, so headset/controller input is not blocked.
    /// Non-VR targets (Desktop / WebGPU mobile browser testing) keep mobile controls enabled even if an XR subsystem exists.
    /// </summary>
    public static bool UseFlatMobileControls =>
        _useMobileControls &&
        (!IsImmersiveXrPresentationActive || !IsVrPlatform);

    /// <summary>True when <see cref="ProjectManager"/> reports VR-style target (native VR or WebGPU in a VR shell browser).</summary>
    public static bool IsVrPlatform =>
        ProjectManager.instance != null &&
        ProjectManager.instance.platforms != null &&
        ProjectManager.instance.platforms.IsVrStylePlatform();

    /// <summary>
    /// Touch-style menu hover (keep hover roots visible for raycasts). Same as mobile controls but never in VR.
    /// </summary>
    public static bool UseMobileMenuHoverUx =>
        _useMobileControls && !(ProjectManager.instance.platforms.platformChoice == platform.Desktop) && !IsVrPlatform && !IsImmersiveXrPresentationActive;

    /// <summary>
    /// Mobile + at least two touches: treat as pinch (zoom), not look. Blocks touch axis / virtual look so
    /// <see cref="Touchscreen"/> primary-touch delta does not orbit or FPS-rotate while pinching.
    /// </summary>
    public static bool SuppressLookWhileMultiTouch =>
        UseFlatMobileControls && Input.touchCount >= 2;

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
