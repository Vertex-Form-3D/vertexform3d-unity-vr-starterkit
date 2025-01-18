using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteInEditMode]
public class HeightMapRenderer : MonoBehaviour
{
    public string m_Path;
    public float m_Size;
    public int m_Resolution;
    public LayerMask m_CullingMask;

    private RenderTexture m_TemporaryTexture;
    private Camera m_HeightmapCamera;

    public void RenderHeightMap()
    {
        if (m_TemporaryTexture != null)
        {
            DestroyImmediate(m_TemporaryTexture);
            m_TemporaryTexture = null;
        }
        m_TemporaryTexture = new RenderTexture(m_Resolution, m_Resolution, GraphicsFormat.B10G11R11_UFloatPack32,
            GraphicsFormat.D24_UNorm_S8_UInt);

        if (m_HeightmapCamera == null)
        {
            CreateHeightmapCamera();
        }
        
        m_HeightmapCamera.orthographic = true;
        m_HeightmapCamera.orthographicSize = m_Size * 0.5f;
        m_HeightmapCamera.cullingMask = m_CullingMask;

        m_HeightmapCamera.targetTexture = m_TemporaryTexture;
        m_HeightmapCamera.Render();
    }

    private void CreateHeightmapCamera()
    {
        GameObject cameraGO = new GameObject("[Heightmap Camera]");
        cameraGO.transform.parent = transform;
        cameraGO.hideFlags = HideFlags.HideAndDontSave;

        cameraGO.transform.localPosition = new Vector3(0, 100, 0);
        cameraGO.transform.SetLocalPositionAndRotation(new Vector3(0, 100, 0), Quaternion.Euler(90, 0, 0));
        m_HeightmapCamera = cameraGO.AddComponent<Camera>();
        m_HeightmapCamera.gameObject.AddComponent<UniversalAdditionalCameraData>().SetRenderer(2); //TODO: Can we avoid this somehow?
        m_HeightmapCamera.enabled = false;
    }

    private void OnEnable()
    {
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
    }

    void OnEndCameraRendering(ScriptableRenderContext Context, Camera camera)
    {
        if (camera != m_HeightmapCamera) return;

        RenderTexture rt = camera.targetTexture;
        
        WriteRenderTextureToPNG(rt, m_Path);

        SetImportSettings(m_Path);
    }

    void WriteRenderTextureToPNG(RenderTexture rt, string path)
    {
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBAHalf, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        byte[] bytes = ImageConversion.EncodeToPNG(tex);
        
        DestroyImmediate(tex);

        string absolutePath = Application.dataPath + "/" + path;

        File.WriteAllBytes(absolutePath, bytes);
        AssetDatabase.Refresh();
    }

    void SetImportSettings(string path)
    {
        string assetsPath = "Assets/" + m_Path;
        TextureImporter importer = (TextureImporter) AssetImporter.GetAtPath(assetsPath);
        importer.sRGBTexture = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }
}

[CustomEditor(typeof(HeightMapRenderer))]
public class HeightMapRendererEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        if (GUILayout.Button("Render"))
        {
            HeightMapRenderer renderer = (HeightMapRenderer) target;
            renderer.RenderHeightMap();
        }
    }
}