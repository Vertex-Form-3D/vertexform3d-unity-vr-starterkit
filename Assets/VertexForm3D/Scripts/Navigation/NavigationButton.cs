using UnityEngine;
using UnityEngine.UI;

namespace VertexFormCore
{
    public class NavigationButton : MonoBehaviour
    {
        public string sceneName;
        [SerializeField] bool flyMode;
        [SerializeField] Button onclickButton;

        void Start()
        {
            onclickButton.onClick.AddListener(OnNavigationButtonClicked);
        }

        private void OnNavigationButtonClicked()
        {
            if (!ScenePlatformSupport.CanEnterScene(sceneName))
                return;

            onclickButton.interactable = false;
            SceneLoader.Instance.isFlyModeEnabled = flyMode;
            SceneLoader.Instance.LoadScnene(sceneName);
        }
    }
}