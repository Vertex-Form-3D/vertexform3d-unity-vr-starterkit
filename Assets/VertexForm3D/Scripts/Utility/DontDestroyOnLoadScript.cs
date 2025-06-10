using UnityEngine;

public class DontDestroyOnLoadScript : MonoBehaviour
{
    void Start()
    {
        transform.parent = null; // Ensure the script is not a child of another object
        DontDestroyOnLoad(gameObject);
    }

}
