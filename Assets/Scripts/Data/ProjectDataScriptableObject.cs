using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Project Data SO", menuName = "ScriptableObjects/Project Data System", order = 1)]
public class ProjectDataScriptableObject : ScriptableObject
{
    public ProjectData projectData;
}

[Serializable]
public class ProjectData
{
    public bool onlyLocalBundles;
    public bool DebugEnabled;
    public string GameVersion;
    public string catalogFileName;
}