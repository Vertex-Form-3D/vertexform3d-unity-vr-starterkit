#if !UNITY_WEBGL
// using CesiumForUnity;
#endif
using System;
using UnityEngine;

public class CesiumSceneHandler : MonoBehaviour
{
    public Action refreshTilesAction;
#if !UNITY_WEBGL
    // Cesium3DTileset tileset;
#endif
    public static CesiumSceneHandler Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }
    void Start()
    {

    }

    private void OnEnable()
    {
        refreshTilesAction += RefreshTileSet;
    }

    private void OnDisable()
    {
        refreshTilesAction -= RefreshTileSet;
    }
    public void RefreshTileSet()
    {
#if !UNITY_WEBGL
        // tileset = GetComponent<Cesium3DTileset>();
        // tileset.RecreateTileset();
#else
        Debug.LogWarning("[CesiumSceneHandler] Cesium is not supported on WebGL.");
#endif
    }
}
