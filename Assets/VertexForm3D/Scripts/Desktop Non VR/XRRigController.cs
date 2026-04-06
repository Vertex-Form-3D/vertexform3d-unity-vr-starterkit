using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VertexFormCore;
using UnityEngine.Events;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.SceneManagement; // For List in raycasting

public class XRRigController : MonoBehaviour
{
    public bool isMultiplayer;
    public PlayerNetworkSetup playerNetworkSetup;
    [SerializeField] private NetworkObject networkObject;
    [SerializeField] private bool rotateCameraWithMovementInThirdPerson = true;
    public OrbitCamera orbitCamera;
    public PersonMode startMode;

    [Header("Object Handler For Cross Platform")]
    public GameObject[] VRObjects;
    public TrackedPoseDriver[] trackedPoseDrivers;
    public InputActionManager IAM;
    public XRInputModalityManager XRIMM;
    public UnityEvent onVRMode;
    public UnityEvent onDesktopMode;
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float normalSpeed = 5f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private bool isSprinting = false;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Camera Settings")]
    public Transform cameraTransform;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minZoomDistance = 0.5f; // First-person distance
    [SerializeField] private float maxZoomDistance = 10f;  // Third-person distance
    [SerializeField] private float thirdPersonThreshold = 2f; // Distance at which to switch to third-person
    private float currentZoomDistance;

    private CharacterController characterController;
    public Vector2 moveInput;
    private float verticalVelocity;
    private bool isJumping;
    public bool isThirdPerson;
    public AvatarInputConverter avatarInputConverter; // Reference to AvatarInputConverter for third-person view 
    [SerializeField] private InputAction move, sprint, jump, pressed, axis, scroll;
    public UnityEvent onFPSModeStartEvent;
    public UnityEvent onThirdPersonModeStartEvent;
    [SerializeField] private float thirdPersonRotationSpeed = 1;
    [SerializeField] private bool inverted = true;
    public Vector2 rotation;
    public Vector2 previousRotation;
    private bool rotateAllowed;
    public bool isHoweringUI;
    private static bool _loggedFirstInput;
    private bool _loggedWrongMode;
    private void Awake()
    {
        Debug.Log("[XRRigController] Awake started");
        characterController = GetComponent<CharacterController>();
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
        if (networkObject == null)
        {
            networkObject = GetComponent<NetworkObject>();
        }

        if (isMultiplayer)
        {
            // Defer all NetworkObject-dependent logic to Start(). During Awake we are still inside
            // Fusion's Spawn() -> Instantiate() call; NetworkObject.IsValid and HasInputAuthority
            // are not set until after Awake returns. So GetPlatformProperty() and IsLocalPlayer()
            // would be false here even for the local player. Run ApplyMultiplayerPlatformAndAuthority in Start().
        }
        else
        {
            Debug.Log("Single-player path: " + ProjectManager.instance.platforms.platformChoice + " " + platform.VR);
            SetPlatformProperty(ProjectManager.instance.platforms.platformChoice == platform.VR);
            bool isVR = ProjectManager.instance.platforms.platformChoice == platform.VR;
            Debug.Log($"[XRRigController] Single-player path - isVR={isVR}, platform={ProjectManager.instance.platforms.platformChoice}");

            foreach (GameObject go in VRObjects)
            {

                if (go != null)
                {
                    go.SetActive(isVR);
                }
            }
            foreach (TrackedPoseDriver tpd in trackedPoseDrivers)
            {
                tpd.enabled = isVR;
            }
            IAM.enabled = XRIMM.enabled = isVR;
            if (ProjectManager.instance.platforms.platformChoice == platform.Desktop)
            {
                Debug.Log("[XRRigController] Single-player Desktop: calling AssignInputActions");
                AssignInputActions();
                if (onDesktopMode != null)
                {
                    onDesktopMode?.Invoke();
                }
            }
            else
            {
                if (onVRMode != null)
                {
                    onVRMode?.Invoke();
                }
                // Canvas[] cans = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                // foreach (Canvas c in cans)
                // {
                //     if (c.renderMode == RenderMode.WorldSpace)
                //     {
                //         c.worldCamera = cameraTransform.GetComponentInChildren<Camera>();
                //     }
                // }
                Destroy(orbitCamera.gameObject);
                Destroy(this);

            }

        }
    }

