using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard;

/// <summary>
/// Routes TMP input between the XR spatial keyboard (<see cref="XRKeyboardDisplay"/>), the mobile soft keyboard,
/// or neither (desktop / PC WebGL — physical keyboard only). Expects <see cref="XRKeyboardDisplay"/> on the same GameObject.
/// </summary>
[DefaultExecutionOrder(100)]
public class VertexFormTMPInputKeyboardPolicy : MonoBehaviour
{
    TMP_InputField _field;
    XRKeyboardDisplay _xrKeyboard;

    platform _lastPlatform = (platform)(-1);
    WebGpuBrowserKind _lastWebKind = (WebGpuBrowserKind)(-1);

    void Awake()
    {
        _field = GetComponent<TMP_InputField>();
        _xrKeyboard = GetComponent<XRKeyboardDisplay>();
    }

    void OnEnable()
    {
        WebGLMobileControlBridge.WebGlRuntimePlatformChoiceApplied += OnWebGlPlatformChoice;
        DesktopMobileControlSettings.Changed += OnMobileControlsChanged;
        if (_field != null)
            _field.onSelect.AddListener(OnTmpSelect);
        InvalidateCache();
    }

    void OnDisable()
    {
        WebGLMobileControlBridge.WebGlRuntimePlatformChoiceApplied -= OnWebGlPlatformChoice;
        DesktopMobileControlSettings.Changed -= OnMobileControlsChanged;
        if (_field != null)
            _field.onSelect.RemoveListener(OnTmpSelect);
    }

    void Start() => TryApply();

    void LateUpdate() => TryApply();

    void OnWebGlPlatformChoice(platform _) => InvalidateCache();

    void OnMobileControlsChanged(bool _) => InvalidateCache();

    void OnTmpSelect(string _) => ApplySoftKeyboardFlagsForCurrentMode();

    void InvalidateCache()
    {
        _lastPlatform = (platform)(-1);
        _lastWebKind = (WebGpuBrowserKind)(-1);
    }

    void TryApply()
    {
        if (_field == null || _xrKeyboard == null)
            return;

        Platforms pl = ProjectManager.instance != null ? ProjectManager.instance.platforms : null;
        if (pl == null)
            return;

        if (pl.platformChoice == _lastPlatform && pl.webGpuBrowserKind == _lastWebKind)
            return;

        _lastPlatform = pl.platformChoice;
        _lastWebKind = pl.webGpuBrowserKind;

        if (pl.KeyboardUsesSpatialXr())
        {
            _xrKeyboard.enabled = true;
            // Spatial keyboard in immersive WebXR / VR: do not let the display auto-close the keyboard
            // when its GameObject is briefly disabled (panel swaps, raycaster toggles, EventSystem
            // deselection from a controller ray pointing at empty space). The default
            // m_HideKeyboardOnDisable=true causes the keyboard to "appear, then hide when pressing
            // anywhere on the screen" in WebXR. The user can still dismiss it via the keyboard's Hide key.
            _xrKeyboard.hideKeyboardOnDisable = false;
            ApplySoftKeyboardFlagsForSpatialVr();
            return;
        }

        _xrKeyboard.enabled = false;

        if (pl.KeyboardUsesMobileSoftKeyboard())
            ApplySoftKeyboardFlagsForMobileWeb();
        else
            ApplySoftKeyboardFlagsForDesktopLike();
    }

    void ApplySoftKeyboardFlagsForCurrentMode()
    {
        if (_field == null)
            return;
        Platforms pl = ProjectManager.instance != null ? ProjectManager.instance.platforms : null;
        if (pl == null)
            return;
        if (pl.KeyboardUsesSpatialXr())
            ApplySoftKeyboardFlagsForSpatialVr();
        else if (pl.KeyboardUsesMobileSoftKeyboard())
            ApplySoftKeyboardFlagsForMobileWeb();
        else
            ApplySoftKeyboardFlagsForDesktopLike();
    }

    void ApplySoftKeyboardFlagsForSpatialVr()
    {
        _field.shouldHideSoftKeyboard = true;
        _field.shouldHideMobileInput = true;
    }

    void ApplySoftKeyboardFlagsForMobileWeb()
    {
        // TMP + WebGL: both flags must allow the browser/OS path; Unity issues note fullscreen + canvas can still block the keyboard — see template (mobile flat avoids true browser fullscreen).
        _field.shouldHideSoftKeyboard = false;
        _field.shouldHideMobileInput = false;
    }

    void ApplySoftKeyboardFlagsForDesktopLike()
    {
        _field.shouldHideSoftKeyboard = true;
        _field.shouldHideMobileInput = true;
    }
}

/// <summary>Adds <see cref="VertexFormTMPInputKeyboardPolicy"/> next to <see cref="XRKeyboardDisplay"/> so prefabs work without re-saving every scene.</summary>
internal static class VertexFormKeyboardPolicyBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureKeyboardPolicies()
    {
        foreach (XRKeyboardDisplay display in Object.FindObjectsByType<XRKeyboardDisplay>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (display == null || display.gameObject.GetComponent<TMP_InputField>() == null)
                continue;
            if (display.gameObject.GetComponent<VertexFormTMPInputKeyboardPolicy>() == null)
                display.gameObject.AddComponent<VertexFormTMPInputKeyboardPolicy>();
        }
    }
}
