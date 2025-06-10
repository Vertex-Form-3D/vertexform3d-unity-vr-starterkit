using UnityEngine;

[ExecuteInEditMode]
public class PlayerSpawnPointScript : MonoBehaviour
{
    private Renderer renderer;


    void OnEnable()
    {
        if (Application.isPlaying)
        {
            renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false; // Enable MeshRenderer when the scene starts
            }
        }
    }
}