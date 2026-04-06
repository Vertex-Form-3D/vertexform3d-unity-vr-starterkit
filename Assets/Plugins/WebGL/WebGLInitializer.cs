using UnityEngine;

public class WebGLInitializer
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        WebGLBrowserHelper.DisableContextMenu();
        WebGLBrowserHelper.ResumeAudioContext();
        Debug.Log("[WebGLInitializer] Browser integration initialized.");
    }
#endif
}
