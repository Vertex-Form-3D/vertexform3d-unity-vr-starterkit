using System;
using ReadyPlayerMe.AvatarCreator;
using ReadyPlayerMe.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ReadyPlayerMe.Samples.AvatarCreatorWizard
{
    public class ReloadPreviouslyEditedAvatar : MonoBehaviour
    {
        private const string TAG = nameof(ReloadPreviouslyEditedAvatar);

        [SerializeField] private Button continueEditingAvatar;
        [SerializeField] private Button cancelEditingAvatar;

        private string avatarId;
        public UnityEvent<string> OnContinueEditingAvatar;
        public UnityEvent OnCancel;
        
        public async void Show(string avatarId)
        {
            this.avatarId = avatarId;
            try
            {
                await new AvatarAPIRequests().GetAvatar(avatarId, true);
                gameObject.SetActive(true);
            }
            catch (Exception e)
            {
                OnCancelEditing();
                SDKLogger.Log(TAG, $"Failed to load draft avatar with id ${avatarId}. {e.Message}");
            }
        }

        private void OnEnable()
        {
            continueEditingAvatar.onClick.AddListener(OnContinueEditing);
            cancelEditingAvatar.onClick.AddListener(OnCancelEditing);
        }

        private void OnDisable()
        {
            continueEditingAvatar.onClick.RemoveListener(OnContinueEditing);
            cancelEditingAvatar.onClick.RemoveListener(OnCancelEditing);
        }

        private void OnCancelEditing()
        {
            OnCancel?.Invoke();
            ClearAvatarId();
        }
        
        private void OnContinueEditing()
        {
            OnContinueEditingAvatar?.Invoke(avatarId);
            ClearAvatarId();
        }

        private void ClearAvatarId()
        {
            AuthManager.StoreLastModifiedAvatar(null);
            avatarId = null;
        }
    }
}
