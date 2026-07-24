using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using QuantumVertex;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DefaultExecutionOrder(-50)]
public class VideoPlayerController : MonoBehaviour
{
    [SerializeField] private bool loadVideoFromAddressables = false; // New field for Addressables
    [SerializeField] private string videoKey = ""; // New field for Addressable video key
    public VideoPlayerType PlayerType;
    [SerializeField] private bool isLooping = false;
    [SerializeField] private bool clearSky = true;
    [SerializeField] private bool showUI = true;
    [SerializeField] private float currentTime = 0f;
    [SerializeField] private float totalTime = 0f;
    [SerializeField] private float skipTime = 5f;
    [SerializeField] private Color minDistanceGizmoColor = Color.green; // Color for min distance gizmo
    [SerializeField] private Color maxDistanceGizmoColor = Color.red; // Color for max distance gizmo
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage rawImage; // For rawImage type
    [SerializeField] private Renderer targetRenderer; // For renderer type
    public Image playPauseIconImage;
    public GameObject playButtonScreen;
    [SerializeField] private Button playPauseButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button skipForwardButton;
    [SerializeField] private Button skipBackwardButton;
    [SerializeField] private Slider timeSlider;
    [SerializeField] private TextMeshProUGUI currentTimeText;
    [SerializeField] private TextMeshProUGUI totalTimeText;

    private bool isPlaying = false;
    private bool autoPlayWhenReady;
    private RenderTexture renderTexture;
    Material originalSkyboxMaterial;
    Material videoSkyboxMaterial;
    private AsyncOperationHandle<VideoClip> videoClipHandle;

#if UNITY_WEBGL && !UNITY_EDITOR
    private static readonly bool UseDirectVideoTexture = true;
#else
    private static readonly bool UseDirectVideoTexture = false;
#endif

    void Awake()
    {
        // Capture autoplay intent, then disable Unity's built-in playOnAwake so playback
        // cannot start before render targets / Prepare() are set up (required on WebGL).
        autoPlayWhenReady = videoPlayer.playOnAwake;
        videoPlayer.playOnAwake = false;
    }

    void Start()
    {
        StartCoroutine(InitializeWhenReady());
    }

    IEnumerator InitializeWhenReady()
    {
        originalSkyboxMaterial = RenderSettings.skybox;

        var streamingSource = GetComponent<StreamingVideoSource>();
        if (streamingSource != null)
            yield return streamingSource.WaitUntilReady();

        if (loadVideoFromAddressables && !string.IsNullOrEmpty(videoKey))
        {
            LoadVideoFromAddressables();
            yield break;
        }

        SetupVideoPlayer();
        InitializeVideoPlayer();
    }

