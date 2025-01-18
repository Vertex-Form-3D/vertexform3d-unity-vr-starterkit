namespace Photon.Voice.Unity.Demos.DemoVoiceUI
{
    using ExitGames.Client.Photon;
    using Unity;
    using UnityEngine;
    using UnityEngine.UI;
    using Realtime;

    [RequireComponent(typeof(Speaker))]
    public class RemoteSpeakerUI : MonoBehaviour, IInRoomCallbacks
    {
        #pragma warning disable 649
        [SerializeField]
        private Text nameText;
        [SerializeField]
        protected Image remoteIsMuting;
        [SerializeField]
        private Image remoteIsTalking;
        [SerializeField]
        private InputField minDelaySoftInputField;
        [SerializeField]
        private InputField maxDelaySoftInputField;
        [SerializeField]
        private InputField maxDelayHardInputField;
        [SerializeField]
        private Text bufferLagText;
        #pragma warning restore 649
        protected Speaker speaker;

        protected VoiceConnection voiceConnection;
        protected LoadBalancingClient loadBalancingClient;

        protected virtual void Start()
        {
            this.speaker = this.GetComponent<Speaker>();
            this.minDelaySoftInputField.text = this.speaker.PlaybackDelayMinSoft.ToString();
            this.minDelaySoftInputField.SetSingleOnEndEditCallback(this.OnMinDelaySoftChanged);
            this.maxDelaySoftInputField.text = this.speaker.PlaybackDelayMaxSoft.ToString();
            this.maxDelaySoftInputField.SetSingleOnEndEditCallback(this.OnMaxDelaySoftChanged);
            this.maxDelayHardInputField.text = this.speaker.PlaybackDelayMaxHard.ToString();
            this.maxDelayHardInputField.SetSingleOnEndEditCallback(this.OnMaxDelayHardChanged);
            this.SetNickname();
            this.SetMutedState();
        }

        private void OnMinDelaySoftChanged(string newMinDelaySoftString)
        {
            int newMinDelaySoftValue;
            int newMaxDelaySoftValue = this.speaker.PlaybackDelayMaxSoft;
            int newMaxDelayHardValue = this.speaker.PlaybackDelayMaxHard;
            if (int.TryParse(newMinDelaySoftString, out newMinDelaySoftValue) && newMinDelaySoftValue >= 0 && newMinDelaySoftValue < newMaxDelaySoftValue)
            {
                this.speaker.SetPlaybackDelaySettings(newMinDelaySoftValue, newMaxDelaySoftValue, newMaxDelayHardValue);
            }
            else
            {
                this.minDelaySoftInputField.text = this.speaker.PlaybackDelayMinSoft.ToString();
            }
        }

        private void OnMaxDelaySoftChanged(string newMaxDelaySoftString)
        {
            int newMinDelaySoftValue = this.speaker.PlaybackDelayMinSoft;
            int newMaxDelaySoftValue;
            int newMaxDelayHardValue = this.speaker.PlaybackDelayMaxHard;
            if (int.TryParse(newMaxDelaySoftString, out newMaxDelaySoftValue) && newMinDelaySoftValue < newMaxDelaySoftValue)
            {
                this.speaker.SetPlaybackDelaySettings(newMinDelaySoftValue, newMaxDelaySoftValue, newMaxDelayHardValue);
            }
            else
            {
                this.maxDelaySoftInputField.text = this.speaker.PlaybackDelayMaxSoft.ToString();
            }
        }

        private void OnMaxDelayHardChanged(string newMaxDelayHardString)
        {
            int newMinDelaySoftValue = this.speaker.PlaybackDelayMinSoft;
            int newMaxDelaySoftValue = this.speaker.PlaybackDelayMaxSoft;
            int newMaxDelayHardValue;
            if (int.TryParse(newMaxDelayHardString, out newMaxDelayHardValue) && newMaxDelayHardValue >= newMaxDelaySoftValue)
            {
                this.speaker.SetPlaybackDelaySettings(newMinDelaySoftValue, newMaxDelaySoftValue, newMaxDelayHardValue);
            }
            else
            {
                this.maxDelayHardInputField.text = this.speaker.PlaybackDelayMaxHard.ToString();
            }
        }

        private void Update()
        {
            // TODO: It would be nice, if we could show if a user is actually talking right now (Voice Detection)
            this.remoteIsTalking.enabled = this.speaker.IsPlaying;
            this.bufferLagText.text = string.Concat("Buffer Lag: ", this.speaker.Lag);
        }

        private void OnDestroy()
        {
            if (this.loadBalancingClient != null)
            {
                this.loadBalancingClient.RemoveCallbackTarget(this);
            }
        }

        private void SetNickname()
        {
            string nick = this.speaker.name;
            if (this.speaker.Actor != null)
            {
                nick = this.speaker.Actor.NickName;
                if (string.IsNullOrEmpty(nick))
                {
                    nick = string.Concat("user ", this.speaker.Actor.ActorNumber);
                }
            }
            this.nameText.text = nick;
        }

        private void SetMutedState()
        {
            this.SetMutedState(this.speaker.Actor.IsMuted());
        }

        protected virtual void SetMutedState(bool isMuted)
        {
            this.remoteIsMuting.enabled = isMuted;
        }

        protected virtual void OnActorPropertiesChanged(Player targetPlayer, Hashtable changedProps)
        {
            if (targetPlayer.ActorNumber == this.speaker.Actor.ActorNumber)
            {
                this.SetMutedState();
                this.SetNickname();
            }
        }

        public virtual void Init(VoiceConnection vC)
        {
            this.voiceConnection = vC;
            this.loadBalancingClient = this.voiceConnection.Client;
            this.loadBalancingClient.AddCallbackTarget(this);
        }

        #region IInRoomCallbacks

        void IInRoomCallbacks.OnPlayerEnteredRoom(Player newPlayer)
        {
        }

        void IInRoomCallbacks.OnPlayerLeftRoom(Player otherPlayer)
        {
        }

        void IInRoomCallbacks.OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
        }

        void IInRoomCallbacks.OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            this.OnActorPropertiesChanged(targetPlayer, changedProps);
        }

        void IInRoomCallbacks.OnMasterClientSwitched(Player newMasterClient)
        {
        }

        #endregion
    }
}