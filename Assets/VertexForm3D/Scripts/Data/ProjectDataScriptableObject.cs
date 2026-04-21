using System;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Project Data SO", menuName = "ScriptableObjects/Project Data System", order = 1)]
public class ProjectDataScriptableObject : ScriptableObject
{
    public ProjectData projectData;
    public Mode mode;
}

public enum platform
{
    VR,
    Desktop,
    /// <summary>WebGL / WebGPU player: use <see cref="WebGpuBrowserKind"/> on <see cref="Platforms"/> for Android vs desktop vs VR shell browser.</summary>
    WebGPU
}

/// <summary>
/// When <see cref="platform.WebGPU"/> is selected, the host page (e.g. WebGL index.html) reports which browser context loaded the build.
/// </summary>
public enum WebGpuBrowserKind
{
    None = 0,
    AndroidBrowser = 1,
    DesktopBrowser = 2,
    VrBrowser = 3
}
[Serializable]
public class ProjectData
{
    [FormerlySerializedAs("platform")]
    public platform platformSelection;
    public bool DebugEnabled;
    public string anonymousUserNamePrefix = "Mystery Guest_";
    public bool onlyLocalBundles = true;
    public string addressableCatalogFilePath;
    public string catalogFileName;
    public SettingClass defaultSetting;
    public string currentPackageVersion = "1.0.0";
    public string versionJsonUrl = "https://storage.googleapis.com/your_bucket_name/version.json";

    public HomeSceneData homeSceneData;
}
[Serializable]
public class HomeSceneData
{
    [Header("Project branding (home / main map UI)")]
    [TextArea(3, 10)]
    [Tooltip("Short description shown on the main map / home panel.")]
    public string projectDescription = "Vertex Form 3D is an open-source VR multiplayer framework for building social applications in Unity. Designed with 3D artists in mind, this package provides essential tools for creating scalable VR environments across Meta Quest and other platforms.";

    [TextArea(3, 10)]
    [Tooltip("Contact emails and/or website (one per line).")]
    public string projectEmails = "info@vertexform3d.com\nvertexform3d.com";
    [Tooltip("Logo image shown on the main map panel.")]
    public Sprite projectLogo;
    [Tooltip("Optional background image for the logo panel (e.g. gradient).")]
    public Sprite projectBackgroundImage;
}

public enum micType
{
    mute,
    unmute
}

public enum grabMode
{
    near,
    distance
}

public enum toggle
{
    on,
    off
}

public enum Mode
{
    player,
    OnBoarding
}

[System.Serializable]
public class SettingClass
{
    public toggle standDefault;
    public micType micType = micType.mute;
    public grabMode grabMode;
    public toggle flyMode;
    public toggle megaphone = toggle.off;

}

