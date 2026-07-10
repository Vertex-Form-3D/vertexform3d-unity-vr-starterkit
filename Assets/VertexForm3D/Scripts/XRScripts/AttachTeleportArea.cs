using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UIElements;
using Photon.Realtime;

namespace VertexFormCore
{
    public class AttachTeleportArea : MonoBehaviour
    {
        private bool keepChecking = true;

        void Start()
        {
            StartCoroutine(ContinuouslyCheckAndDisableRaycasters());

#if !UNITY_WEBGL
            int teleportLayer = InteractionLayerMask.GetMask(new string[] { "Teleport" });

#endif
        }

        IEnumerator ContinuouslyCheckAndDisableRaycasters()
        {
            while (keepChecking)
            {
                PanelRaycaster[] panelRaycasters = FindObjectsByType<PanelRaycaster>(FindObjectsSortMode.InstanceID);
                foreach (var panelRaycaster in panelRaycasters)
                {
                    if (panelRaycaster.enabled)
                    {
                        panelRaycaster.enabled = false;
                    }
                }
                yield return new WaitForSeconds(0.5f);
            }
        }

        private void OnDestroy()
        {
            keepChecking = false;
        }
    }
}
