using UnityEngine;

/// <summary>
/// WebGL: the <c>VertexFormMobileDetect</c> HTML template calls
/// <c>SendMessage("WebGLInputBridge", "SetMobileControlsFromJs", "true"|"false")</c> after load.
/// On player builds, <see cref="WebGLInputBridgeBootstrap"/> creates this automatically so bootstrap/addressable
/// first scenes do not need a manual GameObject. You can still place one in a scene to configure <see cref="startWithMobileControls"/>.
/// </summary>
public class WebGLMobileControlBridge : MonoBehaviour
{
    [Tooltip("If true, mobile controls are used as soon as this loads (before JS). Usually leave false and set from the page.")]
    [SerializeField]
    private bool startWithMobileControls;

    private void Awake()
    {
        if (startWithMobileControls)
            DesktopMobileControlSettings.SetUseMobileControls(true);
    }

    /// <summary>SendMessage-friendly: pass \"true\" / \"false\" or \"1\" / \"0\".</summary>
    public void SetMobileControlsFromJs(string value)
    {
        if (string.IsNullOrEmpty(value))
            return;
        string v = value.Trim().ToLowerInvariant();
        bool mobile = v == "1" || v == "true" || v == "yes" || v == "on";
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
}

#if UNITY_WEBGL && !UNITY_EDITOR
/// <summary>Ensures <see cref="WebGLMobileControlBridge"/> exists before the host page calls <c>SendMessage</c>.</summary>
internal static class WebGLInputBridgeBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureBridgeExists()
    {
        if (Object.FindFirstObjectByType<WebGLMobileControlBridge>(FindObjectsInactive.Include) != null)
            return;

        var go = new GameObject("WebGLInputBridge");
        go.transform.SetParent(null);
        go.AddComponent<WebGLMobileControlBridge>();
        Object.DontDestroyOnLoad(go);
    }
}
#endif
