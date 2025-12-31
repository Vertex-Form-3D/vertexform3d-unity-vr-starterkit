#if FUSION_WEAVER
using System;

namespace Photon.Voice.Fusion.Editor
{
    using Unity.Editor;
    using UnityEditor;
    using UnityEngine;
    using global::Fusion;

    [CustomEditor(typeof(FusionVoiceClient), true)]
    public class FusionVoiceClientEditor : VoiceConnectionEditor
    {
        private SerializedProperty autoConnectAndJoinSp;
        private SerializedProperty useFusionAppSettingsSp;
        private SerializedProperty useFusionAuthValuesSp;

        protected override void OnEnable()
        {
            base.OnEnable();
            this.autoConnectAndJoinSp = this.serializedObject.FindProperty("AutoConnectAndJoin");
            this.useFusionAppSettingsSp = this.serializedObject.FindProperty("UseFusionAppSettings");
            this.useFusionAuthValuesSp = this.serializedObject.FindProperty("UseFusionAuthValues");
        }

        protected override void DisplayAppSettings()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(this.useFusionAppSettingsSp, new GUIContent("Use Fusion App Settings", "Use App Settings From Fusion's PhotonServerSettings"));
            if (GUILayout.Button("FusionAppSettings", EditorStyles.miniButton, GUILayout.Width(150)))
            {
#if FUSION2
                Selection.objects = new Object[] { global::Fusion.Photon.Realtime.PhotonAppSettings.Global };
                EditorGUIUtility.PingObject(global::Fusion.Photon.Realtime.PhotonAppSettings.Global);
#else
                Selection.objects = new Object[] { global::Fusion.Photon.Realtime.PhotonAppSettings.Instance };
                EditorGUIUtility.PingObject(global::Fusion.Photon.Realtime.PhotonAppSettings.Instance);
#endif
            }
            EditorGUILayout.EndHorizontal();
            if (!this.useFusionAppSettingsSp.boolValue)
            {
                EditorGUI.indentLevel++;
                base.DisplayAppSettings();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(this.useFusionAuthValuesSp, new GUIContent("Use Fusion Auth Values", "Use the same Authentication Values From PUN client"));
        }

        protected override void ShowHeader()
        {
            base.ShowHeader();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(this.autoConnectAndJoinSp, new GUIContent("Auto Connect And Join", "Auto connect voice client and join a voice room when Fusion client is joined to a Fusion room"));
            if (EditorGUI.EndChangeCheck())
            {
                this.serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
#endif