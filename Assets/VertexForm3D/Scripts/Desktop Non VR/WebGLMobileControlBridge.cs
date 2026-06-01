using System;
using UnityEngine;

/// <summary>
/// WebGL: the <c>VertexFormMobileDetect</c> HTML template calls
/// <c>SendMessage("WebGLInputBridge", "SetMobileControlsFromJs", "true"|"false")</c> after load.
/// That only records “mobile browser” (UA / touch hints). When the user enters immersive WebXR,
/// <see cref="DesktopMobileControlSettings.UseFlatMobileControls"/> turns off automatically so on-screen
/// joysticks hide and Input System / XR controller move is not blocked.
/// The template should call <c>SetWebGpuRuntimeFromJs</c> with <c>VrBrowser</c>, <c>DesktopBrowser</c>, or <c>AndroidBrowser</c>
/// so <see cref="Platforms.platformChoice"/> is <see cref="platform.WebGPU"/> and <see cref="Platforms.webGpuBrowserKind"/> matches the host.
/// Legacy <c>SetWebGlPlatformChoiceFromJs</c> (<c>VR</c>/<c>Desktop</c>) still sets native-style <see cref="platform"/> only and clears web browser kind.
/// Fusion reads platform when the local player spawns — send messages as early as possible after load (before joining a room) so the networked
/// <see cref="VertexFormCore.PlayerNetworkSetup.Platform"/> and <see cref="VertexFormCore.PlayerNetworkSetup.WebGpuBrowserKind"/> match. Single-player rigs already ran <c>Awake</c>
/// with the asset default; changing platform here is best-effort for UI and later logic, not a full rig re-init.
/// On player builds, <see cref="WebGLInputBridgeBootstrap"/> creates this automatically so bootstrap/addressable
/// first scenes do not need a manual GameObject. You can still place one in a scene to configure <see cref="startWithMobileControls"/>.
/// </summary>
public class WebGLMobileControlBridge : MonoBehaviour
{

    [Tooltip("If false, ignores SetWebGpuRuntimeFromJs / SetWebGlPlatformChoiceFromJs from the page (keeps Platforms asset choice).")]
    [SerializeField]
    private bool applyWebGlPlatformHintFromPage = true;

    private platform? _pendingWebGlPlatformChoice;
    private WebGpuBrowserKind? _pendingWebGpuBrowserKind;
    private static bool _mobileHintFromPage;
    private static bool _mobileHintReceived;

    /// <summary>Fired when <see cref="Platforms.platformChoice"/> is set from the host page (after <see cref="ProjectManager"/> exists).</summary>
    public static event Action<platform> WebGlRuntimePlatformChoiceApplied;

    private void Update()
    {
        if (_pendingWebGlPlatformChoice.HasValue && TryApplyWebGlPlatformChoice(_pendingWebGlPlatformChoice.Value))
            _pendingWebGlPlatformChoice = null;
        if (_pendingWebGpuBrowserKind.HasValue && TryApplyWebGpuRuntime(_pendingWebGpuBrowserKind.Value))
            _pendingWebGpuBrowserKind = null;
    }

    /// <summary>SendMessage-friendly: pass \"true\" / \"false\" or \"1\" / \"0\".</summary>
    public void SetMobileControlsFromJs(string value)
    {
        if (string.IsNullOrEmpty(value))
            return;
        string v = value.Trim().ToLowerInvariant();
        bool mobile = v == "1" || v == "true" || v == "yes" || v == "on";
        _mobileHintFromPage = mobile;
        _mobileHintReceived = true;
        DesktopMobileControlSettings.SetUseMobileControls(mobile);
        Debug.Log($"[WebGLMobileControlBridge] Template mobile heuristic → UseMobileControls={mobile} (raw: \"{value}\")");
    }

    /// <summary>Optional second SendMessage from index.html with <c>navigator.userAgent</c> for logging.</summary>
    public void LogBrowserUserAgent(string userAgent)
    {
        Debug.Log($"[WebGLMobileControlBridge] navigator.userAgent: {userAgent ?? "(null)"}");
    }

    /// <summary>Optional: short line from JS (e.g. pointer:coarse, maxTouchPoints) for debugging.</summary>
    public void LogBrowserHints(string hints)
    {
        Debug.Log($"[WebGLMobileControlBridge] Browser hints: {hints ?? "(null)"}");
    }

    public void SetMobileControls(bool useMobile) =>
        DesktopMobileControlSettings.SetUseMobileControls(useMobile);

    /// <summary>
    /// SendMessage-friendly: <c>VrBrowser</c>, <c>DesktopBrowser</c>, or <c>AndroidBrowser</c> (also accepts <c>VR</c>/<c>Desktop</c>/<c>Android</c>).
    /// Sets <see cref="platform.WebGPU"/> and <see cref="Platforms.webGpuBrowserKind"/>.
    /// </summary>
    public void SetWebGpuRuntimeFromJs(string value)
    {
        if (!applyWebGlPlatformHintFromPage)
            return;
        if (!TryParseWebGpuBrowserKind(value, out WebGpuBrowserKind kind))
        {
            Debug.LogWarning($"[WebGLMobileControlBridge] SetWebGpuRuntimeFromJs: unrecognized value \"{value}\" (expected VrBrowser/DesktopBrowser/AndroidBrowser).");
            return;
        }

        if (TryApplyWebGpuRuntime(kind))
            _pendingWebGpuBrowserKind = null;
        else
            _pendingWebGpuBrowserKind = kind;
    }