    private void Start()
    {
        if (isMultiplayer)
        {
            ApplyMultiplayerPlatformAndAuthority();
        }

        // Multiplayer desktop: Fusion sets HasInputAuthority after Awake. Assign input here when we're the local player.
        if (isMultiplayer && !GetPlatformProperty() && !inputassigned)
        {
            if (IsLocalPlayer())
            {
                Debug.Log("[XRRigController] Start: local player detected, calling AssignInputActions");
                AssignInputActions();
            }
        }
        if (isMultiplayer)
        {
            // On Start, assign all World Space canvases to the current camera so worldCamera is never null at scene open.
            AssignWorldSpaceCanvasesToCurrentCamera();
        }
    }

    /// <summary>Applies VR/Desktop and local/remote state. Call from Start(), not Awake — Fusion sets NetworkObject.IsValid and HasInputAuthority only after spawn completes.</summary>
    private void ApplyMultiplayerPlatformAndAuthority()
    {
        bool isVR = GetPlatformProperty();
        bool isLocal = IsLocalPlayer();
        Debug.Log($"[XRRigController] Multiplayer path (Start) - isVR={isVR}, IsLocalPlayer={isLocal}");

        foreach (GameObject go in VRObjects)
        {
            if (go != null)
            {
                go.SetActive(isVR);
            }
        }
        foreach (TrackedPoseDriver tpd in trackedPoseDrivers)
        {
            tpd.enabled = isVR;
        }
        if (!isLocal)
        {
            XRIMM.enabled = false;
            foreach (TrackedPoseDriver tpd in trackedPoseDrivers)
            {
                tpd.enabled = false;
            }
            foreach (GameObject go in VRObjects)
            {
                if (go != null)
                {
                    go.SetActive(false);
                }
            }
        }
        else
        {
            IAM.enabled = XRIMM.enabled = isVR;
        }
        if (isVR)
        {
            if (onVRMode != null)
            {
                onVRMode?.Invoke();
            }
            Destroy(orbitCamera.gameObject);
            Destroy(this);
        }
        else
        {
            if (onDesktopMode != null)
            {
                onDesktopMode?.Invoke();
            }
        }
    }

