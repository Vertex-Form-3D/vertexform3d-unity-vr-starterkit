using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class PlayerTag : MonoBehaviour
{
    [SerializeField] Transform _cameraTransform;
    void Awake()
    {
        TryResolveCamera();
    }

    void LateUpdate()
    {
        if (_cameraTransform == null)
            TryResolveCamera();

        if (_cameraTransform == null)
            return;

        // Face the camera while keeping the text upright
        transform.LookAt(_cameraTransform);
        transform.rotation = Quaternion.LookRotation(
            transform.position - _cameraTransform.position,
            Vector3.up
        );
    }

    void TryResolveCamera()
    {
        if (_cameraTransform != null)
            return;

        var main = Camera.main;
        if (main != null)
            _cameraTransform = main.transform;
    }
}
