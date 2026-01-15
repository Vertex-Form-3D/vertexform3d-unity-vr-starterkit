using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Project Data SO", menuName = "ScriptableObjects/Project Data System", order = 1)]
public class ProjectDataScriptableObject : ScriptableObject
{
    public ProjectData projectData;
    public Mode mode;
}

[Serializable]
public class ProjectData
{
    public bool DebugEnabled;
    public string anonymousUserNamePrefix = "Mystery Guest_";
    public bool onlyLocalBundles = true;
    public string addressableCatalogFilePath;
    public string catalogFileName;
    public SettingClass defaultSetting;
    public string currentPackageVersion = "1.0.0";
    public string versionJsonUrl = "https://storage.googleapis.com/your_bucket_name/version.json";
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
    Spectator
}

[System.Serializable]
public class SettingClass
{
    public toggle standDefault;
    public micType micType = micType.mute;
    public grabMode grabMode;
    public toggle flyMode;
    public toggle megaphone;

}
