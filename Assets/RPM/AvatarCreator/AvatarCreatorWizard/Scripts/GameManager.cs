using ReadyPlayerMe.AvatarCreator;
using ReadyPlayerMe.Core;
using UnityEngine;

namespace ReadyPlayerMe.Samples.AvatarCreatorWizard
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private AvatarCreatorStateMachine avatarCreatorStateMachine;
        [SerializeField] private AvatarConfig inGameConfig;

        private AvatarObjectLoader avatarObjectLoader;

        private void OnEnable()
        {
            avatarCreatorStateMachine.AvatarSaved += OnAvatarSaved;
        }

        private void OnDisable()
        {
            avatarCreatorStateMachine.AvatarSaved -= OnAvatarSaved;
            avatarObjectLoader?.Cancel();
        }

        private void OnAvatarSaved(string avatarId)
        {
            avatarCreatorStateMachine.gameObject.SetActive(false);

            var startTime = Time.time;
            avatarObjectLoader = new AvatarObjectLoader();
            avatarObjectLoader.AvatarConfig = inGameConfig;
            avatarObjectLoader.OnCompleted += (sender, args) =>
            {
                AvatarAnimationHelper.SetupAnimator(args.Metadata, args.Avatar);
                DebugPanel.AddLogWithDuration("Created avatar loaded", Time.time - startTime);
            };

            avatarObjectLoader.LoadAvatar($"{Env.RPM_MODELS_BASE_URL}/{avatarId}.glb");
        }
    }
}
