using UnityEngine;
using UnityEngine.SceneManagement;
using VertexFormCore;

public class SelfMapDisable : MonoBehaviour
{
    void Start()
    {
        return;
        if (SceneManager.GetActiveScene().name != "Map")
        {
            gameObject.SetActive(false);
        }
    }
}
