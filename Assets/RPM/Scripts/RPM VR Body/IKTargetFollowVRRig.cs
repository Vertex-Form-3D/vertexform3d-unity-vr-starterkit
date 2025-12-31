using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class VRMap
{
    public bool rotate = true;
    public bool position = true;
    public Transform vrControllerTarget;
    public Vector3 controllerTrackingPositionOffset;
    public Vector3 controllerTrackingRotationOffset;

    [Space(5)]
    public Transform vrHandTarget;
    public Vector3 handTrackingPositionOffset;
    public Vector3 handTrackingRotationOffset;

    [Space(5)]
    public Transform ikTarget;

    public void Map()
    {
        if (vrControllerTarget.gameObject.activeInHierarchy)
        {
            if (position)
            {
                ikTarget.position = vrControllerTarget.TransformPoint(controllerTrackingPositionOffset);
            }
            if (rotate)
            {
                ikTarget.rotation = vrControllerTarget.rotation * Quaternion.Euler(controllerTrackingRotationOffset);
            }
        }
        else
        {
            if (position)
            {
                ikTarget.position = vrHandTarget.TransformPoint(handTrackingPositionOffset);
            }
            if (rotate)
            {
                ikTarget.rotation = vrHandTarget.rotation * Quaternion.Euler(handTrackingRotationOffset);
            }
        }
    }
}

public class IKTargetFollowVRRig : MonoBehaviour
{
    public static bool UseLegsIK = true;
    public VRMap head;
    public VRMap leftHand;
    public VRMap rightHand;
    public Transform leftLegTransform;
    public Transform rightLegTransform;
    public CharacterController characterController;
    [Header("Body")] public Vector3 headBodyPositionOffset;
    public float headBodyYawOffset;

    [SerializeField] private InputActionReference leftStickInput;
    [SerializeField] private float movementThreshold = 0.1f;
    [SerializeField] private float blendSpeed = 5f;
    [Range(0, 1)] public float turnSmoothness = 0.1f;

    public bool useHeadIK = true;
    private Animator animator;
    private float currentBlendValue = 1f; // Start in idle (1)
    private const float WalkForwardValue = 0f;
    private const float IdleValue = 1f;
    private const float WalkBackwardValue = 2f;
    public bool footIKActive = true;

