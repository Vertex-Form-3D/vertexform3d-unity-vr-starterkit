using UnityEngine;
using TMPro;
using Photon.Voice.PUN;

namespace VertextFormCore
{
    public class VoiceDebugUI : MonoBehaviour
    {

        [SerializeField]
        private TextMeshProUGUI voiceState;

        private PhotonVoiceNetwork punVoiceNetwork;

        private void Awake()
        {
            this.punVoiceNetwork = PhotonVoiceNetwork.Instance;
            InvokeRepeating(nameof(CheckInstance), 1, 2);
        }

        private void OnEnable()
        {
            this.punVoiceNetwork.Client.StateChanged += this.VoiceClientStateChanged;
        }

        private void OnDisable()
        {
            this.punVoiceNetwork.Client.StateChanged -= this.VoiceClientStateChanged;
        }

        public void CheckInstance()
        {
            if (this.punVoiceNetwork == null)
            {
                this.punVoiceNetwork = PhotonVoiceNetwork.Instance;
            }
            else
            {
                CancelInvoke(nameof(CheckInstance));
            }
        }


        private void VoiceClientStateChanged(Photon.Realtime.ClientState fromState, Photon.Realtime.ClientState toState)
        {
            this.UpdateUiBasedOnVoiceState(toState);
        }

        private void UpdateUiBasedOnVoiceState(Photon.Realtime.ClientState voiceClientState)
        {
            if (voiceState==null)
            {
                return;
            }
            this.voiceState.text = string.Format("PhotonVoice: {0}", voiceClientState);
            if (voiceClientState == Photon.Realtime.ClientState.Joined)
            {
                voiceState.gameObject.SetActive(false);
            }
        }
    }
}