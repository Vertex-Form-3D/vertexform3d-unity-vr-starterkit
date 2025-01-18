using System.Collections;
using UnityEngine;
using UnityEngine.XR.Content.Interaction;
using UnityEngine.XR.Content.Walkthrough;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Climbing;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace VertextFormCore
{
    public class XRISceneSetup : MonoBehaviour
    {
        [SerializeField] WalkthroughStep[] walkthroughSteps;
        [SerializeField] ClimbInteractable[] climbInteractables;
        [SerializeField] TeleportationAnchor[] teleportationAnchors;
        [SerializeField] LocomotionSetup locomotionSetup;
        void Start()
        {
            StartCoroutine(IESetUpScene());
        }

        IEnumerator IESetUpScene()
        {
            while (SpawnManager.Instance.localVRPlayer == null)
            {
                yield return new WaitForSeconds(.5f);
            }
            SetTeleportationProvider(SpawnManager.Instance.localVRPlayer.GetComponent<PlayerNetworkSetup>().tp);
            SetClimpProvider(SpawnManager.Instance.localVRPlayer.GetComponent<PlayerNetworkSetup>().cp);
            yield return new WaitForSeconds(1f);
            SpawnManager.Instance.localVRPlayer.transform.rotation = Quaternion.identity;
            SetUpLocomotionManager();
        }
        void SetTeleportationProvider(TeleportationProvider tp)
        {

            foreach (var anchor in teleportationAnchors)
            {
                anchor.teleportationProvider = tp;
            }

            if (walkthroughSteps.Length > 0)
            {
                foreach (var step in walkthroughSteps)
                {
                    step.m_TeleportationProvider = tp;
                }
                walkthroughSteps[walkthroughSteps.Length - 1].GetComponent<ButtonPressTrigger>().ButtonPressHandler();
            }
        }

        void SetClimpProvider(ClimbProvider provider)
        {
            foreach (var climp in climbInteractables)
            {
                climp.climbProvider = provider;
            }
        }

        void SetUpLocomotionManager()
        {
            locomotionSetup.m_Manager = SpawnManager.Instance.localVRPlayer.GetComponent<PlayerNetworkSetup>().locomotionManager;
        }
    }
}
