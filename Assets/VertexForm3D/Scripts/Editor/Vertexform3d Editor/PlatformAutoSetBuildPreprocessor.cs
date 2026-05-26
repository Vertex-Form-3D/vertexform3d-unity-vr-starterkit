using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Before any player build, force the <see cref="Platforms"/> asset's <see cref="platform"/> choice
/// to match the active build target so VR/Desktop/WebGPU runtime paths line up with the platform Unity is building for.
/// Avoids the common mistake of building Android with the platform left on Desktop (or similar).
/// </summary>
internal sealed class PlatformAutoSetBuildPreprocessor : IPreprocessBuildWithReport
{
    public int callbackOrder => -1000; // run early so anything else inspecting Platforms during preprocess sees the updated value

    public void OnPreprocessBuild(BuildReport report)
    {
        BuildTarget target = report.summary.platform;
        if (!TryResolvePlatform(target, out platform desired))
            return;

        Platforms asset = LoadPlatformsAsset();
        if (asset == null)
        {
            Debug.LogWarning("[PlatformAutoSet] No Platforms asset found (t:Platforms). Skipping auto-set.");
            return;
        }

        if (asset.platformChoice == desired)
            return;

        platform previous = asset.platformChoice;
        asset.platformChoice = desired;
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssetIfDirty(asset);

        Debug.Log($"[PlatformAutoSet] Build target is {target}. Platforms.platformChoice: {previous} -> {desired}.");
    }

    private static bool TryResolvePlatform(BuildTarget target, out platform desired)
    {
        switch (target)
        {
            case BuildTarget.Android:
            case BuildTarget.iOS:
                desired = platform.VR;
                return true;
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
            case BuildTarget.StandaloneOSX:
            case BuildTarget.StandaloneLinux64:
                desired = platform.Desktop;
                return true;
            case BuildTarget.WebGL:
                desired = platform.WebGPU;
                return true;
            default:
                desired = default;
                return false;
        }
    }

    private static Platforms LoadPlatformsAsset()
    {
        string[] guids = AssetDatabase.FindAssets("t:Platforms");
        if (guids == null || guids.Length == 0)
            return null;

        // Prefer the canonical location if multiple assets exist.
        string preferred = "Assets/VertexForm3D/Scripts/Data/Platforms.asset";
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path == preferred)
                return AssetDatabase.LoadAssetAtPath<Platforms>(path);
        }

        return AssetDatabase.LoadAssetAtPath<Platforms>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }
}
