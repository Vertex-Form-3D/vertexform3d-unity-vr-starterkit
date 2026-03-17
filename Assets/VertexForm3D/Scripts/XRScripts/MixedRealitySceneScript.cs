using System;
using System.Collections;
using UnityEngine;
using VertexFormCore;

public class MixedRealitySceneScript : MonoBehaviour
{
    IEnumerator Start()
    {
        while (RoomManager.Instance == null || RoomManager.Instance.localVRPlayer == null || RoomManager.Instance.GetLocalPlayerSetup() == null)
        {
            yield return new WaitForSeconds(0.5f);
        }
        var handler = RoomManager.Instance.localVRPlayer.GetComponent<MixedRealityHandler>();
        if (handler != null)
            handler.EnableMixedReality();
    }
}
