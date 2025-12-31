using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Hands.Samples.GestureSample;
using Fusion;

public class XRHandPinchTeleport : MonoBehaviour
{
    public XRNode handNode = XRNode.LeftHand;
    public TeleportationProvider teleportationProvider;
    public XRRayInteractor rayInteractor;
    public StaticHandGesture teleportHandGesture;
    public XRInteractorLineVisual lineVisual;
    public float pinchThreshold = 0.02f;
    bool canTeleport;
    private XRHandSubsystem handSubsystem;
    private bool teleportQueued = false;
    public NetworkObject networkObject;
    void Start()
    {
        if (networkObject != null)
        {
            if (!networkObject.HasInputAuthority)
            {
                Destroy(teleportHandGesture);
                Destroy(rayInteractor.gameObject);
                Destroy(this);
            }
        }
        var subsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        if (subsystems.Count > 0)
            handSubsystem = subsystems[0];
        teleportHandGesture.gesturePerformed.AddListener(OnTeleportGesturePerformed);
        teleportHandGesture.gestureEnded.AddListener(OnTeleportGestureEnded);
    }

    private void OnTeleportGestureEnded()
    {
        EnableRay(false);
    }

    private void OnTeleportGesturePerformed()
    {
        EnableRay(true);
    }

    void Update()
    {
        if (handSubsystem == null || !handSubsystem.running) return;

        XRHand hand = handNode == XRNode.LeftHand ? handSubsystem.leftHand : handSubsystem.rightHand;
        if (!hand.isTracked|| !canTeleport) return;

        var thumb = hand.GetJoint(XRHandJointID.ThumbTip);
        var index = hand.GetJoint(XRHandJointID.IndexTip);

        if (thumb.TryGetPose(out Pose thumbPose) && index.TryGetPose(out Pose indexPose))
        {
            float distance = Vector3.Distance(thumbPose.position, indexPose.position);

            if (distance < pinchThreshold)
            {
                if (!teleportQueued && rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
                {
                    if (GradientsAreEqual(lineVisual.GetComponent<LineRenderer>().colorGradient,lineVisual.validColorGradient))
                    {
                        teleportQueued = true;

                        var request = new TeleportRequest
                        {
                            destinationPosition = hit.point,
                            matchOrientation = MatchOrientation.None
                        };
                        teleportationProvider.QueueTeleportRequest(request);
                    }
                }
            }
            else
            {
                teleportQueued = false;
            }
        }
    }
    public bool GradientsAreEqual(Gradient g1, Gradient g2)
    {
        if (g1.mode != g2.mode) return false;

        var colorKeys1 = g1.colorKeys;
        var colorKeys2 = g2.colorKeys;
        if (colorKeys1.Length != colorKeys2.Length) return false;
        for (int i = 0; i < colorKeys1.Length; i++)
        {
            if (colorKeys1[i].color != colorKeys2[i].color ||
                !Mathf.Approximately(colorKeys1[i].time, colorKeys2[i].time))
                return false;
        }

        var alphaKeys1 = g1.alphaKeys;
        var alphaKeys2 = g2.alphaKeys;
        if (alphaKeys1.Length != alphaKeys2.Length) return false;
        for (int i = 0; i < alphaKeys1.Length; i++)
        {
            if (!Mathf.Approximately(alphaKeys1[i].alpha, alphaKeys2[i].alpha) ||
                !Mathf.Approximately(alphaKeys1[i].time, alphaKeys2[i].time))
                return false;
        }

        return true;
    }
    private void EnableRay(bool enable)
    {
        canTeleport = enable;
        rayInteractor.enabled = enable;
        lineVisual.enabled = enable;
    }
}
