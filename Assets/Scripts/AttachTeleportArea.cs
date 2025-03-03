using System.Collections;
using UnityEngine;
using CesiumForUnity;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UIElements;

namespace VertextFormCore
{
    public class AttachTeleportArea : MonoBehaviour
    {
        private bool keepChecking = true;

        void Start()
        {
            // Start the coroutine to keep monitoring PanelRaycasters
            StartCoroutine(ContinuouslyCheckAndDisableRaycasters());

            int teleportLayer = InteractionLayerMask.GetMask(new string[] { "Teleport" });
            Cesium3DTileset tileset = GetComponent<Cesium3DTileset>();
            if (tileset != null)
            {
                tileset.OnTileGameObjectCreated += go =>
                {
                    var ta = go.AddComponent<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationArea>();
                    ta.interactionLayers = teleportLayer;
                    if (SpawnManager.Instance.localVRPlayer)
                    {
                        ta.teleportationProvider = SpawnManager.Instance.localVRPlayer.GetComponent<PlayerNetworkSetup>().tp;
                    }
                };
            }
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
                yield return new WaitForSeconds(0.5f); // Check every half second
            }
        }

        private void OnDestroy()
        {
            keepChecking = false;
        }
    }
}