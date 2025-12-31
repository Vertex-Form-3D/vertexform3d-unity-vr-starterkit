using UnityEngine;

public class LegHintController : MonoBehaviour
{
    [Header("References")]
    public Transform hips;
    public Transform leftFootTarget;
    public Transform rightFootTarget;
    public Transform leftHint;
    public Transform rightHint;

    [Header("Offsets")]
    public float forwardOffset = 0.3f;
    public float outwardOffset = 0.2f;
    public float verticalOffset = -0.05f;

    void LateUpdate()
    {
        UpdateHint(leftHint, leftFootTarget, -hips.right);
        UpdateHint(rightHint, rightFootTarget, hips.right);
    }

    void UpdateHint(Transform hint, Transform footTarget, Vector3 outwardDirection)
    {
        Vector3 forwardDir = Vector3.ProjectOnPlane(hips.forward, Vector3.up).normalized;
        Vector3 basePos = hips.position;

        Vector3 desiredPos =
            basePos
            + forwardDir * forwardOffset
            + outwardDirection * outwardOffset
            + Vector3.up * verticalOffset;

        // You can smooth this if needed
        hint.position = Vector3.Lerp(hint.position, desiredPos, Time.deltaTime * 10f);
    }

    private void OnDrawGizmos()
    {
        if (leftHint)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(leftHint.position, 0.02f);
        }
        if (rightHint)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(rightHint.position, 0.02f);
        }
    }
}
