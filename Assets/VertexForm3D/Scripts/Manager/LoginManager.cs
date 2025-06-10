using UnityEngine;
using Photon.Pun;
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
            ConnectToPhotonServer();
        }

        public void ConnectToPhotonServer()
        {
            if (PlayerName_InputName != null)
            {
                PhotonNetwork.LocalPlayer.NickName = !string.IsNullOrEmpty(PlayerName_InputName.text) ? PlayerName_InputName.text : ProjectManager.instance.projectDataSO.projectData.anonymousUserNamePrefix + Random.Range(1111, 9999);                
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
            EditorGUILayout.HelpBox("This script in responsible for connecting to Photon Servers. ", MessageType.Info);
            LoginManager loginManager = (LoginManager)target;

            if (GUILayout.Button("connect anonymously"))
            {
                loginManager.ConnectAnonymously();
            }
        }
    }
}
#endif