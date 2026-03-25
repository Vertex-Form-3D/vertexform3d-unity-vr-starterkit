using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using VertexFormCore;


public class PlayerTeleportationHandler : MonoBehaviour
{
    [Header("Teleport Settings")]
    [Tooltip("The XR Rig (usually XROrigin) that will be teleported")]
    public XROrigin xrOrigin;

    [Tooltip("The target position and rotation to teleport to")]
    public Transform teleportTarget;

    [Tooltip("Optional: If true, matches the target's rotation (Y-axis only recommended)")]
    public bool matchTargetRotation = true;

    [Tooltip("Smooth teleport (fade) or instant?")]
    public bool useSmoothTeleport = true;

    // Reference to the Teleportation Provider (usually already on XR Origin)
    private UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider teleportationProvider;

    private void Awake()
    {
        GetXROrigin();
    }

    void GetXROrigin()
    {
        if (xrOrigin == null)
            if (GetXrOriginCoroutine != null)
            {
                StopCoroutine(GetXrOriginCoroutine);
            }
        GetXrOriginCoroutine = StartCoroutine(IEGetXROrigin());
    }
    Coroutine GetXrOriginCoroutine;
    IEnumerator IEGetXROrigin()
    {
        while (RoomManager.Instance.localVRPlayer == null || RoomManager.Instance.GetLocalPlayerSetup() == null)
        {
            yield return new WaitForSeconds(1);
        }
        xrOrigin = RoomManager.Instance.localVRPlayer.GetComponentInChildren<XROrigin>();
        var pns = RoomManager.Instance.GetLocalPlayerSetup();
        if (pns != null) teleportationProvider = pns.tp;
    }

    // Call this method to teleport (e.g., from a button, trigger, or UI)
    public void TeleportPlayer()
    {
        if (xrOrigin == null || teleportTarget == null || teleportationProvider == null)
        {
            Debug.LogWarning("Teleport failed: Missing references.");
            return;
        }

        // Create teleport request
        var request = new UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportRequest
        {
            destinationPosition = teleportTarget.position,
            destinationRotation = matchTargetRotation ? teleportTarget.rotation : xrOrigin.transform.rotation,
            matchOrientation = matchTargetRotation ? UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.MatchOrientation.TargetUpAndForward : UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.MatchOrientation.None
        };

        // Queue the teleport (handles fade-in/out if Locomotion System has fade set up)
        teleportationProvider.QueueTeleportRequest(request);
    }

    // Optional: Instant teleport without fade (bypasses Locomotion System)
    public void TeleportPlayerInstant()
    {
        if (xrOrigin == null || teleportTarget == null) return;

        Vector3 destination = teleportTarget.position;

        // Optional: Keep player height offset (e.g., camera is 1.8m above rig)
        Vector3 cameraLocalPos = xrOrigin.Camera.transform.position;
        destination.y += cameraLocalPos.y;

        xrOrigin.MoveCameraToWorldLocation(destination);

        if (matchTargetRotation)
        {
            Vector3 euler = teleportTarget.rotation.eulerAngles;
            xrOrigin.RotateAroundCameraUsingOriginUp(Quaternion.Euler(0, euler.y, 0).eulerAngles.y);
        }
    }

    // Example: Call from UI Button
    public void OnTeleportButtonPressed()
    {
        if (useSmoothTeleport)
            TeleportPlayer();
        else
            TeleportPlayerInstant();
    }
}
