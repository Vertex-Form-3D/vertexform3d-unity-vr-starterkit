using UnityEngine;

[DisallowMultipleComponent]
public class HeadCollisionBlocker : MonoBehaviour
{
    [Tooltip("Rig to move (XR Origin). Leave empty to auto-detect from parents.")]
    [SerializeField] Transform rigOverride;
    [Tooltip("Head sphere radius in metres")]
    [SerializeField] float radius = 0.15f;
    [Tooltip("Layers that block the head. Player colliders are ignored automatically.")]
    [SerializeField] LayerMask blockers = ~0;
    [Tooltip("Only push the rig horizontally (avoids pushing it into the floor)")]
    [SerializeField] bool horizontalOnly = true;
    [Tooltip("With Horizontal Only, still allow pushing the rig upward. Never pushes down. " +
             "Usually off: gravity fights it and causes jitter.")]
    [SerializeField] bool allowUpwardPush = false;
    [Tooltip("Head movement larger than this in one frame is treated as a teleport (metres)")]
    [SerializeField] float teleportThreshold = 0.5f;
    [Tooltip("Black out the view when the eyes get this close to geometry (metres). 0 = off.")]
    [SerializeField] float blackoutRadius = 0.08f;

    [Header("Push back on contact")]
    [Tooltip("When the head touches an object, move the rig backward (opposite to where the head faces) " +
             "by this distance in metres. 0 = off.")]
    [SerializeField] float pushBackDistance = 2f;
    [Tooltip("Minimum time between two push-backs (seconds)")]
    [SerializeField] float pushBackCooldown = 0.5f;

    static readonly string[] RigTypeNames = { "XROrigin", "XRRig", "OVRCameraRig" };

    Transform rig, head;
    CharacterController cc;
    SphereCollider probe;
    Vector3 lastValid;
    readonly RaycastHit[] hits = new RaycastHit[16];
    readonly Collider[] overlaps = new Collider[16];

    Camera cam;
    CameraClearFlags savedClear;
    Color savedBg;
    int savedMask;
    bool blackedOut;

    bool wasInContact;
    float lastPushTime = float.NegativeInfinity;

    public Transform Rig => rig;
    public Transform Head => head;
    public bool IsBlackedOut => blackedOut;

    void Start()
    {
        head = transform;
        cam = GetComponent<Camera>();
        rig = rigOverride != null ? rigOverride : FindRig(head);

        if (rig == head)
        {
            Debug.LogError($"{nameof(HeadCollisionBlocker)}: '{name}' has no parent rig to move. " +
                           "Attach this script to the tracked head object under the XR Origin, " +
                           "or assign Rig Override.", this);
            enabled = false;
            return;
        }

        cc = rig.GetComponent<CharacterController>();
        if (cc != null) cc.minMoveDistance = 0f; // otherwise sub-mm corrections are silently ignored

        var probeGo = new GameObject("HeadProbe") { hideFlags = HideFlags.HideAndDontSave };
        probe = probeGo.AddComponent<SphereCollider>();
        probe.isTrigger = true;
        probe.radius = radius;
        probe.enabled = false; // only used for ComputePenetration

        lastValid = head.position;
        Debug.Log($"{nameof(HeadCollisionBlocker)}: head = {head.name}, rig = {rig.name}", this);
    }

    void OnDisable() => SetBlackout(false);

    void OnDestroy()
    {
        SetBlackout(false);
        if (probe) Destroy(probe.gameObject);
    }

