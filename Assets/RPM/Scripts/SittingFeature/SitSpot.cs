using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using VertexFormCore;

[RequireComponent(typeof(SphereCollider))]
public class SitSpot : MonoBehaviour
{
    [Header("Seat Setup")]
    [HideInInspector] public UnityEvent<SitSpot> OnSitRequest = new();
    public Transform SitPoint;
    [SerializeField] private float interactionRange = 1.2f;
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private Button sitButton;

    private bool isPlayerNearby;
    private bool isOccupied;

    private void Start()
    {
        if (!SitPoint)
        {
            SitPoint = new GameObject("SitPoint").transform;
            SitPoint.SetParent(transform);
            SitPoint.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        uiCanvas.enabled = false;
        sitButton.onClick.AddListener(() => { HandleSit(); /*OnSitRequest.Invoke(this); */});

        var col = GetComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = interactionRange;
    }

    public void HandleSit()
    {
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.localVRPlayer.GetComponentInChildren<SittingController>().HandleSitRequest(this);
        }
        else
        {
            SittingController sittingController = FindAnyObjectByType<SittingController>();
            sittingController.HandleSitRequest(this);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (isOccupied) return;

        if (other.CompareTag("Player"))
        {
            uiCanvas.transform.LookAt(other.transform.GetComponentInChildren<Camera>().transform);
            uiCanvas.transform.rotation*= Quaternion.Euler(Vector3.up*180);
            isPlayerNearby = true;
            uiCanvas.enabled = true;
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

    public void SetOccupied(bool value)
    {
        isOccupied = value;
        uiCanvas.enabled = !value && isPlayerNearby;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Draw the actual SitPoint position (Y=0)
        if (SitPoint == null)
        {
            Gizmos.color = Color.yellow;
            DrawCrosshair(transform.position, 0.15f);
            DrawArrowWithHandles(transform.position, transform.position + transform.forward * 0.2f);
        }
        else
        {
            Gizmos.color = Color.cyan;
            DrawCrosshair(SitPoint.position, 0.15f);
            Gizmos.color = Color.green;
            DrawArrowWithHandles(SitPoint.position, SitPoint.position + SitPoint.forward * 0.2f);
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