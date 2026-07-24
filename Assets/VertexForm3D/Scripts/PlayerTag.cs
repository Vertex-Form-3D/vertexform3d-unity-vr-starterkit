using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class PlayerTag : MonoBehaviour
{
    [SerializeField] Transform _cameraTransform;
    void Awake()
    {
        if (_cameraTransform == null)
        {
            _cameraTransform = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (_cameraTransform != null)
        {
            // Face the camera while keeping the text upright
            transform.LookAt(_cameraTransform);
            transform.rotation = Quaternion.LookRotation(
                transform.position - _cameraTransform.position,
                Vector3.up
            );
        }
    }
}
