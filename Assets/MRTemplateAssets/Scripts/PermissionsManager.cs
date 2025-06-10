using System;
using System.Collections;
using System.Collections.Generic;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif // UNITY_ANDROID
using UnityEngine.Events;

namespace UnityEngine.XR.Templates.MR
{
    /// <summary>
    /// Utility class to help define and manage Android device permissions and specify corresponding permission callbacks via <see cref="UnityEvent"/>.
    /// </summary>
    public class PermissionsManager : MonoBehaviour
    {
        const string k_DefaultPermissionId = "com.oculus.permission.USE_SCENE";

        [SerializeField]
        private bool m_ProcessPermissionsOnStart = true;

        [Serializable]
        class PermissionRequest
        {
            [SerializeField, Tooltip("The Android system permission to request when this component starts.")]
            public string m_PermissionId = k_DefaultPermissionId;

            [SerializeField, Tooltip("Whether to request permission from the operating system.")]
            public bool m_Enabled = true;

            [HideInInspector]
            public bool m_Requested = false;

            [HideInInspector]
            public bool m_ResponseReceived = false;

            [HideInInspector]
            public bool m_Granted = false;

            [SerializeField]
            public UnityEvent<string> m_PermissionGranted;

            [SerializeField]
            public UnityEvent<string> m_PermissionDenied;
        }

#pragma warning disable CS0414
        [SerializeField, Tooltip("The system permissions to request when this component Starts.")]
        List<PermissionRequest> m_PermissionRequests = new List<PermissionRequest>();

        int m_CurrentPermissionIdx = 0;
#pragma warning restore CS0414

        IEnumerator Start()
        {
            if (m_ProcessPermissionsOnStart)
                yield return ProcessPermissions();
        }

        public IEnumerator ProcessPermissions()
        {
            var permissions = new List<string>();

#if UNITY_ANDROID
            for (m_CurrentPermissionIdx = 0; m_CurrentPermissionIdx < m_PermissionRequests.Count;)
            {
                var permission = m_PermissionRequests[m_CurrentPermissionIdx];
                if (!permission.m_Enabled)
                {
                    m_CurrentPermissionIdx++;
                    continue;
                }

                if (!Permission.HasUserAuthorizedPermission(permission.m_PermissionId) && !permission.m_Requested)
                {
                    permissions.Add(permission.m_PermissionId);
                    permission.m_Requested = true;
                }
                else
                {
                    Debug.Log($"User has permission for: {permission.m_PermissionId}", this);
                }
                m_CurrentPermissionIdx++;
            }

            if (permissions.Count > 0)
            {
                var callbacks = new PermissionCallbacks();
                callbacks.PermissionDenied += OnPermissionDenied;
                callbacks.PermissionGranted += OnPermissionGranted;

                Permission.RequestUserPermissions(permissions.ToArray(), callbacks);
            }
#endif // UNITY_ANDROID

            yield break;
        }

        void OnPermissionGranted(string permissionStr)
        {
            var permission = m_PermissionRequests[m_CurrentPermissionIdx];
            permission.m_Granted = true;
            permission.m_ResponseReceived = true;
            m_CurrentPermissionIdx++;

            Debug.Log($"User granted permission for: {permissionStr}", this);
            permission.m_PermissionGranted.Invoke(permissionStr);
        }

        void OnPermissionDenied(string permissionStr)
        {
            var permission = m_PermissionRequests[m_CurrentPermissionIdx];
            permission.m_Granted = false;
            permission.m_ResponseReceived = true;
            m_CurrentPermissionIdx++;

            Debug.LogWarning($"User denied permission for: {permissionStr}", this);
            permission.m_PermissionDenied.Invoke(permissionStr);
        }
    }
}
