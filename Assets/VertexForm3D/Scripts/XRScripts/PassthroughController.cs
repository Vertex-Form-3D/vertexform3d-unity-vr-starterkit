using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;

public class PassthroughController : MonoBehaviour
{
    public static PassthroughController Instance;
    public ARCameraManager arCameraManager; // Reference to the ARCameraManager
    public ARCameraBackground cameraBackground; // Reference to the ARCameraBackground
    public ARSession ARsession;
    public ARPlaneManager arPlaneManager; // Reference to the ARPlaneManager
    UniversalAdditionalCameraData cameraData;
    bool wasPostProcessingEnabled = false; // Flag to track if post-processing was enabled
    public static bool IsPassthroughOn { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }
    private void Start()
    {
        cameraData = Camera.main.GetComponent<UniversalAdditionalCameraData>();
        if (IsPassthroughOn)
        {
            EnablePassthrough();
        }
    }

    public void EnablePassthrough()
    {
        // Check if OpenXR is active
        if (XRGeneralSettings.Instance.Manager.activeLoader != null)
        {
            // Set the camera clear flags to solid color with alpha 0 for passthrough visibility
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
            Camera.main.clearFlags = CameraClearFlags.Skybox; // Restore default rendering
            arPlaneManager.enabled = false; // Disable ARPlaneManager
            cameraData.renderPostProcessing = wasPostProcessingEnabled; // Restore post-processing state
            Camera.main.allowHDR = true;
            IsPassthroughOn = false;
        }
    }
}