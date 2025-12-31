using System;
using UnityEngine;
using UnityEngine.UI;

namespace ReadyPlayerMe.Samples.AvatarCreatorWizard
{
    public class LoadingManager : MonoBehaviour
    {
        public enum LoadingType
        {
            Fullscreen,
            Popup
        }

        [Serializable]
        private class LoadingData
        {
            public GameObject container;
            public Text text;
            public GameObject animation;
        }

        [SerializeField] private LoadingData fullscreenLoading;
        [SerializeField] private LoadingData popupLoading;

        private LoadingType currentLoadingType = LoadingType.Fullscreen;

        public void EnableLoading(string text = "Loading your avatar", LoadingType type = LoadingType.Fullscreen, bool enableLoadingAnimation = true)
        {
            DisableLoading();
            
            switch (type)
            {
                case LoadingType.Fullscreen:
                    fullscreenLoading.text.text = text;
                    fullscreenLoading.container.SetActive(true);
                    fullscreenLoading.animation.SetActive(enableLoadingAnimation);
                    currentLoadingType = type;
                    break;
                case LoadingType.Popup:
                    popupLoading.text.text = text;
                    popupLoading.container.SetActive(true);
                    popupLoading.animation.SetActive(enableLoadingAnimation);
                    currentLoadingType = type;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public void DisableLoading()
        {
            switch (currentLoadingType)
            {
                case LoadingType.Fullscreen:
                    fullscreenLoading.container.SetActive(false);
                    break;
                case LoadingType.Popup:
                    popupLoading.container.SetActive(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(currentLoadingType), currentLoadingType, null);
            }
        }
    }
}
