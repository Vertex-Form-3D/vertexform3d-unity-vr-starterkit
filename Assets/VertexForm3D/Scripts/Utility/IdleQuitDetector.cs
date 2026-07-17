using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using VertexFormCore;

/// <summary>
/// Local AFK handling: after no input for <see cref="idleTimeout"/>, either quits the app
/// (leaving the Fusion/Photon session first so others stop seeing/hearing them) or returns
/// the user to the home scene. Quit is supported on every platform: Android/Quest APK and
/// PC Standalone use Application.Quit(); WebGL additionally calls a browser hook that closes
/// the tab when allowed, or replaces the page with a "Session ended / Rejoin" screen.
///
/// Use <see cref="enableOnWebGL"/>, <see cref="enableOnAndroidVR"/>, and <see cref="enableOnDesktop"/>
/// to turn idle handling on or off per platform (all enabled by default).
///
/// Activity is detected automatically on every platform — any button press on any device (keyboard,
/// mouse, gamepad, XR controllers, touch), pointer movement, and VR headset presence / head motion —
/// so no Input Action assets need to be wired. <see cref="actionAssets"/> is an optional extra source.
/// Call <see cref="NotifyUserActivity"/> from WebGL bridges or other custom input paths if needed.
/// </summary>
public class IdleQuitDetector : MonoBehaviour
{
    public enum IdleTimeoutAction
    {
        /// <summary>Leave the multiplayer room (voice/network stop) and load the home scene.</summary>
        ReturnToHomeScene = 0,
        /// <summary>Leave the room first (optional) then close the application on every platform. On WebGL the browser tab is closed when allowed, otherwise a "Session ended" screen replaces the page.</summary>
        QuitApplication = 1,
    }

    [Tooltip("Optional extra Input System assets whose actions also reset the AFK timer. Device-wide button presses, pointer motion and VR headset presence are already detected automatically.")]
    public InputActionAsset[] actionAssets;

    [Header("Platform Enable")]
    [Tooltip("Run AFK idle handling on WebGL builds.")]
    public bool enableOnWebGL = true;

    [Tooltip("Run AFK idle handling on Android / Quest VR builds.")]
    public bool enableOnAndroidVR = true;

    [Tooltip("Run AFK idle handling on Desktop standalone builds (Windows, Mac, Linux). Also applies in the Unity Editor.")]
    public bool enableOnDesktop = true;

    [Header("Idle Settings")]
    [Tooltip("Time in seconds of inactivity before considering the user idle")]
    public float idleTimeout = 600f;

    [Tooltip("What to do once the user is idle.")]
    public IdleTimeoutAction idleAction = IdleTimeoutAction.QuitApplication;

    [Tooltip("If true, the AFK timer only runs while connected to a multiplayer room (where others can see/hear you). Outside a room the timer stays at zero.")]
    public bool onlyCountWhenInRoom = true;

    [Tooltip("QuitApplication only: disconnects Fusion before exit so other clients see you leave and voice/network stop.")]
    public bool leaveNetworkSessionFirst = true;

    [Tooltip("Max seconds to wait for Fusion shutdown before forcing exit anyway.")]
    public float maxWaitForNetworkLeave = 20f;

    [Tooltip("WebGL + QuitApplication: message shown on the \"Session ended\" page when the browser refuses to close the tab (most tabs the user opened themselves).")]
    public string webGLSessionEndedMessage = "You were disconnected because you were inactive for too long.";

    // PUBLIC READ-ONLY PROPERTIES

    // True when the user has been idle for the full idleTimeout
    public bool IsIdle { get; private set; }

    // Current idle time in seconds (0 to idleTimeout, then stays at idleTimeout)
    public float CurrentIdleTime { get; private set; }

    // Normalized idle progress (0 to 1) - useful for UI progress bars
    public float IdleProgress => Mathf.Clamp01(CurrentIdleTime / idleTimeout);