    void LoadVideoFromAddressables()
    {
        videoClipHandle = Addressables.LoadAssetAsync<VideoClip>(videoKey);
        videoClipHandle.Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                videoPlayer.clip = handle.Result;
                SetupVideoPlayer();
                InitializeVideoPlayer();
            }
            else
            {
                Debug.LogError($"Failed to load video from Addressables with key: {videoKey}");
            }
        };
    }

    void InitializeVideoPlayer()
    {
        videoPlayer.isLooping = isLooping;

#if UNITY_WEBGL && !UNITY_EDITOR
        // Browsers block autoplay with sound; background login video does not need audio.
        if (videoPlayer.audioTrackCount > 0)
            videoPlayer.SetDirectAudioMute(0, true);
#endif

        videoPlayer.loopPointReached += OnVideoLoop;
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.errorReceived += OnVideoError;

        if (UseDirectVideoTexture)
        {
            videoPlayer.sendFrameReadyEvents = true;
            videoPlayer.frameReady += OnVideoFrameReady;
        }

        if (playPauseButton != null) playPauseButton.onClick.AddListener(PlayOrPauseVideo);
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartVideo);
            restartButton.gameObject.SetActive(false);
        }
        if (skipForwardButton != null) skipForwardButton.onClick.AddListener(SkipForward);
        if (skipBackwardButton != null) skipBackwardButton.onClick.AddListener(SkipBackward);
        if (timeSlider != null) timeSlider.onValueChanged.AddListener(OnSliderValueChanged);

        if (!autoPlayWhenReady)
            isPlaying = false;

        videoPlayer.Prepare();
    }

    void SetupVideoPlayer()
    {
        switch (PlayerType)
        {
            case VideoPlayerType.skybox:
                videoSkyboxMaterial = Resources.Load<Material>("CustomEditor/Video Player/VideoSkybox_Mat");
                if (videoSkyboxMaterial == null)
                {
                    Debug.LogError("VideoSkybox_Mat not found in Resources/CustomEditor!");
                    break;
                }

                if (UseDirectVideoTexture)
                {
                    videoPlayer.renderMode = VideoRenderMode.APIOnly;
                    videoPlayer.targetTexture = null;
                }
                else
                {
                    renderTexture = new RenderTexture(1920, 1080, 24);
                    videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                    videoPlayer.targetTexture = renderTexture;
                    videoSkyboxMaterial.mainTexture = renderTexture;
                }

                RenderSettings.skybox = videoSkyboxMaterial;
                DynamicGI.UpdateEnvironment();
                break;

            case VideoPlayerType.rawImage:
                if (UseDirectVideoTexture)
                {
                    videoPlayer.renderMode = VideoRenderMode.APIOnly;
                    videoPlayer.targetTexture = null;
                }
                else
                {
                    renderTexture = new RenderTexture(1920, 1080, 24);
                    videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                    videoPlayer.targetTexture = renderTexture;
                    if (rawImage != null)
                        rawImage.texture = renderTexture;
                    else
                        Debug.LogError("RawImage component not assigned for rawImage PlayerType!");
                }
                break;

            case VideoPlayerType.renderer:
                if (UseDirectVideoTexture)
                {
                    videoPlayer.renderMode = VideoRenderMode.APIOnly;
                    videoPlayer.targetTexture = null;
                }
                else
                {
                    renderTexture = new RenderTexture(1920, 1080, 24);
                    videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                    videoPlayer.targetTexture = renderTexture;
                    if (targetRenderer != null)
                        targetRenderer.material.mainTexture = renderTexture;
                    else
                        Debug.LogError("Renderer component not assigned for renderer PlayerType!");
                }
                break;
        }
    }

    void ApplyDirectVideoTexture(Texture texture)
    {
        if (texture == null)
            return;

        switch (PlayerType)
        {
            case VideoPlayerType.skybox:
                if (videoSkyboxMaterial != null)
                {
                    videoSkyboxMaterial.mainTexture = texture;
                    RenderSettings.skybox = videoSkyboxMaterial;
                    DynamicGI.UpdateEnvironment();
                }
                break;
            case VideoPlayerType.rawImage:
                if (rawImage != null)
                    rawImage.texture = texture;
                break;
            case VideoPlayerType.renderer:
                if (targetRenderer != null)
                    targetRenderer.material.mainTexture = texture;
                break;
        }
    }

    void OnVideoFrameReady(VideoPlayer source, long frameIdx)
    {
        if (UseDirectVideoTexture)
            ApplyDirectVideoTexture(source.texture);
    }

    void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogError($"[VideoPlayerController] Video error on '{source.url}': {message}", this);
    }

    void Update()
    {
        if (m_VideoJumpPending)
        {
            // We're trying to jump to a new position, but we're checking to make sure the video player is updated to our new jump frame.
            if (m_LastFrameBeforeScrub == videoPlayer.frame)
                return;

            // If the video player has been updated with desired jump frame, reset these values.
            m_LastFrameBeforeScrub = long.MinValue;
            m_VideoJumpPending = false;
        }

        if (!m_IsDragging && !m_VideoJumpPending)
        {
            if (videoPlayer.frameCount > 0)
            {
                var progress = (float)videoPlayer.frame / videoPlayer.frameCount;
                if (timeSlider != null) timeSlider.value = progress;
            }
        }
    }

    void OnDrawGizmos()
    {
        if (videoPlayer == null)
        {
            return;
        }
        // Draw minimum distance sphere
        if (videoPlayer.GetComponent<AudioSource>() != null && videoPlayer.GetComponent<AudioSource>().spatialBlend != 0)
        {
            Gizmos.color = minDistanceGizmoColor;
            Gizmos.DrawWireSphere(transform.position, videoPlayer.GetComponent<AudioSource>().minDistance);

            // Draw maximum distance sphere
            Gizmos.color = maxDistanceGizmoColor;
            Gizmos.DrawWireSphere(transform.position, videoPlayer.GetComponent<AudioSource>().maxDistance);
        }
    }

    void OnVideoPrepared(VideoPlayer source)
    {
        totalTime = (float)source.length;
        if (timeSlider != null)
        {
            timeSlider.minValue = 0f;
            timeSlider.maxValue = 1f;
        }
        UpdateTimeDisplay();

        if (autoPlayWhenReady)
            VideoPlay();
        else if (playPauseIconImage != null)
            playPauseIconImage.sprite = Resources.Load<Sprite>("play");
    }

    public void PlayOrPauseVideo()
    {
        if (isPlaying)
        {
            VideoStop();
        }
        else
        {
            VideoPlay();
        }
    }

    void VideoStop()
    {
        videoPlayer.Pause();
        isPlaying = false;
        if (playPauseIconImage != null) playPauseIconImage.sprite = Resources.Load<Sprite>("play");
        if (PlayerType == VideoPlayerType.skybox)
        {
            if (clearSky)
            {
                RenderSettings.skybox = originalSkyboxMaterial; // Revert to original skybox
                DynamicGI.UpdateEnvironment();
            }
        }
        if (restartButton != null) restartButton.gameObject.SetActive(false); // Hide restart button
    }

    void VideoPlay()
    {
        videoPlayer.Play();
        isPlaying = true;
        if (playPauseIconImage != null) playPauseIconImage.sprite = Resources.Load<Sprite>("pause");
        if (PlayerType == VideoPlayerType.skybox)
        {
            if (UseDirectVideoTexture)
                ApplyDirectVideoTexture(videoPlayer.texture);
            else if (videoSkyboxMaterial != null && renderTexture != null)
            {
                videoSkyboxMaterial.mainTexture = renderTexture;
                RenderSettings.skybox = videoSkyboxMaterial;
                DynamicGI.UpdateEnvironment();
            }
        }
        if (restartButton != null) restartButton.gameObject.SetActive(false);
    }

    void RestartVideo()
    {
        videoPlayer.time = 0;
        videoPlayer.Play();
        isPlaying = true;

        if (playPauseIconImage != null) playPauseIconImage.sprite = Resources.Load<Sprite>("pause");
        if (PlayerType == VideoPlayerType.skybox)
        {
            if (UseDirectVideoTexture)
                ApplyDirectVideoTexture(videoPlayer.texture);
            else if (videoSkyboxMaterial != null && renderTexture != null)
            {
                videoSkyboxMaterial.mainTexture = renderTexture;
                RenderSettings.skybox = videoSkyboxMaterial;
                DynamicGI.UpdateEnvironment();
            }
        }
        if (playButtonScreen != null) playButtonScreen.gameObject.SetActive(true); // Hide restart button
        if (restartButton != null) restartButton.gameObject.SetActive(false); // Hide restart button
    }

    void OnVideoLoop(VideoPlayer source)
    {
        if (isLooping)
        {
            RestartVideo();
        }
        else
        {
            isPlaying = false;
            if (PlayerType == VideoPlayerType.skybox)
            {
                if (clearSky)
                {
                    RenderSettings.skybox = originalSkyboxMaterial; // Revert to original skybox
                    DynamicGI.UpdateEnvironment();
                }
            }
            if (playButtonScreen != null) playButtonScreen.gameObject.SetActive(false); // Show restart button
            if (restartButton != null) restartButton.gameObject.SetActive(true); // Show restart button
        }
    }

    void OnSliderValueChanged(float value)
    {
        float newTime = value * totalTime;
        videoPlayer.time = newTime;
        currentTime = newTime;
        UpdateTimeDisplay();
    }

    void SkipForward()
    {
        if (videoPlayer.isPrepared)
        {
            float newTime = (float)videoPlayer.time + skipTime;
            newTime = Mathf.Clamp(newTime, 0f, totalTime);
            videoPlayer.time = newTime;
            currentTime = newTime;
            UpdateTimeDisplay();
        }
    }

    void SkipBackward()
    {
        if (videoPlayer.isPrepared)
        {
            float newTime = (float)videoPlayer.time - skipTime;
            newTime = Mathf.Clamp(newTime, 0f, totalTime);
            videoPlayer.time = newTime;
            currentTime = newTime;
            UpdateTimeDisplay();
        }
    }

    void UpdateTimeDisplay()
    {
        if (currentTimeText != null) currentTimeText.text = FormatTime(currentTime);
        if (totalTimeText != null) totalTimeText.text = FormatTime(totalTime);
    }

    string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    bool m_IsDragging;
    bool m_VideoJumpPending;
    long m_LastFrameBeforeScrub;

    public void OnPointerDown()
    {
        m_VideoJumpPending = true;
        VideoStop();
        VideoJump();
    }

    public void OnRelease()
    {
        m_IsDragging = false;
        VideoPlay();
        VideoJump();
    }

    public void OnDrag()
    {
        m_IsDragging = true;
        m_VideoJumpPending = true;
    }

    void VideoJump()
    {
        m_VideoJumpPending = true;
        var frame = videoPlayer.frameCount * timeSlider.value;
        m_LastFrameBeforeScrub = videoPlayer.frame;
        videoPlayer.frame = (long)frame;
    }

    public void SetLooping(bool value)
    {
        isLooping = value;
        videoPlayer.isLooping = value;
    }

    public void SetCurrentTime(float time)
    {
        if (videoPlayer.isPrepared)
        {
            time = Mathf.Clamp(time, 0f, totalTime);
            videoPlayer.time = time;
            currentTime = time;
            UpdateTimeDisplay();
        }
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoLoop;
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.errorReceived -= OnVideoError;
            if (UseDirectVideoTexture)
                videoPlayer.frameReady -= OnVideoFrameReady;
        }

        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
        if (videoClipHandle.IsValid())
        {
            Addressables.Release(videoClipHandle);
        }
    }
}

