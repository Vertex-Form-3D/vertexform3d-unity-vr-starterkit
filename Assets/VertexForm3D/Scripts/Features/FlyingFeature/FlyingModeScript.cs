using Fusion;
using UnityEngine;
using UnityEngine.XR;
using VertexFormCore;

[RequireComponent(typeof(InputData))]
public class FlyingModeScript : MonoBehaviour
{
    [SerializeField] private Vector3 flydirection;
    private InputData _inputData;

    [SerializeField] Transform leftHand;
    public float flyingSpeed = 1;
    public float flyingSensitivity = 2;
    public float normalSpeed = 2;
    public float normalSensitivity = 2;
    public float intensity = .3f;
    public bool isFlying;
    [SerializeField] NetworkObject networkObject;
    [SerializeField] private bool testingInEditor;

    private CharacterController characterController;
    public float groundCheckDistance = 0.1f;
    public bool isGrounded;
    public float flyingTime;

    void Start()
    {
        if (networkObject != null)
        {
            if (!networkObject.HasInputAuthority)
            {
                Destroy(this);
            }
        }
        characterController = GetComponent<CharacterController>();
        _inputData = GetComponent<InputData>();
    }

    void Update()
    {
        if (ProjectManager.instance.platformAndSettings.platformChoice == platform.Desktop)
        {
            isFlying = Input.GetKey(KeyCode.F);
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                flyingSensitivity += intensity * Time.deltaTime;
                flyingSpeed = flyingSensitivity;
            }
            else
            {
                flyingSpeed = normalSpeed;
                flyingSensitivity = normalSensitivity;
            }
        }
        else
        {
            if (_inputData._leftController.TryGetFeatureValue(CommonUsages.trigger, out float leftTrigger))
            {
                bool wasFlying = isFlying;
                isFlying = leftTrigger == 1;
                if (isFlying && !wasFlying)
                {
                    StartFlying();
                }
                else if (!isFlying && wasFlying)
                {
                    StopFlying();
                }
                if (isFlying && _inputData._leftController.TryGetFeatureValue(CommonUsages.gripButton, out bool leftGripBtn))
                {
                    if (leftGripBtn)
                    {
                        flyingSensitivity += intensity * Time.deltaTime;
                        flyingSpeed = flyingSensitivity * leftTrigger;
                    }
                    else
                    {
                        flyingSpeed = normalSpeed;
                        flyingSensitivity = normalSensitivity;
                    }
                }
            }
        }
        Fly();
    }
    public void StartFlying()
    {
        Debug.Log("Flying started");

    }

    public void StopFlying()
    {
        Debug.Log("Flying stopped");



    }
    private void Fly()
    {
        if (isFlying || testingInEditor)
        {
            if (ProjectManager.instance.platformAndSettings.platformChoice == platform.Desktop)
            {
                if (characterController.GetComponent<XRRigController>().isThirdPerson)
                {
                    flydirection = characterController.GetComponent<XRRigController>().orbitCamera.transform.forward;
                }
                else
                {
                    flydirection = characterController.GetComponent<XRRigController>().cameraTransform.transform.forward;
                }
            }
            else
            {
                flydirection = leftHand.transform.forward;
            }
            flyingTime += Time.deltaTime;
            characterController.Move(flydirection.normalized * flyingSpeed * Time.deltaTime);
        }
        else
        {
            flyingSensitivity = normalSensitivity;
        }
    }
}