    // Optional events
    public UnityEvent<bool> onIdleStateChanged;     // Called when IsIdle changes
    public UnityEvent<float> onIdleTimeUpdated;    // Called every frame with current idle time

    private float lastActivityRealtime;
    private bool actionStarted;
    private IDisposable anyButtonPressSubscription;
    private Vector3 lastHeadPosition;
    private bool hasLastHeadPosition;

    // Mouse movement below this many pixels per frame is treated as noise, not activity.
    private const float PointerMoveThresholdPixels = 2f;
    // Head movement below this (meters per frame) is treated as a headset resting on a desk.
    private const float HeadMoveThresholdMeters = 0.005f;

    private void OnEnable()
    {
        actionStarted = false;
        IsIdle = false;
        ResetTimer();

        // Catch-all: any button on any connected device (keyboard, mouse, gamepad,
        // XR controllers, touchscreen presses) resets the timer without wiring assets.
        anyButtonPressSubscription = InputSystem.onAnyButtonPress.Call(_ => ResetTimer());

        if (actionAssets == null)
            return;

        foreach (var asset in actionAssets)
        {
            if (asset != null)
            {
                foreach (var actionMap in asset.actionMaps)
                {
                    foreach (var action in actionMap.actions)
                    {
                        action.performed += OnAnyInputPerformed;
                    }
                }
            }
        }
    }

    private void OnDisable()
    {
        anyButtonPressSubscription?.Dispose();
        anyButtonPressSubscription = null;

        if (actionAssets == null)
            return;

        foreach (var asset in actionAssets)
        {
            if (asset != null)
            {
                foreach (var actionMap in asset.actionMaps)
                {
                    foreach (var action in actionMap.actions)
                    {
                        action.performed -= OnAnyInputPerformed;
                    }
                }
            }
        }
    }

    /// <summary>Call from custom controls (e.g. WebGL JS bridge) so AFK timer resets.</summary>
    public void NotifyUserActivity()
    {
        ResetTimer();
    }

    private void OnAnyInputPerformed(InputAction.CallbackContext context)
    {
        ResetTimer();
    }

    private void ResetTimer()
    {
        lastActivityRealtime = Time.realtimeSinceStartup;

        if (IsIdle)
        {
            IsIdle = false;
            onIdleStateChanged?.Invoke(false);
        }
    }

    private static bool IsInRoom =>
        RoomManager.Instance != null && RoomManager.Instance.IsRunnerBusy;

    /// <summary>
    /// True when idle handling is enabled for the platform this build is running on.
    /// </summary>
    public bool IsEnabledOnCurrentPlatform
    {
        get
        {
#if UNITY_EDITOR
            return enableOnDesktop;
#elif UNITY_WEBGL
            return enableOnWebGL;
#elif UNITY_ANDROID
            return enableOnAndroidVR;
#else
            return enableOnDesktop;
#endif
        }
    }

    private void Update()
    {
        if (actionStarted)
            return;

        // Platform toggle off: keep timer at zero and skip idle actions.
        if (!IsEnabledOnCurrentPlatform)
        {
            ResetTimer();
            CurrentIdleTime = 0f;
            return;
        }

        if (DetectPolledActivity())
            ResetTimer();

        // Outside a room nobody can hear/see this user, so optionally pause the timer there.
        if (onlyCountWhenInRoom && !IsInRoom)
            ResetTimer();

        CurrentIdleTime = Time.realtimeSinceStartup - lastActivityRealtime;

        // Notify anyone listening about the updated idle time
        onIdleTimeUpdated?.Invoke(CurrentIdleTime);

        bool currentlyIdle = CurrentIdleTime >= idleTimeout;

        if (currentlyIdle != IsIdle)
        {
            IsIdle = currentlyIdle;
            onIdleStateChanged?.Invoke(IsIdle);

            if (IsIdle)
            {
                actionStarted = true;

                if (idleAction == IdleTimeoutAction.ReturnToHomeScene)
                {
                    Debug.LogWarning($"[IdleQuitDetector] User idle for {idleTimeout} seconds. Returning to home scene...");
                    StartCoroutine(CoReturnToHome());
                }
                else
                {
                    Debug.LogWarning($"[IdleQuitDetector] User idle for {idleTimeout} seconds. Quitting application...");
                    if (leaveNetworkSessionFirst)
                        StartCoroutine(CoExitAfterNetworkCleanup());
                    else
                        DoPlatformExit();
                }
            }
        }
    }

