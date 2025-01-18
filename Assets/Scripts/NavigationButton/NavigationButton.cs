using UnityEngine;
using UnityEngine.UI;

namespace VertextFormCore
{
    public class NavigationButton : MonoBehaviour
    {
        public string sceneName;

        Button onclickButton;

        void Start()
        {
            onclickButton = GetComponent<Button>();
            onclickButton.onClick.AddListener(OnNavigationButtonClicked);
        }

        private void OnNavigationButtonClicked()
        {
            onclickButton.interactable = false;
            SceneLoader.Instance.LoadScnene(sceneName);
        }
    }
}