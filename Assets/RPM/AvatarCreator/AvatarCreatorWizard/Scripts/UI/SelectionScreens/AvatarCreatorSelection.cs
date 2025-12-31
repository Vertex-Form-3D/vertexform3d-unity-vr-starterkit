using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ReadyPlayerMe.AvatarCreator;
using ReadyPlayerMe.Core;
using UnityEngine;
using UnityEngine.UI;
using TaskExtensions = ReadyPlayerMe.AvatarCreator.TaskExtensions;

namespace ReadyPlayerMe.Samples.AvatarCreatorWizard
{
    public class AvatarCreatorSelection : State, IDisposable
    {
        private const string TAG = nameof(AvatarCreatorSelection);
        private const string UPDATING_YOUR_AVATAR_LOADING_TEXT = "Updating your avatar";
        private const int NUMBER_OF_ASSETS_TO_PRECOMPILE = 20;

        [SerializeField] private GameObject changeCanvas;
        [SerializeField] private GameObject ui;
        [SerializeField] private GameObject mirror;
        [SerializeField] private LoadRPMAvatar loadRPMAvatar;
        [SerializeField] private CategoryUICreator categoryUICreator;
        [SerializeField] private AssetButtonCreator assetButtonCreator;
        [SerializeField] private Button saveButton;
        [SerializeField] private AvatarConfig inCreatorConfig;
        [SerializeField] private RuntimeAnimatorController animator;
        [SerializeField] private SignupElement signupElement;
        private PartnerAssetsManager partnerAssetManager;
        private AvatarManager avatarManager;
        public Transform avatarParent;
        public GameObject currentAvatar;
        private Quaternion lastRotation;

        private CancellationTokenSource ctxSource = new();
        public List<AssetType> categoriesAssetsLoaded;

        public override StateType StateType => StateType.Editor;
        public override StateType NextState => StateType.End;

        private void Start()
        {
            partnerAssetManager = new PartnerAssetsManager();
        }

        public override void ActivateState()
        {
            saveButton.onClick.AddListener(OnSaveButton);
            signupElement.OnContinueWithoutSignup.AddListener(Save);
            signupElement.OnSendEmail.AddListener(OnSendEmail);
            categoryUICreator.OnCategorySelected += OnCategorySelected;
            Setup();
        }

        public override void DeactivateState()
        {
            saveButton.onClick.RemoveListener(OnSaveButton);
            signupElement.OnContinueWithoutSignup.RemoveListener(Save);
            signupElement.OnSendEmail.RemoveListener(OnSendEmail);
            categoryUICreator.OnCategorySelected -= OnCategorySelected;
            Cleanup();
        }

        private async void Setup()
        {
            LoadingManager.EnableLoading();

            avatarManager = new AvatarManager(
                inCreatorConfig,
                ctxSource.Token,
                AvatarCreatorData.AvatarProperties.Gender);
            avatarManager.OnError += OnErrorCallback;

            currentAvatar = await LoadAvatar();

            if (string.IsNullOrEmpty(avatarManager.AvatarId))
                return;

            CreateUI();

            if (avatarParent != null && currentAvatar != null)
            {
                currentAvatar.transform.SetParent(avatarParent);
                currentAvatar.transform.localPosition = currentAvatar.transform.localEulerAngles = Vector3.zero;
                currentAvatar.transform.localScale = Vector3.one * .6f;
            }
            await LoadAssets();
            await LoadAvatarColors();
            ToggleCategoryButtons();

            LoadingManager.DisableLoading();
        }

        private void ToggleCategoryButtons()
        {
            var assets = AvatarCreatorData.AvatarProperties.Assets;
            if (!assets.TryGetValue(AssetType.Outfit, out var outfitId))
            {
                return;
            }

            if (partnerAssetManager.IsLockedAssetCategories(AssetType.Outfit, outfitId.ToString()))
            {
                categoryUICreator.SetActiveCategoryButtons(false);
                categoryUICreator.SetDefaultSelection(AssetType.Outfit);
            }
            else
            {
                categoryUICreator.SetActiveCategoryButtons(true);
            }
        }

        private void Cleanup()
        {
            if (currentAvatar != null)
            {
                Destroy(currentAvatar);
            }

            avatarManager.Delete(true);
            partnerAssetManager.DeleteAssets();

            Dispose();
            categoryUICreator.ResetUI();
            assetButtonCreator.ResetUI();
        }

        private void OnErrorCallback(string error)
        {
            if (error.Equals("Avatar draft not found"))
            {
                return;
            }

            SDKLogger.Log(TAG, $"An error occured: {error}");
            avatarManager.OnError -= OnErrorCallback;
            partnerAssetManager.OnError -= OnErrorCallback;
            StateMachine.GoToPreviousState();
            LoadingManager.EnableLoading(error, LoadingManager.LoadingType.Popup, false);
            SDKLogger.Log(TAG, "Going to previous state");
        }

