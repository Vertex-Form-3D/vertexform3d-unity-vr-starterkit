using System;
using UnityEngine;
using UnityEngine.UI;

namespace ReadyPlayerMe.Samples.AvatarCreatorWizard
{
    public class ProfileUI : MonoBehaviour
    {
        [SerializeField] private Text username;
        [SerializeField] private GameObject userPanel;
        [SerializeField] private Button profileButton;
        [SerializeField] private Text profileText;
        [SerializeField] private Button signOutButton;

        public Action SignedOut;

        private void OnEnable()
        {
            profileButton.onClick.AddListener(ToggleProfilePanel);
            signOutButton.onClick.AddListener(OnSignOutButton);
        }

        private void OnDisable()
        {
            profileButton.onClick.RemoveListener(ToggleProfilePanel);
            signOutButton.onClick.RemoveListener(OnSignOutButton);
        }

        public void SetProfileData(string user, string profileButtonText)
        {
            username.text = user;
            profileText.text = profileButtonText;
            profileButton.gameObject.SetActive(true);
        }
        
        public void ClearProfile()
        {
            username.text = string.Empty;
            profileText.text = string.Empty;
            profileButton.gameObject.SetActive(false);
        }

        private void OnSignOutButton()
        {
            ToggleProfilePanel();
            SignedOut?.Invoke();
        }

        private void ToggleProfilePanel()
        {
            userPanel.SetActive(!userPanel.activeSelf);
        }
    }
}
