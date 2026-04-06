using UnityEngine;
using UnityEngine.Rendering.Universal;
#if !UNITY_WEBGL
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;
#endif

public class MixedRealityHandler : MonoBehaviour
{
    public static PassthroughController Instance;
#if !UNITY_WEBGL
    public ARCameraManager arCameraManager;
    public ARCameraBackground cameraBackground;
    public ARSession ARsession;
    public ARPlaneManager arPlaneManager;
#endif
    public UniversalAdditionalCameraData cameraData;
    bool wasPostProcessingEnabled = false;
    public bool IsPassthroughOn;
    
    private void Start()
    {
        
    }

    public void EnableMixedReality()
    {
#if !UNITY_WEBGL
        if (XRGeneralSettings.Instance != null &&
            XRGeneralSettings.Instance.Manager != null &&
            XRGeneralSettings.Instance.Manager.activeLoader != null)
        {
            ARsession.gameObject.SetActive(true);
            arCameraManager.enabled=cameraBackground.enabled = true;
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = new Color(0, 0, 0, 0);
            Camera.main.allowHDR = false;
            arPlaneManager.enabled = true;
            wasPostProcessingEnabled = cameraData.renderPostProcessing;
            cameraData.renderPostProcessing = false;
            IsPassthroughOn = true;
        }
        else
        {
            Debug.LogError("XR Loader not initialized. Passthrough cannot be enabled.");
        }
#else
        Debug.LogWarning("[MixedRealityHandler] Mixed Reality is not supported on WebGL.");
#endif
    }

    public void DisablePassthrough()
    {
#if !UNITY_WEBGL
        if (XRGeneralSettings.Instance != null &&
            XRGeneralSettings.Instance.Manager != null &&
            XRGeneralSettings.Instance.Manager.activeLoader != null)
        {
            ARsession.gameObject.SetActive(false);
            arCameraManager.enabled=cameraBackground.enabled = false;
            Camera.main.clearFlags = CameraClearFlags.Skybox;
            arPlaneManager.enabled = false;
            cameraData.renderPostProcessing = wasPostProcessingEnabled;
            Camera.main.allowHDR = true;
            IsPassthroughOn = false;
        }
#endif
    }
}
