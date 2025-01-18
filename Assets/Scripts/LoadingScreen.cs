using TMPro;
using UnityEngine;
using VertextFormCore;

namespace VertextFormCore
{
    public class LoadingScreen : MonoBehaviour
    {
        public TextMeshProUGUI loadingText;
        void Start()
        {
            InvokeRepeating(nameof(ShowLoading), 1, 1);
        }

        public void ShowLoading()
        {
            loadingText.text = "Loading..." + SceneLoader.Instance.completePerchantage + "%";
        }
        public void LoadHome()
        {
            VirtualRoomManager.Instance.LeaveRoomAndLoadHomeScene();
        }
    }
}