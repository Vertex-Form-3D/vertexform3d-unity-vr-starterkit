using UnityEngine;

namespace ReadyPlayerMe.Samples.AvatarCreatorWizard
{
    public enum StateType
    {
        None,
        LoginWithCodeFromEmail,
        AvatarSelection,
        GenderSelection,
        SelfieSelection,
        CameraPhoto,
        DefaultAvatarSelection,
        Editor,
        End
    }

    public abstract class State : MonoBehaviour
    {
        protected StateMachine StateMachine;
        protected AvatarCreatorData AvatarCreatorData;
        protected LoadingManager LoadingManager;

        public abstract StateType StateType { get; }
        public abstract StateType NextState { get; }

        public abstract void ActivateState();
        public abstract void DeactivateState();

        public void Initialize(StateMachine stateMachine, AvatarCreatorData avatarCreatorData, LoadingManager loadingManager)
        {
            StateMachine = stateMachine;
            AvatarCreatorData = avatarCreatorData;
            LoadingManager = loadingManager;
            gameObject.SetActive(false);
        }
    }
}
