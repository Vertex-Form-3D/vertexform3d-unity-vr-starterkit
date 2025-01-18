// ----------------------------------------------------------------------------
// <copyright file="ChangePOV.cs" company="Exit Games GmbH">
// Photon Voice Demo for PUN- Copyright (C) 2016 Exit Games GmbH
// </copyright>
// <summary>
// "Camera manager" class that handles the switch between the three different cameras.
// </summary>
// <author>developer@photonengine.com</author>
// ----------------------------------------------------------------------------

#pragma warning disable 0649 // Field is never assigned to, and will always have its default value

namespace ExitGames.Demos.DemoPunVoice {
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using Photon.Realtime;
    using Photon.Pun;

    public class ChangePOV : MonoBehaviour, IMatchmakingCallbacks {
        private FirstPersonController firstPersonController;
        private ThirdPersonController thirdPersonController;
        private OrthographicController orthographicController;

        private Vector3 initialCameraPosition;
        private Quaternion initialCameraRotation;
        private Camera defaultCamera;

        [SerializeField]
        private GameObject ButtonsHolder;

        [SerializeField]
        private Button FirstPersonCamActivator;

        [SerializeField]
        private Button ThirdPersonCamActivator;

        [SerializeField]
        private Button OrthographicCamActivator;

        public delegate void OnCameraChanged(Camera newCamera);

        public static event OnCameraChanged CameraChanged;

        private void OnEnable() {
            CharacterInstantiation.CharacterInstantiated += this.OnCharacterInstantiated;
            PhotonNetwork.AddCallbackTarget(this);
        }

        private void OnDisable() {
            CharacterInstantiation.CharacterInstantiated -= this.OnCharacterInstantiated;
            PhotonNetwork.RemoveCallbackTarget(this);
        }


        private void Start() {
            this.defaultCamera = Camera.main;
            this.initialCameraPosition = new Vector3(this.defaultCamera.transform.position.x,
                this.defaultCamera.transform.position.y, this.defaultCamera.transform.position.z);
            this.initialCameraRotation = new Quaternion(this.defaultCamera.transform.rotation.x,
                this.defaultCamera.transform.rotation.y, this.defaultCamera.transform.rotation.z,
                this.defaultCamera.transform.rotation.w);
            //Check if we are running either in the Unity editor or in a standalone build.
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBPLAYER
            this.FirstPersonCamActivator.onClick.AddListener(this.FirstPersonMode);
#else
            this.FirstPersonCamActivator.gameObject.SetActive(false);
#endif
            this.ThirdPersonCamActivator.onClick.AddListener(this.ThirdPersonMode);
            this.OrthographicCamActivator.onClick.AddListener(this.OrthographicMode);
        }

        private void OnCharacterInstantiated(GameObject character)
        {
            this.firstPersonController = character.GetComponent<FirstPersonController>();
            this.firstPersonController.enabled = false;
            this.thirdPersonController = character.GetComponent<ThirdPersonController>();
            this.thirdPersonController.enabled = false;
            this.orthographicController = character.GetComponent<OrthographicController>();
            this.ButtonsHolder.SetActive(true);
        }

        private void FirstPersonMode() {
            this.ToggleMode(this.firstPersonController);
        }

        private void ThirdPersonMode() {
            this.ToggleMode(this.thirdPersonController);
        }

        private void OrthographicMode() {
            this.ToggleMode(this.orthographicController);
        }

        private void ToggleMode(BaseController controller) {
            if (controller == null) { return; } // this should not happen, throw error
            if (controller.ControllerCamera == null) { return; } // probably game is closing 
            controller.ControllerCamera.gameObject.SetActive(true);
            controller.enabled = true;
            this.FirstPersonCamActivator.interactable = !(controller == this.firstPersonController);
            this.ThirdPersonCamActivator.interactable = !(controller == this.thirdPersonController);
            this.OrthographicCamActivator.interactable = !(controller == this.orthographicController);
            this.BroadcastChange(controller.ControllerCamera); // BroadcastChange(Camera.main);
        }

        private void BroadcastChange(Camera camera) {
            if (camera == null) { return; } // should not happen, throw error
            if (CameraChanged != null) {
                CameraChanged(camera);
            }
        }

        #region IMatchmakingCallbacks interface methods

        public void OnFriendListUpdate(List<FriendInfo> friendList)
        {
            
        }

        public void OnCreatedRoom()
        {
            
        }

        public void OnCreateRoomFailed(short returnCode, string message)
        {
            
        }

        public void OnJoinedRoom()
        {
            
        }

        public void OnJoinRoomFailed(short returnCode, string message)
        {
            
        }

        public void OnJoinRandomFailed(short returnCode, string message)
        {
            
        }

        public void OnLeftRoom()
        {
            if (ConnectionHandler.AppQuits)
            {
                return;
            }
            this.defaultCamera.gameObject.SetActive(true);
            this.FirstPersonCamActivator.interactable = true;
            this.ThirdPersonCamActivator.interactable = true;
            this.OrthographicCamActivator.interactable = false;
            this.defaultCamera.transform.position = this.initialCameraPosition;
            this.defaultCamera.transform.rotation = this.initialCameraRotation;
            this.ButtonsHolder.SetActive(false);
        }

        #endregion
    }
}