    /// <summary>
    /// SendMessage-friendly: <c>VR</c> / <c>Desktop</c>, or same truthy/falsy strings as mobile (VR = true, Desktop = false).
    /// Updates <see cref="ProjectManager.instance.platforms.platformChoice"/> when the project manager is loaded.
    /// Clears <see cref="Platforms.webGpuBrowserKind"/> when not using <see cref="platform.WebGPU"/>.
    /// </summary>
    public void SetWebGlPlatformChoiceFromJs(string value)
    {
        if (!applyWebGlPlatformHintFromPage)
            return;
        if (!TryParseWebGlPlatformChoice(value, out platform choice))
        {
            Debug.LogWarning($"[WebGLMobileControlBridge] SetWebGlPlatformChoiceFromJs: unrecognized value \"{value}\" (expected VR/Desktop/true/false).");
            return;
        }

        if (TryApplyWebGlPlatformChoice(choice))
            _pendingWebGlPlatformChoice = null;
        else
            _pendingWebGlPlatformChoice = choice;
    }

    private static bool TryParseWebGlPlatformChoice(string raw, out platform choice)
    {
        choice = platform.Desktop;
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        string v = raw.Trim();
        if (string.Equals(v, "VR", StringComparison.OrdinalIgnoreCase))
        {
            choice = platform.VR;
            return true;
        }

        if (string.Equals(v, "Desktop", StringComparison.OrdinalIgnoreCase))
        {
            choice = platform.Desktop;
            return true;
        }

        string low = v.ToLowerInvariant();
        if (low is "1" or "true" or "yes" or "on")
        {
            choice = platform.VR;
            return true;
        }

        if (low is "0" or "false" or "no" or "off")
        {
            choice = platform.Desktop;
            return true;
        }

        return false;
    }

    private static bool TryParseWebGpuBrowserKind(string raw, out WebGpuBrowserKind kind)
    {
        kind = WebGpuBrowserKind.None;
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        string v = raw.Trim();
        if (string.Equals(v, "AndroidBrowser", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(v, "Android", StringComparison.OrdinalIgnoreCase))
        {
            kind = WebGpuBrowserKind.MobileBrowser;
            return true;
        }

        if (string.Equals(v, "DesktopBrowser", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(v, "Desktop", StringComparison.OrdinalIgnoreCase))
        {
            kind = WebGpuBrowserKind.DesktopBrowser;
            return true;
        }

        if (string.Equals(v, "WebXRBrowser", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(v, "VR", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(v, "VrBrowser", StringComparison.OrdinalIgnoreCase))
        {
            kind = WebGpuBrowserKind.WebXRBrowser;
            return true;
        }

        return false;
    }

    private static bool TryApplyWebGlPlatformChoice(platform choice)
    {
        if (ProjectManager.instance == null || ProjectManager.instance.platforms == null)
            return false;

        Platforms pl = ProjectManager.instance.platforms;
        bool clearWeb = choice != platform.WebGPU;
        WebGpuBrowserKind nextWeb = clearWeb ? WebGpuBrowserKind.None : pl.webGpuBrowserKind;
        if (pl.platformChoice != choice || pl.webGpuBrowserKind != nextWeb)
        {
            pl.platformChoice = choice;
            if (clearWeb)
                pl.webGpuBrowserKind = WebGpuBrowserKind.None;
            Debug.Log($"[WebGLMobileControlBridge] WebGL runtime platform choice → {choice}");
            WebGlRuntimePlatformChoiceApplied?.Invoke(choice);
        }
        if (choice != platform.WebGPU)
            DesktopMobileControlSettings.SetUseMobileControls(false);

        return true;
    }

    private static bool TryApplyWebGpuRuntime(WebGpuBrowserKind kind)
    {
        if (ProjectManager.instance == null || ProjectManager.instance.platforms == null)
            return false;

        Platforms pl = ProjectManager.instance.platforms;
        if (pl.platformChoice != platform.WebGPU || pl.webGpuBrowserKind != kind)
        {
            pl.platformChoice = platform.WebGPU;
            pl.webGpuBrowserKind = kind;
            Debug.Log($"[WebGLMobileControlBridge] WebGPU runtime → {kind}");
            WebGlRuntimePlatformChoiceApplied?.Invoke(platform.WebGPU);
        }
        DesktopMobileControlSettings.SetUseMobileControls(
            kind == WebGpuBrowserKind.MobileBrowser || (_mobileHintReceived && _mobileHintFromPage));

        return true;
    }
}

#if UNITY_WEBGL && !UNITY_EDITOR
/// <summary>Ensures <see cref="WebGLMobileControlBridge"/> exists before the host page calls <c>SendMessage</c>.</summary>
internal static class WebGLInputBridgeBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureBridgeExists()
    {
        if (UnityEngine.Object.FindFirstObjectByType<WebGLMobileControlBridge>(FindObjectsInactive.Include) != null)
            return;

        var go = new GameObject("WebGLInputBridge");
        go.transform.SetParent(null);
        go.AddComponent<WebGLMobileControlBridge>();
        UnityEngine.Object.DontDestroyOnLoad(go);
    }
}
#endif
