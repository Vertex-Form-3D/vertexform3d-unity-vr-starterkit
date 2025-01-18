// ----------------------------------------------------------------------------
// <copyright file="ThirdPersonController.cs" company="Exit Games GmbH">
// Photon Voice Demo for PUN- Copyright (C) 2016 Exit Games GmbH
// </copyright>
// <summary>
// Third person character controller class.
// </summary>
// <author>developer@photonengine.com</author>
// ----------------------------------------------------------------------------

namespace ExitGames.Demos.DemoPunVoice {

    using UnityEngine;

    public class ThirdPersonController : BaseController {

        [SerializeField]
        private float movingTurnSpeed = 360;

        protected override void Move(float h, float v) {
            this.rigidBody.linearVelocity = v * this.speed * this.transform.forward;
            this.transform.rotation *= Quaternion.AngleAxis(this.movingTurnSpeed * h * Time.deltaTime, Vector3.up);
        }
    }

}