using UnityEngine;
using UnityEditor;

namespace VertextFormCore
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