using UnityEngine;

/// <summary>
/// Enables this GameObject only on the selected platform kinds (Desktop, Mobile, VR, WebXR).
/// Resolves the runtime kind from <see cref="ProjectManager.instance"/>'s <see cref="Platforms"/>
/// (both <see cref="platform"/> and <see cref="WebGpuBrowserKind"/>) and re-evaluates when
/// <see cref="WebGLMobileControlBridge.WebGlRuntimePlatformChoiceApplied"/> fires so WebGL builds
/// that learn their platform from the host page after load still gate correctly.
/// On unmatched platforms the GameObject is disabled or destroyed depending on <see cref="actionWhenNotMatched"/>.
/// </summary>
[DisallowMultipleComponent]
public class PlatformGameObjectActivator : MonoBehaviour
{
    public enum PlatformKind
    {
        /// <summary>Native standalone Desktop player or WebGPU desktop browser.</summary>
        Desktop,
        /// <summary>WebGPU build running in a mobile browser (Android Chrome, etc.).</summary>
        Mobile,
        /// <summary>Native VR player (Quest / Quest Link).</summary>
        DesktopVR,
        /// <summary>WebGPU build running inside an immersive WebXR browser shell.</summary>
        WebXR
    }

    public enum UnmatchedAction
    {
        Disable,
        Destroy
    }

    [Tooltip("GameObject stays active only when the runtime matches one of these.")]
    [SerializeField]
    private PlatformKind[] allowedPlatforms = new[]
    {
        PlatformKind.Desktop,
        PlatformKind.Mobile,
        PlatformKind.DesktopVR,
        PlatformKind.WebXR
    };

    [Tooltip("What to do when the current platform is not in the allowed list.")]
    [SerializeField] private UnmatchedAction actionWhenNotMatched = UnmatchedAction.Disable;

    private bool _destroyed;

    private void OnEnable()
    {
        WebGLMobileControlBridge.WebGlRuntimePlatformChoiceApplied += OnWebGlPlatformApplied;
        Debug.Log($"[{nameof(PlatformGameObjectActivator)}] OnEnable on '{name}'. Allowed=[{FormatAllowed()}] UnmatchedAction={actionWhenNotMatched}", this);
        Evaluate("OnEnable");
    }

    private void OnDisable()
    {
        WebGLMobileControlBridge.WebGlRuntimePlatformChoiceApplied -= OnWebGlPlatformApplied;
    }

    private void OnWebGlPlatformApplied(platform p)
    {
        Debug.Log($"[{nameof(PlatformGameObjectActivator)}] WebGlRuntimePlatformChoiceApplied → {p} on '{name}'. Re-evaluating.", this);
        Evaluate("WebGlRuntimePlatformChoiceApplied");
    }

    private void Evaluate(string reason)
    {
        if (_destroyed)
        {
            Debug.Log($"[{nameof(PlatformGameObjectActivator)}] Skip evaluate (already destroyed). reason={reason}", this);
            return;
        }

        Platforms pl = ProjectManager.instance != null ? ProjectManager.instance.platforms : null;
        if (pl == null)
        {
            // ProjectManager has DefaultExecutionOrder -100000 so it normally exists first; if not, leave active and try again next enable.
            Debug.LogWarning($"[{nameof(PlatformGameObjectActivator)}] ProjectManager.instance.platforms is null on '{name}' (reason={reason}); leaving GameObject active.", this);
            return;
        }

        PlatformKind current = ResolveCurrent(pl);
        bool allowed = IsAllowed(current);
        Debug.Log($"[{nameof(PlatformGameObjectActivator)}] Evaluate '{name}' reason={reason} platformChoice={pl.platformChoice} webGpuBrowserKind={pl.webGpuBrowserKind} useMobileControls={DesktopMobileControlSettings.UseMobileControls} resolved={current} allowed={allowed}", this);

        if (allowed)
        {
            if (!gameObject.activeSelf)
            {
                Debug.Log($"[{nameof(PlatformGameObjectActivator)}] Re-enabling '{name}' (matched {current}).", this);
                gameObject.SetActive(true);
            }
            return;
        }

        if (actionWhenNotMatched == UnmatchedAction.Destroy)
        {
            Debug.Log($"[{nameof(PlatformGameObjectActivator)}] Destroying '{name}' (resolved={current} not in allowed list).", this);
            _destroyed = true;
            Destroy(gameObject);
        }
        else
        {
            Debug.Log($"[{nameof(PlatformGameObjectActivator)}] Disabling '{name}' (resolved={current} not in allowed list).", this);
            gameObject.SetActive(false);
        }
    }

    private string FormatAllowed()
    {
        if (allowedPlatforms == null || allowedPlatforms.Length == 0) return "(none)";
        return string.Join(",", allowedPlatforms);
    }

    private bool IsAllowed(PlatformKind current)
    {
        if (allowedPlatforms == null) return false;
        for (int i = 0; i < allowedPlatforms.Length; i++)
            if (allowedPlatforms[i] == current) return true;
        return false;
    }

    /// <summary>Maps (<see cref="platform"/>, <see cref="WebGpuBrowserKind"/>) into a single <see cref="PlatformKind"/>.</summary>
    public static PlatformKind ResolveCurrent(Platforms p)
    {
        switch (p.platformChoice)
        {
            case platform.VR:
                return PlatformKind.DesktopVR;
            case platform.Desktop:
                return PlatformKind.Desktop;
            case platform.WebGPU:
                switch (p.webGpuBrowserKind)
                {
                    case WebGpuBrowserKind.WebXRBrowser:
                        return PlatformKind.WebXR;
                    case WebGpuBrowserKind.MobileBrowser:
                        return PlatformKind.Mobile;
                    case WebGpuBrowserKind.DesktopBrowser:
                        return PlatformKind.Desktop;
                    case WebGpuBrowserKind.None:
                    default:
                        // Before the WebGL page calls SendMessage, fall back to the mobile-controls hint.
                        return DesktopMobileControlSettings.UseMobileControls
                            ? PlatformKind.Mobile
                            : PlatformKind.Desktop;
                }
            default:
                return PlatformKind.Desktop;
        }
    }
}