        private void OnDestroy()
        {
            ctxSource.Cancel();
            ctxSource.Dispose();
        }

        private async Task LoadAssets()
        {
            var startTime = Time.time;

            partnerAssetManager.OnError += OnErrorCallback;
            categoriesAssetsLoaded = new List<AssetType>();

            await partnerAssetManager.GetAssets(AvatarCreatorData.AvatarProperties.Gender, ctxSource.Token);
            await CreateAssetsByCategory(AssetType.FaceShape);

            SDKLogger.Log(TAG, $"Loaded all partner assets {Time.time - startTime:F2}s");
        }

        private async void OnCategorySelected(AssetType category)
        {
            await CreateAssetsByCategory(category);
            avatarManager.PrecompileAvatar(AvatarCreatorData.AvatarProperties.Id, partnerAssetManager.GetPrecompileData(new[] { category }, NUMBER_OF_ASSETS_TO_PRECOMPILE));
        }

        private async Task<GameObject> LoadAvatar()
        {
            var startTime = Time.time;

            GameObject avatar;

            if (string.IsNullOrEmpty(AvatarCreatorData.AvatarProperties.Id))
            {
                AvatarCreatorData.AvatarProperties.Assets ??= GetDefaultAssets();
                var avatarResponse = await avatarManager.CreateAvatarAsync(AvatarCreatorData.AvatarProperties);
                avatar = avatarResponse.AvatarObject;
                AvatarCreatorData.AvatarProperties = avatarResponse.Properties;
            }
            else
            {
                var id = AvatarCreatorData.AvatarProperties.Id;

                if (!AvatarCreatorData.IsExistingAvatar)
                {
                    var avatarTemplateResponse = await avatarManager.CreateAvatarFromTemplateAsync(id);
                    avatar = avatarTemplateResponse.AvatarObject;
                    AvatarCreatorData.AvatarProperties = avatarTemplateResponse.Properties;
                }
                else
                {
                    avatar = await avatarManager.GetAvatar(id, AvatarCreatorData.AvatarProperties.isDraft);
                }
            }

            if (avatar == null)
            {
                return null;
            }

            ProcessAvatar(avatar);

            SDKLogger.Log(TAG, $"Avatar loaded in {Time.time - startTime:F2}s");
            return avatar;
        }

        private async Task LoadAvatarColors()
        {
            var startTime = Time.time;
            var colors = await avatarManager.LoadAvatarColors();
            var equippedColors = GetEquippedColors();

            assetButtonCreator.CreateColorUI(colors, UpdateAvatar, equippedColors);
            SDKLogger.Log(TAG, $"All colors loaded in {Time.time - startTime:F2}s");
        }

        private Dictionary<AssetType, int> GetEquippedColors()
        {
            var colorAssetTypes = AssetTypeHelper.GetAssetTypesByFilter(AssetFilter.Color).ToHashSet();
            return AvatarCreatorData.AvatarProperties.Assets
            .Where(kvp => colorAssetTypes.Contains(kvp.Key))
            .ToDictionary(
                kvp => kvp.Key,
                kvp =>
                {
                    try
                    {
                        return Convert.ToInt32(kvp.Value);
                    }
                    catch
                    {
                        return 0;
                    }
                });
        }

        private void CreateUI()
        {
            categoryUICreator.Setup();
            assetButtonCreator.SetSelectedAssets(AvatarCreatorData.AvatarProperties.Assets);
            assetButtonCreator.CreateClearButton(UpdateAvatar);
            saveButton.gameObject.SetActive(true);
        }

        private async Task CreateAssetsByCategory(AssetType category)
        {
            if (categoriesAssetsLoaded.Contains(category))
            {
                return;
            }

            categoriesAssetsLoaded.Add(category);

            var assets = partnerAssetManager.GetAssetsByCategory(category);
            if (assets == null || assets.Count == 0)
            {
                return;
            }
            assetButtonCreator.CreateAssetButtons(assets, category, OnAssetButtonClicked);
            await partnerAssetManager.DownloadIconsByCategory(category, assetButtonCreator.SetAssetIcon, ctxSource.Token);

            if (category == AssetType.EyeShape)
            {
                await CreateAssetsByCategory(AssetType.EyeColor);
            }
        }

        private void OnAssetButtonClicked(string id, AssetType category)
        {
            categoryUICreator.SetActiveCategoryButtons(!partnerAssetManager.IsLockedAssetCategories(category, id));
            UpdateAvatar(id, category);
        }

