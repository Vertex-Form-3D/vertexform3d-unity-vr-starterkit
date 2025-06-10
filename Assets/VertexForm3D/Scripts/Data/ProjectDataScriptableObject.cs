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
    public string addressableCatalogFilePath;
    public bool onlyLocalBundles = true;
    public bool DebugEnabled;
    public string anonymousUserNamePrefix= "Mystery Guest_";
    public string catalogFileName;
}