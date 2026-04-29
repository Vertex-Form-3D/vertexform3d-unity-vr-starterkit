#if UNITY_EDITOR
using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace VertexFormCore.Editor
{
    /// <summary>
    /// Cesium for Unity lists WebGL in CesiumRuntime.asmdef, so IL2CPP still compiles the runtime for WebGL.
    /// That C++ references Itanium/EH symbols (__wasm_lpad_context, _Unwind_CallPersonality) that the
    /// Unity WebGL toolchain does not provide, failing wasm-ld. Excluding WebGL from the runtime asmdef
    /// removes Cesium from WebGL players until Cesium ships a compatible WebGL runtime.
    /// </summary>
    public static class CesiumWebGLAsmdefPatcher
    {
        private const string RelativeAsmdef = "Runtime/CesiumRuntime.asmdef";

        [InitializeOnLoadMethod]
        private static void PatchOnEditorLoad()
        {
            TryPatchIfNeeded(silent: true);
        }

        /// <summary>Removes WebGL from CesiumRuntime includePlatforms if present. Returns true if the file was changed.</summary>
        public static bool TryPatchIfNeeded(bool silent)
        {
            string path = FindCesiumRuntimeAsmdefPath();
            if (string.IsNullOrEmpty(path))
            {
                if (!silent)
                    Debug.LogWarning("[CesiumWebGLAsmdefPatcher] CesiumRuntime.asmdef not found (is com.cesium.unity installed?).");
                return false;
            }

            string text;
            try
            {
                text = File.ReadAllText(path);
            }
            catch (Exception e)
            {
                if (!silent)
                    Debug.LogError($"[CesiumWebGLAsmdefPatcher] Failed to read asmdef: {e.Message}");
                return false;
            }

            JObject root;
            try
            {
                root = JObject.Parse(text);
            }
            catch (Exception e)
            {
                if (!silent)
                    Debug.LogError($"[CesiumWebGLAsmdefPatcher] Invalid JSON in asmdef: {e.Message}");
                return false;
            }

            var platforms = root["includePlatforms"] as JArray;
            if (platforms == null)
            {
                if (!silent)
                    Debug.LogWarning("[CesiumWebGLAsmdefPatcher] No includePlatforms array in CesiumRuntime.asmdef.");
                return false;
            }

            JToken webgl = null;
            foreach (var t in platforms)
            {
                if (t != null && string.Equals(t.ToString(), "WebGL", StringComparison.Ordinal))
                {
                    webgl = t;
                    break;
                }
            }

            if (webgl == null)
                return false;

            webgl.Remove();
            try
            {
                File.WriteAllText(path, root.ToString(Newtonsoft.Json.Formatting.Indented) + Environment.NewLine);
            }
            catch (Exception e)
            {
                if (!silent)
                    Debug.LogError($"[CesiumWebGLAsmdefPatcher] Failed to write asmdef: {e.Message}");
                return false;
            }

            if (!silent)
                Debug.Log($"[CesiumWebGLAsmdefPatcher] Removed WebGL from CesiumRuntime includePlatforms: {path}");

            AssetDatabase.Refresh();
            return true;
        }

        private static string FindCesiumRuntimeAsmdefPath()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectRoot))
                return null;

            string viaPackages = Path.Combine(projectRoot, "Packages", "com.cesium.unity", RelativeAsmdef);
            if (File.Exists(viaPackages))
                return viaPackages;

            string cacheRoot = Path.Combine(projectRoot, "Library", "PackageCache");
            if (!Directory.Exists(cacheRoot))
                return null;

            try
            {
                foreach (string dir in Directory.GetDirectories(cacheRoot, "com.cesium.unity@*"))
                {
                    string p = Path.Combine(dir, RelativeAsmdef.Replace('/', Path.DirectorySeparatorChar));
                    if (File.Exists(p))
                        return p;
                }
            }
            catch
            {
                // ignored
            }

            return null;
        }

        // [MenuItem("VertexForm3D SDK/Patch Cesium asmdef (exclude WebGL runtime)", priority = 199)]
        // public static void PatchFromMenu()
        // {
        //     if (TryPatchIfNeeded(silent: false))
        //         Debug.Log("[CesiumWebGLAsmdefPatcher] Done. Cesium runtime will not be compiled for WebGL.");
        //     else
        //         Debug.Log("[CesiumWebGLAsmdefPatcher] No change (WebGL already excluded or package missing).");
        // }
    }
}
#endif
