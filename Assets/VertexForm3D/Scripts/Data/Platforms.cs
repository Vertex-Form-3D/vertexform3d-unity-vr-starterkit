using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Shared rules for <see cref="Platforms"/> and Fusion player setup (platform + optional web kind).</summary>
public static class PlatformPresentation
{
    public static bool IsVrStyle(platform platformChoice, WebGpuBrowserKind webGpuBrowserKind) =>
        platformChoice == platform.VR ||
        (platformChoice == platform.Web && webGpuBrowserKind == WebGpuBrowserKind.WebXRBrowser);

    /// <summary>Flat screen / M&amp;K or touch UI path (includes Web Android and desktop browsers, and Web before JS sets a kind).</summary>
    public static bool IsDesktopStyle(platform platformChoice, WebGpuBrowserKind webGpuBrowserKind)
    {
        if (platformChoice == platform.Desktop)
            return true;
        if (platformChoice == platform.Web)
            return webGpuBrowserKind != WebGpuBrowserKind.WebXRBrowser;
        return false;
    }
}

[CreateAssetMenu(fileName = "Platform and Settings", menuName = "ScriptableObjects/Platform and Settings", order = 1)]
[Icon("Assets/VertexForm3D/UI/vertexform-Logo.png")]
public class Platforms : ScriptableObject
{
    public platform platformChoice = platform.VR;

    [Tooltip("When platform is Web, set from WebGL index.html (SendMessage). In Editor, use this for testing. Ignored for VR/Desktop native targets.")]
    public WebGpuBrowserKind webGpuBrowserKind = WebGpuBrowserKind.None;

    [HideInInspector]
    public List<PlatformSetupGuide> platformGuides = new List<PlatformSetupGuide>();

    public bool IsVrStylePlatform() => PlatformPresentation.IsVrStyle(platformChoice, webGpuBrowserKind);

    public bool IsDesktopStylePlatform() => PlatformPresentation.IsDesktopStyle(platformChoice, webGpuBrowserKind);

    /// <summary>Use the XR Interaction Toolkit spatial / world keyboard with <c>XRKeyboardDisplay</c> (native VR or WebGPU in a VR shell browser).</summary>
    public bool KeyboardUsesSpatialXr() => IsVrStylePlatform();

    /// <summary>
    /// Flat Web (WebGL): use the device/browser soft keyboard (not the XR spatial keyboard).
    /// Uses <see cref="WebGpuBrowserKind.AndroidBrowser"/> and/or the page mobile hint from <see cref="DesktopMobileControlSettings.UseMobileControls"/>
    /// so keyboard routing works before browser kind is applied and on phones that still report <c>DesktopBrowser</c>.
    /// </summary>
    public bool KeyboardUsesMobileSoftKeyboard() =>
        platformChoice == platform.Web &&
        !PlatformPresentation.IsVrStyle(platformChoice, webGpuBrowserKind) &&
        (webGpuBrowserKind == WebGpuBrowserKind.MobileBrowser || DesktopMobileControlSettings.UseMobileControls);

    private void Reset()
    {
        platformGuides = new List<PlatformSetupGuide>
        {
            new PlatformSetupGuide
            {
                title = "Android \u00b7 VR",
                subtitle = "Meta Quest",
                steps = new List<string>
                {
                    "Select <b>VR</b> from the <b>Platform</b> dropdown in Project Data.",
                    "Go to <b>Edit \u2192 Build Profiles</b> and set <b>Android</b> as the active platform."
                }
            },
            new PlatformSetupGuide
            {
                title = "Desktop",
                subtitle = "Windows \u00b7 PC",
                steps = new List<string>
                {
                    "Select <b>Desktop</b> from the <b>Platform</b> dropdown in Project Data.",
                    "Go to <b>Edit \u2192 Build Profiles</b> and set <b>Windows</b> as the active platform."
                }
            },
            new PlatformSetupGuide
            {
                title = "Web",
                subtitle = "Browser (WebGL / WebGPU)",
                steps = new List<string>
                {
                    "Select <b>Web</b> from the <b>Platform</b> dropdown for WebGL / WebGPU builds.",
                    "Go to <b>Edit \u2192 Build Profiles</b> and set <b>WebGL/WebGPU</b> as the active platform.",
                    "The VertexForm template calls Unity with <b>WebXRBrowser</b>, <b>DesktopBrowser</b>, or <b>MobileBrowser</b> so runtime matches Quest shell vs PC vs Android Chrome."
                },
                note = "Native targets keep VR or Desktop or Web "
            },
            new PlatformSetupGuide
            {
                title = "Testing VR on Desktop via Quest Link",
                subtitle = "XR Plug-in Management \u00b7 Windows tab",
                steps = new List<string>
                {
                    "Open <b>Edit \u2192 Project Settings \u2192 XR Plug-in Management</b>",
                    "Select the <b>Windows</b> tab in the XR Plug-in panel.",
                    "Enable <b>Initialize XR on Startup</b>"
                },
                note = "Why this matters: Enabling \u2018Initialize XR on Startup\u2019 ensures your VR experience launches correctly when running the project on desktop through Quest Link \u2014 without this, XR may not initialize at play time."
            }
        };
    }
}

[Serializable]
public class PlatformSetupGuide
{
    public string title;
    public string subtitle;
    public List<string> steps = new List<string>();
    [TextArea(2, 5)]
    public string note;
}
