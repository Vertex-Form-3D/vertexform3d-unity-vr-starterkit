using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Fusion;
#if !UNITY_WEBGL
using UnityEngine.XR;
#endif
using UnityEngine.XR.Interaction.Toolkit;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace VertexFormCore
{
    public class SelfieHandler : NetworkBehaviour
    {
        [Header("Selfie Camera Settings")]
        public Camera selfieCam;
        public RawImage selfieImage;
        [Networked] public bool CanTakeSelfie { get; set; } = true;

        public enum SelfieQuality
        {
            Low = 912,      // ~912x512  (16:9)
            Medium = 1820,  // ~1820x1024 (16:9)
            High = 2730,    // ~2730x1536 (close to 2K 16:9)
            Ultra = 3840    // 3840x2160 (4K 16:9)
        }

        [SerializeField] private SelfieQuality selfieQuality = SelfieQuality.Medium;
        [SerializeField] private RenderTexture selfieCamTexture;

        public AudioSource audioSource;

        [Header("Multiplayer Settings")]
        [SerializeField] private bool isSelfieStick = true;
        [Networked] public int OwnerPlayerId { get; set; }

        private NetworkObject networkObject;
        private bool primaryButtonPressed = false;
        private bool isInitialized = false;

        // 16:9 Aspect Ratio
        private const float ASPECT_RATIO = 16f / 9f;

        void Awake()
        {
            networkObject = GetComponent<NetworkObject>();
        }

        public override void Spawned()
        {
            base.Spawned();
            InitializeForPlayer(Object.InputAuthority);
        }

        public void setCanSelfieStatus(bool canTake)
        {
            CanTakeSelfie = canTake;
        }

        public void InitializeForPlayer(PlayerRef playerId)
        {
            OwnerPlayerId = playerId.PlayerId;
            CanTakeSelfie = true;


            if (Object.HasInputAuthority)
            {
                InitializeComponents();
            }
            else
            {
                CreatePreviewRenderTexture();
            }
        }

        private void InitializeComponents()
        {
            if (isInitialized) return;

            // Create Selfie Camera if missing
            if (selfieCam == null)
            {
                GameObject camObj = new GameObject("SelfieCamera_" + Object.InputAuthority);
                camObj.transform.SetParent(transform);
                selfieCam = camObj.AddComponent<Camera>();
                selfieCam.cullingMask = ~(1 << LayerMask.NameToLayer("UI"));
                selfieCam.clearFlags = CameraClearFlags.SolidColor;
                selfieCam.backgroundColor = Color.black;
                selfieCam.nearClipPlane = 0.01f;
                selfieCam.farClipPlane = 50f;
                selfieCam.enabled = true;
            }


            // Find or warn about RawImage
            if (selfieImage == null)
            {
                selfieImage = GetComponentInChildren<RawImage>();
                if (selfieImage == null)
                    Debug.LogWarning("SelfieHandler: No RawImage found for preview!");
                else
                {
                    // Optional: Force RawImage to maintain 16:9 aspect
                    var aspectFitter = selfieImage.GetComponent<AspectRatioFitter>() ?? selfieImage.gameObject.AddComponent<AspectRatioFitter>();
                    aspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                    aspectFitter.aspectRatio = ASPECT_RATIO;
                }
            }

            // Audio Source
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    GameObject audioObj = new GameObject("SelfieShutterSound");
                    audioObj.transform.SetParent(transform);
                    audioSource = audioObj.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 0f;
                }
            }

            CreatePreviewRenderTexture();
            isInitialized = true;
            Debug.Log($"SelfieHandler initialized for player {Object.InputAuthority} at {selfieQuality} (Landscape 16:9)");
        }

        private void CreatePreviewRenderTexture()
        {
            if (selfieCamTexture != null)
            {
                selfieCam.targetTexture = null;
                if (Application.isPlaying) Destroy(selfieCamTexture);
                else DestroyImmediate(selfieCamTexture);
            }

            int width = (int)selfieQuality;
            int height = Mathf.RoundToInt(width / ASPECT_RATIO);

            selfieCamTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                name = $"SelfiePreview_RT_{width}x{height}",
                antiAliasing = selfieQuality >= SelfieQuality.High ? 4 : 2,
                useDynamicScale = true
            };
            selfieCamTexture.Create();

            selfieCam.targetTexture = selfieCamTexture;

            if (selfieImage != null)
                selfieImage.texture = selfieCamTexture;
        }

        void Update()
        {
            if (!Object.HasInputAuthority || !isInitialized) return;
            ProcessInput();
        }

        private void ProcessInput()
        {
#if !UNITY_WEBGL
            if (InputData.Instance?._leftController != null &&
                InputData.Instance._leftController.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryBtn))
            {
                if (primaryBtn && !primaryButtonPressed)
                {
                    primaryButtonPressed = true;
                    if (CanTakeSelfie)
                        RPC_TakeSelfie();
                }
                else if (!primaryBtn)
                {
                    primaryButtonPressed = false;
                }
            }
