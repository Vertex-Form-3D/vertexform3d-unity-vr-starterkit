using System.Collections.Generic;
using UnityEngine.XR.Hands;

namespace UnityEngine.XR.Templates.MR
{
    /// <summary>
    /// This class is a convenience wrapper to handle external start/stop
    /// of a currently running XR Hand Subsystem.
    /// </summary>
    /// <seealso cref="XRHandSubsystem"/>
    public class HandSubsystemManager : MonoBehaviour
    {
        static List<XRHandSubsystem> s_HandSubsystems;
        XRHandSubsystem m_HandSubsystem;

        void OnEnable()
        {
            if (m_HandSubsystem == null)
            {
                TryGetHandSubsystem(out m_HandSubsystem);
            }
        }

        /// <summary>
        /// This function will attempt to find a currently running hand tracking subsystem and stop it.
        /// </summary>
        /// <seealso cref="XRHandSubsystem"/>
        public void DisableHandTracking()
        {
            if (m_HandSubsystem != null || TryGetHandSubsystem(out m_HandSubsystem))
            {
                m_HandSubsystem.Stop();
            }
        }

        /// <summary>
        /// This function will attempt to find a current hand tracking subsystem and start it up.
        /// </summary>
        /// <seealso cref="XRHandSubsystem"/>
        public void EnableHandTracking()
        {
            if (m_HandSubsystem != null || TryGetHandSubsystem(out m_HandSubsystem))
            {
                m_HandSubsystem.Start();
            }
        }

        // This is taken from XRInputTrackingAggregator and should be removed once the internal version
        // has been made publicly available.
        static bool TryGetHandSubsystem(out XRHandSubsystem handSubsystem)
        {
            s_HandSubsystems ??= new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(s_HandSubsystems);
            if (s_HandSubsystems.Count == 0)
            {
                handSubsystem = default;
                return false;
            }

            if (s_HandSubsystems.Count > 1)
            {
                for (var i = 0; i < s_HandSubsystems.Count; ++i)
                {
                    handSubsystem = s_HandSubsystems[i];
                    if (handSubsystem.running)
                        return true;
                }
            }

            handSubsystem = s_HandSubsystems[0];
            return true;
        }
    }
}
