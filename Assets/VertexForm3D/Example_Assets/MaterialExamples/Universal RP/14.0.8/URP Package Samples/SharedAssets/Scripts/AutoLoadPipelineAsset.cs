using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public class AutoLoadPipelineAsset : MonoBehaviour
{
    [SerializeField]
    private UniversalRenderPipelineAsset m_PipelineAsset;
    private RenderPipelineAsset m_PreviousPipelineAsset;
    private bool m_overrodeQualitySettings;

    void OnEnable()
    {
        UpdatePipeline();
    }

    void OnDisable()
    {
        ResetPipeline();
    }

    private void UpdatePipeline()
    {
        if (m_PipelineAsset)
        {
            if (QualitySettings.renderPipeline != null && QualitySettings.renderPipeline != m_PipelineAsset)
            {
                m_PreviousPipelineAsset = QualitySettings.renderPipeline;
                QualitySettings.renderPipeline = m_PipelineAsset;
                m_overrodeQualitySettings = true;
            }
            else if (GraphicsSettings.defaultRenderPipeline != m_PipelineAsset)
            {
                m_PreviousPipelineAsset = GraphicsSettings.defaultRenderPipeline;
                GraphicsSettings.defaultRenderPipeline = m_PipelineAsset;
                m_overrodeQualitySettings = false;
            }
        }
    }

    private void ResetPipeline()
    {
        if (m_PreviousPipelineAsset)
        {
            if (m_overrodeQualitySettings)
            {
                QualitySettings.renderPipeline = m_PreviousPipelineAsset;
            }
            else
            {
                GraphicsSettings.defaultRenderPipeline = m_PreviousPipelineAsset;
            }

        }
    }
}
