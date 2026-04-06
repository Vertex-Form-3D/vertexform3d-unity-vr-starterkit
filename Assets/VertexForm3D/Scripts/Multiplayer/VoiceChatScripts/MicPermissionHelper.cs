using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
namespace VertexFormCore
{
    public class MicPermissionHelper : MonoBehaviour
    {
        void Start()
        {
#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Permission.RequestUserPermission(Permission.Microphone);
            }
#elif UNITY_WEBGL && !UNITY_EDITOR
            Debug.Log("[MicPermissionHelper] WebGL: browser will prompt for microphone access when Photon Voice starts recording.");
#endif
        }
    }
}