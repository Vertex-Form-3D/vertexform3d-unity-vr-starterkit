// ----------------------------------------------------------------------------
// <copyright file="OrthographicController.cs" company="Exit Games GmbH">
// Photon Voice Demo for PUN- Copyright (C) 2016 Exit Games GmbH
// </copyright>
// <summary>
// Character controller class for Orthographic camera mode.
// </summary>
// <author>developer@photonengine.com</author>
// ----------------------------------------------------------------------------

namespace ExitGames.Demos.DemoPunVoice {

    using UnityEngine;
    
    public class OrthographicController : ThirdPersonController {

        public float smoothing = 5f;        // The speed with which the camera will be following.
        private Vector3 offset;

        protected override void Init() {
            base.Init();
            // should be default camera
            this.ControllerCamera = Camera.main;//GameObject.Find("OrthographicCamera").GetComponent<Camera>();
        }

        protected override void SetCamera() {
            base.SetCamera();
            // Calculate the initial offset.
            this.offset = this.camTrans.position - this.transform.position;
        }

        protected override void Move(float h, float v) {
            base.Move(h, v);
            this.CameraFollow();
        }


        private void CameraFollow() {
            // Create a postion the camera is aiming for based on the offset from the target.
            Vector3 targetCamPos = this.transform.position + this.offset;

            // Smoothly interpolate between the camera's current position and it's target position.
            this.camTrans.position = Vector3.Lerp(this.camTrans.position, targetCamPos, this.smoothing * Time.deltaTime);
        }
    }

}