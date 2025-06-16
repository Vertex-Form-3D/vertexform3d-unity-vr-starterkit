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
    public bool DebugEnabled;
    public string anonymousUserNamePrefix = "Mystery Guest_";
    public bool onlyLocalBundles = true;
    public string addressableCatalogFilePath;
    public string catalogFileName;
}