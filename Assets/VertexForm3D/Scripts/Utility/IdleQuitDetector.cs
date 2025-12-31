using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.Events;
using Fusion;

public class IdleQuitDetector : MonoBehaviour
{
    public InputActionAsset[] actionAssets;
    [Tooltip("Time in seconds of inactivity before considering the user idle and quitting")]
    public float idleTimeout = 600f;

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

    private float lastActivityTime;
    private void Start()
    {

    }
    private void OnEnable()
    {

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

    private void OnAnyInputPerformed(InputAction.CallbackContext context)
    {
        ResetTimer();
    }

    private void ResetTimer()
    {
        lastActivityTime = Time.time;

        if (IsIdle)
        {
            IsIdle = false;
            onIdleStateChanged?.Invoke(false);
        }
    }

    private void Update()
    {
        CurrentIdleTime = Time.time - lastActivityTime;

        // Notify anyone listening about the updated idle time
        onIdleTimeUpdated?.Invoke(CurrentIdleTime);

        bool currentlyIdle = CurrentIdleTime >= idleTimeout;

        if (currentlyIdle != IsIdle)
        {
            IsIdle = currentlyIdle;
            onIdleStateChanged?.Invoke(IsIdle);

            if (IsIdle)
            {
                Debug.Log($"User idle for {idleTimeout} seconds. Quitting application...");
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }
    }
}