using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class PlayerTag : MonoBehaviour
{
    [SerializeField] Transform _cameraTransform;
    private NetworkObject _networkObject;

    void Start()
    {
        _networkObject = GetComponentInParent<NetworkObject>();

        // // Disable the name tag for the local player
        // if (_networkObject != null && _networkObject.HasInputAuthority)
        // {
        //     gameObject.SetActive(false);
        //     return;
        // }

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