        private void OnSaveButton()
        {
            if (AuthManager.IsSignedIn)
            {
                Save();
            }
            else
            {
                signupElement.gameObject.SetActive(true);
            }
        }

        private void OnSendEmail(string email)
        {
            Save();
        }

        private async void Save()
        {
            AuthManager.StoreLastModifiedAvatar(null);
            var startTime = Time.time;

            LoadingManager.EnableLoading("Saving avatar...", LoadingManager.LoadingType.Popup);

            if (AvatarCreatorData.AvatarProperties.isDraft)
            {
                var avatarId = await TaskExtensions.HandleCancellation(avatarManager.Save());
                if (avatarId != null)
                {
                    AvatarCreatorData.AvatarProperties.Id = avatarId;
                    FinishAndCloseCreator();
                }

            }
            else
            {
                FinishAndCloseCreator();
            }

            PlayerPrefs.SetString("IS_RPM", "true");
            Debug.Log("RPM AVATAR URL: " + AvatarCreatorData.AvatarProperties.Id);
            PlayerPrefs.SetString("RPM_AVATAR_ID", AvatarCreatorData.AvatarProperties.Id);
            loadRPMAvatar.LoadAvatar(AvatarCreatorData.AvatarProperties.Id);
            SDKLogger.Log(TAG, $"Avatar saved in {Time.time - startTime:F2}s");
        }

        private void FinishAndCloseCreator()
        {
            Invoke(nameof(DelayFinish), 5f);
        }

        private void DelayFinish()
        {
            StateMachine.SetState(StateType.GenderSelection);
            LoadingManager.DisableLoading();
            if (mirror != null)
            {
                mirror.SetActive(true);
            }
            ui.gameObject.SetActive(false);
            changeCanvas.SetActive(true);
        }

        private Dictionary<AssetType, object> GetDefaultAssets()
        {
            if (string.IsNullOrEmpty(AvatarCreatorData.AvatarProperties.Base64Image))
            {
                return AvatarCreatorData.AvatarProperties.Gender == OutfitGender.Feminine
                    ? AvatarPropertiesConstants.FemaleDefaultAssets
                    : AvatarPropertiesConstants.MaleDefaultAssets;
            }

            return new Dictionary<AssetType, object>();
        }

        private async void UpdateAvatar(object assetId, AssetType category)
        {
            var startTime = Time.time;

            var payload = new AvatarProperties
            {
                Assets = new Dictionary<AssetType, object>()
            };

            payload.Assets.Add(category, assetId);
            lastRotation = currentAvatar.transform.rotation;
            LoadingManager.EnableLoading(UPDATING_YOUR_AVATAR_LOADING_TEXT, LoadingManager.LoadingType.Popup);

            if (!AvatarCreatorData.AvatarProperties.isDraft)
            {
                await avatarManager.Delete(true);
            }

            var avatar = await avatarManager.UpdateAsset(category, assetId);
            if (avatar == null)
            {
                return;
            }
            AvatarCreatorData.AvatarProperties.isDraft = true;
            AuthManager.StoreLastModifiedAvatar(AvatarCreatorData.AvatarProperties.Id);
            ProcessAvatar(avatar);
            Destroy(currentAvatar);
            currentAvatar = avatar;
            if (avatarParent != null && currentAvatar != null)
            {
                currentAvatar.transform.SetParent(avatarParent);
                currentAvatar.transform.localPosition = currentAvatar.transform.localEulerAngles = Vector3.zero;
                currentAvatar.transform.localScale = Vector3.one * .6f;
            }
            LoadingManager.DisableLoading();
            SDKLogger.Log(TAG, $"Avatar updated in {Time.time - startTime:F2}s");
        }

        private void ProcessAvatar(GameObject avatar)
        {
            if (AvatarCreatorData.AvatarProperties.BodyType != BodyType.None && AvatarCreatorData.AvatarProperties.BodyType != BodyType.HalfBody)
            {
                avatar.GetComponent<Animator>().runtimeAnimatorController = animator;
            }

            // Vector3 pos = new(-0.55f, 0, 0);    // optional
            // Quaternion rotation = Quaternion.Euler(0, 11, 0);
            // avatar.transform.SetLocalPositionAndRotation(pos, rotation);

            //avatar.transform.rotation = lastRotation;
            // avatar.AddComponent<MouseRotationHandler>();
            // avatar.AddComponent<AvatarRotator>();
        }

        public void Dispose()
        {
            partnerAssetManager.OnError -= OnErrorCallback;
            partnerAssetManager?.Dispose();

            avatarManager.OnError -= OnErrorCallback;
            avatarManager?.Dispose();
        }
    }
}
