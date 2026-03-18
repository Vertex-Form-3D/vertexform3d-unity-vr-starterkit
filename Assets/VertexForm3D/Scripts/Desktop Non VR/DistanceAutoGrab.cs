using Photon.Voice;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class DistanceAutoGrab : MonoBehaviour
{
    [SerializeField] private XRInteractionManager interactionManager; // Assign in Inspector
    [SerializeField] private bool useFarGrab = false; // Toggle between near (false) and far (true) grabbing
    [SerializeField] private float grabRange = 3f; // Max distance for far grab (e.g., 3 meters)
    [SerializeField] private float pullSpeed = 5f; // Speed to pull object in far grab mode
    [SerializeField] private bool usePhysicsPull = true; // If true, use physics to pull; if false, instant grab in far mode
    [SerializeField] private LayerMask grabLayerMask; // Layers for grabable objects
    [SerializeField] private float nearGrabRadius = 0.5f; // Trigger collider radius for near grab
    [SerializeField] private float farGrabRadius = 0.5f; // Trigger collider radius for far grab
    [SerializeField] private Transform handTransform; // Reference to the hand transform
    public float rotationSpeed = 45f;
    public IXRSelectInteractor interactor;
    public XRGrabInteractable targetInteractable; // Next potential target (set by scan when not grabbing)
    private XRGrabInteractable currentGrabbedInteractable; // Actually held object - used for ungrab/trigger so reference is never lost
    public float checkInterval = 0.2f; // Scan interval for far grab
    private float checkTimer = 0f;
    private bool isGrabbing = false; // Tracks if a grab is active for toggle logic
    public bool canRotate;
    public bool inverted;
    public SphereCollider triggerCollider;
    public Vector2 mousePos;

    [Header("Input Actions")]
    public InputAction grabKeyPressed;
    public InputAction triggerKeyPressed;
    public InputAction rotateKeyPressed;
    public InputAction axis;

    private void Awake()
    {
        if (ProjectManager.instance.platforms.platformChoice == platform.VR)
        {
            Destroy(this);
        }
        interactor = GetComponent<IXRSelectInteractor>();
        if (interactor == null)
        {
            Debug.LogError("No IXRSelectInteractor found. Add XR Socket Interactor or XR Simple Interactor.");
            enabled = false; // Disable script to prevent errors
            return;
        }

        triggerCollider = GetComponent<SphereCollider>();
        if (triggerCollider == null)
        {
            Debug.LogError("No SphereCollider found. Near grab requires a trigger collider.");
            enabled = false; // Disable script to prevent errors
            return;
        }
        else
        {
            triggerCollider.isTrigger = true;
            triggerCollider.radius = nearGrabRadius;
        }

        if (handTransform == null)
        {
            Debug.LogWarning("Hand Transform not assigned. Using this transform as fallback.");
            handTransform = transform; // Fallback to this transform if not assigned
        }

        Invoke(nameof(GetInteractionManager), 1f);
        InitController();
    }

    public void InitController()
    {
        grabKeyPressed.Enable();
        triggerKeyPressed.Enable();
        rotateKeyPressed.Enable();
        axis.Enable();

        grabKeyPressed.performed += OnGrabPerformed;
        triggerKeyPressed.performed += OnTriggerPerformed;
        rotateKeyPressed.performed += (ctx) => { StartCoroutine(Rotate()); };
        rotateKeyPressed.canceled += (ctx) => { canRotate = false; };
        axis.performed += context => { mousePos = context.ReadValue<Vector2>(); };

        axis.canceled += context => { mousePos = Vector2.zero; };
        FindFarGrabTarget();
    }

    private void OnDestroy()
    {
        grabKeyPressed.Disable();
        triggerKeyPressed.Disable();
        rotateKeyPressed.Disable();
        axis.Disable();
        grabKeyPressed.performed -= OnGrabPerformed;
        triggerKeyPressed.performed -= OnTriggerPerformed;
    }

    private void GetInteractionManager()
    {
        if (interactionManager == null)
        {
            interactionManager = FindAnyObjectByType<XRInteractionManager>();
            if (interactionManager == null)
            {
                Debug.LogError("No XRInteractionManager found in scene.");
                enabled = false;
            }
        }
    }

    private IEnumerator Rotate()
    {
        canRotate = true;
        while (canRotate)
        {
            // Apply rotation based on mouse input
            float deltaTime = Time.deltaTime;
            float xRotation = mousePos.y * rotationSpeed * deltaTime * (inverted ? -1 : 1);
            float yRotation = mousePos.x * rotationSpeed * deltaTime * (inverted ? 1 : -1);

            // Create rotation quaternions
            Quaternion rotX = Quaternion.Euler(xRotation, 0f, 0f);
            Quaternion rotY = Quaternion.Euler(0f, yRotation, 0f);

            // Apply rotations sequentially
            Quaternion currentRotation = handTransform.localRotation;
            Quaternion targetRotation = currentRotation * rotY * rotX;

            // Convert to Euler angles for clamping
            Vector3 euler = targetRotation.eulerAngles;
            // Normalize angles to -180 to 180 range
            float xAngle = NormalizeAngle(euler.x);
            float yAngle = NormalizeAngle(euler.y);

            // Clamp angles
            xAngle = Mathf.Clamp(xAngle, -80f, 40f);
            yAngle = Mathf.Clamp(yAngle, -60f, 90f);

            // Apply clamped rotation, keeping z at 0
            handTransform.localRotation = Quaternion.Euler(xAngle, yAngle, 0f);

            yield return null;
        }
    }

    // Helper function to normalize angles to -180 to 180
    private float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    private void OnGrabPerformed(InputAction.CallbackContext ctx)
    {
        if (!isGrabbing)
        {
            Grab();
        }
        else
        {
            UnGrab();
        }
    }

    private void OnTriggerPerformed(InputAction.CallbackContext ctx)
    {
        var held = currentGrabbedInteractable ?? targetInteractable;
        if (isGrabbing && held != null)
        {
            held.activated?.Invoke(null);
            Debug.Log($"Trigger activated on: {held.name}");
        }
    }

    public void Grab()
    {
        if (targetInteractable != null && !targetInteractable.isSelected)
        {
            interactionManager.SelectEnter(interactor, targetInteractable);
            if (interactor.interactablesSelected.Count > 0 && interactor.interactablesSelected[0] == targetInteractable)
            {
                isGrabbing = true;
                currentGrabbedInteractable = targetInteractable; // Keep reference so we can always ungrab
                StopFindFarGrabTarget();
                //Debug.Log($"Grabbed: {targetInteractable.name}");
            }
            else
            {
                //Debug.LogWarning("Failed to grab target interactable.");
                isGrabbing = false; // Ensure state is consistent
            }
        }
        else
        {
            //Debug.Log("No valid target interactable to grab.");
            isGrabbing = false; // Reset state if no valid target
        }
    }

    public void UnGrab()
    {
        // Prefer our stored reference so ungrab works even if interactablesSelected is out of sync
        var toRelease = currentGrabbedInteractable;
        if (toRelease == null && interactor.interactablesSelected.Count > 0)
            toRelease = interactor.interactablesSelected[0] as XRGrabInteractable;

        if (toRelease != null)
        {
            interactionManager.SelectExit(interactor, toRelease);
            //Debug.Log($"Released: {toRelease.name}");
        }

        isGrabbing = false;
        currentGrabbedInteractable = null;
        targetInteractable = null; // Clear so coroutine can set next target
        FindFarGrabTarget();
    }

    public Coroutine FindFarGrabTargetCoroutine;

    public IEnumerator IEFindFarGrabTarget()
    {
        grabRange = useFarGrab ? farGrabRadius : nearGrabRadius;
        while (true)
        {
            if (!isGrabbing)
            {
                Collider[] hits = Physics.OverlapSphere(transform.position, grabRange, grabLayerMask);
                float closestDistance = float.MaxValue;
                XRGrabInteractable closestInteractable = null;

                foreach (var hit in hits)
                {
                    XRGrabInteractable interactable = hit.GetComponent<XRGrabInteractable>();
                    if (interactable == null)
                    {
                        interactable = hit.GetComponentInParent<XRGrabInteractable>();
                    }

                    if (interactable != null && !interactable.isSelected)
                    {
                        float distance = Vector3.Distance(transform.position, interactable.transform.position);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestInteractable = interactable;
                        }
                    }
                }

                targetInteractable = closestInteractable;
                if (targetInteractable == null)
                {
                    //Debug.Log("No valid grab target found.");
                }
            }
            yield return new WaitForSeconds(checkInterval);
        }
    }

    private void FindFarGrabTarget()
    {
        if (FindFarGrabTargetCoroutine != null)
        {
            StopCoroutine(FindFarGrabTargetCoroutine);
        }
        FindFarGrabTargetCoroutine = StartCoroutine(IEFindFarGrabTarget());
    }

    private void StopFindFarGrabTarget()
    {
        if (FindFarGrabTargetCoroutine != null)
        {
            StopCoroutine(FindFarGrabTargetCoroutine);
            FindFarGrabTargetCoroutine = null;
        }
    }

    public void ToggleGrabMode(bool farGrab)
    {
        useFarGrab = farGrab;
        triggerCollider.radius = useFarGrab ? farGrabRadius : nearGrabRadius;
        targetInteractable = null;
        currentGrabbedInteractable = null;
        isGrabbing = false; // Reset grabbing state on mode switch
        FindFarGrabTarget();
        //Debug.Log($"Switched to {(farGrab ? "Far" : "Near")} grab mode");
    }

    private void Update()
    {
        // Periodically verify isGrabbing state (e.g. object was destroyed or selection lost)
        if (isGrabbing && interactor.interactablesSelected.Count == 0)
        {
            //Debug.LogWarning("isGrabbing was true but no object is selected. Resetting state.");
            isGrabbing = false;
            currentGrabbedInteractable = null;
            targetInteractable = null;
            FindFarGrabTarget();
        }

        // Apply pull effect for far grab with physics (use held object reference)
        var held = currentGrabbedInteractable ?? targetInteractable;
        if (isGrabbing && useFarGrab && usePhysicsPull && held != null)
        {
            Rigidbody rb = held.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 direction = (handTransform.position - held.transform.position).normalized;
                rb.AddForce(direction * pullSpeed, ForceMode.VelocityChange);
            }
        }
    }

    private void OnDrawGizmos()
    {
        grabRange = useFarGrab ? farGrabRadius : nearGrabRadius;
        if (useFarGrab)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, grabRange);
        }
    }

    private void OnDrawGizmosSelected()
    {
        grabRange = useFarGrab ? farGrabRadius : nearGrabRadius;
        if (useFarGrab)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, grabRange);
        }
    }
}