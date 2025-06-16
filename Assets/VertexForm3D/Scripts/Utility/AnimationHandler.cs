using DG.Tweening;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AnimationHandler : MonoBehaviour
{
    public enum AnimationType
    {
        Position,
        Rotation,
        Scale
    }

    [Header("Animation Settings")]
    [Tooltip("The type of animation to perform: Position, Rotation, or Scale")]
    [SerializeField] private AnimationType animationType = AnimationType.Position;

    [Tooltip("Duration of the animation in seconds")]
    [SerializeField] private float duration = 1f;

    [Tooltip("If true, the end value is relative to the initial transform")]
    [SerializeField] private bool isRelative;

    [Tooltip("Target value for the animation (position, rotation, or scale)")]
    [SerializeField] private Vector3 endValue = new Vector3(0f, 0f, 0f);

    [Tooltip("Easing function to apply to the animation")]
    [SerializeField] private Ease easeType = Ease.Linear;

    [Tooltip("If true, the animation will loop")]
    [SerializeField] private bool isLooping = false;

    [Tooltip("Type of loop: Yoyo (back and forth), Restart, or Incremental")]
    [SerializeField] private LoopType loopType = LoopType.Yoyo;

    [Tooltip("Number of loops (-1 for infinite loops)")]
    [SerializeField] private int loopCount = -1;

    [Tooltip("If true, the animation starts automatically when the GameObject starts")]
    [SerializeField] private bool AnimateInStart = true;

    [Tooltip("Reference to the current active tween animation")]
    private Tween currentTween;

    [Tooltip("Initial position of the GameObject when the script starts")]
    private Vector3 initialPosition;

    [Tooltip("Initial rotation of the GameObject when the script starts")]
    private Quaternion initialRotation;

    [Tooltip("Initial scale of the GameObject when the script starts")]
    private Vector3 initialScale;

    private void Awake()
    {
        // Store initial transforms
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialScale = transform.localScale;
    }

    private void Start()
    {
        if (AnimateInStart)
        {
            StartAnimation();
        }
    }
    private void OnDestroy()
    {
        // Clean up tween
        currentTween?.Kill();
    }

    public void StartAnimation()
    {
        // Kill any existing tween
        currentTween?.Kill();

        // Create new tween based on animation type
        switch (animationType)
        {
            case AnimationType.Position:
                currentTween = transform.DOMove(endValue, duration)
                    .SetEase(easeType)
                    .SetLoops(isLooping ? loopCount : 0, loopType)
                    .SetRelative(isRelative);
                break;

            case AnimationType.Rotation:
                currentTween = transform.DORotate(endValue, duration, RotateMode.FastBeyond360)
                    .SetEase(easeType)
                    .SetLoops(isLooping ? loopCount : 0, loopType)
                    .SetRelative(isRelative);
                break;

            case AnimationType.Scale:
                currentTween = transform.DOScale(endValue, duration)
                    .SetEase(easeType)
                    .SetLoops(isLooping ? loopCount : 0, loopType)
                    .SetRelative(isRelative);
                break;
        }
    }

    public void StopAnimation()
    {
        currentTween?.Pause();
    }

    public void ResetAnimation()
    {
        // Kill current tween
        currentTween?.Kill();

        // Reset to initial transforms
        switch (animationType)
        {
            case AnimationType.Position:
                transform.position = initialPosition;
                break;
            case AnimationType.Rotation:
                transform.rotation = initialRotation;
                break;
            case AnimationType.Scale:
                transform.localScale = initialScale;
                break;
        }
    }

    public void RestartAnimation()
    {
        ResetAnimation();
        StartAnimation();
    }

    // Optional: Method to change animation settings at runtime
    public void SetAnimationSettings(AnimationType type, float newDuration, Vector3 newEndValue,
        Ease newEaseType, bool newIsLooping, LoopType newLoopType, int newLoopCount)
    {
        animationType = type;
        duration = newDuration;
        endValue = newEndValue;
        easeType = newEaseType;
        isLooping = newIsLooping;
        loopType = newLoopType;
        loopCount = newLoopCount;
    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(AnimationHandler))]
public class AnimationScriptEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();

        // Get reference to the AnimationScript
        AnimationHandler animationScript = (AnimationHandler)target;

        // Add space before buttons
        EditorGUILayout.Space();

        // Create a horizontal layout for buttons
        EditorGUILayout.BeginVertical();

        // Start Animation Button
        if (GUILayout.Button("Start Animation"))
        {
            animationScript.StartAnimation();
        }

        // Stop Animation Button
        if (GUILayout.Button("Stop Animation"))
        {
            animationScript.StopAnimation();
        }

        // Reset Animation Button
        if (GUILayout.Button("Reset Animation"))
        {
            animationScript.ResetAnimation();
        }
        
        if (GUILayout.Button("Restart Animation"))
        {
            animationScript.RestartAnimation();
        }

        EditorGUILayout.EndVertical();
    }
}
#endif