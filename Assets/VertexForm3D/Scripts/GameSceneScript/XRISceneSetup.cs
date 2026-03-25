using System.Collections;
using UnityEngine;
using UnityEngine.XR.Content.Interaction;
using UnityEngine.XR.Content.Walkthrough;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Climbing;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace VertexFormCore
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
            while (RoomManager.Instance == null || RoomManager.Instance.localVRPlayer == null || RoomManager.Instance.GetLocalPlayerSetup() == null)
            {
                yield return new WaitForSeconds(.5f);
            }
            var pns = RoomManager.Instance.GetLocalPlayerSetup();
            if (pns == null) yield break;
            SetTeleportationProvider(pns.tp);
            SetClimpProvider(pns.cp);
            yield return new WaitForSeconds(1f);
            RoomManager.Instance.localVRPlayer.transform.rotation = Quaternion.identity;
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
            var pns = RoomManager.Instance.GetLocalPlayerSetup();
            if (pns != null)
                locomotionSetup.m_Manager = pns.locomotionManager;
        }
    }
}
