using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VertexFormCore;

namespace VertexFormCore
{
    public class LoadingScreen : MonoBehaviour
    {
        public static LoadingScreen Instance;
        public TextMeshProUGUI loadingText;
        public Image fadeImage;
        [Header("Disconnect Popup")]
        public GameObject disconnectPopupRoot;
        public float disconnectPopupDuration = 2f;
        [Header("Loading Progress")]
        public float progressLerpSpeed = 35f;
        private bool _disconnectPopupInProgress;
        private float _displayedProgress;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        void Start()
        {
            InvokeRepeating(nameof(ShowLoading), 0.1f, 0.1f);
        }

        public void ShowLoading()
        {
            if (SceneLoader.Instance == null || loadingText == null)
            {
                return;
            }

            float targetProgress = Mathf.Clamp(SceneLoader.Instance.completePerchantage, 0f, 100f);
            _displayedProgress = Mathf.MoveTowards(_displayedProgress, targetProgress, progressLerpSpeed * 0.1f);

            int shownProgress = Mathf.RoundToInt(_displayedProgress);
            loadingText.text = "Loading..." + shownProgress + "%";

            bool loadingComplete = SceneLoader.Instance.completePerchantage >= 100 && SceneLoader.Instance.sceneIsLoaded;
            bool localPlayerReady = RoomManager.Instance != null && RoomManager.Instance.localVRPlayer != null;
            if (loadingComplete && localPlayerReady && shownProgress >= 100)
            {
                FadeOut();
                CancelInvoke(nameof(ShowLoading));
            }
        }

        private void OnEnable()
        {
            FadeIn();
        }
        public void FadeIn()
        {
            fadeImage.DOFade(1, 1f).SetEase(Ease.InOutQuad);
        }

        public void FadeOut()
        {
            fadeImage.DOFade(0, 2f).SetEase(Ease.InOutQuad);
        }
        public void LoadHome()
        {
            VirtualRoomManager.Instance.LeaveRoomAndLoadHomeScene();
        }

        public void ShowDisconnectPopupAndLoadHome(string reason)
        {
            if (_disconnectPopupInProgress)
            {
                return;
            }

            _disconnectPopupInProgress = true;
            StartCoroutine(ShowDisconnectPopupRoutine(reason));
        }

        private IEnumerator ShowDisconnectPopupRoutine(string reason)
        {
            if (disconnectPopupRoot != null)
            {
                disconnectPopupRoot.SetActive(true);
            }

            if (loadingText != null)
            {
                loadingText.text = string.IsNullOrWhiteSpace(reason)
                    ? "Connection lost. Returning to Home..."
                    : $"Connection lost.\n{reason}\nReturning to Home...";
            }

            yield return new WaitForSeconds(disconnectPopupDuration);
            LoadHome();
        }
    }
}