using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Video;

namespace Unity.XRTemplate
{
    /// <summary>
    /// Create a RenderTexture for rendering video to a target renderer.
    /// </summary>
    [RequireComponent(typeof(VideoPlayer))]
    public class VideoPlayerRenderTexture : MonoBehaviour
    {
        private string k_ShaderName;

        string GetShaderName()
        {
            if (GraphicsSettings.currentRenderPipeline)
            {
                string pipelineName = GraphicsSettings.currentRenderPipeline.name;
                if (pipelineName.Contains("UniversalRenderPipeline"))
                {
                    return "Universal Render Pipeline/Unlit";
                }
                else if (pipelineName.Contains("HDRenderPipeline"))
                {
                    return "HDRP/Unlit";
                }
            }

            // Fallback for the built-in render pipeline
            return "Unlit/Texture";
        }

        [SerializeField, Tooltip("The target Renderer which will display the video.")]
        Renderer m_Renderer;

        [SerializeField, Tooltip("The width of the RenderTexture which will be created.")]
        int m_RenderTextureWidth = 1920;

        [SerializeField, Tooltip("The height of the RenderTexture which will be created.")]
        int m_RenderTextureHeight = 1080;

        [SerializeField, Tooltip("The bit depth of the depth channel for the RenderTexture which will be created.")]
        int m_RenderTextureDepth;

        void Start()
        {
            k_ShaderName = GetShaderName();

            var renderTexture = new RenderTexture(m_RenderTextureWidth, m_RenderTextureHeight, m_RenderTextureDepth);
            renderTexture.Create();
            var material = new Material(Shader.Find(k_ShaderName));
            material.mainTexture = renderTexture;
            GetComponent<VideoPlayer>().targetTexture = renderTexture;
            m_Renderer.material = material;
        }
    }
}
