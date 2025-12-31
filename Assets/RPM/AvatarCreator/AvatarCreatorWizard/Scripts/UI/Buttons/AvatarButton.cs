using System;
using System.Threading;
using System.Threading.Tasks;
using ReadyPlayerMe.AvatarCreator;
using ReadyPlayerMe.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ReadyPlayerMe.Samples.AvatarCreatorWizard
{
    public class AvatarButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private RawImage image;
        [SerializeField] private GameObject loading;
        [SerializeField] private GameObject buttonsPanel;
        [SerializeField] private Button customizeButton;
        [SerializeField] private Button selectButton;

        private RectTransform rawImageRectTransform;
        private string avatarId;
        private bool showButtons;

        private CancellationTokenSource ctxSource;

        private void Start()
        {
            AuthManager.OnSignedOut += OnSignedOut;
        }

        private void OnSignedOut()
        {
            ctxSource?.Cancel();
        }

        private async void OnEnable()
        {
            if (!string.IsNullOrEmpty(avatarId) && image.texture == null)
            {
                await LoadImage();
            }
        }

        public void Init(string id, Action onCustomize, Action onSelect, bool isCurrentPartner)
        {
            avatarId = id;
            gameObject.name = $"AvatarButton_{id}";
            customizeButton.onClick.AddListener(() => onCustomize());
            selectButton.onClick.AddListener(() => onSelect());
            showButtons = isCurrentPartner;
            LoadImage();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (showButtons)
            {
                buttonsPanel.SetActive(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (showButtons)
            {
                buttonsPanel.SetActive(false);
            }
        }

        private void OnDisable()
        {
            ctxSource?.Cancel();
        }

        private async Task LoadImage()
        {
            if (rawImageRectTransform == null)
            {
                rawImageRectTransform = image.GetComponent<RectTransform>();
            }
            loading.SetActive(true);
            ctxSource = new CancellationTokenSource();
            try
            {
                var previousSize = rawImageRectTransform.sizeDelta;
                var texture = await AvatarRenderHelper.GetPortrait(avatarId, ctxSource.Token);
               if (!ctxSource.Token.IsCancellationRequested)
                {
                    image.texture = texture;
                    rawImageRectTransform.sizeDelta = previousSize;
                    loading.SetActive(false);
                }
            }
            catch (Exception e)
            {
                SDKLogger.LogWarning("AvatarButton", $"Failed to load image with avatar ID {avatarId}: {e.Message}");
                gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            AuthManager.OnSignedOut -= OnSignedOut;
        }
    }
}
