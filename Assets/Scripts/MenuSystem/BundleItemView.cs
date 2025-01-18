using UnityEngine;
using UnityEngine.UI;

namespace VertextFormCore
{
    public class BundleItemView : ViewElement<string>
    {
        [SerializeField] TMPro.TMP_Text bunelName;
        [SerializeField] Button DownloadButton;

        private void Start()
        {
            DownloadButton.onClick.AddListener(this.OnClicked);
        }

        public override void UpdateView(string t)
        {
            bunelName.text = t;
            DownloadButton.gameObject.SetActive(PlayerPrefs.GetInt(t) == (int)CashStatus.NotCased);
        }
    }
}
