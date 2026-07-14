#if !UNITY_WEBGL
// using CesiumForUnity;
#endif
using UnityEngine;
using VertexFormCore;

public class CesiumWorldManager : MonoBehaviour
{
#if !UNITY_WEBGL
    // public CesiumGeoreference georeference;
    // public Cesium3DTileset tileset;
#endif
    public bool changeLatLong;
    public GameObject podGround;
    public CesiumWorldClass cesiumWorld = new CesiumWorldClass();

    void Start()
    {
    }

    public void SetLatLong()
    {
#if !UNITY_WEBGL
        // georeference.latitude = cesiumWorld.latitude;
        // georeference.longitude = cesiumWorld.longitude;
        // georeference.height = cesiumWorld.height;
        // if (cesiumWorld.loadFromURL)
        // {
        //     tileset.tilesetSource = CesiumDataSource.FromUrl;
        //     tileset.url = cesiumWorld.URL;
        // }
        // else
        // {
        //     tileset.tilesetSource = CesiumDataSource.FromCesiumIon;
        // }
        // tileset.RecreateTileset();
#else
        Debug.LogWarning("[CesiumWorldManager] Cesium is not supported on WebGL.");
#endif
    }
}

[System.Serializable]
public class CesiumWorldClass
{
    public string placeName;
    public bool loadFromURL;
    public string URL;
    public double latitude;
    public double longitude;
    public double height;
}
