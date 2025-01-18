// ----------------------------------------------------------------------------
// <copyright file="FirstPersonController.cs" company="Exit Games GmbH">
// Photon Voice Demo for PUN- Copyright (C) 2016 Exit Games GmbH
// </copyright>
// <summary>
// Custom fist person character controller.
// </summary>
// <author>developer@photonengine.com</author>
// ----------------------------------------------------------------------------

namespace ExitGames.Demos.DemoPunVoice {

    using UnityEngine;

    public class FirstPersonController : BaseController {

        [SerializeField]
        private MouseLookHelper mouseLook = new MouseLookHelper();

        private float oldYRotation;
        private Quaternion velRotation;

        public Vector3 Velocity {
            get { return this.rigidBody.linearVelocity; }
        }

        protected override void SetCamera() {
            base.SetCamera();
            this.mouseLook.Init(this.transform, this.camTrans);
        }

        protected override void Move(float h, float v) {
            // always move along the camera forward as it is the direction that it being aimed at
            Vector3 desiredMove = this.camTrans.forward * v + this.camTrans.right * h;
            desiredMove.x = desiredMove.x * this.speed;
            desiredMove.z = desiredMove.z * this.speed;
            desiredMove.y = 0;
            this.rigidBody.linearVelocity = desiredMove;
        }

        private void Update() {
            this.RotateView();
        }

        private void RotateView() {
            // get the rotation before it's changed
            this.oldYRotation = this.transform.eulerAngles.y;
            this.mouseLook.LookRotation(this.transform, this.camTrans);
            // Rotate the rigidbody velocity to match the new direction that the character is looking
            this.velRotation = Quaternion.AngleAxis(this.transform.eulerAngles.y - this.oldYRotation, Vector3.up);
            this.rigidBody.linearVelocity = this.velRotation * this.rigidBody.linearVelocity;
        }
    }

}