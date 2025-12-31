using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VertexFormCore
{
    public class LoginManager : MonoBehaviour
    {
        public TMP_InputField PlayerName_InputName;

        public void ConnectAnonymously()
        {
            ConnectToServer();
        }

        public void ConnectToServer()
        {
            if (PlayerName_InputName != null)
            {
                string playerName = !string.IsNullOrEmpty(PlayerName_InputName.text) ?
                    PlayerName_InputName.text :
                    ProjectManager.instance.projectDataSO.projectData.anonymousUserNamePrefix + Random.Range(1111, 9999);
                ProjectManager.UserName = playerName;
                // Store player name for later use in Fusion
                PlayerPrefs.SetString("PlayerName", playerName);
                PlayerPrefs.Save();

                SceneManager.LoadScene(1);
            }
        }
    }
}

#if UNITY_EDITOR
namespace VertexFormCore
{
    [CustomEditor(typeof(LoginManager))]
    public class LoginManagerScript : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("This script is responsible for setting up player name and loading the main scene.", MessageType.Info);
            LoginManager loginManager = (LoginManager)target;

            if (GUILayout.Button("connect anonymously"))
            {
                loginManager.ConnectAnonymously();
            }
        }
    }
}
#endif