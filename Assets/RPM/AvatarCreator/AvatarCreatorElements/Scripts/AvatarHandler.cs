using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ReadyPlayerMe.AvatarCreator;
using ReadyPlayerMe.Core;
using UnityEngine;
using UnityEngine.Events;
using TaskExtensions = ReadyPlayerMe.AvatarCreator.TaskExtensions;

namespace ReadyPlayerMe.Samples.AvatarCreatorElements
{
    public class AvatarHandler : MonoBehaviour
    {
        [SerializeField] private RuntimeAnimatorController animationController;

        public UnityEvent<AvatarProperties> OnAvatarLoaded;
        public UnityEvent OnAvatarLoading;

        private AvatarManager avatarManager;
        private readonly CancellationTokenSource cancellationTokenSource = new();
        private GameObject avatar;
        public AvatarProperties ActiveAvatarProperties { get; private set; }

        private void Awake()
        {
            avatarManager = new AvatarManager(token: cancellationTokenSource.Token);
        }

        public async Task LoadAvatar(string avatarId)
        {
            await LoadAvatarWithId(avatarId);
        }

        public async Task SelectAsset(IAssetData assetData)
        {
            OnAvatarLoading?.Invoke();
            GameObject newAvatar;
            if (assetData.AssetType == AssetType.Headwear && ActiveAvatarProperties.Assets.ContainsKey(AssetType.HairStyle))
            {
                var assets = new Dictionary<AssetType, object>();
                assets.Add(AssetType.HairStyle, ActiveAvatarProperties.Assets[AssetType.HairStyle]);
                assets.Add(AssetType.Headwear, assetData.Id);
                newAvatar = await avatarManager.UpdateAssets(assets);
            }
            else
            {
                newAvatar = await avatarManager.UpdateAsset(assetData.AssetType, assetData.Id);
            }
            ActiveAvatarProperties.Assets[assetData.AssetType] = assetData.Id;
            SetupLoadedAvatar(newAvatar, ActiveAvatarProperties);
        }

        public async Task SaveActiveAvatar()
        {
            await avatarManager.Save();
            AuthManager.StoreLastModifiedAvatar(avatarManager.AvatarId);
        }

        public async Task CreateNewAvatarFromTemplate()
        {
            await CreateTemplateAvatar();
        }

        public async void LoadPreviousOrCreateNewAvatar()
        {
            var session = AuthManager.UserSession;
            await TaskExtensions.HandleCancellation(string.IsNullOrEmpty(session.LastModifiedAvatarId) ? CreateTemplateAvatar() : LoadAvatarWithId(session.LastModifiedAvatarId));
        }

        private async Task<AvatarProperties> LoadAvatarWithId(string avatarId)
        {
            OnAvatarLoading?.Invoke();
            var newAvatar = await avatarManager.GetAvatar(avatarId);
            var newAvatarProperties = await avatarManager.GetAvatarProperties(avatarId);

            SetupLoadedAvatar(newAvatar, newAvatarProperties);
            AuthManager.StoreLastModifiedAvatar(avatarId);

            return newAvatarProperties;

        }

        /// <summary>
        ///     Creates an avatar from a template and sets its initial properties.
        /// </summary>
        /// <returns>The properties of the created avatar.</returns>
        private async Task<AvatarProperties> CreateTemplateAvatar()
        {
            OnAvatarLoading?.Invoke();
            var avatarTemplateFetcher = new AvatarTemplateFetcher(cancellationTokenSource.Token);
            var templates = await avatarTemplateFetcher.GetTemplates();
            var avatarTemplate = templates[1];
            return await LoadAvatarFromTemplate(avatarTemplate.Id);
        }

        public async Task<AvatarProperties> LoadAvatarFromTemplate(string templateId)
        {
            OnAvatarLoading?.Invoke();
            var templateAvatarResponse = await avatarManager.CreateAvatarFromTemplateAsync(templateId);

            SetupLoadedAvatar(templateAvatarResponse.AvatarObject, templateAvatarResponse.Properties);

            return templateAvatarResponse.Properties;
        }

        private void SetupLoadedAvatar(GameObject newAvatar, AvatarProperties newAvatarProperties)
        {
            if (avatar != null)
            {
                Destroy(avatar);
            }

            avatar = newAvatar;
            ActiveAvatarProperties = newAvatarProperties;

            SetupAvatar(newAvatarProperties);
            OnAvatarLoaded?.Invoke(newAvatarProperties);
        }

        /// <summary>
        ///     Sets additional elements and components on the created avatar, such as mouse rotation and animation controller.
        /// </summary>
        private void SetupAvatar(AvatarProperties newAvatarProperties)
        {
            //avatar.AddComponent<MouseRotationHandler>();
            // avatar.AddComponent<AvatarRotator>();
            Vector3 pos = new(-0.55f, 0, 0);    // optional
            Quaternion rotation = Quaternion.Euler(0, 31, 0);
            avatar.transform.SetLocalPositionAndRotation(pos, rotation);
            var animator = avatar.GetComponent<Animator>();
            AvatarAnimationHelper.SetupAnimator(new AvatarMetadata
                { BodyType = newAvatarProperties.BodyType, OutfitGender = newAvatarProperties.Gender }, animator);
            animator.runtimeAnimatorController = animationController;
        }

        private void OnDestroy()
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
        }

    }
}