    [Header("Foot IK")]
    public Transform leftFootIKTarget;   // Target for Left Foot TwoBoneIKConstraint
    public Transform rightFootIKTarget;  // Target for Right Foot TwoBoneIKConstraint
    public Transform leftFootIKHint;     // Optional hint (knee direction)
    public Transform rightFootIKHint;    // Optional hint
    public float footIKRayDistance = 2f;
    public float footIKRayStartHeight = 1f; // Height above animated foot to start raycast
    public float footHeightOffset = 0.05f;  // Small offset so foot doesn't sink into ground
    public LayerMask groundLayerMask = ~0; // Set to your ground layers
    [Header("Multiplayer Handler")]
    public float footOffset = .75f;
    public NetworkObject networkObject;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        if (networkObject != null)
        {
            if (!networkObject.HasInputAuthority)
            {
                return;
            }
        }
        leftStickInput.action.Enable();
    }

    private void OnDisable()
    {
        if (networkObject != null)
        {
            if (!networkObject.HasInputAuthority)
            {
                return;
            }
        }
        leftStickInput.action.Disable();
    }

    private void Update()
    {        
        if (networkObject != null)
        {
            if (!networkObject.HasInputAuthority)
            {
                return;
            }
        }
        if (footIKActive)
        {
            ManageFootIK();
        }
        PlayLegsAnimation();

    }

    private void LateUpdate()
    {
        if (useHeadIK)
        {
            if (head.position)
            {
                transform.position = head.ikTarget.position + headBodyPositionOffset;
                //transform.position = new Vector3(head.ikTarget.position.x + headBodyPositionOffset.x, transform.position.y, head.ikTarget.position.z + headBodyPositionOffset.z);
            }
            if (head.rotate)
            {
                float yaw = head.vrControllerTarget.eulerAngles.y;
                transform.rotation = Quaternion.Lerp(transform.rotation,
                    Quaternion.Euler(transform.eulerAngles.x, yaw, transform.eulerAngles.z), turnSmoothness);
            }

            head.Map();
            leftHand.Map();
            rightHand.Map();
        }
    }

    public void ManageFoot()
    {
        Vector3 leftLegValue = leftLegTransform.transform.localPosition;
        Vector3 rightLegValue = rightLegTransform.transform.localPosition;
        rightLegValue.y = leftLegValue.y = -(characterController.height - footOffset);
        leftLegTransform.transform.localPosition = leftLegValue;
        rightLegTransform.transform.localPosition = rightLegValue;
    }

    private void PlayLegsAnimation()
    {
        // Get stick input (Y axis for forward/backward)
        float stickY = leftStickInput.action.ReadValue<Vector2>().y;

        // Determine target blend value based on input
        float targetBlendValue;

        if (stickY > movementThreshold) // Moving forward
        {
            // Map input from (threshold to 1) to (0 to 1)
            float normalizedInput = Mathf.InverseLerp(movementThreshold, 1f, stickY);
            targetBlendValue = Mathf.Lerp(IdleValue, WalkForwardValue, normalizedInput);
        }
        else if (stickY < -movementThreshold) // Moving backward
        {
            // Map input from (-threshold to -1) to (1 to 2)
            float normalizedInput = Mathf.InverseLerp(-movementThreshold, -1f, stickY);
            targetBlendValue = Mathf.Lerp(IdleValue, WalkBackwardValue, normalizedInput);
        }
        else // Idle
        {
            targetBlendValue = IdleValue;
        }

        // Smoothly transition to target blend value
        currentBlendValue = Mathf.Lerp(currentBlendValue, targetBlendValue, Time.deltaTime * blendSpeed);

        // Set the animator parameter
        animator.SetFloat("Walk", currentBlendValue);
    }


    // New proper foot IK method
    private void ManageFootIK()
    {
        if (leftFootIKTarget == null || rightFootIKTarget == null) return;

        // Left foot
        PlaceFootOnGround(leftLegTransform, leftFootIKTarget, leftFootIKHint);

        // Right foot
        PlaceFootOnGround(rightLegTransform, rightFootIKTarget, rightFootIKHint);
    }

    private void PlaceFootOnGround(Transform footBone, Transform ikTarget, Transform ikHint)
    {
        // Start raycast from slightly above the current animated foot position
        Vector3 rayOrigin = footBone.position + Vector3.up * footIKRayStartHeight;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, footIKRayDistance + footIKRayStartHeight, groundLayerMask, QueryTriggerInteraction.Ignore))
        {
            // Position target on ground
            ikTarget.position = hit.point + Vector3.up * footHeightOffset;

            // Rotate target to match ground normal (for slopes)
            Quaternion groundRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            //ikTarget.localRotation = groundRotation * Quaternion.Euler(0, footBone.localRotation.eulerAngles.y, 0); // Preserve animated yaw

            // Optional: Position hint for better knee bend (outward from hip)
            if (ikHint != null)
            {
                Vector3 kneeDirection = (footBone.position - transform.position).normalized;
                kneeDirection = Vector3.ProjectOnPlane(kneeDirection, hit.normal); // Avoid inward collapse
                ikHint.position = footBone.position + kneeDirection * 0.5f; // Adjust distance as needed
            }
        }
        else
        {
            // No ground hit - fallback to animated position (e.g., when foot is in air)
            ikTarget.position = footBone.position;
            ikTarget.rotation = footBone.rotation;
        }
    }
    public void SetUseHeadIK(bool useHeadIK)
    {
        this.useHeadIK = useHeadIK;
    }
}