public enum VideoPlayerType
{
    rawImage,
    renderer,
    skybox
}

#if UNITY_EDITOR

[CustomEditor(typeof(VideoPlayerController))]
public class VideoPlayerControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Get the target VideoPlayerController instance
        VideoPlayerController controller = (VideoPlayerController)target;

        // Get serialized properties
        SerializedProperty playerTypeProp = serializedObject.FindProperty("PlayerType");
        SerializedProperty isLoopingProp = serializedObject.FindProperty("isLooping");
        SerializedProperty clearSkyProp = serializedObject.FindProperty("clearSky");
        SerializedProperty showUIProp = serializedObject.FindProperty("showUI");
        SerializedProperty loadVideoFromAddressablesProp = serializedObject.FindProperty("loadVideoFromAddressables"); // New property
        SerializedProperty videoKeyProp = serializedObject.FindProperty("videoKey"); // New property
        SerializedProperty currentTimeProp = serializedObject.FindProperty("currentTime");
        SerializedProperty totalTimeProp = serializedObject.FindProperty("totalTime");
        SerializedProperty skipTimeProp = serializedObject.FindProperty("skipTime");
        SerializedProperty videoPlayerProp = serializedObject.FindProperty("videoPlayer");
        SerializedProperty playPauseIconImageProp = serializedObject.FindProperty("playPauseIconImage");
        SerializedProperty playButtonScreenProp = serializedObject.FindProperty("playButtonScreen");
        SerializedProperty playPauseButtonProp = serializedObject.FindProperty("playPauseButton");
        SerializedProperty restartButtonProp = serializedObject.FindProperty("restartButton");
        SerializedProperty skipForwardButtonProp = serializedObject.FindProperty("skipForwardButton");
        SerializedProperty skipBackwardButtonProp = serializedObject.FindProperty("skipBackwardButton");
        SerializedProperty timeSliderProp = serializedObject.FindProperty("timeSlider");
        SerializedProperty currentTimeTextProp = serializedObject.FindProperty("currentTimeText");
        SerializedProperty totalTimeTextProp = serializedObject.FindProperty("totalTimeText");
        SerializedProperty rawImageProp = serializedObject.FindProperty("rawImage");
        SerializedProperty targetRendererProp = serializedObject.FindProperty("targetRenderer");

        // Update the serialized object
        serializedObject.Update();

        // Draw non-UI fields
        EditorGUILayout.PropertyField(loadVideoFromAddressablesProp); // Draw new field
        if (loadVideoFromAddressablesProp.boolValue)
        {
            EditorGUILayout.PropertyField(videoKeyProp); // Draw videoKey if loadVideoFromAddressables is true
        }
        EditorGUILayout.PropertyField(playerTypeProp);
        EditorGUILayout.PropertyField(isLoopingProp);
        EditorGUILayout.PropertyField(clearSkyProp);
        EditorGUILayout.PropertyField(showUIProp); // Draw showUI toggle
        EditorGUILayout.PropertyField(currentTimeProp);
        EditorGUILayout.PropertyField(totalTimeProp);
        EditorGUILayout.PropertyField(skipTimeProp);
        EditorGUILayout.PropertyField(videoPlayerProp);

        // Show conditional fields based on PlayerType
        switch ((VideoPlayerType)playerTypeProp.enumValueIndex)
        {
            case VideoPlayerType.rawImage:
                EditorGUILayout.PropertyField(rawImageProp, new GUIContent("Raw Image"));
                break;

            case VideoPlayerType.renderer:
                EditorGUILayout.PropertyField(targetRendererProp, new GUIContent("Target Renderer"));
                break;

            case VideoPlayerType.skybox:
                // No additional fields to show for skybox
                break;
        }

        // Conditionally draw UI-related fields based on showUI
        if (showUIProp.boolValue)
        {
            EditorGUILayout.PropertyField(playPauseIconImageProp);
            EditorGUILayout.PropertyField(playButtonScreenProp);
            EditorGUILayout.PropertyField(playPauseButtonProp);
            EditorGUILayout.PropertyField(restartButtonProp);
            EditorGUILayout.PropertyField(skipForwardButtonProp);
            EditorGUILayout.PropertyField(skipBackwardButtonProp);
            EditorGUILayout.PropertyField(timeSliderProp);
            EditorGUILayout.PropertyField(currentTimeTextProp);
            EditorGUILayout.PropertyField(totalTimeTextProp);
        }

        // Apply any changes to the serialized object
        serializedObject.ApplyModifiedProperties();
    }
}
#endif