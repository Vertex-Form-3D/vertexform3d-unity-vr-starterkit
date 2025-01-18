using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
class LodLightmapCopy : IProcessSceneWithReport
{
    public int callbackOrder => -1;
    
    private static bool m_enabled = true;
    
    public void OnProcessScene(Scene scene, BuildReport report)
    {
        Execute();
    }
    
    [PostProcessBuild(1)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        Execute();
    }
    
    static LodLightmapCopy()
    {
        Execute();
        Lightmapping.bakeCompleted += Execute;
        EditorApplication.playModeStateChanged += AssignLightmapOnEdit; 
        SceneManager.sceneLoaded += AssignLightmapsOnLoad;
        EditorSceneManager.sceneOpened += AssignLightmapsOnOpen;
    }

    static void AssignLightmapsOnLoad(Scene scene, LoadSceneMode mode)
    {
        Execute(scene);
    }

    static void AssignLightmapsOnOpen(Scene scene, OpenSceneMode mode)
    {
        Execute(scene);
    }

    static void AssignLightmapOnEdit(PlayModeStateChange state)
    {
        if(state == PlayModeStateChange.EnteredEditMode) Execute();
    }
    
    private static void Execute()
    {
        Execute(SceneManager.GetActiveScene());
    }
    
    private static void Execute(Scene scene)
    {
        if(!m_enabled) return;
        if(Debug.isDebugBuild)
        
        //TODO: See if GameObject.FindObjectsOfType<LODGroup>(); works

        foreach (var gameObject in scene.GetRootGameObjects())
        {
            foreach (var lodGroup in gameObject.GetComponentsInChildren<LODGroup>())
            {
                AssignLightmaps(lodGroup);
            }
        }
    }
    
    private static void AssignLightmaps(LODGroup lodGroup)
    {
        var lods = lodGroup.GetLODs();
        var lod0 = lods[0].renderers;
            
        if(lod0.Length == 0 || lod0[0] == null || lod0[0].lightmapIndex == -1) return;
        
        //copy settings to other lods
        for (var i = 1; i < lods.Length; i++)
        {
            
            for (var j = 0; j < lod0.Length; j++)
            {
                Renderer renderer = lods[i].renderers[j];

                if (renderer == null) continue;
                
                renderer.lightmapIndex = lod0[j].lightmapIndex;
                renderer.lightmapScaleOffset = lod0[j].lightmapScaleOffset;
            }
        }
    }
}
