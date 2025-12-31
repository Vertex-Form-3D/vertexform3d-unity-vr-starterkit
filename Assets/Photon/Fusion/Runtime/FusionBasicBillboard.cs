namespace Fusion {
  using UnityEngine;


  /// <summary>
  /// Component which automatically faces this GameObject toward the supplied Camera. If Camera == null, will face towards Camera.main.
  /// </summary>
  [Fusion.ScriptHelp(BackColor = ScriptHeaderBackColor.Olive)]
  [ExecuteAlways]
  public class FusionBasicBillboard : Fusion.Behaviour {

    /// <summary>
    /// Force a particular camera to billboard this object toward. Leave null to use Camera.main.
    /// </summary>
    [InlineHelp]
    public Camera Camera;

    // Camera find is expensive, so do it once per update for ALL implementations
    static float _lastCameraFindTime;
    static Camera _currentCam;

    private void OnEnable() {
      UpdateLookAt();
    }

    private void OnDisable() {
      transform.localRotation = default;
    }

    Camera MainCamera {
      set {
        _currentCam = value;
      }
      get {

        var time = Time.time;
        // Only look for the camera once per Update.
        if (time == _lastCameraFindTime)
          return _currentCam;

        _lastCameraFindTime = time;
        var cam = Camera.main;
        _currentCam = cam;
        return cam;
      }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos() {
      LateUpdate();
    }
#endif

    private void LateUpdate() {
      UpdateLookAt();
    }

    public void UpdateLookAt() {

      var cam = Camera ? Camera : MainCamera;

      if (cam) {
        if (enabled) {
          transform.rotation = cam.transform.rotation;
        }
      }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() {
      _currentCam = default;
      _lastCameraFindTime = default;
    }
  }
}