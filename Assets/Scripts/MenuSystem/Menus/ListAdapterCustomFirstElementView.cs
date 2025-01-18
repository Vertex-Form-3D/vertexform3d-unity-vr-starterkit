using System.Collections.Generic;
using UnityEngine;

namespace VertextFormCore
{
    public class ListAdapterCustomFirstElementView<T> : ListAdapter<T>
    {
        OnListItemClikedListener tempListener;
        protected ViewElement<T> customFirstElement;
        protected RectTransform customFirstElementRoot;
        public ListAdapterCustomFirstElementView(ViewElement<T> mainPrefab, RectTransform mainItemRoot, ViewElement<T> listPrefab, RectTransform listRoot, List<T> data, OnListItemClikedListener onListItemClikedListener = null, int defultSelectedIndex = 0)
            : base(listPrefab, listRoot, data, onListItemClikedListener)
        {
            this.defultSelectedIndex = defultSelectedIndex;
            this.customFirstElement = mainPrefab;
            this.customFirstElementRoot = mainItemRoot;
        }

        protected override ViewElement<T> GetElementView(int i)
        {
            ViewElement<T> viewElement;

            if (i < viewElements.Count)
            {
                viewElement = viewElements[i];
            }
            else
            {
                if (i == 0)
                {
                    viewElement = GameObject.Instantiate(customFirstElement, customFirstElementRoot);
                    viewElements.Add(viewElement);
                }
                else
                {
                    viewElement = GameObject.Instantiate(viewPrefab, listRoot);
                    viewElements.Add(viewElement);
                }
            }

            viewElement.gameObject.SetActive(true);
            return viewElement;
        }
    }
}