    /// <summary>Finds all World Space canvases and sets their worldCamera to the current camera (first-person or orbit based on isThirdPerson).</summary>
    private void AssignWorldSpaceCanvasesToCurrentCamera()
    {
        Camera currentCam = GetCurrentRenderingCamera();
        if (currentCam == null) return;

        Canvas[] cans = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Canvas c in cans)
        {
            if (c.renderMode == RenderMode.WorldSpace)
            {
                c.worldCamera = currentCam;
            }
        }
    }

    /// <summary>Returns the camera that should be used for rendering (and for World Space canvas event camera).</summary>
    private Camera GetCurrentRenderingCamera()
    {
        if (isThirdPerson && orbitCamera != null)
        {
            Camera orbitCam = orbitCamera.GetComponent<Camera>();
            if (orbitCam != null) return orbitCam;
        }
        if (cameraTransform != null)
        {
            Camera fpCam = cameraTransform.GetComponentInChildren<Camera>(true);
            if (fpCam != null) return fpCam;
        }
        return Camera.main;
    }

    private void OnDestroy()
    {
        if (inputassigned)
        {
            pressed.Disable();
            axis.Disable();
            jump.Disable();
            sprint.Disable();
            move.Disable();
            scroll.Disable();
        }
    }

    private void Update()
    {
        if (isMultiplayer)
        {
            if (!IsLocalPlayer())
            {
                return;
            }
        }

        HandleMovement();
        _loggedWrongMode = false;
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (isMultiplayer)
            {
                RoomManager.Instance.LeaveRoomAndLoadOnBoardingScene();
            }
            else
            {
                SceneManager.LoadScene(0);
            }
        }
    }


    public void SetPlatformProperty(bool isVR)
    {
        // With Fusion NetworkObject, platform is determined by ProjectManager; no custom properties to set.
        Debug.Log($"SetPlatformProperty (isVR={isVR}) - using ProjectManager when NetworkObject is in use.");
    }

    /// <summary>True if this rig is for a VR player. In multiplayer uses the networked Platform from PlayerNetworkSetup so each player (local and remote) is set up according to their own platform.</summary>
    public bool GetPlatformProperty()
    {
        if (isMultiplayer && networkObject != null && networkObject.IsValid && playerNetworkSetup != null)
        {
            bool isVR = playerNetworkSetup.Platform == platform.VR;
            Debug.Log("GetPlatformProperty (multiplayer): " + (networkObject.HasInputAuthority ? "local" : "remote") + " -> " + playerNetworkSetup.Platform + " isVR=" + isVR);
            return isVR;
        }
        // Single-player or missing refs: use local ProjectManager
        bool singlePlayerVR = ProjectManager.instance != null && ProjectManager.instance.platforms.platformChoice == platform.VR;
        Debug.Log("GetPlatformProperty (single): " + singlePlayerVR);
        return singlePlayerVR;
    }

    /// <summary>True if this instance is the local player (has input authority).</summary>
    private bool IsLocalPlayer()
    {
        return networkObject != null && networkObject.IsValid && networkObject.HasInputAuthority;
    }

    public bool inputassigned;

    public void AssignInputActions()
    {
        // Validate InputAction references (must be assigned in Inspector)
        if (pressed == null) { Debug.LogError("[XRRigController] 'pressed' InputAction is NULL - assign it in the Inspector!"); return; }
        if (axis == null) { Debug.LogError("[XRRigController] 'axis' InputAction is NULL - assign it in the Inspector!"); return; }
        if (jump == null) { Debug.LogError("[XRRigController] 'jump' InputAction is NULL - assign it in the Inspector!"); return; }
        if (sprint == null) { Debug.LogError("[XRRigController] 'sprint' InputAction is NULL - assign it in the Inspector!"); return; }
        if (move == null) { Debug.LogError("[XRRigController] 'move' InputAction is NULL - assign it in the Inspector!"); return; }
        if (scroll == null) { Debug.LogError("[XRRigController] 'scroll' InputAction is NULL - assign it in the Inspector!"); return; }
        pressed.Enable();
        axis.Enable();
        jump.Enable();
        sprint.Enable();
        move.Enable();
        scroll.Enable();
        inputassigned = true;
        Debug.Log("[XRRigController] Input actions enabled successfully (move, sprint, jump, pressed, axis, scroll)");
        pressed.performed += _ =>
        {
            if (!DesktopPointerUIHelper.IsPointerOverUIThisFrame())
            {
                if (this.isActiveAndEnabled)
                {
                    if (isMultiplayer)
                    {
                        if (!IsLocalPlayer())
                        {
                            return;
                        }
                    }
                    StartCoroutine(Rotate());
                }
            }
        };
        pressed.canceled += _ => { rotateAllowed = false; };
        sprint.performed += _ => { isSprinting = true; };
        sprint.canceled += _ => { isSprinting = false; };
        jump.performed += _ => { isJumping = true; };
        jump.canceled += _ => { isJumping = false; };
        move.performed += context =>
        {
            moveInput = context.ReadValue<Vector2>();
            if (!_loggedFirstInput) { Debug.Log("[XRRigController] First move input received - input callbacks are working."); _loggedFirstInput = true; }
        };
        axis.performed += context => { rotation = context.ReadValue<Vector2>(); };
        scroll.performed += context => { HandleZoom(context.ReadValue<float>()); };

        // Initialize zoom distance
        currentZoomDistance = isThirdPerson ? maxZoomDistance : minZoomDistance;
        Debug.Log($"[XRRigController] AssignInputActions: startMode={startMode}, currentZoomDistance={currentZoomDistance}, thirdPersonThreshold={thirdPersonThreshold}");
        if (startMode == PersonMode.Third)
        {
            Debug.Log("[XRRigController] AssignInputActions: calling SwitchToThirdPerson (startMode=Third)");
            SwitchToThirdPerson();
        }
        else
        {
            Debug.Log("[XRRigController] AssignInputActions: calling SwitchToFirstPerson (startMode=First)");
            SwitchToFirstPerson();
        }
        Debug.Log($"[XRRigController] AssignInputActions done - isThirdPerson={isThirdPerson}, orbitCamera.active={orbitCamera != null && orbitCamera.gameObject.activeInHierarchy}");
    }
    private void HandleMovement()
    {
        // While sitting, don't apply movement (position is fixed). Pressing move keys leaves the seat.
        if (playerNetworkSetup != null && playerNetworkSetup.IsSitting)
        {
            if (moveInput.sqrMagnitude > 0.01f)
                playerNetworkSetup.LeaveCurrentSeatIfAny();
            return;
        }

        if (isSprinting)
        {
            moveSpeed = sprintSpeed;
        }
        else
        {
            moveSpeed = normalSpeed;
        }
        // Calculate movement direction based on camera orientation
        Vector3 moveDirection = Vector3.zero;
        if (cameraTransform != null)
        {
            // Get the camera's forward and right vectors, ignoring the Y component for flat movement
            Vector3 cameraForward;
            Vector3 cameraRight;
            if (isThirdPerson)
            {
                cameraForward = orbitCamera.transform.forward;
                cameraRight = orbitCamera.transform.right;

                if (moveInput != Vector2.zero)
                {
                    cameraTransform.rotation = orbitCamera.transform.rotation;

                    // Rotate cameraTransform to match movement direction in third-person if enabled
                    if (rotateCameraWithMovementInThirdPerson)
                    {
                        // Calculate the target direction based on movement input
                        Vector3 targetDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;
                        if (targetDirection != Vector3.zero)
                        {
                            // Smoothly rotate the cameraTransform to face the movement direction
                            Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
                            cameraTransform.rotation = Quaternion.Slerp(
                                cameraTransform.rotation,
                                targetRotation,
                                rotationSpeed * Time.deltaTime
                            );
                        }
                    }
                }
            }
            else
            {
                cameraForward = cameraTransform.forward;
                cameraRight = cameraTransform.right;
            }
            cameraForward.y = 0f; // Ignore vertical tilt of the camera
            cameraForward = cameraForward.normalized;

            cameraRight.y = 0f; // Ignore vertical tilt
            cameraRight = cameraRight.normalized;

            // Calculate movement direction based on input relative to camera
            moveDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;
            moveDirection *= moveSpeed;
        }
        else
        {
            // Fallback to local transform if cameraTransform is missing
            moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);
            moveDirection = transform.TransformDirection(moveDirection);
            moveDirection *= moveSpeed;
        }

        // Apply gravity
        if (characterController.isGrounded)
        {
            verticalVelocity = -0.5f;
            if (isJumping)
            {
                verticalVelocity = jumpForce;
                isJumping = false;
            }
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        // Apply vertical velocity
        if (GetComponent<FlyingModeScript>())
        {
            if (!GetComponent<FlyingModeScript>().enabled)
            {
                moveDirection.y = verticalVelocity;
            }
        }
        else
        {
            moveDirection.y = verticalVelocity;
        }

        // Move the character controller
        if (!Input.GetKey(KeyCode.F))
        {
            if (moveDirection != Vector3.zero)
            {
                characterController.Move(moveDirection * Time.deltaTime);
            }
        }
    }

    private static bool _loggedFirstScroll;

    private void HandleZoom(float scrollInput)
    {
        if (isMultiplayer)
        {
            if (!IsLocalPlayer())
            {
                return;
            }
        }
        if (!_loggedFirstScroll)
        {
            Debug.Log($"[XRRigController] HandleZoom: first scroll received - scrollInput={scrollInput}");
            _loggedFirstScroll = true;
        }
        // Store the previous state to detect mode changes
        bool wasThirdPerson = isThirdPerson;
        float prevZoom = currentZoomDistance;

        // Adjust zoom distance based on scroll input
        currentZoomDistance -= scrollInput * zoomSpeed;
        currentZoomDistance = Mathf.Clamp(currentZoomDistance, minZoomDistance, maxZoomDistance);

        // Toggle between first and third person based on zoom distance
        isThirdPerson = currentZoomDistance > thirdPersonThreshold;

        // Check for mode switch and call appropriate function
        if (wasThirdPerson != isThirdPerson)
        {
            Debug.Log($"[XRRigController] HandleZoom: MODE SWITCH {wasThirdPerson} -> {isThirdPerson} (zoom {prevZoom:F2} -> {currentZoomDistance:F2}, threshold={thirdPersonThreshold}), calling {(isThirdPerson ? "onThirdPersonModeStart" : "onFPSModeStart")}");
            if (isThirdPerson)
            {
                onThirdPersonModeStart();
            }
            else
            {
                onFPSModeStart();
            }
        }
        if (isThirdPerson)
        {
            cameraTransform.rotation = orbitCamera.transform.rotation;
        }
    }

    private IEnumerator Rotate()
    {
        rotateAllowed = true;
        while (rotateAllowed)
        {
            // Only rotate if the pointer is not over a UI element
            if (!isThirdPerson)
            {
                if (previousRotation != rotation)
                {
                    rotation *= thirdPersonRotationSpeed;

                    // Rotate around Y axis (horizontal)
                    cameraTransform.transform.Rotate(Vector3.up * (inverted ? 1 : -1), rotation.x, Space.World);

                    // Rotate around X axis (vertical) and clamp
                    float currentXRotation = cameraTransform.transform.eulerAngles.x;
                    // Convert to signed angle (-180 to 180)
                    if (currentXRotation > 180) currentXRotation -= 360;

                    float rotationAmount = rotation.y * (inverted ? -1 : 1);
                    float newXRotation = currentXRotation + rotationAmount;

                    // Clamp the X rotation
                    newXRotation = Mathf.Clamp(newXRotation, -50f, 60f);

                    // Calculate the required rotation to reach the clamped angle
                    float deltaXRotation = newXRotation - currentXRotation;

                    cameraTransform.transform.Rotate(cameraTransform.right, deltaXRotation, Space.World);

                    previousRotation = rotation;
                }
            }
            yield return null;
        }
    }

    private void onFPSModeStart()
    {
        Debug.Log("[XRRigController] onFPSModeStart: switching to first-person");
        Canvas[] cans = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Canvas c in cans)
        {

            if (c.renderMode == RenderMode.WorldSpace)
            {
                c.worldCamera = cameraTransform.GetComponentInChildren<Camera>();
            }
        }
        // Called when switching to first-person mode
        Debug.Log("mode First-person activated");
        if (orbitCamera == null)
        {
            Debug.LogError("[XRRigController] onFPSModeStart: orbitCamera is NULL!");
            return;
        }
        orbitCamera.gameObject.SetActive(false);
        // Use main camera for rendering in first-person
        if (cameraTransform != null)
        {
            Camera mainCam = cameraTransform.GetComponentInChildren<Camera>();
            if (mainCam != null) mainCam.enabled = true;

        }
        cameraTransform.rotation = orbitCamera.transform.rotation;
        if (avatarInputConverter != null)
        {
            //avatarInputConverter.isThirdPerson=false;
        }
        if (onFPSModeStartEvent != null)
        {
            onFPSModeStartEvent.Invoke();
        }
    }

    private void onThirdPersonModeStart()
    {
        Debug.Log("[XRRigController] onThirdPersonModeStart: switching to third-person");
        if (orbitCamera == null)
        {
            Debug.LogError("[XRRigController] onThirdPersonModeStart: orbitCamera is NULL!");
            return;
        }
        // Called when switching to third-person mode
        Debug.Log("mode Third-person activated");
        Canvas[] cans = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Canvas c in cans)
        {
            if (c.renderMode == RenderMode.WorldSpace)
            {
                c.worldCamera = orbitCamera.GetComponent<Camera>();
            }
        }
        // Disable main (first-person) camera so orbit camera is the one that renders
        if (cameraTransform != null)
        {
            Camera mainCam = cameraTransform.GetComponentInChildren<Camera>();
            if (mainCam != null) mainCam.enabled = false;

        }
        // Add third-person specific initialization here
        orbitCamera.gameObject.SetActive(true);
        Camera orbitCam = orbitCamera.GetComponent<Camera>();
        if (orbitCam != null) orbitCam.enabled = true;
        Vector3 dir = cameraTransform.forward;
        orbitCamera.currentYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        orbitCamera.currentPitch = -Mathf.Asin(dir.y) * Mathf.Rad2Deg;
        orbitCamera.currentDistance = Mathf.Max(orbitCamera.minZoomDistance, currentZoomDistance);
        if (avatarInputConverter != null)
        {
            //avatarInputConverter.isThirdPerson=true;
        }
        if (onThirdPersonModeStartEvent != null)
        {
            onThirdPersonModeStartEvent.Invoke();
        }
    }

    // New function to switch to first-person mode
    [ContextMenu("SwitchToFirstPerson")]
    public void SwitchToFirstPerson()
    {
        Debug.Log($"[XRRigController] SwitchToFirstPerson called - isThirdPerson={isThirdPerson}, orbitCamera={orbitCamera != null}");
        if (isMultiplayer)
        {
            if (!IsLocalPlayer())
            {
                Debug.Log("[XRRigController] SwitchToFirstPerson: not local player, skipping");
                return;
            }
        }

        if (!isThirdPerson)
        {
            Debug.Log("[XRRigController] SwitchToFirstPerson: already first person, skipping");
            return; // Already in first-person mode
        }

        isThirdPerson = false;
        currentZoomDistance = minZoomDistance;
        Debug.Log("[XRRigController] SwitchToFirstPerson: calling onFPSModeStart");
        onFPSModeStart();
    }

    /// <summary>Set the look direction (first-person camera and orbit camera yaw/pitch) to match the given world rotation. Call when sitting so the view faces the seat forward.</summary>
    public void SetLookRotation(Quaternion worldRotation)
    {
        if (cameraTransform != null)
            cameraTransform.rotation = worldRotation;
        if (orbitCamera != null)
        {
            Vector3 forward = worldRotation * Vector3.forward;
            orbitCamera.currentYaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            orbitCamera.currentPitch = -Mathf.Asin(Mathf.Clamp(forward.y, -1f, 1f)) * Mathf.Rad2Deg;
        }
    }

    // New function to switch to third-person mode
    [ContextMenu("SwitchToThirdPerson")]
    public void SwitchToThirdPerson()
    {
        Debug.Log($"[XRRigController] SwitchToThirdPerson called - isThirdPerson={isThirdPerson}, orbitCamera={orbitCamera != null}");
        if (isMultiplayer)
        {
            if (!IsLocalPlayer())
            {
                Debug.Log("[XRRigController] SwitchToThirdPerson: not local player, skipping");
                return;
            }
        }

        if (isThirdPerson)
        {
            Debug.Log("[XRRigController] SwitchToThirdPerson: already third person, skipping");
            return; // Already in third-person mode
        }

        isThirdPerson = true;
        currentZoomDistance = maxZoomDistance;
        Debug.Log("[XRRigController] SwitchToThirdPerson: calling onThirdPersonModeStart");
        onThirdPersonModeStart();
    }
}

public enum PersonMode
{
    First,
    Third
}