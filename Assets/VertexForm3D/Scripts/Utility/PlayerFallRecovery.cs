using UnityEngine;
using VertexFormCore;

/// <summary>
/// Pulls the local player back into the world if they end up underneath it.
///
/// A terrain collider is one-sided: once a player is below it there is nothing to stand on and
/// no way back up, so they are stuck until they quit. That can happen for several unrelated
/// reasons — a spawn point missing from the world scene, a stale addressable bundle, spawn
/// overlap ejecting a capsule downward, or entering before the world finished streaming in —
/// and it is worth catching regardless of the cause rather than chasing each one separately.
///
/// IMPORTANT: falling is not the trigger, and distance fallen is not the trigger. Players jump
/// off buildings on purpose, and any distance threshold would snatch them out of the air partway
/// down. What separates a jump from falling out of the world is that a jump *ends* — there is
/// ground below you, so you land. So this looks down for anything at all to land on, and only
/// treats a fall as a fall out of the world when there is nothing beneath the player whatsoever.
/// An intentional fall of any height is left completely alone.
///
/// SETUP: none. RoomManager attaches this to itself, so it follows the DontDestroyOnLoad manager
/// object and needs no scene or prefab change. It watches
/// <c>RoomManager.Instance.localVRPlayer</c>, so no player prefab needs changing either and it
/// works with every avatar, present and future.
///
/// Local only. Each client rescues itself and the corrected position replicates through Fusion
/// like any other movement, so there is nothing networked here and nothing for the host to do.
/// </summary>
public class PlayerFallRecovery : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("How far below the player to look for anything to land on. If ANY solid surface is " +
             "found within this range the player is left alone, no matter how far they are " +
             "falling — that is an intentional jump and it will end by itself. Only a fall with " +
             "nothing at all underneath it counts as falling out of the world.")]
    [SerializeField] float groundSearchDistance = 1000f;

    [Tooltip("How long a player must be falling with nothing beneath them before being rescued. " +
             "Stops a momentary gap in streamed geometry from triggering a teleport.")]
    [SerializeField] float voidGraceSeconds = 3f;

    [Tooltip("How often to run the (long) downward search while a player is airborne. " +
             "No need to do this every frame.")]
    [SerializeField] float voidCheckInterval = 0.25f;

    [Header("Safe position tracking")]
    [Tooltip("How often the player's position is recorded while they are standing on solid ground.")]
    [SerializeField] float sampleInterval = 0.25f;

    [SerializeField] float groundProbeUp = 0.3f;
    [SerializeField] float groundProbeDown = 1.0f;

    [Header("Recovery")]
    [Tooltip("How far to nudge a rescued player sideways when falling back to a spawn point, " +
             "so they do not land inside whoever is standing there.")]
    [SerializeField] float spawnPointScatter = 1f;

    [SerializeField] bool logRecoveries = true;

    GameObject _trackedPlayer;
    Transform _rig;
    XRRigController _rigController;
    FlyingModeScript _flying;

    bool _hasSafePosition;
    Vector3 _safePosition;
    float _sampleTimer;
    float _freefallTimer;
    float _voidCheckTimer;
    bool _nothingBelow;
    bool _loggedUnrecoverable;

    void Update()
    {
        if (!ResolveRig())
            return;

        // Flying deliberately spends long periods off the ground, so recovery stands down.
        if (_flying != null && _flying.enabled)
        {
            ResetFallState();
            return;
        }

        bool grounded = IsGrounded(_rig.position);

        if (grounded)
        {
            ResetFallState();

            _sampleTimer -= Time.deltaTime;
            if (_sampleTimer <= 0f)
            {
                _sampleTimer = sampleInterval;
                _safePosition = _rig.position;
                _hasSafePosition = true;
            }

            return;
        }

        _freefallTimer += Time.deltaTime;

        // Only start asking "is there anything below?" once the fall has lasted a moment.
        if (_freefallTimer < voidGraceSeconds)
            return;

        _voidCheckTimer -= Time.deltaTime;
        if (_voidCheckTimer <= 0f)
        {
            _voidCheckTimer = voidCheckInterval;
            _nothingBelow = !Physics.Raycast(_rig.position, Vector3.down, groundSearchDistance,
                                             ~0, QueryTriggerInteraction.Ignore);
        }

        if (_nothingBelow)
            Recover("nothing below the player — fell out of the world");
    }

    void ResetFallState()
    {
        _freefallTimer = 0f;
        _voidCheckTimer = 0f;
        _nothingBelow = false;
        _loggedUnrecoverable = false;
    }

    /// <summary>
    /// Returns the player to solid ground. Public so it can be wired to an "I'm stuck" button
    /// in the menu — useful for the cases this component cannot detect on its own, such as
    /// being wedged inside geometry rather than below it.
    /// </summary>
    public void RecoverNow() => Recover("manual unstuck");

    void Recover(string reason)
    {
        if (!ResolveRig())
            return;

        Vector3 target;
        string source;

        if (_hasSafePosition)
        {
            // Preferred: put them back where they were actually standing. Far less disruptive
            // than a spawn point — they keep their place in the world and their conversation —
            // and it cannot create a pile-up at the spawn.
            target = _safePosition;
            source = "last safe position";
        }
        else if (TryGetSpawnPointFallback(out target))
        {
            source = "spawn point";
        }
        else
        {
            // Only reachable for a player who has never touched ground AND has no spawn point
            // to fall back on — i.e. the world genuinely is not loaded yet. Keep quiet about it
            // after the first report; the moment the scene arrives, the next check rescues them.
            if (!_loggedUnrecoverable)
            {
                _loggedUnrecoverable = true;
                Debug.LogWarning("[PlayerFallRecovery] Player is below the world with no safe " +
                                 "position and no PlayerSpawnPoint in the loaded scene yet — " +
                                 "will retry once the world is present.");
            }
            return;
        }

        ResetFallState();

        if (_rigController != null)
        {
            _rigController.TeleportTo(target);
        }
        else
        {
            // No rig controller (spectator rig, or a prefab that moves some other way).
            // A plain transform write still beats leaving them under the map.
            _rig.position = target;
        }

        if (logRecoveries)
            Debug.Log($"[PlayerFallRecovery] Recovered local player ({reason}) to {target} via {source}.");
    }

    bool ResolveRig()
    {
        GameObject player = RoomManager.Instance != null ? RoomManager.Instance.localVRPlayer : null;

        if (player == null)
        {
            _trackedPlayer = null;
            _rig = null;
            _rigController = null;
            _flying = null;
            return false;
        }

        if (player != _trackedPlayer)
        {
            // New player object (first spawn, or a scene change). Everything learned about the
            // previous world is meaningless now — most importantly the safe position, which
            // would otherwise teleport someone back into the scene they just left.
            _trackedPlayer = player;
            _rigController = player.GetComponentInChildren<XRRigController>();
            _rig = _rigController != null ? _rigController.transform : player.transform;
            _flying = _rig.GetComponent<FlyingModeScript>();

            _hasSafePosition = false;
            _sampleTimer = 0f;
            ResetFallState();
        }

        return _rig != null;
    }

    bool IsGrounded(Vector3 position)
    {
        Vector3 origin = position + Vector3.up * groundProbeUp;
        return Physics.Raycast(origin, Vector3.down, groundProbeUp + groundProbeDown,
                               ~0, QueryTriggerInteraction.Ignore);
    }

    bool TryGetSpawnPointFallback(out Vector3 position)
    {
        position = default;

        var spawnPoints = FindObjectsByType<PlayerSpawnPointScript>(
            FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);

        if (spawnPoints == null || spawnPoints.Length == 0)
            return false;

        Vector3 basePos = spawnPoints[Random.Range(0, spawnPoints.Length)].transform.position;

        // A rescue is a rare, one-off event rather than a synchronised group arrival, so a
        // random nudge is enough here to avoid landing inside whoever is standing on the spawn.
        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector3 candidate = basePos + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * spawnPointScatter;

        position = SnapDown(candidate, basePos);
        return true;
    }

    static Vector3 SnapDown(Vector3 candidate, Vector3 fallback)
    {
        if (Physics.Raycast(candidate + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit,
                            2.5f, ~0, QueryTriggerInteraction.Ignore))
            return hit.point + Vector3.up * 0.05f;

        return fallback;
    }
}
