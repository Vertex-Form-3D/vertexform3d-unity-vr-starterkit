using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VertexFormCore;

namespace VertexFormCore
{
    public class LoadingScreen : MonoBehaviour
    {
        public TextMeshProUGUI loadingText;
        public Image fadeImage;
        void Start()
        {
            InvokeRepeating(nameof(ShowLoading), 1, 1);
        }

        public void ShowLoading()
        {
            Debug.Log("Loading: " + SceneLoader.Instance.completePerchantage + "%");
            loadingText.text = "Loading..." + SceneLoader.Instance.completePerchantage + "%";
            if (SceneLoader.Instance.completePerchantage >= 100 && SceneLoader.Instance.sceneIsLoaded)
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
    }
}