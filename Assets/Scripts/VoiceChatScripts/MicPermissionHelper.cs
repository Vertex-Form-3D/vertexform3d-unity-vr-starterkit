using UnityEngine;
#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif
namespace VertextFormCore
{
    public class MicPermissionHelper : MonoBehaviour
    {
        void Start()
        {
#if PLATFORM_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Permission.RequestUserPermission(Permission.Microphone);
                //dialog = new GameObject(); } #endif Debug.Log(Microphone.devices.ToString()); }
            }
#endif
        }
    }
}