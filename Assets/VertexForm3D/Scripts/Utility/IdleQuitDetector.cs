using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using VertexFormCore;

/// <summary>
/// Local AFK handling: after no input for <see cref="idleTimeout"/>, leaves the Fusion session (optional)
/// then exits the app or reloads a scene. Wire <see cref="actionAssets"/> to every Input System asset you use;
/// call <see cref="NotifyUserActivity"/> from WebGL bridges, UI-only paths, or other non–Input System sources.
/// </summary>
public class IdleQuitDetector : MonoBehaviour
{
    public InputActionAsset[] actionAssets;
    [Tooltip("Time in seconds of inactivity before considering the user idle and quitting")]
    public float idleTimeout = 600f;

    [Tooltip("If true, disconnects Fusion before exit so other clients see you leave and voice/network stop.")]
    public bool leaveNetworkSessionFirst = true;

    [Tooltip("Max seconds to wait for Fusion shutdown before forcing exit anyway.")]
    public float maxWaitForNetworkLeave = 20f;

    [Tooltip("WebGL: if true, loads build index 0 after quit (quit often no-ops in a normal browser tab). If false, only Application.Quit() — pair with a WebGL template that closes the tab when the player quits.")]
    public bool webGLLoadFirstSceneOnIdle = true;

    // PUBLIC READ-ONLY PROPERTIES

    // True when the user has been idle for the full idleTimeout
    public bool IsIdle;

    // Current idle time in seconds (0 to idleTimeout, then stays at idleTimeout)
    public float CurrentIdleTime;

    // Normalized idle progress (0 to 1) - useful for UI progress bars
    public float IdleProgress => Mathf.Clamp01(CurrentIdleTime / idleTimeout);

    // Optional events
    public UnityEvent<bool> onIdleStateChanged;     // Called when IsIdle changes
    public UnityEvent<float> onIdleTimeUpdated;    // Called every frame with current idle time

    private float lastActivityRealtime;
    private bool exitStarted;

    private void OnEnable()
    {
        exitStarted = false;
        ResetTimer();
        IsIdle = false;

        // Subscribe to all input actions
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

    private void Update()
    {
        if (exitStarted)
            return;

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
                exitStarted = true;
                Debug.Log($"User idle for {idleTimeout} seconds. Quitting application...");
                if (leaveNetworkSessionFirst)
                    StartCoroutine(CoExitAfterNetworkCleanup());
                else
                    DoPlatformExit();
            }
        }
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

    private void DoPlatformExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
        Application.Quit();
        if (webGLLoadFirstSceneOnIdle && SceneManager.sceneCount > 0)
            SceneManager.LoadScene(0);
#else
        Application.Quit();
#endif
    }
}