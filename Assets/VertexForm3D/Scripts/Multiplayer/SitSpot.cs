#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using VertexFormCore;
using Fusion;

[RequireComponent(typeof(SphereCollider))]
public class SitSpot : NetworkBehaviour
{
    [Header("Seat Setup")]
    [HideInInspector] public UnityEvent<SitSpot> OnSitRequest = new();

    [Networked] public bool isOccupied { get; set; }
    public Transform SitPoint;

    [SerializeField] private float interactionRange = 1.2f;
    [SerializeField] private float movementThreshold = 0.3f; // Distance threshold to detect movement
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private Button sitButton;

    [Header("Special Seat")]
    [SerializeField] private Button specialSeatButton;
    [SerializeField] private UnityEvent onSpecialSeatButtonClicked;

    private bool isPlayerNearby;
    private Transform sittingPlayerTransform; // Transform that we move (PlayerNetworkSetup), so distance check matches
    private float sitStartTime; // Grace period after sitting before we check "moved away"
    [SerializeField][Min(0.5f)] private float sitGracePeriod = 1.5f;
    [SerializeField] private bool isSpecialSeat = false;
    private const float MinGracePeriod = 1.2f; // Enforced minimum so prefabs with 0 still work

    private void Start()
    {
        if (!SitPoint)
        {
            SitPoint = new GameObject("SitPoint").transform;
            SitPoint.SetParent(transform);
            SitPoint.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        uiCanvas.enabled = false;
        sitButton.onClick.AddListener(() => { HandleSit(); });

        if (specialSeatButton != null)
        {
            specialSeatButton.gameObject.SetActive(false);
            specialSeatButton.onClick.AddListener(OnSpecialSeatButtonClicked);
        }

        var col = GetComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = interactionRange;
    }

    public void HandleSit()
    {
        if (Runner == null || isOccupied) return;

        if (RoomManager.Instance != null)
        {
            var playerNetworkSetup = RoomManager.Instance.GetLocalPlayerSetup();
            if (playerNetworkSetup != null)
            {
                sittingPlayerTransform = playerNetworkSetup.transform;
                sitStartTime = Time.time;
                SetOccupied(true);
                playerNetworkSetup.SittingOnObject(SitPoint);
                playerNetworkSetup.SetCurrentSitSpot(this);
                Debug.Log($"[SitSpot] Player started sitting on {gameObject.name}");
            }
        }
    }

    public void HandleLeave()
    {
        if (Runner == null || !isOccupied) return;

        if (RoomManager.Instance != null)
        {
            var playerNetworkSetup = RoomManager.Instance.GetLocalPlayerSetup();
            if (playerNetworkSetup != null)
            {
                playerNetworkSetup.ClearCurrentSitSpot();
                playerNetworkSetup.SetStandingHeight(true);
            }
            // Clear the sitting player reference and grace period
            sittingPlayerTransform = null;
            SetOccupied(false);
            Debug.Log($"[SitSpot] Player left the seat ({gameObject.name})");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Runner == null) return;

        if (other.CompareTag("Player"))
        {
            uiCanvas.transform.LookAt(other.transform.GetComponentInChildren<Camera>().transform);
            uiCanvas.transform.rotation *= Quaternion.Euler(Vector3.up * 180);
            isPlayerNearby = true;
            UpdateUI();
            Debug.Log("Player entered sit spot trigger");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            uiCanvas.enabled = false;
        }
    }

    private void Update()
    {
        // Only check movement if the NetworkBehaviour is spawned
        if (Runner == null) return;

        // Check if player has moved away from the seat (after grace period to avoid one-frame glitches)
        float grace = Mathf.Max(sitGracePeriod, MinGracePeriod);
        if (isOccupied && sittingPlayerTransform != null && SitPoint != null && (Time.time - sitStartTime) >= grace)
        {
            // Calculate horizontal distance (ignore Y axis for movement detection)
            Vector3 sitPosition = SitPoint.position;
            Vector3 playerPosition = sittingPlayerTransform.position;

            // Only check horizontal movement (X and Z axes)
            Vector2 sitPos2D = new Vector2(sitPosition.x, sitPosition.z);
            Vector2 playerPos2D = new Vector2(playerPosition.x, playerPosition.z);

            float horizontalDistance = Vector2.Distance(sitPos2D, playerPos2D);

            if (horizontalDistance > movementThreshold)
            {
                // Player has moved away, automatically leave the seat
                Debug.Log($"[SitSpot] Player moved away from seat (distance: {horizontalDistance:F2} > {movementThreshold}), leaving.");
                HandleLeave();
            }
        }
    }

    public void SetOccupied(bool value)
    {
        if (Runner == null) return;
        isOccupied = value;
        UpdateUI();
    }

    private void UpdateUI()
    {
        // Only access networked properties if spawned
        if (Runner == null)
        {
            uiCanvas.enabled = false;
            if (specialSeatButton != null) specialSeatButton.gameObject.SetActive(false);
            return;
        }

        bool localPlayerSitting = isOccupied && sittingPlayerTransform != null;
        bool showSpecialButton = isSpecialSeat && specialSeatButton != null && localPlayerSitting;

        if (showSpecialButton)
        {
            uiCanvas.enabled = true;
            sitButton.gameObject.SetActive(false);
            specialSeatButton.gameObject.SetActive(true);
        }
        else if (isPlayerNearby && !isOccupied)
        {
            uiCanvas.enabled = true;
            sitButton.gameObject.SetActive(true);
            if (specialSeatButton != null) specialSeatButton.gameObject.SetActive(false);
        }
        else
        {
            uiCanvas.enabled = false;
            if (specialSeatButton != null) specialSeatButton.gameObject.SetActive(false);
        }
    }

    /// <summary>Called when the special seat button is clicked. Override or use OnSpecialSeatButtonClicked event in inspector.</summary>
    protected virtual void OnSpecialSeatButtonClicked()
    {
        onSpecialSeatButtonClicked?.Invoke();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Draw the actual SitPoint position (Y=0)
        if (SitPoint == null)
        {
            Gizmos.color = Color.yellow;
            DrawCrosshair(transform.position, 0.15f);
            Gizmos.color = new Color(1f, 0.5f, 0f); // Orange color
            DrawArrowWithHandles(transform.position, transform.position + transform.forward * 0.12f);
        }
        else
        {
            Gizmos.color = Color.cyan;
            DrawCrosshair(SitPoint.position, 0.15f);
            Gizmos.color = new Color(1f, 0.5f, 0f); // Orange color
            DrawArrowWithHandles(SitPoint.position, SitPoint.position + SitPoint.forward * 0.12f);
        }
    }

    private void DrawCrosshair(Vector3 position, float size)
    {
        float halfSize = size * 0.5f;
        Gizmos.DrawLine(position - Vector3.right * halfSize, position + Vector3.right * halfSize);
        Gizmos.DrawLine(position - Vector3.forward * halfSize, position + Vector3.forward * halfSize);
        Gizmos.DrawLine(position - Vector3.up * halfSize * 0.5f, position + Vector3.up * halfSize * 0.5f);
    }

    private void DrawArrowWithHandles(Vector3 start, Vector3 end)
    {
        Handles.color = Gizmos.color;
        Handles.DrawLine(start, end);
        Handles.ArrowHandleCap(0, end, Quaternion.LookRotation(end - start), 0.1f, EventType.Repaint);
    }
#endif
}