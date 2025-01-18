// ----------------------------------------------------------------------------
// <copyright file="SoundsForJoinAndLeave.cs" company="Exit Games GmbH">
// Photon Voice Demo for PUN- Copyright (C) 2016 Exit Games GmbH
// </copyright>
// <summary>
// Script to play sound when player joins or leaves room.
// </summary>
// <author>developer@photonengine.com</author>
// ----------------------------------------------------------------------------

namespace ExitGames.Demos.DemoPunVoice
{
    using Photon.Pun;
    using UnityEngine;
    using Player = Photon.Realtime.Player;

    public class SoundsForJoinAndLeave : MonoBehaviourPunCallbacks
    {
        public AudioClip JoinClip;
        public AudioClip LeaveClip;
        private AudioSource source;

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            if (this.JoinClip != null)
            {
                if (this.source == null) this.source = FindObjectOfType<AudioSource>();
                this.source.PlayOneShot(this.JoinClip);
            }
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            if (this.LeaveClip != null)
            {
                if (this.source == null) this.source = FindObjectOfType<AudioSource>();
                this.source.PlayOneShot(this.LeaveClip);
            }
        }
    }
}