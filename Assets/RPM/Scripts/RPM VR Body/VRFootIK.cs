using UnityEngine;

public class VRFootIK : MonoBehaviour
{
    private Animator animator;
    public Vector3 footOffset;
    [Range(0, 1)]
    public float rightFootPosWeight = 1;
    [Range(0, 1)]
    public float rightFootRotWeight = 1;
    [Range(0, 1)]
    public float leftFootPosWeight = 1;
    [Range(0, 1)]
    public float leftFootRotWeight = 1;
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }
    private void OnAnimatorIK(int layerIndex)
    {
        Vector3 rightFootPos = animator.GetIKPosition(AvatarIKGoal.RightFoot);
        RaycastHit hit;
        bool hasHit = Physics.Raycast(rightFootPos + Vector3.up, Vector3.down, out hit);
        if (hasHit)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, rightFootPosWeight);
            animator.SetIKPosition(AvatarIKGoal.RightFoot, hit.point + footOffset);
            Quaternion footRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, hit.normal), hit.normal);
            animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, rightFootRotWeight);
            animator.SetIKRotation(AvatarIKGoal.RightFoot, footRotation);
        }
        else
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0);
        }
        Vector3 leftFootPos = animator.GetIKPosition(AvatarIKGoal.LeftFoot);
        hasHit = Physics.Raycast(leftFootPos + Vector3.up, Vector3.down, out hit);
        if (hasHit)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, leftFootPosWeight);
            animator.SetIKPosition(AvatarIKGoal.LeftFoot, hit.point + footOffset);
            Quaternion footRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, hit.normal), hit.normal);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, leftFootRotWeight);
            animator.SetIKRotation(AvatarIKGoal.LeftFoot, footRotation);
        }
        else
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0);
        }
    }
}