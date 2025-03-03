
using Photon.Pun;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

namespace VertextFormCore
{
    [RequireComponent(typeof(PhotonView))]
    [RequireComponent(typeof(PhotonTransformView))]
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(XRGeneralGrabTransformer))]

    public class XRGrabNetworkInteractable : MonoBehaviour
    {
        PhotonView photonView;
        Rigidbody rb;
        bool initialGravityStatus;
        bool initialKinematicStatus;
        XRGrabInteractable grabInteractable;
        public Vector3 initialPosition;
        public Vector3 initialRotation;
        public bool shouldReset;
        public UnityEvent onSelectEnterEvent;
        public UnityEvent onSelectExitEvent;
        private void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            rb = GetComponent<Rigidbody>();
            initialGravityStatus = rb.useGravity;
            initialKinematicStatus = rb.isKinematic;
            photonView = GetComponent<PhotonView>();
            grabInteractable.selectEntered.AddListener(selectEntered);
            grabInteractable.selectExited.AddListener(selectExited);
        }


        public XRGrabInteractable GetXRGrabInteractable()
        {
            return grabInteractable;
        }
        private void selectEntered(SelectEnterEventArgs arg0)
        {
            if (!photonView.IsMine)
            {
                photonView.RequestOwnership();
            }
            else
            {
                if (SetInitialTransformOverNetworkCoroutine != null)
                {
                    StopCoroutine(SetInitialTransformOverNetworkCoroutine);
                }
            }
            if (onSelectEnterEvent != null)
            {
                photonView.RPC(nameof(SelectEnterRPC), RpcTarget.AllBuffered);
            }
            photonView.RPC(nameof(HandleRigidBodyGravity), RpcTarget.AllBuffered, false, true, photonView.ControllerActorNr, "Enter");
        }

        private void selectExited(SelectExitEventArgs arg0)
        {
            if (photonView.IsMine)
            {
                if (onSelectExitEvent != null)
                {
                    photonView.RPC(nameof(SelectExitRPC), RpcTarget.AllBuffered);
                }
                photonView.RPC(nameof(HandleRigidBodyGravity), RpcTarget.AllBuffered, initialGravityStatus, initialKinematicStatus, photonView.ControllerActorNr, "Exit");
                if (shouldReset)
                {
                    SetInitialTransformOverNetworkCoroutine = StartCoroutine(IESetInitialTransformOverNetwork());
                }
            }
        }

        Coroutine SetInitialTransformOverNetworkCoroutine;
        IEnumerator IESetInitialTransformOverNetwork()
        {
            yield return new WaitForSeconds(10f);
            SetInitialTransformOverNetwork();
            SetInitialTransformOverNetworkCoroutine = null;
        }
        public void SetInitialTransformOverNetwork()
        {
            if (photonView.IsMine)
            {
                transform.localPosition = initialPosition;
                transform.rotation = Quaternion.Euler(initialRotation);
            }
        }

        [PunRPC]
        public void SelectEnterRPC()
        {
            onSelectEnterEvent?.Invoke();
        }
        [PunRPC]
        public void SelectExitRPC()
        {
            onSelectExitEvent?.Invoke();
        }
        [PunRPC]
        public void HandleRigidBodyGravity(bool gravity, bool kinematic, int actorNumber, string eventName)
        {
            if (eventName == "Enter" && grabInteractable.isSelected && actorNumber != photonView.ControllerActorNr)
            {
                DisableGrabbing();
            }
            rb.useGravity = gravity;
            rb.isKinematic = kinematic;
        }

        void EnableGrabbing()
        {
            grabInteractable.enabled = true;
        }

        void DisableGrabbing()
        {
            grabInteractable.enabled = false;
            Invoke(nameof(EnableGrabbing), .5f);
        }
        public void SetInitialPosition()
        {
            initialPosition = transform.localPosition;
        }

        public void SetInitialRotation()
        {
            initialRotation = transform.rotation.eulerAngles;
        }
    }
}