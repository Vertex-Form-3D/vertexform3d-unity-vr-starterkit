#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace VertexFormCore.Editor
{
    /// <summary>
    /// Disables the Cesium native WebGL plugin before WebGL builds and re-enables it after.
    /// You must also exclude Cesium from the WebGL player via <see cref="CesiumWebGLAsmdefPatcher"/>
    /// (WebGL in CesiumRuntime.asmdef causes IL2CPP to emit C++ that fails wasm-ld).
    /// </summary>
    public class WebGLCesiumStripper : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        private const string CesiumWebGLPluginGuid = "";
        private static PluginImporter[] _cesiumPlugins;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL)
                return;

            _cesiumPlugins = FindCesiumWebGLPlugins();
            foreach (var plugin in _cesiumPlugins)
            {
                if (plugin.GetCompatibleWithPlatform(BuildTarget.WebGL))
                {
                    Debug.Log($"[WebGLCesiumStripper] Disabling Cesium plugin for WebGL: {plugin.assetPath}");
                    plugin.SetCompatibleWithPlatform(BuildTarget.WebGL, false);
                    plugin.SaveAndReimport();
                }
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL)
                return;

            if (_cesiumPlugins == null) return;
            foreach (var plugin in _cesiumPlugins)
            {
                if (!plugin.GetCompatibleWithPlatform(BuildTarget.WebGL))
                {
                    Debug.Log($"[WebGLCesiumStripper] Re-enabling Cesium plugin for WebGL: {plugin.assetPath}");
                    plugin.SetCompatibleWithPlatform(BuildTarget.WebGL, true);
                    plugin.SaveAndReimport();
                }
            }
            _cesiumPlugins = null;
        }

        private static PluginImporter[] FindCesiumWebGLPlugins()
        {
            var results = new System.Collections.Generic.List<PluginImporter>();

            // Find all native plugins under the Cesium package WebGL folder
            string[] searchPaths = new[]
            {
                "Packages/com.cesium.unity/Plugins/WebGL",
            };

            foreach (string searchPath in searchPaths)
            {
                string[] guids = AssetDatabase.FindAssets("", new[] { searchPath });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(path)) continue;
                    if (!path.EndsWith(".a") && !path.EndsWith(".so") && !path.EndsWith(".bc")) continue;

                    var importer = AssetImporter.GetAtPath(path) as PluginImporter;
                    if (importer != null)
                    {
                        results.Add(importer);
                    }
                }
            }

            return results.ToArray();
        }

        // [MenuItem("VertexForm3D SDK/Strip Cesium for WebGL", priority = 201)]
        // public static void ManuallyDisableCesiumWebGL()
        // {
        //     var plugins = FindCesiumWebGLPlugins();
        //     if (plugins.Length == 0)
        //     {
        //         Debug.LogWarning("[WebGLCesiumStripper] No Cesium WebGL native plugins found via asset search.");
        //         return;
        //     }

        //     int count = 0;
        //     foreach (var plugin in plugins)
        //     {
        //         if (plugin.GetCompatibleWithPlatform(BuildTarget.WebGL))
        //         {
        //             plugin.SetCompatibleWithPlatform(BuildTarget.WebGL, false);
        //             plugin.SaveAndReimport();
        //             count++;
        //         }
        //     }
        //     Debug.Log($"[WebGLCesiumStripper] Disabled {count} Cesium WebGL native plugins out of {plugins.Length} found. Rebuild WebGL now.");
        // }
    }
}
#endif
