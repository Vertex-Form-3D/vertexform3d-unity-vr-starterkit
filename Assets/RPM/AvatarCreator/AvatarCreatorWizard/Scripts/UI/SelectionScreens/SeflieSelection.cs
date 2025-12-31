using UnityEngine;
using UnityEngine.UI;

namespace ReadyPlayerMe.Samples.AvatarCreatorWizard
{
    public class SeflieSelection : State
    {
        private const string TERMS_URL = "https://readyplayer.me/terms";
        private const string PRIVACY_URL = "https://readyplayer.me/privacy";

        [SerializeField] private Button photoButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button termsButton;
        [SerializeField] private Button privacyButton;

        public override StateType StateType => StateType.SelfieSelection;
        public override StateType NextState => StateType.DefaultAvatarSelection;

        public override void ActivateState()
        {
            photoButton.onClick.AddListener(OnPhotoButton);
            continueButton.onClick.AddListener(OnContinueButton);
            termsButton.onClick.AddListener(OnTermsButton);
            privacyButton.onClick.AddListener(OnPrivacyButton);
        }
        
        public override void DeactivateState()
        {
            photoButton.onClick.RemoveListener(OnPhotoButton);
            continueButton.onClick.RemoveListener(OnContinueButton);
            termsButton.onClick.RemoveListener(OnTermsButton);
            privacyButton.onClick.RemoveListener(OnPrivacyButton);
        }
        
        private void OnPhotoButton()
        {
            StateMachine.SetState(StateType.CameraPhoto);
        }

        private void OnContinueButton()
        {
            StateMachine.SetState(StateType.DefaultAvatarSelection);
        }

        private void OnTermsButton()
        {
            Application.OpenURL(TERMS_URL);
        }

        private void OnPrivacyButton()
        {
            Application.OpenURL(PRIVACY_URL);
        }
    }
}
