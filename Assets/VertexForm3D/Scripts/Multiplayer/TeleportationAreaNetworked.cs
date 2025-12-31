using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using VertexFormCore;

[RequireComponent(typeof(TeleportationArea))]
public class TeleportationAreaNetworked : MonoBehaviour
{
    TeleportationArea teleportationArea;

    private void OnEnable()
    {
        teleportationArea = GetComponent<TeleportationArea>();
        teleportationArea.interactionLayers = LayerMask.GetMask("Teleport");
        if (RoomManager.Instance != null)
        {
            SetTeleportation();
        }
    }
    void SetTeleportation()
    {
        if (RoomManager.Instance.localVRPlayer != null)
        {
            teleportationArea.teleportationProvider = RoomManager.Instance.localVRPlayer.GetComponent<PlayerNetworkSetup>().tp;
        }
        else
        {
            Invoke(nameof(SetTeleportation), 1);
        }
    }

}
