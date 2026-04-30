using Photon.Voice;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class OrbitCamera : MonoBehaviour
{
    [SerializeField] private XRRigController rigController;
    [Header("Target Settings")]
    [SerializeField] private Transform target; // The object to orbit around
    public Vector3 targetOffset = Vector3.up * 1f; // Offset from target's position (e.g., head height)

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 100f; // Speed of camera rotation
    [SerializeField] private bool invertY = true; // Invert vertical mouse input
    [SerializeField] private float minVerticalAngle = -80f; // Minimum vertical angle (degrees)
    [SerializeField] private float maxVerticalAngle = 80f; // Maximum vertical angle (degrees)
    [SerializeField] private float minHorizontalAngle = -80f; // Minimum horizontal angle (degrees)
    [SerializeField] private float maxHorizontalAngle = 80f; // Maximum horizontal angle (degrees)

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 2f; // Speed of zooming
    public float minZoomDistance = 2f; // Closest zoom distance
    [SerializeField] private float maxZoomDistance = 10f; // Farthest zoom distance

    [Header("Collision Settings")]
    [SerializeField] private LayerMask collisionLayers; // Layers to collide with
    [SerializeField] private float cameraRadius = 0.3f; // Radius for sphere cast (used for visualization)
    [SerializeField] private float collisionBuffer = 0.3f; // Extra buffer distance from collision point
    public Collider collisionCollider;
    [SerializeField] private List<Collider> ignoredColliders = new List<Collider>(); // Array of colliders to exclude from collision detection
    public bool isIgnored = false;
    public Vector2 rotationValue;
    public Vector2 previousValue;
    private bool rotateAllowed;
    public float currentYaw = 0f; // Horizontal rotation angle
    public float currentPitch = 0f; // Vertical rotation angle
    public float currentDistance; // Current distance from target
    private float targetDistance;
    [SerializeField] private float zoomSmoothTime = 0.15f;
    private float zoomVelocity;
    public InputAction pressed;
    public InputAction axis;
    public InputAction scroll;
    public Camera mainCamera;
    private Vector3 defaultTargetOffset;
    private void Awake()
    {
        if (rigController == null)
            rigController = GetComponentInParent<XRRigController>();

        defaultTargetOffset = targetOffset;
        Debug.Log("[OrbitCamera] Awake started");
        if (ProjectManager.instance.platforms.IsVrStylePlatform())
        {
            Destroy(this);
            return;
        }
        if (target == null)
        {
            Debug.LogWarning("[OrbitCamera] Awake: target is NULL - orbit will not work until SetTarget() is called.");
        }
        // Initialize zoom distance
        currentDistance = (minZoomDistance + maxZoomDistance) / 2f;

        // Initialize rotation based on camera's starting position
        if (target != null)
        {
            Vector3 directionToCamera = transform.position - (target.position + targetOffset);
            currentDistance = directionToCamera.magnitude;
            currentYaw = Mathf.Atan2(directionToCamera.x, directionToCamera.z) * Mathf.Rad2Deg;
            currentPitch = -Mathf.Asin(directionToCamera.y / currentDistance) * Mathf.Rad2Deg;
            Debug.Log($"[OrbitCamera] Awake: initialized from target, currentDistance={currentDistance:F2}, yaw={currentYaw:F1}, pitch={currentPitch:F1}");
        }
        targetDistance = currentDistance;

        if (pressed == null) { Debug.LogError("[OrbitCamera] Awake: 'pressed' InputAction is NULL - assign in Inspector!"); return; }
        if (axis == null) { Debug.LogError("[OrbitCamera] Awake: 'axis' InputAction is NULL - assign in Inspector!"); return; }
        if (scroll == null) { Debug.LogError("[OrbitCamera] Awake: 'scroll' InputAction is NULL - assign in Inspector!"); return; }
        axis.Enable();
        scroll.Enable();
        pressed.Enable();
        Debug.Log("[OrbitCamera] Awake: input actions enabled (pressed, axis, scroll)");

        pressed.performed += OnPressedPerformed;
        pressed.canceled += OnPressedCanceled;
        axis.performed += OnAxisPerformed;
        scroll.performed += OnScrollPerformed;
    }

    private void OnPressedPerformed(InputAction.CallbackContext _)
    {
        if (rigController != null && rigController.IsUiInputLocked)
            return;
        if (!DesktopPointerUIHelper.IsPointerOverUIThisFrame())
        {
            if (isActiveAndEnabled)
                StartCoroutine(Rotate());
        }
    }

    private void OnPressedCanceled(InputAction.CallbackContext _) => rotateAllowed = false;

    private void OnAxisPerformed(InputAction.CallbackContext context)
    {
        if (rigController != null && rigController.IsUiInputLocked)
        {
            rotationValue = Vector2.zero;
            return;
        }
        rotationValue = DesktopMobileControlSettings.SuppressLookWhileMultiTouch
            ? Vector2.zero
            : context.ReadValue<Vector2>();
    }

    private void OnScrollPerformed(InputAction.CallbackContext context) =>
        HandleZoom(context.ReadValue<float>());
    public void ResetTargetOffset()
    {
        targetOffset = defaultTargetOffset;
    }
    public void SetTargetOffsetToDefault()
    {
        defaultTargetOffset = targetOffset;
    }
    private void OnDestroy()
    {
        if (pressed != null)
        {
            pressed.performed -= OnPressedPerformed;
            pressed.canceled -= OnPressedCanceled;
            pressed.Disable();
        }
        if (axis != null)
        {
            axis.performed -= OnAxisPerformed;
            axis.Disable();
        }
        if (scroll != null)
        {
            scroll.performed -= OnScrollPerformed;
            scroll.Disable();
        }
    }

    private IEnumerator Rotate()
    {
        rotateAllowed = true;
        while (rotateAllowed)
        {
            if (rigController != null && rigController.IsUiInputLocked)
            {
                rotationValue = Vector2.zero;
                previousValue = Vector2.zero;
                yield return null;
                continue;
            }
            if (DesktopMobileControlSettings.SuppressLookWhileMultiTouch)
            {
                rotationValue = Vector2.zero;
                previousValue = Vector2.zero;
            }
            if (previousValue != rotationValue)
            {
                float mouseX = rotationValue.x * rotationSpeed * Time.deltaTime;
                float mouseY = rotationValue.y * rotationSpeed * Time.deltaTime * (invertY ? -1 : 1);

                currentYaw += mouseX;
                //currentYaw = Mathf.Clamp(currentYaw, minHorizontalAngle, maxHorizontalAngle); // Uncomment if you want to clamp yaw
                currentPitch = Mathf.Clamp(currentPitch + mouseY, minVerticalAngle, maxVerticalAngle);

                previousValue = rotationValue;
            }
            yield return null;
        }
    }

    /// <summary>Third-person mobile: apply pointer delta in the same units as the Look action (pixels per frame).</summary>
    public void ApplyTouchLookDelta(Vector2 deltaPixels)
    {
        if (!isActiveAndEnabled || target == null)
            return;
        if (rigController != null && rigController.IsUiInputLocked)
            return;
        float mouseX = deltaPixels.x * rotationSpeed * Time.deltaTime;
        float mouseY = deltaPixels.y * rotationSpeed * Time.deltaTime * (invertY ? -1 : 1);
        currentYaw += mouseX;
        currentPitch = Mathf.Clamp(currentPitch + mouseY, minVerticalAngle, maxVerticalAngle);
    }

    private static bool _loggedNullTarget;

    private void LateUpdate()
    {
        if (target == null)
        {
            if (!_loggedNullTarget)
            {
                Debug.LogWarning("[OrbitCamera] LateUpdate: target is NULL - skipping update. Set target via SetTarget() or assign in Inspector.");
                _loggedNullTarget = true;
            }
            return;
        }
        _loggedNullTarget = false;

        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref zoomVelocity, zoomSmoothTime);
        currentDistance = Mathf.Clamp(currentDistance, minZoomDistance, maxZoomDistance);

        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        Vector3 direction = rotation * Vector3.back;
        Vector3 targetPos = target.position + targetOffset;
        Vector3 desiredPosition = targetPos + direction * currentDistance;

        desiredPosition = HandleCollision(targetPos, desiredPosition, currentDistance, out float adjustedDistance);

        transform.position = desiredPosition;
        transform.rotation = rotation;
    }

    private static bool _loggedFirstOrbitZoom;

    private void HandleZoom(float scrollInput)
    {
        if (rigController != null && rigController.IsUiInputLocked)
            return;

        if (!_loggedFirstOrbitZoom)
        {
            Debug.Log($"[OrbitCamera] HandleZoom: first orbit scroll received, scrollInput={scrollInput}");
            _loggedFirstOrbitZoom = true;
        }
        targetDistance -= scrollInput * zoomSpeed;
        targetDistance = Mathf.Clamp(targetDistance, minZoomDistance, maxZoomDistance);
    }

    /// <summary>
    /// Scroll-equivalent zoom from touch pinch (or other code paths). Mouse wheel uses the Input System <see cref="scroll"/> action in <see cref="Awake"/>.
    /// </summary>
    public void ApplyExternalScroll(float scrollInput) => HandleZoom(scrollInput);

    /// <summary>
    /// Finds the closest blocking hit along the ray. Ignored colliders are skipped so a farther
    /// non-ignored collider still pulls the camera in. Order of RaycastAll hits is not guaranteed.
    /// </summary>
    private bool TryGetFirstBlockingHit(Vector3 origin, Vector3 direction, float maxDistance, out RaycastHit blockingHit)
    {
        blockingHit = default;
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, maxDistance, collisionLayers);
        if (hits.Length == 0)
            return false;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit h in hits)
        {
            if (ignoredColliders != null && ignoredColliders.Contains(h.collider))
                continue;
            blockingHit = h;
            return true;
        }

        return false;
    }

    private Vector3 HandleCollision(Vector3 targetPos, Vector3 desiredPosition, float desiredDistance, out float adjustedDistance)
    {
        Vector3 direction = (desiredPosition - targetPos).normalized;
        float maxDistance = Mathf.Min(desiredDistance, maxZoomDistance);

        if (TryGetFirstBlockingHit(targetPos, direction, maxDistance, out RaycastHit hit))
        {
            collisionCollider = hit.collider;
            isIgnored = false;
            adjustedDistance = hit.distance + collisionBuffer;
            adjustedDistance = Mathf.Clamp(adjustedDistance, minZoomDistance, maxZoomDistance);
            Debug.Log($"Raycast hit: Adjusting distance to {adjustedDistance} at {hit.point}");
            return targetPos + direction * adjustedDistance;
        }

        // No hit, or every hit was ignored — do not leave a stale blocking collider reference
        collisionCollider = null;
        isIgnored = true;
        adjustedDistance = desiredDistance;
        return desiredPosition;
    }

    private void OnDrawGizmos()
    {
        if (target == null) return;

        // Target position with offset
        Vector3 targetPos = target.position + targetOffset;

        // Draw target position as a yellow sphere
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(targetPos, 0.3f);

        // Draw line from target to camera
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(targetPos, transform.position);

        // Draw camera's collision sphere (for visualization, even though using raycast)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, cameraRadius);

        // If collision occurred, visualize the adjusted position and buffer
        if (!isIgnored)
        {
            Vector3 direction = (transform.position - targetPos).normalized;
            float maxDistance = Mathf.Min(currentDistance, maxZoomDistance);

            if (TryGetFirstBlockingHit(targetPos, direction, maxDistance, out RaycastHit hit))
            {
                // Draw collision point as a green sphere
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(hit.point, 0.2f);

                // Draw buffer distance as a magenta line
                Gizmos.color = Color.magenta;
                Vector3 bufferPoint = targetPos + direction * (hit.distance + collisionBuffer);
                Gizmos.DrawLine(hit.point, bufferPoint);
                Gizmos.DrawWireSphere(bufferPoint, 0.15f);
            }
        }
    }

    // Optional: Method to set a new target dynamically
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}