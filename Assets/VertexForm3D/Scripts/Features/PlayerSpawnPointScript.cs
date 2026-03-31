using UnityEngine;

[ExecuteInEditMode]
public class PlayerSpawnPointScript : MonoBehaviour
{
    private MeshRenderer renderer;
    
    void OnEnable()
    {
        renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer != null)
        {
            renderer.enabled = !Application.isPlaying;
        }
    }
}