#endif

#if UNITY_EDITOR || UNITY_WEBGL
            if (Input.GetKeyDown(KeyCode.S) && CanTakeSelfie)
            {
                RPC_TakeSelfie();
            }
#endif
        }

        public void PlayShutterSound()
        {
            if (audioSource != null && audioSource.clip != null)
                audioSource.Play();
            else if (audioSource != null)
                audioSource.PlayOneShot(audioSource.clip);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
        private void RPC_TakeSelfie()
        {
            PlayShutterSound();
            StartCoroutine(CaptureAndSavePhoto());
        }

        [ContextMenu("Take Selfie (Editor)")]
        public void TakeSelfie()
        {
            if (!isInitialized || !CanTakeSelfie) return;
            StartCoroutine(CaptureAndSavePhoto());
        }

        private IEnumerator CaptureAndSavePhoto()
        {
            yield return new WaitForEndOfFrame();

            if (selfieCam == null)
            {
                Debug.LogError("Selfie camera is missing!");
                yield break;
            }

            int width = (int)selfieQuality;
            int height = Mathf.RoundToInt(width / ASPECT_RATIO);

            RenderTexture tempRT = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture prevRT = selfieCam.targetTexture;

            selfieCam.targetTexture = tempRT;
            selfieCam.Render();

            RenderTexture.active = tempRT;
            Texture2D photo = new Texture2D(width, height, TextureFormat.RGB24, false);
            photo.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            photo.Apply();

            // Restore preview
            selfieCam.targetTexture = prevRT;
            RenderTexture.active = null;

            byte[] bytes = photo.EncodeToPNG();
            Destroy(photo);
            RenderTexture.ReleaseTemporary(tempRT);

            string fileName = $"Selfie_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}_{width}x{height}.png";

#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLFileSaver.Download(bytes, fileName);
            Debug.Log($"Landscape selfie download triggered: {fileName} ({width}x{height})");
#elif UNITY_EDITOR
            string folder = Path.Combine(Application.persistentDataPath, "Selfies");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            string savePath = Path.Combine(folder, fileName);
            File.WriteAllBytes(savePath, bytes);
            Debug.Log($"Landscape selfie saved ({width}x{height}): {savePath}");
#else
            NativeGallery.SaveImageToGallery(bytes, "VR Selfies", fileName);
            Debug.Log($"Landscape selfie saved to gallery: {fileName} ({width}x{height})");
#endif

            yield return new WaitForSeconds(0.15f);
        }

        public void SetSelfieQuality(SelfieQuality quality)
        {
            if (selfieQuality == quality || !Object.HasInputAuthority) return;

            selfieQuality = quality;
            if (isInitialized)
                CreatePreviewRenderTexture();

            Debug.Log($"Selfie quality changed to: {quality} → {(int)quality}x{Mathf.RoundToInt((int)quality / ASPECT_RATIO)} (16:9)");
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (selfieCamTexture != null)
            {
                selfieCam.targetTexture = null;
                if (Application.isPlaying) Destroy(selfieCamTexture);
                else DestroyImmediate(selfieCamTexture);
                selfieCamTexture = null;
            }

            if (selfieCam != null && selfieCam.gameObject != null)
                Destroy(selfieCam.gameObject);

            base.Despawned(runner, hasState);
        }
    }
}