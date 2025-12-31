using System;
using UnityEngine;
using UnityEngine.UI;

namespace ReadyPlayerMe.Samples.AvatarCreatorWizard
{
    public class AssetButton : MonoBehaviour
    {
        [SerializeField] private RawImage icon;
        [SerializeField] private GameObject selected;
        [SerializeField] private Button button;
        [SerializeField] private GameObject mask;
        [SerializeField] private GameObject loading;

        private RectTransform rawImageRectTransform;

        public void AddListener(Action action)
        {
            button.onClick.AddListener(action.Invoke);
        }

        public void SetColor(string colorHex)
        {
            ColorUtility.TryParseHtmlString(colorHex, out var color);
            icon.color = color;
        }

        public void SetIcon(Texture texture)
        {
            if (rawImageRectTransform == null)
            {
                rawImageRectTransform = icon.GetComponent<RectTransform>();
            }
            var previousSize = rawImageRectTransform.sizeDelta;
            icon.texture = texture;
            icon.enabled = true;
            rawImageRectTransform.sizeDelta = previousSize;
            loading.SetActive(false);
        }

        public void SetSelect(bool isSelected)
        {
            selected.SetActive(isSelected);
        }

        private void EnableMask()
        {
            mask.GetComponent<Mask>().enabled = true;
            mask.GetComponent<Image>().color = Color.white;
        }
    }
}
