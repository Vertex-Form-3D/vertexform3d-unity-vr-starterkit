
using Fusion;
using Fusion.Addons.Physics;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

namespace VertexFormCore
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(NetworkRigidbody3D))]
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(XRGeneralGrabTransformer))]
    public class XRGrabNetworkInteractable : NetworkBehaviour
    {
        private Rigidbody rb;
        private NetworkRigidbody3D networkRigidbody;
        private bool initialGravityStatus;
        private bool initialKinematicStatus;
        private XRGrabInteractable grabInteractable;
        public Vector3 initialPosition;
        public Vector3 initialRotation;
        public bool shouldReset;
        public UnityEvent onSelectEnterEvent;
        public UnityEvent onSelectExitEvent;

        [Networked] public bool IsGrabbed { get; set; }
        [Networked] public NetworkId GrabbedBy { get; set; }
        [Networked] public NetworkBool InitialIsKinematicState { get; set; }
        [Networked] private Vector3 LocalPositionOffset { get; set; }
        [Networked] private Quaternion LocalRotationOffset { get; set; }

        public bool expectedIsKinematic = true;
        [Tooltip("For object with a rigidbody, if true, apply hand velocity on ungrab")]
        public bool applyVelocityOnRelease = true;

        // Velocity computation
        const int velocityBufferSize = 5;
        Vector3 lastPosition;
        Quaternion previousRotation;
        Vector3[] lastMoves = new Vector3[velocityBufferSize];
        Vector3[] lastAngularVelocities = new Vector3[velocityBufferSize];
        float[] lastDeltaTime = new float[velocityBufferSize];
        int lastMoveIndex = 0;
        ChangeDetector funChangeDetector;
        ChangeDetector renderChangeDetector;

        [Header("Advanced options")]
        public bool extrapolateWhileTakingAuthority = true;
        public bool isTakingAuthority = false;

        Vector3 localPositionOffsetWhileTakingAuthority;
        Quaternion localRotationOffsetWhileTakingAuthority;
        Transform grabberTransformWhileTakingAuthority;

        enum Status
        {
            NotGrabbed,
            Grabbed,
            WillBeGrabbedUponAuthorityReception
        }
        Status status = Status.NotGrabbed;

        private Coroutine resetCoroutine;

        Vector3 Velocity
        {
            get
            {
                Vector3 move = Vector3.zero;
                float time = 0;
                for (int i = 0; i < velocityBufferSize; i++)
                {
                    if (lastDeltaTime[i] != 0)
                    {
                        move += lastMoves[i];
                        time += lastDeltaTime[i];
                    }
                }
                if (time == 0) return Vector3.zero;
                return move / time;
            }
        }

        Vector3 AngularVelocity
        {
            get
            {
                Vector3 culmulatedAngularVelocity = Vector3.zero;
                int step = 0;
                for (int i = 0; i < velocityBufferSize; i++)
                {
                    if (lastDeltaTime[i] != 0)
                    {
                        culmulatedAngularVelocity += lastAngularVelocities[i];
                        step++;
                    }
                }
                if (step == 0) return Vector3.zero;
                return culmulatedAngularVelocity / step;
            }
        }

        private void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            rb = GetComponent<Rigidbody>();
            networkRigidbody = GetComponent<NetworkRigidbody3D>();
            initialGravityStatus = rb.useGravity;
            initialKinematicStatus = rb.isKinematic;

            grabInteractable.selectEntered.AddListener(OnSelectEntered);
            grabInteractable.selectExited.AddListener(OnSelectExited);
        }

        public override void Spawned()
        {
            base.Spawned();
            if (networkRigidbody && Object.HasStateAuthority)
            {
                // Save initial kinematic state for later join player
                InitialIsKinematicState = networkRigidbody.Rigidbody.isKinematic;
            }
            funChangeDetector = GetChangeDetector(NetworkBehaviour.ChangeDetector.Source.SimulationState);
            renderChangeDetector = GetChangeDetector(NetworkBehaviour.ChangeDetector.Source.SnapshotFrom);
        }

        public XRGrabInteractable GetXRGrabInteractable()
        {
            return grabInteractable;
        }

        private void OnSelectEntered(SelectEnterEventArgs arg0)
        {
            // Only proceed if we have input authority
            if (!Object.HasInputAuthority)
            {
                StartCoroutine(WaitForInputAuthority(arg0));
            }
            else
            {
                HandleGrabEntered(arg0);
            }
        }

        private IEnumerator WaitForInputAuthority(SelectEnterEventArgs arg0)
        {
            if (!Object.HasStateAuthority)
            {
                Object.RequestStateAuthority();
                yield return new WaitUntil(() => Object.HasStateAuthority);
            }
            Object.AssignInputAuthority(Runner.LocalPlayer);
            yield return new WaitUntil(() => Object.HasInputAuthority);

            HandleGrabEntered(arg0);
        }

        private void HandleGrabEntered(SelectEnterEventArgs arg0)
        {
            if (resetCoroutine != null)
            {
                StopCoroutine(resetCoroutine);
                resetCoroutine = null;
            }

            // Calculate position/rotation offsets relative to grabbing transform
            Transform grabberTransform = arg0.interactorObject.GetAttachTransform(grabInteractable);
            localPositionOffsetWhileTakingAuthority = grabberTransform.InverseTransformPoint(transform.position);
            localRotationOffsetWhileTakingAuthority = Quaternion.Inverse(grabberTransform.rotation) * transform.rotation;
            grabberTransformWhileTakingAuthority = grabberTransform;

            status = Status.Grabbed;
            isTakingAuthority = true;

            // Set networked offsets for network synchronization
            LocalPositionOffset = localPositionOffsetWhileTakingAuthority;
            LocalRotationOffset = localRotationOffsetWhileTakingAuthority;

            if (onSelectEnterEvent != null)
            {
                RPC_SelectEnter();
            }

            RPC_HandleRigidBodyGravity(false, true, Object.Id, "Enter");
            isTakingAuthority = false;
        }

        private void OnSelectExited(SelectExitEventArgs arg0)
        {
            Debug.Log("OnSelectExited");
            if (Object.HasInputAuthority)
            {
                status = Status.NotGrabbed;
                grabberTransformWhileTakingAuthority = null;

                if (onSelectExitEvent != null)
                {
                    RPC_SelectExit();
                }

                RPC_HandleRigidBodyGravity(initialGravityStatus, initialKinematicStatus, Object.Id, "Exit");

                if (shouldReset)
                {
                    resetCoroutine = StartCoroutine(ResetTransformCoroutine());
                }
            }
        }

        void LockObjectPhysics()
        {
            // While grabbed, we disable physics forces on the object, to force a position based tracking
            if (networkRigidbody) networkRigidbody.Rigidbody.isKinematic = true;
        }

        void UnlockObjectPhysics()
        {
            // We restore the default isKinematic state if needed
            if (networkRigidbody) networkRigidbody.Rigidbody.isKinematic = InitialIsKinematicState;

            // We apply release velocity if needed
            if (networkRigidbody && networkRigidbody.Rigidbody.isKinematic == false && applyVelocityOnRelease)
            {
                networkRigidbody.Rigidbody.linearVelocity = Velocity;
                networkRigidbody.Rigidbody.angularVelocity = AngularVelocity;
            }

            // Reset velocity tracking
            for (int i = 0; i < velocityBufferSize; i++) lastDeltaTime[i] = 0;
            lastMoveIndex = 0;
        }

        bool TryDetectGrabChange(ChangeDetector changeDetector, out bool previousGrabbed, out bool currentGrabbed)
        {
            previousGrabbed = false;
            currentGrabbed = false;

            foreach (var changedNetworkedVarName in changeDetector.DetectChanges(this, out var previous, out var current))
            {
                if (changedNetworkedVarName == nameof(IsGrabbed))
                {
                    var reader = GetPropertyReader<bool>(changedNetworkedVarName);
                    previousGrabbed = reader.Read(previous);
                    currentGrabbed = reader.Read(current);
                    return true;
                }
            }
            return false;
        }

        public override void FixedUpdateNetwork()
        {
            // Check if the grab state changed
            if (TryDetectGrabChange(funChangeDetector, out var previousGrabbed, out var currentGrabbed))
            {
                if (previousGrabbed && !currentGrabbed)
                {
                    // Object ungrabbed
                    UnlockObjectPhysics();
                }
                if (!previousGrabbed && currentGrabbed)
                {
                    // Object grabbed
                    LockObjectPhysics();
                }
            }

            // We only update the object position if we have the state authority
            if (!Object.HasStateAuthority) return;

            if (!IsGrabbed) return;

            // Follow grabber using XRI's natural grab behavior - let XRI handle the positioning
            // The NetworkTransform will sync the final position
        }

        private void Update()
        {
            if (Runner)
            {
                // Velocity tracking for release momentum
                lastMoves[lastMoveIndex] = transform.position - lastPosition;
                if (Time.deltaTime > 0)
                {
                    lastAngularVelocities[lastMoveIndex] = previousRotation.AngularVelocityChange(transform.rotation, Time.deltaTime);
                }
                lastDeltaTime[lastMoveIndex] = Time.deltaTime;
                lastMoveIndex = (lastMoveIndex + 1) % velocityBufferSize;
                lastPosition = transform.position;
                previousRotation = transform.rotation;
            }
        }

        public override void Render()
        {
            // Check if the grab state changed, to trigger callbacks
            if (TryDetectGrabChange(renderChangeDetector, out var previousGrabbed, out var currentGrabbed))
            {
                if (previousGrabbed && !currentGrabbed)
                {
                    // Trigger ungrab events on render for all clients
                }
                if (!previousGrabbed && currentGrabbed)
                {
                    // Trigger grab events on render for all clients
                }
            }

            if (isTakingAuthority && extrapolateWhileTakingAuthority)
            {
                // If we are currently taking the authority on the object due to a grab, the network info are still not set
                // but we will extrapolate anyway to avoid having the grabbed object staying still until we receive the authority
                ExtrapolateWhileTakingAuthority();
                return;
            }

            // No need to extrapolate if the object is not grabbed or if XRI is handling positioning
            if (!IsGrabbed) return;

            // Let Unity XRI handle the visual positioning naturally
            // The extrapolation is less critical since XRI provides smooth grab behavior
        }

        void ExtrapolateWhileTakingAuthority()
        {
            // No need to extrapolate if the object is not really grabbed
            if (grabberTransformWhileTakingAuthority == null) return;

            // Extrapolation: Make visual representation follow grabber, adding position/rotation offsets
            Follow(followedTransform: grabberTransformWhileTakingAuthority, localPositionOffsetWhileTakingAuthority, localRotationOffsetWhileTakingAuthority);
        }

        void Follow(Transform followedTransform, Vector3 localPositionOffsetToFollowed, Quaternion localRotationOffsetTofollowed)
        {
            transform.position = followedTransform.TransformPoint(localPositionOffsetToFollowed);
            transform.rotation = followedTransform.rotation * localRotationOffsetTofollowed;
        }

        private IEnumerator ResetTransformCoroutine()
        {
            yield return new WaitForSeconds(10f);
            SetInitialTransformOverNetwork();
            resetCoroutine = null;
        }

        public void SetInitialTransformOverNetwork()
        {
            if (Object.HasInputAuthority)
            {
                transform.localPosition = initialPosition;
                transform.rotation = Quaternion.Euler(initialRotation);
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
        public void RPC_SelectEnter()
        {
            onSelectEnterEvent?.Invoke();
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
        public void RPC_SelectExit()
        {
            onSelectExitEvent?.Invoke();
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
        public void RPC_HandleRigidBodyGravity(bool gravity, bool kinematic, NetworkId objectId, string eventName)
        {
            if (eventName == "Enter")
            {
                if (grabInteractable.isSelected && objectId != Object.Id)
                {
                    DisableGrabbing();
                }
                IsGrabbed = true;
                GrabbedBy = default(NetworkId);
            }
            else if (eventName == "Exit")
            {
                IsGrabbed = false;
                GrabbedBy = default(NetworkId);
            }

            rb.useGravity = gravity;
            //rb.isKinematic = kinematic;
        }

        void EnableGrabbing()
        {
            grabInteractable.enabled = true;
        }

        void DisableGrabbing()
        {
            grabInteractable.enabled = false;
            Invoke(nameof(EnableGrabbing), 0.5f);
        }

        public void SetInitialPosition()
        {
            initialPosition = transform.localPosition;
        }

        public void SetInitialRotation()
        {
            initialRotation = transform.rotation.eulerAngles;
        }
    }
}

// Extension method for angular velocity calculation
public static class QuaternionExtensions
{
    public static Vector3 AngularVelocityChange(this Quaternion from, Quaternion to, float deltaTime)
    {
        if (deltaTime == 0) return Vector3.zero;

        Quaternion deltaRotation = to * Quaternion.Inverse(from);
        deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);

        if (angle > 180f) angle -= 360f;

        return axis * (angle * Mathf.Deg2Rad / deltaTime);
    }
}