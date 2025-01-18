using CesiumForUnity;
using System;
using UnityEngine;

public class CesiumSceneHandler : MonoBehaviour
{
    public Action refreshTilesAction;
    Cesium3DTileset tileset;
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
        tileset=GetComponent<Cesium3DTileset>();
        tileset.RecreateTileset();
    }
}
