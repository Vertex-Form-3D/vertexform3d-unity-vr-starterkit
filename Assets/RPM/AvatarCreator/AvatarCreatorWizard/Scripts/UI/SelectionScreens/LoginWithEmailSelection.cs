using TMPro; 
using UnityEngine;
using UnityEngine.UI;
using ReadyPlayerMe.Core;
using ReadyPlayerMe.AvatarCreator;

namespace ReadyPlayerMe.Samples.AvatarCreatorWizard
{
    public class LoginWithEmailSelection : State
    {
        private const string TAG = nameof(LoginWithEmailSelection);
        private enum LoginWithEmailState
        {
            EnterEmail,
            TransferAssetsPrompt,
            EnterCode
        }

        [SerializeField] private Button sendActivationCodeButton;
        [SerializeField] private Button haveCodeButton;
        [SerializeField] private Button changeEmailButton;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button transferAssets;
        [SerializeField] private Button dontTransferAssets;

        [SerializeField] private TMP_InputField emailField;
        [SerializeField] private TMP_InputField codeField;

        [SerializeField] private GameObject emailPanel;
        [SerializeField] private GameObject codePanel;

        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject transferAssetsPanel;

        public override StateType StateType => StateType.LoginWithCodeFromEmail;
        public override StateType NextState => StateType.GenderSelection;

        private bool transferAvatarsAndAssets;
        public override void ActivateState()
        {
            sendActivationCodeButton.onClick.AddListener(OnSendActivationCode);
            haveCodeButton.onClick.AddListener(OnHaveCodeButton);
            changeEmailButton.onClick.AddListener(OnChangeEmail);
            transferAssets.onClick.AddListener(OnTransferAssets);
            dontTransferAssets.onClick.AddListener(OnDontTransferAssets);
            loginButton.onClick.AddListener(OnLogin);
            SetState(LoginWithEmailState.EnterEmail);
        }


        public override void DeactivateState()
        {
            sendActivationCodeButton.onClick.RemoveListener(OnSendActivationCode);
            haveCodeButton.onClick.RemoveListener(OnHaveCodeButton);
            changeEmailButton.onClick.RemoveListener(OnChangeEmail);
            transferAssets.onClick.RemoveListener(OnTransferAssets);
            dontTransferAssets.onClick.RemoveListener(OnDontTransferAssets);
            loginButton.onClick.RemoveListener(OnLogin);
        }

        private void OnDontTransferAssets()
        {
            transferAvatarsAndAssets = false;
            SetState(LoginWithEmailState.EnterCode);
        }

        private void OnTransferAssets()
        {
            transferAvatarsAndAssets = true;
            SetState(LoginWithEmailState.EnterCode);
        }

        private void OnSendActivationCode()
        {
            AuthManager.SendEmailCode(emailField.text);
            SetState(AuthManager.IsSignedInAnonymously ? LoginWithEmailState.TransferAssetsPrompt : LoginWithEmailState.EnterCode);

        }

        private void SetState(LoginWithEmailState loginWithEmailState)
        {
            switch (loginWithEmailState)
            {
                case LoginWithEmailState.EnterEmail:
                    transferAvatarsAndAssets = false;
                    emailPanel.SetActive(true);
                    codePanel.SetActive(false);
                    loginPanel.SetActive(true);
                    transferAssetsPanel.SetActive(false);
                    break;
                case LoginWithEmailState.EnterCode:
                    emailPanel.SetActive(false);
                    codePanel.SetActive(true);
                    loginPanel.SetActive(true);
                    transferAssetsPanel.SetActive(false);
                    break;
                case LoginWithEmailState.TransferAssetsPrompt:
                    loginPanel.SetActive(false);
                    transferAssetsPanel.SetActive(true);
                    break;
            }
        }

        private void OnHaveCodeButton()
        {
            SetState(LoginWithEmailState.TransferAssetsPrompt);
        }

        private void OnChangeEmail()
        {
            SetState(LoginWithEmailState.EnterEmail);
        }

        private async void OnLogin()
        {
            LoadingManager.EnableLoading("Signing In");

            AuthManager.OnSignInError += OnSignInError;

            var loginWithCode = transferAvatarsAndAssets ? AuthManager.LoginWithCode(codeField.text, AuthManager.UserSession.Id) : AuthManager.LoginWithCode(codeField.text);
            if (!await loginWithCode)
            {
                return;
            }

            OnChangeEmail();
            LoadingManager.DisableLoading();
            StateMachine.SetState(StateType.AvatarSelection);
            SDKLogger.Log(TAG, "Login successful");
        }

        private void OnSignInError(string error)
        {
            AuthManager.OnSignInError -= OnSignInError;
            LoadingManager.EnableLoading(error, LoadingManager.LoadingType.Popup, false);
            SDKLogger.Log(TAG, $"Login failed with error: {error}");
        }
    }
}
