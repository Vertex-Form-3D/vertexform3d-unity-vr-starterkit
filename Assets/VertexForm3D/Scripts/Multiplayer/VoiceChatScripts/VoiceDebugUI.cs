using UnityEngine;
using TMPro;
using Photon.Voice.Fusion;
using Photon.Realtime;

namespace VertexFormCore
{
    public class VoiceDebugUI : MonoBehaviour
    {

        [SerializeField]
        private TextMeshProUGUI voiceState;

        private FusionVoiceClient fusionVoiceClient;

        private void Awake()
        {
            // Find the FusionVoiceClient in the scene
            fusionVoiceClient = FindFirstObjectByType<FusionVoiceClient>();
            InvokeRepeating(nameof(CheckInstance), 1, 2);
        }

        private void OnEnable()
        {
            if (fusionVoiceClient != null && fusionVoiceClient.Client != null)
            {
                fusionVoiceClient.Client.StateChanged += this.VoiceClientStateChanged;
            }
        }

        private void OnDisable()
        {
            if (fusionVoiceClient != null && fusionVoiceClient.Client != null)
            {
                fusionVoiceClient.Client.StateChanged -= this.VoiceClientStateChanged;
            }
        }

        public void CheckInstance()
        {
            if (fusionVoiceClient == null)
            {
                fusionVoiceClient = FindFirstObjectByType<FusionVoiceClient>();
                if (fusionVoiceClient != null && fusionVoiceClient.Client != null)
                {
                    fusionVoiceClient.Client.StateChanged += this.VoiceClientStateChanged;
                }
            }
            else
            {
                CancelInvoke(nameof(CheckInstance));
            }
        }

        private void VoiceClientStateChanged(ClientState fromState, ClientState toState)
        {
            this.UpdateUiBasedOnVoiceState(toState);
        }

        private void UpdateUiBasedOnVoiceState(ClientState voiceClientState)
        {
            if (voiceState == null)
            {
                return;
            }
            this.voiceState.text = string.Format("FusionVoice: {0}", voiceClientState);
            if (voiceClientState == ClientState.Joined)
            {
                voiceState.gameObject.SetActive(false);
            }
        }
    }
}