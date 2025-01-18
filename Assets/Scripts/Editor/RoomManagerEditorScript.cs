using UnityEngine;
using UnityEditor;
using VertextFormCore;

namespace VertextFormCore
{

    [CustomEditor(typeof(RoomManager))]
    public class RoomManagerEditorScript : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("This script is responsible for creating and joining rooms.", MessageType.Info);

            RoomManager roomManager = (RoomManager)target;


            if (GUILayout.Button("Home"))
            {
                if (VirtualRoomManager.Instance == null) return;
                VirtualRoomManager.Instance.LeaveRoomAndLoadHomeScene();
            }
        }
    }
}