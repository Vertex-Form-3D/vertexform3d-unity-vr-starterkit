#if PHOTON_VOICE_FMOD_ENABLE
using UnityEngine;

namespace Photon.Voice.Unity.FMOD
{
    [AddComponentMenu("Photon Voice/FMOD/Speaker FMOD")]
    public class SpeakerFMOD : Speaker
    {
        [SerializeField]
        private bool useEvent;
        [SerializeField]
        private FMODUnity.EventReference eventReference; // todo: expose as a property to make it possible to switch event at runtime & re initialize Speaker

        protected override IAudioOut<float> CreateAudioOut()
        {
            if (this.useEvent)
            {
                var instance = FMODUnity.RuntimeManager.CreateInstance(this.eventReference);
                if (instance.isValid())
                {
                    FMODUnity.RuntimeManager.AttachInstanceToGameObject(instance, this.transform, this.GetComponent<Rigidbody>());
                    instance.start();
                }
                return new Voice.FMOD.AudioOutEvent<float>(FMODUnity.RuntimeManager.CoreSystem, instance, this.playDelayConfig, this.Logger, string.Empty, true);
            }
            else
            {
                return new Voice.FMOD.AudioOut<float>(FMODUnity.RuntimeManager.CoreSystem, this.playDelayConfig, this.Logger, string.Empty, true);
            }
        }

#if UNITY_EDITOR
        [UnityEditor.CustomEditor(typeof(SpeakerFMOD), true)]
        public class SpeakerFMODEditor : Editor.SpeakerEditor
        {
            UnityEditor.SerializedProperty useEvent;
            UnityEditor.SerializedProperty eventReference;
            public override void OnEnable()
            {
                base.OnEnable();
                this.useEvent = serializedObject.FindProperty("useEvent");
                this.eventReference = serializedObject.FindProperty("eventReference");
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();
                UnityEditor.EditorGUILayout.Space();
                UnityEditor.EditorGUILayout.PropertyField(useEvent);
                UnityEditor.EditorGUILayout.PropertyField(eventReference);
                serializedObject.ApplyModifiedProperties();
            }
        }
#endif

    }
}
#endif