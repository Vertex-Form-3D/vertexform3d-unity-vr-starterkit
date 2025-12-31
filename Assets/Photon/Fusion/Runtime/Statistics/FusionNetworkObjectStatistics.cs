namespace Fusion.Statistics {
using UnityEngine;

  [RequireComponent(typeof(NetworkObject)), DisallowMultipleComponent]
  [AddComponentMenu("Fusion/Statistics/Network Object Statistics")]
  public class FusionNetworkObjectStatistics : MonoBehaviour {
    [HideInInspector]
    public NetworkObject NetworkObject;

    private void ToggleMonitoring(bool value) {
      NetworkObject = GetComponent<NetworkObject>();
      if (NetworkObject.Runner && NetworkObject.Runner.IsRunning) {
        if (NetworkObject.Runner.TryGetComponent<FusionStatistics>(out var statistics)) {
          if (statistics.MonitorNetworkObject(NetworkObject, this, value))
            return;
        }
      }

      // If not running or don't have the statistics manager or NO is already added on the graph, destroy for now.
      Destroy(this);
    }

    private void OnEnable() {
      ToggleMonitoring(true);
    }

    private void OnDisable() {
      ToggleMonitoring(false);
    }
  }
}