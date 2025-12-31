using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using Fusion;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

public class SittingController : NetworkBehaviour
{
    [Header("XR Input")]
    [SerializeField] private InputActionReference moveAction;

    [Header("Avatar Setup")]
    [SerializeField] private float sitYOffset = -0.52f;
    [SerializeField] private Animator animator;
    [SerializeField] private RigBuilder rigBuilder;
    [SerializeField] private Transform avatarRoot;
    [SerializeField] private XRHandToAvatar leftXRHand;
    [SerializeField] private XRHandToAvatar rightXRHand;
    [SerializeField] private LoadRPMAvatar loadRPMAvatar;
    [SerializeField] private IKTargetFollowVRRig iKTargetFollowVRRig;
    [SerializeField] private XRBodyTransformer xrBodyTransformer;
    [SerializeField] private CharacterController locomotionController;
    [SerializeField] private Transform cameraOffsetTransform;
    private bool isFemale;
    public NetworkObject networkObject;
    private SitSpot currentSpot;
    private bool isSitting;

    [OnChangedRender(nameof(HandleRig))]
    [Networked] public NetworkBool IsSitting { get; set; }

    public void SetAvatarGender(bool isFemale)
    {
        this.isFemale = isFemale;
    }

    public void HandleRig()
    {
        rigBuilder.enabled = !isSitting;
    }

    private void Start()
    {
        HandleRig();
        moveAction.action.performed += ctx =>
        {
            if (isSitting && IsPlayerTryingToMove())
            {
                StandUp();
            }
        };
    }
    public void HandleSitRequest(SitSpot spot)
    {
        if (isSitting) return;
        SitAt(spot);
    }
    public float yval;
    private void SitAt(SitSpot spot)
    {
        // Disable movement
        iKTargetFollowVRRig.SetUseHeadIK(false);
        xrBodyTransformer.enabled = false;
        locomotionController.enabled = false;
        yval = cameraOffsetTransform.localPosition.y;
        currentSpot = spot;
        currentSpot.SetOccupied(true);
        isSitting = true;
        rigBuilder.enabled = false;

        // Align perfectly with seat with Y offset

        // Play animation
        var animName = isFemale ? "FemaleSit" : "MaleSit";
        animator.SetBool(animName, true);

        leftXRHand.enabled = false;
        rightXRHand.enabled = false;
        if (networkObject != null)
        {
            IsSitting = isSitting;
        }
        Vector3 sitPosition = spot.SitPoint.position + Vector3.up * sitYOffset;
        avatarRoot.SetPositionAndRotation(sitPosition, spot.SitPoint.rotation);
        rigBuilder.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        cameraOffsetTransform.localPosition = Vector3.up * -.3f;
#if UNITY_EDITOR
        if (IsDeviceSimulatorActive())
        {
            cameraOffsetTransform.localPosition = Vector3.up * .45f;
        }
#endif
    }

    private bool IsDeviceSimulatorActive()
    {
        var ds = FindAnyObjectByType<XRDeviceSimulator>();
        if (ds != null)
        {
            return true;
        }
        return false;
    }

    private void StandUp()
    {
        var animName = isFemale ? "FemaleSit" : "MaleSit";
        animator.SetBool(animName, false);
        iKTargetFollowVRRig.SetUseHeadIK(true);
        rigBuilder.enabled = true;
        locomotionController.enabled = true;
        xrBodyTransformer.enabled = true;
        currentSpot.SetOccupied(false);
        leftXRHand.enabled = true;
        rightXRHand.enabled = true;
        isSitting = false;
        currentSpot = null;
        cameraOffsetTransform.localPosition = Vector3.up * yval;
        if (networkObject != null)
        {
            IsSitting = isSitting;
        }
    }

    private bool IsPlayerTryingToMove()
    {
        if (moveAction == null || moveAction.action == null)
            return false;

        Vector2 input = moveAction.action.ReadValue<Vector2>();
        return input.sqrMagnitude > 0.1f;
    }
}
