using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

public static class WebGLFileSaver
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void WebGLFileSaver_SaveFile(byte[] array, int size, string fileName);

    public static void Download(byte[] data, string fileName)
    {
        WebGLFileSaver_SaveFile(data, data.Length, fileName);
    }
#else
    public static void Download(byte[] data, string fileName)
    {
        Debug.LogWarning("[WebGLFileSaver] Download is only available in WebGL builds.");
    }
#endif
}
