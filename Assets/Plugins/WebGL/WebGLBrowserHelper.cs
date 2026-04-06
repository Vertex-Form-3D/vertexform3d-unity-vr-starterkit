using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

public static class WebGLBrowserHelper
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void WebGLBrowser_DisableContextMenu();
    [DllImport("__Internal")] private static extern void WebGLBrowser_RequestFullscreen();
    [DllImport("__Internal")] private static extern void WebGLBrowser_ExitFullscreen();
    [DllImport("__Internal")] private static extern int WebGLBrowser_IsFullscreen();
    [DllImport("__Internal")] private static extern void WebGLBrowser_ResumeAudioContext();

    public static void DisableContextMenu() => WebGLBrowser_DisableContextMenu();
    public static void RequestFullscreen() => WebGLBrowser_RequestFullscreen();
    public static void ExitFullscreen() => WebGLBrowser_ExitFullscreen();
    public static bool IsFullscreen() => WebGLBrowser_IsFullscreen() != 0;
    public static void ResumeAudioContext() => WebGLBrowser_ResumeAudioContext();
#else
    public static void DisableContextMenu() { }
    public static void RequestFullscreen() { }
    public static void ExitFullscreen() { }
    public static bool IsFullscreen() => false;
    public static void ResumeAudioContext() { }
#endif
}
