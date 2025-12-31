using System;
using System.Collections;
using UnityEngine;
using VertexFormCore;

public class MixedRealitySceneScript : MonoBehaviour
{
    IEnumerator Start()
    {
        while (RoomManager.Instance == null || RoomManager.Instance.localVRPlayer == null)
        {
            yield return new WaitForSeconds(0.5f);
        }
        InitializeMixedRealityScene();
    }

    private void InitializeMixedRealityScene()
    {
        RoomManager.Instance.localVRPlayer.GetComponent<MixedRealityHandler>().EnableMixedReality();
    }
}
