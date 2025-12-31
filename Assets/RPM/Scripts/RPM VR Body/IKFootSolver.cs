using UnityEngine;

public class IKFootSolver : MonoBehaviour
{
    [Header("IK Settings")]
    [SerializeField] private LayerMask terrainLayer = default;
    [SerializeField] private Transform body = default;

    [Header("Foot Offsets")]
    [SerializeField] private Vector3 footOffset = default;
    [SerializeField] private float footYPosOffset = 0.1f;
    [SerializeField] private Vector3 footRotOffset = default;

    [Header("Raycast Settings")]
    [SerializeField] private float rayStartYOffset = 0;
    [SerializeField] private float rayLength = 1.5f;

    // new: manually defined local-space offset for left/right foot
    [SerializeField] private float footSpacing = 0.15f; // positive for right, negative for left

    private Vector3 currentPosition;
    private Vector3 currentNormal;

    private void Start()
    {
        UpdateFootPosition(true);
    }

    private void Update()
    {
        UpdateFootPosition();

        // 1. Preserve Y rotation
        float originalYRotation = transform.eulerAngles.y;

        // 2. Calculate surface tilt (X/Z only)
        Quaternion normalRotation = Quaternion.FromToRotation(Vector3.up, currentNormal);
        Vector3 normalEuler = normalRotation.eulerAngles;
        Quaternion surfaceTilt = Quaternion.Euler(normalEuler.x, 0, normalEuler.z);

        // 3. Apply offset rotation
        Quaternion offsetRotation = Quaternion.Euler(footRotOffset.x, 0, footRotOffset.z);

        // 4. Combine (preserving Y)
        transform.rotation = Quaternion.Euler(
            surfaceTilt.eulerAngles.x + offsetRotation.eulerAngles.x,
            originalYRotation,
            surfaceTilt.eulerAngles.z + offsetRotation.eulerAngles.z
        );

        // 5. Apply position
        transform.position = currentPosition + Vector3.up * footYPosOffset + footOffset;
    }

    private void UpdateFootPosition(bool forceUpdate = false)
    {
        // Instead of body.right (which rotates with body), calculate offset in *body local space* 
        Vector3 localOffset = body.TransformDirection(Vector3.right * footSpacing);

        // Stable world-space ray origin
        Vector3 rayOrigin = body.position + localOffset + Vector3.up * rayStartYOffset;

        Debug.DrawRay(rayOrigin, Vector3.down * rayLength, Color.green);

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayLength, terrainLayer.value))
        {
            if (forceUpdate || Vector3.Distance(currentPosition, hit.point) > 0.01f)
            {
                currentPosition = hit.point;
                currentNormal = hit.normal;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(currentPosition, 0.05f);
    }
}
