using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using VertextFormCore;

[RequireComponent(typeof(TeleportationArea))]
public class TeleportationAreaNetworked : MonoBehaviour
{
    TeleportationArea teleportationArea;

    private void OnEnable()
    {
        teleportationArea = GetComponent<TeleportationArea>();
        SetTeleportation();
    }
    void SetTeleportation()
    {
        if (SpawnManager.Instance.localVRPlayer != null)
        {
            teleportationArea.teleportationProvider = SpawnManager.Instance.localVRPlayer.GetComponent<PlayerNetworkSetup>().tp;
        }
        else
        {
            Invoke(nameof(SetTeleportation), 1);
        }
    }

}
