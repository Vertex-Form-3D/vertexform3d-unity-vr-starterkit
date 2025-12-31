// ----------------------------------------------------------------------------
// <copyright file="VoiceLogger.cs" company="Exit Games GmbH">
//   Photon Voice for Unity - Copyright (C) 2018 Exit Games GmbH
// </copyright>
// <summary>
// Logger for voice components.
// </summary>
// <author>developer@photonengine.com</author>
// ----------------------------------------------------------------------------


namespace Photon.Voice.Unity
{
    using UnityEngine;

    [AddComponentMenu("Photon Voice/Voice Logger")]
    [DisallowMultipleComponent]
    public class VoiceLogger : MonoBehaviour
    {
        public LogLevel LogLevel = LogLevel.Warning;

        // required for the MonoBehaviour to have the 'enabled' checkbox
        private void Start()
        {
        }

        static public VoiceLogger FindLogger(GameObject gameObject)
        {
            // serach through the hierarchy
            for (var go = gameObject; go != null; go = go.transform.parent == null ? null : go.transform.parent.gameObject)
            {
                var vl = go.GetComponent<VoiceLogger>();
                if (vl != null && vl.enabled)
                {
                    return vl;
                }
            }

            // look for VoiceLogger at the root
            VoiceLogger vlRoot = null;
            foreach (var vl in Object.FindObjectsOfType<VoiceLogger>())
            {
                if (vl.transform.parent == null && vl.enabled)
                {
                    if (vlRoot != null)
                    {
                        UnityLogger.Log(LogLevel.Info, vl, "LOGGER", vlRoot.name, "Disabling VoiceLogger duplicates at the scene root.");
                        vl.enabled = false;
                    }
                    else
                    {
                        vlRoot = vl;
                    }
                }
            }
            return vlRoot;
        }

        static public VoiceLogger CreateRootLogger()
        {
            var logObject = new GameObject("VoiceLogger");
            return logObject.AddComponent<VoiceLogger>();
        }

#if UNITY_EDITOR
        static public void EditorVoiceLoggerOnInspectorGUI(GameObject gameObject)
        {
            var vl = FindLogger(gameObject);
            if (vl == null)
            {
                vl = CreateRootLogger();
                // vl.gameObject.hideFlags = HideFlags.HideInHierarchy; //NotEditable HideInInspector HideInHierarchy
                // Let the Editor know that the scene has been updated
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(vl.gameObject.scene);
            }

            var newLevel = (LogLevel)UnityEditor.EditorGUILayout.EnumPopup("Log Level", vl.LogLevel);
            if (newLevel != vl.LogLevel)
            {
                vl.LogLevel = newLevel;
                // the Editor does not trace changes in another component automatically and needs a hint
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(vl.gameObject.scene);
            }
        }
#endif
    }

    public static class UnityLogger
    {
        public static void Log(LogLevel level, Object obj, string tag, string objName, string fmt, params object[] args)
        {
            // obj.name is available only on the main thread, so we pass objName here
            fmt = GetFormatString(level, tag, objName, fmt);
            if (obj == null)
            {
                switch (level)
                {
                    case LogLevel.Error: Debug.LogErrorFormat(fmt, args); break;
                    case LogLevel.Warning: Debug.LogWarningFormat(fmt, args); break;
                    default: Debug.LogFormat(fmt, args); break;
                }
            }
            else
            {
                switch (level)
                {
                    case LogLevel.Error: Debug.LogErrorFormat(obj, fmt, args); break;
                    case LogLevel.Warning: Debug.LogWarningFormat(obj, fmt, args); break;
                    default: Debug.LogFormat(obj, fmt, args); break;
                }
            }
        }
        private static string GetFormatString(LogLevel level, string tag, string objName,  string fmt)
        {
            return string.Format("[{0}] [{1}] [{2}] [{3}] {4}", GetTimestamp(), level, tag, objName, fmt);
        }

        private static string GetTimestamp()
        {
            return System.DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss", new System.Globalization.CultureInfo("en-US"));
        }
    }
}
