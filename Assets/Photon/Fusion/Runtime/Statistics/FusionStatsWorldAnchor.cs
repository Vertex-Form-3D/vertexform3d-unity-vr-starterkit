namespace Fusion.Statistics {
  using System;
  using UnityEngine;

  [DisallowMultipleComponent]
  [AddComponentMenu("Fusion/Statistics/Statistics World Anchor")]
  public class FusionStatsWorldAnchor : MonoBehaviour {
    private void OnEnable() {
      FusionStatsConfig.SetWorldAnchorCandidate(transform, true);
    }

    private void OnDisable() {
      FusionStatsConfig.SetWorldAnchorCandidate(transform, false);
    }

    private void OnDestroy() {
      // Saving stats if is child
      var stats = transform.GetComponentInChildren<FusionStatsCanvas>();
      if (stats) {
        stats.transform.SetParent(null);
        stats.GetComponentInChildren<FusionStatsConfig>(true).ResetToCanvasAnchor();
      }
    }
  }
}