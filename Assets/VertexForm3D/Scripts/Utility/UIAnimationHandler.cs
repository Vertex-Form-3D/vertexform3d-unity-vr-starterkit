#if UNITY_EDITOR
using UnityEditor;
#endif
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum UIAnimationType
{
    position,
    Fade,
    Color
}

public enum UIType
{
    Image,
    RawImage,
    Text
}

public class UIAnimationHandler : MonoBehaviour
{
    public bool animateInStart = true; // Whether to start the animation on awake
    [Header("Animation Targets")]
    public UIAnimationType animationType = UIAnimationType.position; // Type of animation to perform
    public UIType uiType = UIType.Image; // Type of UI element to animate (Image, RawImage, Text)
    [SerializeField] private Image image;
    [SerializeField] private RawImage rawImage;
    [SerializeField] private TMP_Text text;
    [SerializeField] private RectTransform uiElement; // The UI element to animate


    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float targetAlpha = 0f;

    [Header("Color Settings")]
    [SerializeField] private Color targetColor = Color.white;
    [SerializeField] private float colorDuration = 1f;

    [Header("Position Settings")]
    [SerializeField] private Vector2 targetAnchorPos = new Vector2(100f, 100f);
    [SerializeField] private float positionDuration = 1f;

    [Header("Loop Settings")]
    [SerializeField] private bool loopAnimations = true;
    [SerializeField] private LoopType loopType = LoopType.Yoyo;

    private Sequence animationSequence;
    private Vector2 initialAnchorPos;
    private float initialImageAlpha;
    private float initialRawImageAlpha;
    private float initialTextAlpha;
    private Color initialImageColor;
    private Color initialRawImageColor;
    private Color initialTextColor;

    private void Awake()
    {
        if (animateInStart)
        {
            StartAnimation();
        }
    }

    private void Start()
    {
        SetInitialValue();
    }

    public void SetInitialValue()
    {
        initialAnchorPos = uiElement.anchoredPosition;
        initialImageAlpha = image != null ? image.color.a : 1f;
        initialRawImageAlpha = rawImage != null ? rawImage.color.a : 1f;
        initialTextAlpha = text != null ? text.color.a : 1f;
        initialImageColor = image != null ? image.color : (text != null ? text.color : Color.white);
        initialRawImageColor = rawImage != null ? rawImage.color : Color.white;
        initialTextColor = text != null ? text.color : Color.white;
    }

    public void StartAnimation()
    {
        // Kill any existing sequence
        StopAnimation();


        // Create a new sequence
        animationSequence = DOTween.Sequence();

        switch (animationType)
        {
            case UIAnimationType.position:
                // Position animation
                animationSequence.Append(uiElement.DOAnchorPos(targetAnchorPos, positionDuration));
                break;
            case UIAnimationType.Fade:
                // Fade animation
                if (image != null)
                {
                    animationSequence.Append(image.DOFade(targetAlpha, fadeDuration));
                }
                if (rawImage != null)
                {
                    animationSequence.Append(rawImage.DOFade(targetAlpha, fadeDuration));
                }
                if (text != null)
                {
                    animationSequence.Append(text.DOFade(targetAlpha, fadeDuration));
                }
                break;
            case UIAnimationType.Color:
                // Color animation
                if (image != null)
                {
                    animationSequence.Append(image.DOColor(targetColor, colorDuration));
                }
                if (rawImage != null)
                {
                    animationSequence.Append(rawImage.DOColor(targetColor, colorDuration));
                }
                if (text != null)
                {
                    animationSequence.Append(text.DOColor(targetColor, colorDuration));
                }
                break;
            default:
                break;
        }

        // Set looping if enabled
        if (loopAnimations)
        {
            animationSequence.SetLoops(-1, loopType);
        }

        // Play the sequence
        animationSequence.Play();
    }

    public void StopAnimation()
    {
        // Kill the sequence if it exists
        if (animationSequence != null)
        {
            animationSequence.Kill();
            animationSequence = null;
        }
    }

    public void ResetAnimation()
    {
        // Stop any running animation
        StopAnimation();

        // Reset to initial values
        if (image != null)
        {
            image.color = new Color(initialImageColor.r, initialImageColor.g, initialImageColor.b, initialImageAlpha);
        }
        if (rawImage != null)
        {
            rawImage.color = new Color(initialRawImageColor.r, initialRawImageColor.g, initialRawImageColor.b, initialRawImageAlpha);
        }
        if (text != null)
        {
            text.color = new Color(initialTextColor.r, initialTextColor.g, initialTextColor.b, initialTextAlpha);
        }

        uiElement.anchoredPosition = initialAnchorPos;
    }

    public void RestartAnimation()
    {
        ResetAnimation();
        StartAnimation();
    }
    private void OnDestroy()
    {
        // Clean up the sequence when the object is destroyed
        StopAnimation();
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(UIAnimationHandler))]
public class UIAnimationHandlerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        UIAnimationHandler handler = (UIAnimationHandler)target;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("animateInStart"));
        // Animation Targets Section
        EditorGUILayout.PropertyField(serializedObject.FindProperty("animationType"));
        EditorGUILayout.Space();

        // Always show uiElement for position animations
        if (handler.animationType == UIAnimationType.position)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("uiElement"));
        }
        else
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("uiType"));
            // Show UI component field based on UIType
            switch (handler.uiType)
            {
                case UIType.Image:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("image"));
                    break;
                case UIType.RawImage:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("rawImage"));
                    break;
                case UIType.Text:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("text"));
                    break;
            }
        }
        // Animate In Start
        EditorGUILayout.Space();

        // Show relevant fields based on Animation Type
        switch (handler.animationType)
        {
            case UIAnimationType.position:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("targetAnchorPos"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("positionDuration"));
                break;
            case UIAnimationType.Fade:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("fadeDuration"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("targetAlpha"));
                break;
            case UIAnimationType.Color:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("targetColor"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("colorDuration"));
                break;
        }

        EditorGUILayout.Space();

        // Loop Settings Section
        EditorGUILayout.PropertyField(serializedObject.FindProperty("loopAnimations"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("loopType"));

        EditorGUILayout.Space();

        // Animation Control Buttons
        EditorGUILayout.LabelField("Animation Controls", EditorStyles.boldLabel);
        if (GUILayout.Button("Start Animation"))
        {
            handler.StartAnimation();
        }

        if (GUILayout.Button("Stop Animation"))
        {
            handler.StopAnimation();
        }

        if (GUILayout.Button("Reset Animation"))
        {
            handler.ResetAnimation();
        }

        if (GUILayout.Button("Restart Animation"))
        {
            handler.RestartAnimation();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
