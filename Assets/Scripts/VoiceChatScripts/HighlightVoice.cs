using UnityEngine;
using UnityEngine.UI;
using Photon.Voice.PUN;
using Photon.Pun;
using DG.Tweening;

namespace VertextFormCore
{
    public class HighlightVoice : MonoBehaviour
    {
        [SerializeField]
        private Image micImage;

        [SerializeField]
        private Image speakerImage;

        [SerializeField]
        private PhotonVoiceView photonVoiceView;


        private void Awake()
        {
            this.micImage.enabled = false;
            this.speakerImage.enabled = false;
            InvokeRepeating(nameof(CheckVoice), .2f, .2f);
        }

        void CheckVoice()
        {
            this.micImage.enabled = this.photonVoiceView.IsRecording;
            this.speakerImage.enabled = this.photonVoiceView.IsSpeaking;
            if (!photonVoiceView.GetComponent<PhotonView>().IsMine)
            {
                if (SpawnManager.Instance.localVRPlayer != null)
                {
                    transform.LookAt(SpawnManager.Instance.localVRPlayer.GetComponent<PlayerNetworkSetup>().cam.transform);
                }
            }
        }
    }
}