    void LateUpdate()
    {
        Vector3 target = head.position;
        Vector3 delta = target - lastValid;
        float dist = delta.magnitude;
        bool contact = false;

        // 0. Teleport: validate the destination instead of sweeping the path
        if (dist > teleportThreshold)
        {
            if (IsBlocked(target))
                MoveRigDirect(lastValid - target); // cancel the teleport
            else
                lastValid = target;                 // accept it
            UpdateBlackout(head.position);
            return;
        }

        // 1. Head moving into geometry: stop at the nearest contact
        if (dist > 1e-5f)
        {
            Vector3 dir = delta / dist;
            int n = Physics.SphereCastNonAlloc(lastValid, radius, dir, hits, dist,
                                               blockers, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                if (IsOwn(hits[i].collider) || hits[i].distance <= 0f) continue;
                nearest = Mathf.Min(nearest, hits[i].distance);
            }

            if (nearest < float.MaxValue)
            {
                contact = true;
                Vector3 allowed = lastValid + dir * Mathf.Max(0f, nearest - 0.001f);
                ApplyCorrection(allowed - target);
                target = head.position;
            }
        }

        // 2. Geometry moving into head (doors, thrown objects): resolve residual overlap
        probe.radius = radius;
        int m = Physics.OverlapSphereNonAlloc(target, radius, overlaps, blockers,
                                              QueryTriggerInteraction.Ignore);
        for (int i = 0; i < m; i++)
        {
            var other = overlaps[i];
            if (IsOwn(other)) continue;
            if (Physics.ComputePenetration(probe, target, Quaternion.identity,
                    other, other.transform.position, other.transform.rotation,
                    out Vector3 pushDir, out float depth))
            {
                contact = true;
                ApplyCorrection(pushDir * depth);
                target = head.position;
            }
        }

        // 3. New contact: push the rig backward
        if (contact && !wasInContact && pushBackDistance > 0f &&
            Time.time - lastPushTime >= pushBackCooldown)
        {
            PushBack();
            lastPushTime = Time.time;
            target = head.position;
        }
        wasInContact = contact;

        // 4. Whatever couldn't be pushed out: hide it
        UpdateBlackout(target);

        lastValid = target;
    }

    /// Can the head stand at this position? Call this from a teleport script before teleporting.
    public bool IsBlocked(Vector3 headPosition)
    {
        int n = Physics.OverlapSphereNonAlloc(headPosition, radius, overlaps, blockers,
                                              QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
            if (!IsOwn(overlaps[i])) return true;
        return false;
    }

    /// Would teleporting the rig to this position put the head inside something?
    public bool IsRigDestinationBlocked(Vector3 rigDestination) =>
        IsBlocked(rigDestination + (head.position - rig.position));

    bool IsOwn(Collider c) => c.transform.IsChildOf(rig) || c.transform.IsChildOf(head);

    void PushBack()
    {
        // Backward = opposite of where the head faces, on the horizontal plane
        Vector3 back = -head.forward;
        back.y = 0f;
        if (back.sqrMagnitude < 1e-4f)
        {
            // Looking straight down/up: the top of the head points forward
            back = -head.up;
            back.y = 0f;
        }
        if (back.sqrMagnitude < 1e-4f) return;

        Vector3 offset = back.normalized * pushBackDistance;
        if (cc != null && cc.enabled) cc.Move(offset); // stops at walls behind the player
        else rig.position += offset;
    }

    void ApplyCorrection(Vector3 correction)
    {
        if (horizontalOnly) correction.y = allowUpwardPush ? Mathf.Max(0f, correction.y) : 0f;
        if (correction.sqrMagnitude < 1e-10f) return;

        if (cc != null && cc.enabled) cc.Move(correction); // won't push the body through walls
        else rig.position += correction;
    }

    void MoveRigDirect(Vector3 offset)
    {
        // Full revert (including Y), bypassing CharacterController collision
        bool ccWasEnabled = cc != null && cc.enabled;
        if (ccWasEnabled) cc.enabled = false;
        rig.position += offset;
        if (ccWasEnabled) cc.enabled = true;
    }

    void UpdateBlackout(Vector3 eye)
    {
        if (cam == null || blackoutRadius <= 0f)
        {
            SetBlackout(false);
            return;
        }

        bool inside = false;
        int n = Physics.OverlapSphereNonAlloc(eye, blackoutRadius, overlaps, blockers,
                                              QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n && !inside; i++)
            inside = !IsOwn(overlaps[i]);

        SetBlackout(inside);
    }

    void SetBlackout(bool on)
    {
        if (cam == null || on == blackedOut) return;
        blackedOut = on;

        if (on)
        {
            savedClear = cam.clearFlags;
            savedBg = cam.backgroundColor;
            savedMask = cam.cullingMask;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.cullingMask = 0; // render nothing
        }
        else
        {
            cam.clearFlags = savedClear;
            cam.backgroundColor = savedBg;
            cam.cullingMask = savedMask;
        }
    }

    Transform FindRig(Transform h)
    {
        // Known rig components (XR Interaction Toolkit / legacy XR / Oculus), matched by name to avoid package dependencies
        for (var t = h.parent; t != null; t = t.parent)
            foreach (var mb in t.GetComponents<MonoBehaviour>())
                if (mb != null && System.Array.IndexOf(RigTypeNames, mb.GetType().Name) >= 0)
                    return t;

        // Fallback: topmost parent of the head
        return h.parent != null ? h.root : h;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, blackoutRadius);
    }
}