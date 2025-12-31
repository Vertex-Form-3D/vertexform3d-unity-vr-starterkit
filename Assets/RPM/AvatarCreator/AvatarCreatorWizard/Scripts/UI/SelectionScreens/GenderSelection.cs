using ReadyPlayerMe.AvatarCreator;
using ReadyPlayerMe.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ReadyPlayerMe.Samples.AvatarCreatorWizard
{
    public class GenderSelection : State
    {
        [SerializeField] private Button male;
        [SerializeField] private Button female;
        [SerializeField] private Button dontSpecify;
        [SerializeField] private GameObject signInPanel;
        [SerializeField] private Button signInButton;

        public override StateType StateType => StateType.GenderSelection;
        public override StateType NextState => StateType.DefaultAvatarSelection;

        public override void ActivateState()
        {
            male.onClick.AddListener(OnMaleSelected);
            female.onClick.AddListener(OnFemaleSelected);
            dontSpecify.onClick.AddListener(OnGenderNotSpecifiedSelected);
            signInButton.onClick.AddListener(OnSignButton);

            if (AuthManager.IsSignedIn)
            {
                signInPanel.SetActive(false);
            }
        }

        public override void DeactivateState()
        {
            male.onClick.RemoveListener(OnMaleSelected);
            female.onClick.RemoveListener(OnFemaleSelected);
            dontSpecify.onClick.RemoveListener(OnGenderNotSpecifiedSelected);
            signInButton.onClick.RemoveListener(OnSignButton);
        }

        private void OnMaleSelected()
        {
            SetNextState(OutfitGender.Masculine);
        }

        private void OnFemaleSelected()
        {
            SetNextState(OutfitGender.Feminine);
        }

        private void OnGenderNotSpecifiedSelected()
        {
            SetNextState(OutfitGender.None);
        }

        private void SetNextState(OutfitGender gender)
        {
            AvatarCreatorData.AvatarProperties.Gender = gender;
            StateMachine.SetState(NextState);
        }


        private void OnSignButton()
        {
            StateMachine.SetState(StateType.LoginWithCodeFromEmail);
        }
    }
}
