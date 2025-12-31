using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ReadyPlayerMe.Samples.AvatarCreatorElements
{
    public class PanelSwitcher : MonoBehaviour
    {
        [SerializeField] private List<ButtonElementLink> buttonElementLinks;
        private void Awake()
        {
            foreach (var elementButtonLink in buttonElementLinks)
            {
                if (elementButtonLink.button == null)
                {
                    continue;
                }
                elementButtonLink.button.onClick.AddListener(() =>
                {
                    ShowElement(elementButtonLink.element);
                });
            }
        }
        /// <summary>
        /// Function, that allows to show element without clicking on the button or having any links to the button.
        /// </summary>
        /// <param name="element">The GameObject to be displayed. This GameObject must be in the list of buttonElementLinks in order to be activated.</param>
        public void ShowElement(GameObject element)
        {
            foreach (var elementSection in buttonElementLinks)
            {
                elementSection.element.SetActive(elementSection.element == element);
            }
        }
    }

    [Serializable]
    public struct ButtonElementLink
    {
        public Button button;
        public GameObject element;
    }
}
