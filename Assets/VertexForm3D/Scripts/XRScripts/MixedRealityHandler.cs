using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;

public class MixedRealityHandler : MonoBehaviour
{
    public static PassthroughController Instance;
    public ARCameraManager arCameraManager; // Reference to the ARCameraManager
    public ARCameraBackground cameraBackground; // Reference to the ARCameraBackground
    public ARSession ARsession;
    public ARPlaneManager arPlaneManager; // Reference to the ARPlaneManager
    public UniversalAdditionalCameraData cameraData;
    bool wasPostProcessingEnabled = false; // Flag to track if post-processing was enabled
    public bool IsPassthroughOn;
    
    private void Start()
    {
        
    }

    public void EnableMixedReality()
    {
        // Check if OpenXR is active
        if (XRGeneralSettings.Instance.Manager.activeLoader != null)
        {
            // Set the camera clear flags to solid color with alpha 0 for passthrough visibility
            ARsession.gameObject.SetActive(true); // Ensure ARSession is active
            arCameraManager.enabled=cameraBackground.enabled = true; // Enable ARCameraManager and ARCameraBackground
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = new Color(0, 0, 0, 0);
            Camera.main.allowHDR = false; // Disable HDR to avoid issues with passthrough
            arPlaneManager.enabled = true; // Enable ARPlaneManager
            wasPostProcessingEnabled = cameraData.renderPostProcessing; // Store the current post-processing state
            cameraData.renderPostProcessing = false; // Disable post-processing effects
            IsPassthroughOn = true;
        }
        else
        {
            Debug.LogError("XR Loader not initialized. Passthrough cannot be enabled.");
        }
    }

    public void DisablePassthrough()
    {
        if (XRGeneralSettings.Instance.Manager.activeLoader != null)
        {
            ARsession.gameObject.SetActive(false);
            arCameraManager.enabled=cameraBackground.enabled = false; // Enable ARCameraManager and ARCameraBackground
            Camera.main.clearFlags = CameraClearFlags.Skybox; // Restore default rendering
            arPlaneManager.enabled = false; // Disable ARPlaneManager
            cameraData.renderPostProcessing = wasPostProcessingEnabled; // Restore post-processing state
            Camera.main.allowHDR = true;
            IsPassthroughOn = false;
        }
    }
}
