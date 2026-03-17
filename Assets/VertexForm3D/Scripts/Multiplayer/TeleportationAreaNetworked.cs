using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using VertexFormCore;

[RequireComponent(typeof(TeleportationArea))]
public class TeleportationAreaNetworked : MonoBehaviour
{
    TeleportationArea teleportationArea;
    const float k_RetryInterval = 0.5f;
    const int k_MaxRetries = 40; // ~20 seconds total
    int retryCount;

    private void OnEnable()
    {
        teleportationArea = GetComponent<TeleportationArea>();
        teleportationArea.interactionLayers = LayerMask.GetMask("Teleport");
        retryCount = 0;
        if (RoomManager.Instance != null)
        {
            SetTeleportation();
        }
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(SetTeleportation));
    }

    void SetTeleportation()
    {
        var pns = RoomManager.Instance?.GetLocalPlayerSetup();
        if (pns != null && pns.tp != null)
        {
            teleportationArea.teleportationProvider = pns.tp;
            Debug.Log($"[Teleport Debug] TeleportationAreaNetworked.SetTeleportation: obj={gameObject.name} provider=set");
            return;
        }
        if (RoomManager.Instance == null) return;
        if (retryCount < k_MaxRetries)
        {
            retryCount++;
            if (retryCount == 1)
                Debug.Log($"[Teleport Debug] TeleportationAreaNetworked.SetTeleportation: obj={gameObject.name} local player not ready, retrying every {k_RetryInterval}s");
            Invoke(nameof(SetTeleportation), k_RetryInterval);
        }
    }
}
