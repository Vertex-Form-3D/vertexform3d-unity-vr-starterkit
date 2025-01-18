using CesiumForUnity;
using UnityEngine;
using VertextFormCore;

public class CesiumWorldManager : MonoBehaviour
{
    public CesiumGeoreference georeference;
    public Cesium3DTileset tileset;
    public bool changeLatLong;
    public GameObject podGround;
    public CesiumWorldClass cesiumWorld = new CesiumWorldClass();

    void Start()
    {
        cesiumWorld = SceneLoader.Instance.cesiumWorldClass;
        SetLatLong();
    }

    /*void Update()
    {
        if (changeLatLong)
        {
            SetLatLong();
            changeLatLong = false;
        }
    }*/
    public void SetLatLong()
    {
        georeference.latitude = cesiumWorld.latitude;
        georeference.longitude = cesiumWorld.longitude;
        georeference.height = cesiumWorld.height;
        if (cesiumWorld.loadFromURL)
        {
            tileset.tilesetSource = CesiumDataSource.FromUrl;
            tileset.url = cesiumWorld.URL;
        }
        else
        {
            tileset.tilesetSource = CesiumDataSource.FromCesiumIon;
        }
        tileset.RecreateTileset();
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
