using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ReadyPlayerMe.AvatarCreator;
using ReadyPlayerMe.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ReadyPlayerMe.Samples.AvatarCreatorWizard
{
    public class DefaultAvatarSelection : State
    {
        private const string TAG = nameof(DefaultAvatarSelection);
        private const string LOADING_MESSAGE = "Fetching default avatars";

        [SerializeField] private Transform parent;
        [SerializeField] private GameObject buttonPrefab;

        public override StateType StateType => StateType.DefaultAvatarSelection;
        public override StateType NextState => StateType.Editor;

        private Dictionary<AvatarTemplateData, GameObject> avatarRenderByTemplateData;
        private CancellationTokenSource ctxSource;
        private AvatarTemplateFetcher templateFetcher;

        private void Awake()
        {
            avatarRenderByTemplateData = new Dictionary<AvatarTemplateData, GameObject>();
        }

        private void OnDestroy()
        {
            ctxSource?.Cancel();
        }

        public override async void ActivateState()
        {
            LoadingManager.EnableLoading(LOADING_MESSAGE);

            if (!AuthManager.IsSignedIn && !AuthManager.IsSignedInAnonymously)
            {
                await AuthManager.LoginAsAnonymous();
            }

            if (avatarRenderByTemplateData.Count == 0)
            {
                LoadingManager.EnableLoading(LOADING_MESSAGE);
                await FetchTemplates();
            }

            foreach (var template in avatarRenderByTemplateData)
            {
                avatarRenderByTemplateData[template.Key].SetActive(template.Key.Gender == AvatarCreatorData.AvatarProperties.Gender);
            }

            LoadingManager.DisableLoading();
        }

        public override void DeactivateState()
        {
            ctxSource?.Cancel();
            foreach (Transform child in parent)
            {
                child.gameObject.SetActive(false);
            }
        }

        private async Task FetchTemplates()
        {
            var startTime = Time.time;
            ctxSource = new CancellationTokenSource();
            templateFetcher = new AvatarTemplateFetcher(ctxSource.Token);

            var templates = await templateFetcher.GetTemplatesWithRenders();
            SDKLogger.Log(TAG, $"Fetched all avatar templates in {Time.time - startTime:F2}s ");

            foreach (var template in templates)
            {
                var button = CreateRenderButton(template.Id, template.Texture);
                avatarRenderByTemplateData.Add(template, button);
            }
        }

        private GameObject CreateRenderButton(string id, Texture renderImage)
        {
            var button = Instantiate(buttonPrefab, parent);
            button.name = id;
            var rawImage = button.GetComponentInChildren<RawImage>();
            button.GetComponent<Button>().onClick.AddListener(() => OnAvatarSelected(id));
            rawImage.texture = renderImage;
            rawImage.SizeToParent();
            return button;
        }

        private void OnAvatarSelected(string avatarId)
        {
            AvatarCreatorData.AvatarProperties.Id = avatarId;
            AvatarCreatorData.AvatarProperties.Base64Image = string.Empty;
            AvatarCreatorData.IsExistingAvatar = false;

            StateMachine.SetState(StateType.Editor);
        }
    }
}
