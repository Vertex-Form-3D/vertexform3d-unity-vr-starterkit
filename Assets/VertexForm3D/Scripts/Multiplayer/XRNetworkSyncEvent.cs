using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.Events;
using System;

public class XRNetworkSyncEvent : NetworkBehaviour
{
    public RpcTargets rpcTargets = RpcTargets.All;
    public UnityEvent networkEvent;

    [Networked] public NetworkBool isInvoked { get; set; }
    public override void Spawned()
    {
        if (isInvoked)
        {
            networkEvent.Invoke();
        }
    }
    public void SyncEventOverTheNetwork()
    {
        if (HasStateAuthority)
        {
            // I have state authority, call the network event directly
            CallNetworkEvent();
        }
        else
        {
            // Request state authority and wait for it
            Object.RequestStateAuthority();
            StartCoroutine(WaitForStateAuthorityAndCall());
        }
    }

    // New coroutine to properly wait for state authority
    IEnumerator WaitForStateAuthorityAndCall()
    {
        float timeout = 2f; // 2 second timeout
        float elapsed = 0f;

        while (!HasStateAuthority && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        if (HasStateAuthority)
        {
            CallNetworkEvent();
        }
        else
        {
            Debug.LogWarning($"Failed to acquire state authority for {gameObject.name} within timeout period");
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_NetworkEvent()
    {
        networkEvent.Invoke();
    }

    IEnumerator WaitNRun(float seconds, Action networkEvent)
    {
        yield return new WaitForSeconds(seconds);
        networkEvent.Invoke();
    }

    public void CallNetworkEvent()
    {
        // Only call RPC if we have the required authority
        if (HasStateAuthority)
        {
            isInvoked = true;
            RPC_NetworkEvent();
        }
        else
        {
            Debug.LogWarning($"Cannot call RPC_NetworkEvent on {gameObject.name}: Missing state authority");
        }
    }
}
