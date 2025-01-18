using UnityEngine;
using UnityEngine.SceneManagement;

namespace VertextFormCore
{
    public class SceneChanger : MonoBehaviour
    {
        public string[] sceneNames; // Array to hold scene names in the inspector

        public void ChangeScene(int index)
        {
            if (index >= 0 && index < sceneNames.Length)
            {
                SceneManager.LoadScene(sceneNames[index]);
            }
            else
            {
                Debug.LogError("Invalid scene index!");
            }
        }
    }
}