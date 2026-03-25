using UnityEngine;

namespace VertexForm3D.Utility
{
    /// <summary>
    /// Ensures each mirror instance writes to and reads from its own runtime RenderTexture.
    /// </summary>
    public class MirrorRenderTextureInstance : MonoBehaviour
    {
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        [SerializeField] private Camera mirrorCamera;
        [SerializeField] private Renderer mirrorSurfaceRenderer;
        [SerializeField] private int textureWidth = 1024;
        [SerializeField] private int textureHeight = 1024;
        [SerializeField] private int depthBuffer = 24;
        [SerializeField] private RenderTextureFormat format = RenderTextureFormat.ARGB32;

        private RenderTexture runtimeTexture;
        private MaterialPropertyBlock propertyBlock;

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
        }

        private void OnDisable()
        {
            Cleanup();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        private void Initialize()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            mirrorCamera ??= GetComponentInChildren<Camera>(true);
            mirrorSurfaceRenderer ??= GetComponentInChildren<Renderer>(true);

            if (mirrorCamera == null || mirrorSurfaceRenderer == null)
            {
                return;
            }

            if (runtimeTexture == null || runtimeTexture.width != textureWidth || runtimeTexture.height != textureHeight)
            {
                Cleanup();
                runtimeTexture = CreateRenderTexture();
            }

            mirrorCamera.targetTexture = runtimeTexture;
            ApplyTextureToMirrorSurface();
        }

        private RenderTexture CreateRenderTexture()
        {
            var texture = new RenderTexture(textureWidth, textureHeight, depthBuffer, format)
            {
                name = $"MirrorRT_{gameObject.GetInstanceID()}",
                useMipMap = false,
                autoGenerateMips = false
            };
            texture.Create();
            return texture;
        }

        private void ApplyTextureToMirrorSurface()
        {
            propertyBlock ??= new MaterialPropertyBlock();
            mirrorSurfaceRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetTexture(BaseMapId, runtimeTexture);
            propertyBlock.SetTexture(MainTexId, runtimeTexture);
            mirrorSurfaceRenderer.SetPropertyBlock(propertyBlock);
        }

        private void Cleanup()
        {
            if (mirrorCamera != null && mirrorCamera.targetTexture == runtimeTexture)
            {
                mirrorCamera.targetTexture = null;
            }

            if (runtimeTexture != null)
            {
                runtimeTexture.Release();
                Destroy(runtimeTexture);
                runtimeTexture = null;
            }
        }
    }
}
