using System;
using System.Collections;
using UnityEngine;
using VertexFormCore;

public class MixedRealitySceneScript : MonoBehaviour
{
    IEnumerator Start()
    {
        while (SpawnManager.Instance == null || SpawnManager.Instance.localVRPlayer == null)
        {
            yield return new WaitForSeconds(0.5f);
        }
        InitializeMixedRealityScene();
    }

    private void InitializeMixedRealityScene()
    {
        SpawnManager.Instance.localVRPlayer.GetComponent<MixedRealityHandler>().EnableMixedReality();
    }
}
