#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace VertexFormCore.Editor
{
    public static class WebGLSetupEditor
    {
        private const string MediumURPAssetPath = "Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Global/RendererData/UniversalRP-MediumQuality.asset";
        private const string WebGLURPAssetPath = "Assets/VertexForm3D/URP Profiles/UniversalRP-WebGL.asset";

        // [MenuItem("VertexForm3D SDK/Setup WebGL Quality", priority = 200)]
        // public static void SetupWebGLQuality()
        // {
        //     CesiumWebGLAsmdefPatcher.TryPatchIfNeeded(silent: false);
        //     CreateWebGLURPAsset();
        //     ConfigureWebGLQualityLevel();
        //     ConfigureWebGLPlayerSettings();
        //     AssetDatabase.SaveAssets();
        //     Debug.Log("[WebGL Setup] WebGL quality tier and player settings configured successfully.");
        // }

        private static void CreateWebGLURPAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(WebGLURPAssetPath) != null)
            {
                Debug.Log("[WebGL Setup] WebGL URP asset already exists, updating properties.");
                UpdateWebGLURPProperties(AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(WebGLURPAssetPath));
                return;
            }

            var sourceAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(MediumURPAssetPath);
            if (sourceAsset == null)
            {
                Debug.LogError($"[WebGL Setup] Source URP asset not found at: {MediumURPAssetPath}. Please duplicate a URP asset manually.");
                return;
            }

            if (!AssetDatabase.CopyAsset(MediumURPAssetPath, WebGLURPAssetPath))
            {
                Debug.LogError("[WebGL Setup] Failed to copy URP asset.");
                return;
            }

            AssetDatabase.Refresh();
            var webGLAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(WebGLURPAssetPath);
            if (webGLAsset != null)
            {
                UpdateWebGLURPProperties(webGLAsset);
                Debug.Log($"[WebGL Setup] Created WebGL URP pipeline asset at: {WebGLURPAssetPath}");
            }
        }

        private static void UpdateWebGLURPProperties(UniversalRenderPipelineAsset asset)
        {
            var so = new SerializedObject(asset);
            SetBool(so, "m_SupportsHDR", false);
            SetInt(so, "m_MSAA", 1);
            SetInt(so, "m_MainLightShadowmapResolution", 1024);
            SetInt(so, "m_AdditionalLightsShadowmapResolution", 512);
            SetInt(so, "m_ShadowCascadeCount", 1);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void SetBool(SerializedObject so, string name, bool val)
        {
            var prop = so.FindProperty(name);
            if (prop != null) prop.boolValue = val;
        }

        private static void SetInt(SerializedObject so, string name, int val)
        {
            var prop = so.FindProperty(name);
            if (prop != null) prop.intValue = val;
        }

        private static void ConfigureWebGLQualityLevel()
        {
            var webGLAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(WebGLURPAssetPath);
            if (webGLAsset == null)
            {
                Debug.LogError("[WebGL Setup] Cannot find WebGL URP asset. Run setup again.");
                return;
            }

            string[] names = QualitySettings.names;
            int webGLIndex = -1;
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i] == "WebGL")
                {
                    webGLIndex = i;
                    break;
                }
            }

            if (webGLIndex < 0)
            {
                Debug.Log("[WebGL Setup] No 'WebGL' quality level found. Configuring WebGL platform to use the 'Medium' level with the WebGL URP asset.");
                Debug.Log("[WebGL Setup] To add a dedicated WebGL quality level, go to Edit > Project Settings > Quality and add a level named 'WebGL'.");

                int mediumIndex = -1;
                for (int i = 0; i < names.Length; i++)
                {
                    if (names[i] == "Medium")
                    {
                        mediumIndex = i;
                        break;
                    }
                }

                if (mediumIndex >= 0)
                {
                    QualitySettings.SetQualityLevel(mediumIndex, false);
                }
            }
            else
            {
                QualitySettings.SetQualityLevel(webGLIndex, false);
                Debug.Log($"[WebGL Setup] Switched to 'WebGL' quality level (index {webGLIndex}).");
            }

            QualitySettings.renderPipeline = webGLAsset;
            QualitySettings.shadows = UnityEngine.ShadowQuality.HardOnly;
            QualitySettings.shadowResolution = UnityEngine.ShadowResolution.Medium;
            QualitySettings.antiAliasing = 0;

            int restoreLevel = 1;
            QualitySettings.SetQualityLevel(restoreLevel, true);

            Debug.Log("[WebGL Setup] Quality settings configured for WebGL.");
        }

        private static void ConfigureWebGLPlayerSettings()
        {
            PlayerSettings.WebGL.memorySize = 512;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;

            Debug.Log("[WebGL Setup] WebGL Player Settings configured (512MB memory, Brotli, explicit exceptions).");
        }
    }
}
#endif
