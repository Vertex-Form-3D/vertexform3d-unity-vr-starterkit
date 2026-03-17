using Fusion;
using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VertexFormCore;

public class LaunchProjectileScript : NetworkBehaviour
{
    [SerializeField]
    [Tooltip("The projectile that's created")]
    GameObject m_ProjectilePrefab = null;

    [SerializeField]
    [Tooltip("The point that the project is created")]
    Transform m_StartPoint = null;

    [SerializeField]
    [Tooltip("The speed at which the projectile is launched")]
    float m_LaunchSpeed = 1.0f;

    XRGrabInteractable m_GrabInteractable;

    private void Start()
    {
        m_GrabInteractable = GetComponent<XRGrabInteractable>();
        m_GrabInteractable.selectEntered.AddListener(OnSelectEntered);
    }

    public string playerName;
    private void OnSelectEntered(SelectEnterEventArgs arg0)
    {
        PlayerNetworkSetup playerNetworkSetup = arg0.interactorObject.transform.GetComponentInParent<PlayerNetworkSetup>();
        if (playerNetworkSetup != null)
        {
            playerName = playerNetworkSetup.PlayerName.ToString();
        }
    }

    public void FireBall()
    {
        if (Object.HasInputAuthority)
        {
            RPC_Fire();
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_Fire()
    {
        GameObject newObject = Instantiate(m_ProjectilePrefab, m_StartPoint.position, m_StartPoint.rotation, null);
        newObject.GetComponent<BulletScript>().ShooterName = playerName;

        Debug.Log("Fire ball");
        if (newObject.TryGetComponent(out Rigidbody rigidBody))
            ApplyForce(rigidBody);
    }

    void ApplyForce(Rigidbody rigidBody)
    {
        Vector3 force = m_StartPoint.forward * m_LaunchSpeed;
        rigidBody.AddForce(force);
    }
}