    /// <summary>
    /// Activity sources that are not button presses: pointer/touch movement and, in VR,
    /// headset presence (proximity sensor) or head motion — a worn headset never sits
    /// perfectly still, while one left on a desk does.
    /// </summary>
    private bool DetectPolledActivity()
    {
        var mouse = Mouse.current;
        if (mouse != null &&
            mouse.delta.ReadValue().sqrMagnitude > PointerMoveThresholdPixels * PointerMoveThresholdPixels)
            return true;

        var touchscreen = Touchscreen.current;
        if (touchscreen != null && touchscreen.primaryTouch.press.isPressed)
            return true;

        var head = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.Head);
        if (head.isValid)
        {
            if (head.TryGetFeatureValue(UnityEngine.XR.CommonUsages.userPresence, out bool present) && present)
                return true;

            if (head.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out Vector3 headPosition))
            {
                bool moved = hasLastHeadPosition &&
                             (headPosition - lastHeadPosition).sqrMagnitude >
                             HeadMoveThresholdMeters * HeadMoveThresholdMeters;
                lastHeadPosition = headPosition;
                hasLastHeadPosition = true;
                if (moved)
                    return true;
            }
        }

        return false;
    }

    private IEnumerator CoReturnToHome()
    {
        if (!IsInRoom)
        {
            // Nothing to leave and nowhere to go back to — just re-arm.
            Debug.Log("[IdleQuitDetector] Idle outside a room with ReturnToHomeScene action — nothing to do.");
            ResetTimer();
            actionStarted = false;
            yield break;
        }

        if (VirtualRoomManager.Instance != null)
        {
            // Same flow as the in-app Home button / disconnect popup: leaves Fusion
            // (voice stops for everyone else) then loads the home scene.
            VirtualRoomManager.Instance.LeaveRoomAndLoadHomeScene();
        }
        else
        {
            Debug.LogWarning("[IdleQuitDetector] VirtualRoomManager missing — leaving room and loading home scene directly.");
            if (RoomManager.Instance != null)
            {
                RoomManager.Instance.LeaveRoom();
                yield return RoomManager.Instance.WaitForRunnerIdle(maxWaitForNetworkLeave);
            }
            SceneManager.LoadScene(1);
        }

        // Re-arm once the room has been left so the detector works again next session.
        float waited = 0f;
        while (waited < maxWaitForNetworkLeave && IsInRoom)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        ResetTimer();
        actionStarted = false;
    }

    private IEnumerator CoExitAfterNetworkCleanup()
    {
        if (RoomManager.Instance != null &&
            RoomManager.Instance.Runner != null &&
            RoomManager.Instance.Runner.IsClient)
        {
            RoomManager.Instance.LeaveRoom();
        }

        float waited = 0f;
        while (waited < maxWaitForNetworkLeave &&
               RoomManager.Instance != null &&
               RoomManager.Instance.Runner != null &&
               RoomManager.Instance.Runner.IsClient)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        DoPlatformExit();
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void VF3D_CloseWindowOrShowEndScreen(string message);
#endif

    private void DoPlatformExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
        // Ask the browser to close the tab (or show the end screen), then shut Unity down.
        try
        {
            VF3D_CloseWindowOrShowEndScreen(webGLSessionEndedMessage);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[IdleQuitDetector] Browser close hook unavailable: {e.Message}");
        }
        Application.Quit();
#else
        // Android/Quest APK, PC/Mac/Linux Standalone: closes the app and drops the
        // Photon/Fusion connection (session leave already ran if leaveNetworkSessionFirst).
        Application.Quit();
#endif
    }
}
