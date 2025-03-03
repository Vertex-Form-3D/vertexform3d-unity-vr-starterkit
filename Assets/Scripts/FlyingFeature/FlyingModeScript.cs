using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using VertextFormCore;

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

    [SerializeField] private bool testingInEditor;

    private CharacterController characterController;
    public float groundCheckDistance = 0.1f;
    public bool isGrounded;


    void Start()
    {
        characterController = GetComponent<CharacterController>();
        _inputData = GetComponent<InputData>();

    }

    void Update()
    {
        if (_inputData._leftController.TryGetFeatureValue(CommonUsages.trigger, out float leftTrigger))
        {
            isFlying = leftTrigger == 1;
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
        Fly();
    }
    private void Fly()
    {
        if (isFlying || testingInEditor)
        {
            flydirection = leftHand.transform.forward;
            characterController.transform.position += flydirection.normalized * flyingSpeed;
        }
        else
        {
            flyingSensitivity = normalSensitivity;
        }
    }

    bool CheckGroundWithRaycast()
    {
        // Perform a raycast from the character's position downwards
        RaycastHit hit;
        Vector3 rayOrigin = transform.position;
        rayOrigin.y += characterController.center.y - characterController.height / 2 + characterController.skinWidth;

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, groundCheckDistance))
        {
            return true;
        }
        return false;
    }
}
